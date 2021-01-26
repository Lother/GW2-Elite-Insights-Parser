﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.EncounterLogic;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.ParsedData;
using GW2EIEvtcParser.ParserHelpers;
using GW2EIGW2API;
using GW2EIGW2API.GW2API;

//recommend CTRL+M+O to collapse all
namespace GW2EIEvtcParser
{
    public class EvtcParser
    {

        //Main data storage after binary parse
        private FightData _fightData;
        private AgentData _agentData;
        private readonly List<AgentItem> _allAgentsList;
        private readonly SkillData _skillData;
        private readonly List<CombatItem> _combatItems;
        private List<Player> _playerList;
        private byte _revision;
        private ushort _id;
        private long _logStartTime;
        private long _logEndTime;
        private string _buildVersion;
        private readonly EvtcParserSettings _parserSettings;
        private readonly GW2APIController _apiController;

        public EvtcParser(EvtcParserSettings parserSettings, GW2EIGW2API.GW2APIController apiController)
        {
            _apiController = apiController;
            _parserSettings = parserSettings;
            _allAgentsList = new List<AgentItem>();
            _skillData = new SkillData(apiController);
            _combatItems = new List<CombatItem>();
            _playerList = new List<Player>();
            _logStartTime = 0;
            _logEndTime = 0;
        }

        //Main Parse method------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Parses the given log. On parsing failure, parsingFailureReason will be filled with the reason of the failure and the method will return null
        /// <see cref="ParsingFailureReason"/>
        /// </summary>
        /// <param name="operation">Operation object bound to the UI</param>
        /// <param name="evtc">The path to the log to parse</param>
        /// <param name="parsingFailureReason">The reason why the parsing failed, if applicable</param>
        /// <returns>the ParsedEvtcLog</returns>
        public ParsedEvtcLog ParseLog(ParserController operation, FileInfo evtc, out ParsingFailureReason parsingFailureReason)
        {
            parsingFailureReason = null;
            try
            {
                if (!evtc.Exists)
                {
                    throw new EvtcFileException("File " + evtc.FullName + " does not exist");
                }
                if (!ParserHelper.IsSupportedFormat(evtc.Name))
                {
                    throw new EvtcFileException("Not EVTC");
                }
                ParsedEvtcLog evtcLog;
                using (var fs = new FileStream(evtc.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (ParserHelper.IsCompressedFormat(evtc.Name))
                    {
                        using (var arch = new ZipArchive(fs, ZipArchiveMode.Read))
                        {
                            if (arch.Entries.Count != 1)
                            {
                                throw new EvtcFileException("Invalid Archive");
                            }
                            using (Stream data = arch.Entries[0].Open())
                            {
                                using (var ms = new MemoryStream())
                                {
                                    data.CopyTo(ms);
                                    ms.Position = 0;
                                    evtcLog = ParseLog(operation, ms, out parsingFailureReason);
                                };
                            }
                        }
                    }
                    else
                    {
                        evtcLog = ParseLog(operation, fs, out parsingFailureReason);
                    }
                }
                return evtcLog;
            }
            catch (Exception ex)
            {
                parsingFailureReason = new ParsingFailureReason(ex);
                return null;
            }
        }

        /// <summary>
        /// Parses from the given stream. On parsing failure, parsingFailureReason will be filled with the reason of the failure and the method will return null
        /// <see cref="ParsingFailureReason"/>
        /// </summary>
        /// <param name="operation">Operation object bound to the UI</param>
        /// <param name="evtcStream">The stream of the log</param>
        /// <param name="parsingFailureReason">The reason why the parsing failed, if applicable</param>
        /// <returns>the ParsedEvtcLog</returns>
        public ParsedEvtcLog ParseLog(ParserController operation, Stream evtcStream, out ParsingFailureReason parsingFailureReason)
        {
            parsingFailureReason = null;
            try
            {
                using (BinaryReader reader = CreateReader(evtcStream))
                {
                    operation.UpdateProgressWithCancellationCheck("Reading Binary");
                    operation.UpdateProgressWithCancellationCheck("Parsing fight data");
                    ParseFightData(reader, operation);
                    operation.UpdateProgressWithCancellationCheck("Parsing agent data");
                    ParseAgentData(reader, operation);
                    operation.UpdateProgressWithCancellationCheck("Parsing skill data");
                    ParseSkillData(reader, operation);
                    operation.UpdateProgressWithCancellationCheck("Parsing combat list");
                    ParseCombatList(reader, operation);
                    operation.UpdateProgressWithCancellationCheck("Linking agents to combat list");
                    CompleteAgents();
                    operation.UpdateProgressWithCancellationCheck("Preparing data for log generation");
                    PreProcessEvtcData();
                    operation.UpdateProgressWithCancellationCheck("Data parsed");
                    return new ParsedEvtcLog(_buildVersion, _fightData, _agentData, _skillData, _combatItems, _playerList, _logEndTime - _logStartTime, _parserSettings, operation);
                }
            }
            catch (Exception ex)
            {
                parsingFailureReason = new ParsingFailureReason(ex);
                return null;
            }
        }

        /*private bool TryRead(Stream stream, byte[] data)
        {
            int offset = 0;
            int count = data.Length;
            while (count > 0)
            {
                int bytesRead = stream.Read(data, offset, count);
                if (bytesRead == 0)
                {
                    return false;
                }
                offset += bytesRead;
                count -= bytesRead;
            }
            return true;
        }*/

        //sub Parse methods
        /// <summary>
        /// Parses fight related data
        /// </summary>
        private void ParseFightData(BinaryReader reader, ParserController operation)
        {
            // 12 bytes: arc build version
            _buildVersion = GetString(reader, 12);
            operation.UpdateProgressWithCancellationCheck("ArcDPS Build " + _buildVersion);

            // 1 byte: revision
            _revision = reader.ReadByte();
            operation.UpdateProgressWithCancellationCheck("ArcDPS Combat Item Revision " + _revision);

            // 2 bytes: fight instance ID
            _id = reader.ReadUInt16();
            operation.UpdateProgressWithCancellationCheck("Fight Instance " + _id);
            // 1 byte: skip
            _ = reader.ReadByte();
        }
        private string GetAgentProfString(uint prof, uint elite)
        {
            // non player
            if (elite == 0xFFFFFFFF)
            {
                if ((prof & 0xffff0000) == 0xffff0000)
                {
                    return "GDG";
                }
                else
                {
                    return "NPC";
                }
            }
            // base profession
            else if (elite == 0)
            {
                switch (prof)
                {
                    case 1:
                        return "Guardian";
                    case 2:
                        return "Warrior";
                    case 3:
                        return "Engineer";
                    case 4:
                        return "Ranger";
                    case 5:
                        return "Thief";
                    case 6:
                        return "Elementalist";
                    case 7:
                        return "Mesmer";
                    case 8:
                        return "Necromancer";
                    case 9:
                        return "Revenant";
                }
            }
            // old elite
            else if (elite == 1)
            {
                switch (prof)
                {
                    case 1:
                        return "Dragonhunter";
                    case 2:
                        return "Berserker";
                    case 3:
                        return "Scrapper";
                    case 4:
                        return "Druid";
                    case 5:
                        return "Daredevil";
                    case 6:
                        return "Tempest";
                    case 7:
                        return "Chronomancer";
                    case 8:
                        return "Reaper";
                    case 9:
                        return "Herald";
                }

            }
            // new way
            else
            {
                GW2APISpec spec = _apiController.GetAPISpec((int)elite);
                if (spec == null)
                {
                    throw new InvalidDataException("Missing or outdated GW2 API Cache");
                }
                if (spec.Elite)
                {
                    return spec.Name;
                }
                else
                {
                    return spec.Profession;
                }
            }
            throw new EvtcAgentException("Unknown profession");
        }

        /// <summary>
        /// Parses agent related data
        /// </summary>
        private void ParseAgentData(BinaryReader reader, ParserController operation)
        {        // 4 bytes: player count
            int agentCount = reader.ReadInt32();

            operation.UpdateProgressWithCancellationCheck("Agent Count " + agentCount);
            // 96 bytes: each player
            for (int i = 0; i < agentCount; i++)
            {
                // 8 bytes: agent
                ulong agent = reader.ReadUInt64();

                // 4 bytes: profession
                uint prof = reader.ReadUInt32();

                // 4 bytes: is_elite
                uint isElite = reader.ReadUInt32();

                // 2 bytes: toughness
                ushort toughness = reader.ReadUInt16();
                // 2 bytes: healing
                ushort concentration = reader.ReadUInt16();
                // 2 bytes: healing
                ushort healing = reader.ReadUInt16();
                // 2 bytes: hitbox width
                uint hbWidth = (uint)(2 * reader.ReadUInt16());
                // 2 bytes: condition
                ushort condition = reader.ReadUInt16();
                // 2 bytes: hitbox height
                uint hbHeight = (uint)(2 * reader.ReadUInt16());
                // 68 bytes: name
                string name = GetString(reader, 68, false);
                //Save
                string agentProf = GetAgentProfString(prof, isElite);
                AgentItem.AgentType type;
                ushort ID = 0;
                switch (agentProf)
                {
                    case "NPC":
                        // NPC
                        if (!ushort.TryParse(prof.ToString().PadLeft(5, '0'), out ID))
                        {
                            ID = 0;
                        }
                        type = AgentItem.AgentType.NPC;
                        break;
                    case "GDG":
                        // Gadget
                        if (!ushort.TryParse((prof & 0x0000ffff).ToString().PadLeft(5, '0'), out ID))
                        {
                            ID = 0;
                        }
                        type = AgentItem.AgentType.Gadget;
                        break;
                    default:
                        // Player
                        type = AgentItem.AgentType.Player;
                        break;
                }
                _allAgentsList.Add(new AgentItem(agent, name, agentProf, ID, type, toughness, healing, condition, concentration, hbWidth, hbHeight));
            }
        }

        /// <summary>
        /// Parses skill related data
        /// </summary>
        private void ParseSkillData(BinaryReader reader, ParserController operation)
        {

            // 4 bytes: player count
            uint skillCount = reader.ReadUInt32();
            operation.UpdateProgressWithCancellationCheck("Skill Count " + skillCount);
            //TempData["Debug"] += "Skill Count:" + skill_count.ToString();
            // 68 bytes: each skill
            for (int i = 0; i < skillCount; i++)
            {
                // 4 bytes: skill ID
                int skillId = reader.ReadInt32();
                // 64 bytes: name
                string name = GetString(reader, 64);
                //Save
                _skillData.Add(skillId, name);
            }
        }

        private static CombatItem ReadCombatItem(BinaryReader reader)
        {
            // 8 bytes: time
            long time = reader.ReadInt64();

            // 8 bytes: src_agent
            ulong srcAgent = reader.ReadUInt64();

            // 8 bytes: dst_agent
            ulong dstAgent = reader.ReadUInt64();

            // 4 bytes: value
            int value = reader.ReadInt32();

            // 4 bytes: buff_dmg
            int buffDmg = reader.ReadInt32();

            // 2 bytes: overstack_value
            ushort overstackValue = reader.ReadUInt16();

            // 2 bytes: skill_id
            ushort skillId = reader.ReadUInt16();

            // 2 bytes: src_instid
            ushort srcInstid = reader.ReadUInt16();

            // 2 bytes: dst_instid
            ushort dstInstid = reader.ReadUInt16();

            // 2 bytes: src_master_instid
            ushort srcMasterInstid = reader.ReadUInt16();

            // 9 bytes: garbage
            _ = reader.ReadBytes(9);

            // 1 byte: iff
            byte iff = reader.ReadByte();

            // 1 byte: buff
            byte buff = reader.ReadByte();

            // 1 byte: result
            byte result = reader.ReadByte();

            // 1 byte: is_activation
            byte isActivation = reader.ReadByte();

            // 1 byte: is_buffremove
            byte isBuffRemove = reader.ReadByte();

            // 1 byte: is_ninety
            byte isNinety = reader.ReadByte();

            // 1 byte: is_fifty
            byte isFifty = reader.ReadByte();

            // 1 byte: is_moving
            byte isMoving = reader.ReadByte();

            // 1 byte: is_statechange
            byte isStateChange = reader.ReadByte();

            // 1 byte: is_flanking
            byte isFlanking = reader.ReadByte();

            // 1 byte: is_flanking
            byte isShields = reader.ReadByte();
            // 1 byte: is_flanking
            byte isOffcycle = reader.ReadByte();
            // 1 bytes: garbage
            _ = reader.ReadByte();

            //save
            // Add combat
            return new CombatItem(time, srcAgent, dstAgent, value, buffDmg, overstackValue, skillId,
                srcInstid, dstInstid, srcMasterInstid, 0, iff, buff, result, isActivation, isBuffRemove,
                isNinety, isFifty, isMoving, isStateChange, isFlanking, isShields, isOffcycle, 0);
        }

        private static CombatItem ReadCombatItemRev1(BinaryReader reader)
        {
            // 8 bytes: time
            long time = reader.ReadInt64();

            // 8 bytes: src_agent
            ulong srcAgent = reader.ReadUInt64();

            // 8 bytes: dst_agent
            ulong dstAgent = reader.ReadUInt64();

            // 4 bytes: value
            int value = reader.ReadInt32();

            // 4 bytes: buff_dmg
            int buffDmg = reader.ReadInt32();

            // 4 bytes: overstack_value
            uint overstackValue = reader.ReadUInt32();

            // 4 bytes: skill_id
            uint skillId = reader.ReadUInt32();

            // 2 bytes: src_instid
            ushort srcInstid = reader.ReadUInt16();

            // 2 bytes: dst_instid
            ushort dstInstid = reader.ReadUInt16();

            // 2 bytes: src_master_instid
            ushort srcMasterInstid = reader.ReadUInt16();
            // 2 bytes: dst_master_instid
            ushort dstmasterInstid = reader.ReadUInt16();

            // 1 byte: iff
            byte iff = reader.ReadByte();

            // 1 byte: buff
            byte buff = reader.ReadByte();

            // 1 byte: result
            byte result = reader.ReadByte();

            // 1 byte: is_activation
            byte isActivation = reader.ReadByte();

            // 1 byte: is_buffremove
            byte isBuffRemove = reader.ReadByte();

            // 1 byte: is_ninety
            byte isNinety = reader.ReadByte();

            // 1 byte: is_fifty
            byte isFifty = reader.ReadByte();

            // 1 byte: is_moving
            byte isMoving = reader.ReadByte();

            // 1 byte: is_statechange
            byte isStateChange = reader.ReadByte();

            // 1 byte: is_flanking
            byte isFlanking = reader.ReadByte();

            // 1 byte: is_flanking
            byte isShields = reader.ReadByte();
            // 1 byte: is_flanking
            byte isOffcycle = reader.ReadByte();
            // 4 bytes: pad
            uint pad = reader.ReadUInt32();

            //save
            // Add combat
            return new CombatItem(time, srcAgent, dstAgent, value, buffDmg, overstackValue, skillId,
                srcInstid, dstInstid, srcMasterInstid, dstmasterInstid, iff, buff, result, isActivation, isBuffRemove,
                isNinety, isFifty, isMoving, isStateChange, isFlanking, isShields, isOffcycle, pad);
        }

        /// <summary>
        /// Parses combat related data
        /// </summary>
        private void ParseCombatList(BinaryReader reader, ParserController operation)
        {
            // 64 bytes: each combat
            long cbtItemCount = (reader.BaseStream.Length - reader.BaseStream.Position) / 64;
            operation.UpdateProgressWithCancellationCheck("Combat Event Count " + cbtItemCount);
            for (long i = 0; i < cbtItemCount; i++)
            {
                CombatItem combatItem = _revision > 0 ? ReadCombatItemRev1(reader) : ReadCombatItem(reader);
                if (!IsValid(combatItem))
                {
                    continue;
                }
                if (combatItem.IsStateChange.HasTime())
                {
                    if (_logStartTime == 0)
                    {
                        _logStartTime = combatItem.Time;
                    }
                    _logEndTime = combatItem.Time;
                }
                _combatItems.Add(combatItem);
            }
            if (!_combatItems.Any())
            {
                throw new EvtcCombatEventException("No combat events found");
            }
            if (_logEndTime - _logStartTime < _parserSettings.TooShortLimit)
            {
                throw new TooShortException(_logEndTime - _logStartTime, _parserSettings.TooShortLimit);
            }
            // 24 hours
            if (_logEndTime - _logStartTime > 86400000)
            {
                throw new TooLongException();
            }
        }

        /// <summary>
        /// Returns true if the combat item contains valid data and should be used, false otherwise
        /// </summary>
        /// <param name="combatItem"></param>
        /// <returns>true if the combat item is valid</returns>
        private static bool IsValid(CombatItem combatItem)
        {
            if (combatItem.IsStateChange == ArcDPSEnums.StateChange.HealthUpdate && combatItem.DstAgent > 20000)
            {
                // DstAgent should be target health % times 100, values higher than 10000 are unlikely. 
                // If it is more than 200% health ignore this record
                return false;
            }
            if (combatItem.SrcInstid == 0 && combatItem.DstAgent == 0 && combatItem.SrcAgent == 0 && combatItem.DstInstid == 0 && combatItem.IFF == ArcDPSEnums.IFF.Unknown)
            {
                return false;
            }
            return combatItem.IsStateChange != ArcDPSEnums.StateChange.Unknown;
        }
        private static void UpdateAgentData(AgentItem ag, long logTime, ushort instid)
        {
            if (ag.InstID == 0)
            {
                ag.SetInstid(instid);
            }
            if (ag.FirstAware == 0)
            {
                ag.OverrideAwareTimes(logTime, logTime);
            }
            else
            {
                ag.OverrideAwareTimes(ag.FirstAware, logTime);
            }
        }

        private void FindAgentMaster(long logTime, ushort masterInstid, ulong minionAgent)
        {
            AgentItem master = _agentData.GetAgentByInstID(masterInstid, logTime);
            if (master != ParserHelper._unknownAgent)
            {
                AgentItem minion = _agentData.GetAgent(minionAgent);
                if (minion != ParserHelper._unknownAgent && minion.Master == null)
                {
                    if (minion.FirstAware <= logTime && logTime <= minion.LastAware)
                    {
                        minion.SetMaster(master);
                    }
                }
            }
        }


        private void CompletePlayers()
        {
            //Fix Disconnected players
            IReadOnlyList<AgentItem> playerAgentList = _agentData.GetAgentByType(AgentItem.AgentType.Player);
            foreach (AgentItem playerAgent in playerAgentList)
            {
                if (playerAgent.InstID == 0 || playerAgent.FirstAware == 0 || playerAgent.LastAware == long.MaxValue)
                {
                    CombatItem tst = _combatItems.Find(x => x.SrcAgent == playerAgent.Agent);
                    if (tst == null)
                    {
                        tst = _combatItems.Find(x => x.DstAgent == playerAgent.Agent);
                        if (tst == null)
                        {
                            continue;
                        }
                        playerAgent.SetInstid(tst.DstInstid);
                    }
                    else
                    {
                        playerAgent.SetInstid(tst.SrcInstid);
                    }
                    playerAgent.OverrideAwareTimes(_logStartTime, _logEndTime);
                }
                bool skip = false;
                var player = new Player(playerAgent, _fightData.Logic.Mode == FightLogic.ParseMode.Instanced5 || _fightData.Logic.Mode == FightLogic.ParseMode.sPvP, false);
                foreach (Player p in _playerList)
                {
                    if (p.Account == player.Account)// same player
                    {
                        if (p.Character == player.Character) // same character, can be fused
                        {
                            skip = true;
                            ulong agent = p.Agent;
                            foreach (CombatItem c in _combatItems)
                            {
                                if (player.Agent == c.DstAgent && c.IsStateChange.DstIsAgent())
                                {
                                    c.OverrideDstAgent(agent);
                                }
                                if (player.Agent == c.SrcAgent && c.IsStateChange.SrcIsAgent())
                                {
                                    c.OverrideSrcAgent(agent);
                                }
                            }
                            _agentData.SwapMasters(player.AgentItem, p.AgentItem);
                            p.AgentItem.OverrideAwareTimes(Math.Min(p.AgentItem.FirstAware, player.AgentItem.FirstAware), Math.Max(p.AgentItem.LastAware, player.AgentItem.LastAware));
                            break;
                        }
                        // different character in raid mode, discard it as it can't have any influence, otherwise add as a separate entity
                        else if (_fightData.Logic.Mode == FightLogic.ParseMode.Instanced10)
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (!skip)
                {
                    _playerList.Add(player);
                }
            }
            if (_parserSettings.AnonymousPlayer)
            {
                for (int i = 0; i < _playerList.Count; i++)
                {
                    _playerList[i].Anonymize(i + 1);
                }
            }
            _playerList = _playerList.OrderBy(a => a.Group).ToList();
            if (_playerList.Exists(x => x.Group == 0))
            {
                _playerList.ForEach(x => x.MakeSquadless());
            }
            uint minToughness = _playerList.Min(x => x.Toughness);
            if (minToughness > 0)
            {
                uint maxToughness = _playerList.Max(x => x.Toughness);
                foreach (Player p in _playerList)
                {
                    p.AgentItem.OverrideToughness((ushort)Math.Round(10.0 * (p.AgentItem.Toughness - minToughness) / Math.Max(1.0, maxToughness - minToughness)));
                }
            }
        }

        private void CompleteAgents()
        {
            var agentsLookup = _allAgentsList.GroupBy(x => x.Agent).ToDictionary(x => x.Key, x => x.ToList().First());
            //var agentsLookup = _allAgentsList.ToDictionary(x => x.Agent);
            // Set Agent instid, firstAware and lastAware
            foreach (CombatItem c in _combatItems)
            {
                if (c.IsStateChange.SrcIsAgent())
                {
                    if (agentsLookup.TryGetValue(c.SrcAgent, out AgentItem agent))
                    {
                        UpdateAgentData(agent, c.Time, c.SrcInstid);
                    }
                }
                if (c.IsStateChange.DstIsAgent())
                {
                    if (agentsLookup.TryGetValue(c.DstAgent, out AgentItem agent))
                    {
                        UpdateAgentData(agent, c.Time, c.DstInstid);
                    }
                }
            }
            _allAgentsList.RemoveAll(x => !(x.InstID != 0 && x.LastAware - x.FirstAware >= 0 && x.FirstAware != 0 && x.LastAware != long.MaxValue) && (x.Type != AgentItem.AgentType.Player && x.Type != AgentItem.AgentType.EnemyPlayer));
            _agentData = new AgentData(_allAgentsList);

            if (_agentData.GetAgentByType(AgentItem.AgentType.Player).Count == 0)
            {
                throw new EvtcAgentException("No players found");
            }

            _fightData = new FightData(_id, _agentData, _parserSettings, _logStartTime, _logEndTime);

            CompletePlayers();

            foreach (CombatItem c in _combatItems)
            {
                if (c.IsStateChange.SrcIsAgent() && c.SrcMasterInstid != 0)
                {
                    FindAgentMaster(c.Time, c.SrcMasterInstid, c.SrcAgent);
                }
                if (c.IsStateChange.DstIsAgent() && c.DstMasterInstid != 0)
                {
                    FindAgentMaster(c.Time, c.DstMasterInstid, c.DstAgent);
                }
            }
        }

        private void OffsetEvtcData()
        {
            long offset = _fightData.Logic.GetFightOffset(_fightData, _agentData, _combatItems);
            // apply offset to everything
            foreach (CombatItem c in _combatItems)
            {
                c.OverrideTime(c.Time - offset);
            }
            foreach (AgentItem a in _allAgentsList)
            {
                a.OverrideAwareTimes(a.FirstAware - offset, a.LastAware - offset);
            }
            _fightData.ApplyOffset(offset);
        }

        /// <summary>
        /// Pre process evtc data for EI
        /// </summary>
        private void PreProcessEvtcData()
        {
            OffsetEvtcData();
            _fightData.Logic.EIEvtcParse(_fightData, _agentData, _combatItems, _playerList);
            if (!_fightData.Logic.Targets.Any())
            {
                throw new MissingKeyActorsException("No Targets found");
            }
        }

        //
        private static string GetString(BinaryReader reader, int length, bool nullTerminated = true)
        {
            byte[] bytes = reader.ReadBytes(length);
            if (nullTerminated)
            {
                for (int i = 0; i < length; ++i)
                {
                    if (bytes[i] == 0)
                    {
                        length = i;
                        break;
                    }
                }
            }
            return System.Text.Encoding.UTF8.GetString(bytes, 0, length);
        }

        private static BinaryReader CreateReader(Stream stream)
        {
            return new BinaryReader(stream, new System.Text.UTF8Encoding(), leaveOpen: true);
        }
    }
}
