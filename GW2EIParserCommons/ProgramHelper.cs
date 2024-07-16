using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using GW2EIBuilders;
using GW2EIDiscord;
using GW2EIDPSReport;
using GW2EIDPSReport.DPSReportJsons;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.ParsedData;
using GW2EIEvtcParser.ParserHelpers;
using GW2EIGW2API;
using GW2EIParserCommons.Exceptions;
using GW2EIWingman;

[assembly: CLSCompliant(false)]
namespace GW2EIParserCommons
{
    public class ProgramHelper
    {

        public ProgramHelper(Version parserVersion, ProgramSettings settings)
        {
            ParserVersion = parserVersion;
            Settings = settings;
        }

        public void ApplySettings(ProgramSettings settings)
        {
            Settings = settings;
        }

        public static IReadOnlyList<string> SupportedFormats => SupportedFileFormats.SupportedFormats;

        public static bool IsSupportedFormat(string path)
        {
            return SupportedFileFormats.IsSupportedFormat(path);
        }

        public static bool IsCompressedFormat(string path)
        {
            return SupportedFileFormats.IsCompressedFormat(path);
        }

        public static bool IsTemporaryCompressedFormat(string path)
        {
            return SupportedFileFormats.IsTemporaryCompressedFormat(path);
        }

        public static bool IsTemporaryFormat(string path)
        {
            return SupportedFileFormats.IsTemporaryFormat(path);
        }

        internal static HTMLAssets htmlAssets { get; } = new HTMLAssets();

        public ProgramSettings Settings { get; private set; }
        private Version ParserVersion { get; }

        private static readonly UTF8Encoding NoBOMEncodingUTF8 = new UTF8Encoding(false);

        public static readonly string SkillAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/SkillList.json";
        public static readonly string SpecAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/SpecList.json";
        public static readonly string TraitAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/TraitList.json";
        public static readonly string EILogPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Logs/";

        public static readonly GW2APIController APIController = new GW2APIController(SkillAPICacheLocation, SpecAPICacheLocation, TraitAPICacheLocation);

        private CancellationTokenSource RunningMemoryCheck = null;

        public int GetMaxParallelRunning()
        {
            return Settings.GetMaxParallelRunning();
        }

        public bool HasFormat()
        {
            return Settings.HasFormat();
        }

        public bool ParseMultipleLogs()
        {
            return Settings.DoParseMultipleLogs();
        }
        public void ExecuteMemoryCheckTask()
        {
            if (Settings.MemoryLimit == 0 && RunningMemoryCheck != null)
            {
                RunningMemoryCheck.Cancel();
                RunningMemoryCheck = null;
            }
            if (Settings.MemoryLimit == 0 || RunningMemoryCheck != null)
            {
                return;
            }
            RunningMemoryCheck = new CancellationTokenSource();// Prepare task
            Task.Run(async () =>
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    while (true)
                    {
                        await Task.Delay(500);
                        proc.Refresh();
                        if (proc.PrivateMemorySize64 > Math.Max(Settings.MemoryLimit, 100) * 1e6)
                        {
                            Environment.Exit(2);
                        }
                    }
                }
            }, RunningMemoryCheck.Token);
        }

        public EmbedBuilder GetEmbedBuilder()
        {
            var builder = new EmbedBuilder();
            return builder;
        }

        private  Embed BuildEmbedLother(ParsedEvtcLog log, string dpsReportPermalink)
        {
            EmbedBuilder builder = GetEmbedBuilder();

            builder.AddField("Encounter Duration", log.FightData.DurationString, true);
            var players = new List<AbstractSingleActor>(log.PlayerList.Where(x => !x.IsFakeActor));
            var target = new List<AbstractSingleActor>(log.FightData.Logic.Targets.Where(x => x.Character.Length > 0 && typeof(PlayerNonSquad) == x.GetType()));
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            int totalDamage = 0;
            long totalDamageTaken = 0;
            int totalBarrier = 0;
            int totalDps = 0;
            int totalCondiCleanse = 0;
            int totalBoonStrips = 0;
            int totalDowns = 0;
            int totalDowneds = 0;
            int totalKill = 0;
            int totalDeaths = 0;
            var boonStrips = new List<KeyValuePair<string, int>>();
            var condiCleanse = new List<KeyValuePair<string, int>>();
            var dps = new List<KeyValuePair<string, (int, int)>>();
            var tag = new List<string>();
            var prof = new Dictionary<ParserHelper.Spec, string>();

            prof.Add(ParserHelper.Spec.Necromancer, "Nec");
            prof.Add(ParserHelper.Spec.Reaper, "Rea");
            prof.Add(ParserHelper.Spec.Scourge, "Scg");
            prof.Add(ParserHelper.Spec.Harbinger, "Har");

            prof.Add(ParserHelper.Spec.Elementalist, "Ele");
            prof.Add(ParserHelper.Spec.Tempest, "Tmp");
            prof.Add(ParserHelper.Spec.Weaver, "Wea");
            prof.Add(ParserHelper.Spec.Catalyst, "Cat");

            prof.Add(ParserHelper.Spec.Mesmer, "Mes");
            prof.Add(ParserHelper.Spec.Chronomancer, "Chr");
            prof.Add(ParserHelper.Spec.Mirage, "Mir");
            prof.Add(ParserHelper.Spec.Virtuoso, "Vir");

            prof.Add(ParserHelper.Spec.Warrior, "War");
            prof.Add(ParserHelper.Spec.Berserker, "Brs");
            prof.Add(ParserHelper.Spec.Spellbreaker, "Spb");
            prof.Add(ParserHelper.Spec.Bladesworn, "Bds");

            prof.Add(ParserHelper.Spec.Revenant, "Rev");
            prof.Add(ParserHelper.Spec.Herald, "Her");
            prof.Add(ParserHelper.Spec.Renegade, "Ren");
            prof.Add(ParserHelper.Spec.Vindicator, "Vin");

            prof.Add(ParserHelper.Spec.Guardian, "Gdn");
            prof.Add(ParserHelper.Spec.Dragonhunter, "Dgh");
            prof.Add(ParserHelper.Spec.Firebrand, "Fbd");
            prof.Add(ParserHelper.Spec.Willbender, "Wbd");

            prof.Add(ParserHelper.Spec.Thief, "Thf");
            prof.Add(ParserHelper.Spec.Daredevil, "Dar");
            prof.Add(ParserHelper.Spec.Deadeye, "Ded");
            prof.Add(ParserHelper.Spec.Specter, "Spe");

            prof.Add(ParserHelper.Spec.Ranger, "Rgr");
            prof.Add(ParserHelper.Spec.Druid, "Dru");
            prof.Add(ParserHelper.Spec.Soulbeast, "Slb");
            prof.Add(ParserHelper.Spec.Untamed, "Unt");

            prof.Add(ParserHelper.Spec.Engineer, "Eng");
            prof.Add(ParserHelper.Spec.Scrapper, "Scr");
            prof.Add(ParserHelper.Spec.Holosmith, "Hls");
            prof.Add(ParserHelper.Spec.Mechanist, "Mec");


            prof.Add(ParserHelper.Spec.NPC, "NPC");
            prof.Add(ParserHelper.Spec.Gadget, "Gadget");
            prof.Add(ParserHelper.Spec.Unknown, "Unknown");

            foreach (Player player in players)
            {
                string name;
                if (prof.ContainsKey(player.Spec))
                {
                    name = $"{player.Character} ({prof[player.Spec]})";
                }
                else
                {
                    name = $"{player.Character} ({player.Spec})";
                }
                if (player.IsCommander(log))
                {
                    tag.Add($"{player.Character} ({player.Account})");
                }
                int playerDamage = 0;
                int playerDps = 0;
                foreach (PlayerNonSquad n in target)
                {
                    FinalDPS adps = player.GetDPSStats(n, log, phases[0].Start, phases[0].End);
                    playerDamage += adps.Damage;
                    playerDps += adps.Dps;
                    FinalOffensiveStats gps = player.GetOffensiveStats(n, log, phases[0].Start, phases[0].End);

                    totalKill += gps.Killed;
                    totalDowneds += gps.Downed;
                }
                dps.Add(new KeyValuePair<string, (int, int)>(name, (playerDamage, playerDps)));
                totalDamage += playerDamage;
                totalDps += playerDps;

                FinalDefensesAll def = player.GetDefenseStats(log, phases[0].Start, phases[0].End);
                totalDowns += def.DownCount;
                totalDeaths += def.DeadCount;
                totalDamageTaken += def.DamageTaken;
                totalBarrier += def.DamageBarrier;

                FinalToPlayersSupport sp = player.GetToPlayerSupportStats(log, phases[0].Start, phases[0].End);
                condiCleanse.Add(new KeyValuePair<string, int>(name, sp.CondiCleanse));
                boonStrips.Add(new KeyValuePair<string, int>(name, sp.BoonStrips));
                totalCondiCleanse += sp.CondiCleanse;
                totalBoonStrips += sp.BoonStrips;
            }
            int length = (dps.Count >= 10) ? 10 : dps.Count;
            dps = dps.OrderByDescending(x => x.Value.Item1).ToList().GetRange(0, length);
            condiCleanse = condiCleanse.OrderByDescending(x => x.Value).ToList().GetRange(0, length);
            boonStrips = boonStrips.OrderByDescending(x => x.Value).ToList().GetRange(0, length);
            if (tag.Count > 0)
            {
                builder.AddField("Commander", string.Join("\n", tag), true);
            }
            string kd = "∞";
            if (totalDeaths > 0)
            {
                kd = ((float)totalKill / totalDeaths).ToString("F2");
            }
            builder.AddField("Squad Summary", "```CSS\n" +
               $" Player      Damage      DPS     Downs   Deaths\n" +
               $"--------  -----------  -------  -------  -------\n" +
               $"   {players.Count,-2}     {totalDamage.ToString("N0"),11}  {totalDps.ToString("N0"),7}     {totalDowns,-2}       {totalDeaths,-2}\n" +
               $"DamageTaken    Barrier    Cleanses  Strips\n" +
               $"-----------  -----------  --------  ------\n" +
               $"{totalDamageTaken.ToString("N0"),11}  {totalBarrier.ToString("N0"),11}    {totalCondiCleanse,-4}     {totalBoonStrips,-4}\n" +
               $" Enemy    Downeds   Kill    K/D\n" +
               $"--------  -------  ------  -----\n" +
               $"   {target.Count,-3}        {totalDowneds,-3}      {totalKill,-3}  {kd}\n" +
                "```");
            string dpsString = "";
            string condiCleanseString = "";
            string boonStripsString = "";
            int c;
            c = 1;
            foreach (KeyValuePair<string, (int, int)> d in dps)
            {
                dpsString += $"{c,2}.  {d.Key,25}  {d.Value.Item1.ToString("N0"),9}  {d.Value.Item2.ToString("N0"),7}\n";
                c++;
            }
            c = 1;
            foreach (KeyValuePair<string, int> cc in condiCleanse)
            {
                condiCleanseString += $"{c,2}.  {cc.Key,25}  {cc.Value.ToString("N0"),6}\n";
                c++;
            }
            c = 1;
            foreach (KeyValuePair<string, int> bs in boonStrips)
            {
                boonStripsString += $"{c,2}.  {bs.Key,25}  {bs.Value.ToString("N0"),6}\n";
                c++;
            }

            builder.AddField("Damage Summary", "```CSS\n" +
               $" #  Player                       Damage      DPS  \n" +
               $"--- --------------------------  ---------  -------\n" +
                dpsString +
                "```");
            builder.AddField("Cleanse Summary", "```CSS\n" +
               $" #  Player                       Cleanses\n" +
               $"--- --------------------------  ----------\n" +
                condiCleanseString +
                "```");
            builder.AddField("Strips Summary", "```CSS\n" +
               $" #  Player                       Strips\n" +
               $"--- --------------------------  --------\n" +
                boonStripsString +
                "```");

            AgentItem pov = log.LogData.PoV;
            AbstractSingleActor povActor = log.FindActor(pov);
            builder.WithFooter(povActor.Account + " - " + povActor.Spec.ToString() + "\n" + log.LogData.LogStartStd + " / " + log.LogData.LogEndStd, povActor.GetIcon());

            builder.WithColor(log.FightData.Success ? Color.Green : Color.Red);
            if (dpsReportPermalink.Length > 0 && dpsReportPermalink != "Upload process failed")
            {
                builder.WithUrl(dpsReportPermalink);
                builder.WithTitle(log.FightData.FightName);
            }
            else
            {
                builder.WithTitle(log.FightData.FightName + $"({dpsReportPermalink})");
            }
            return builder.Build();
        }

        private  Embed BuildEmbedLother2(ParsedEvtcLog log)
        {
            EmbedBuilder builder = GetEmbedBuilder();

            var players = new List<AbstractSingleActor>(log.PlayerList.Where(x => !x.IsFakeActor));
            var target = new List<AbstractSingleActor>(log.FightData.Logic.Targets.Where(x => x.Character.Length > 0 && typeof(PlayerNonSquad) == x.GetType()));
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);

            var dpsinpower = new List<KeyValuePair<string, int>>();
            var dpsincounti = new List<KeyValuePair<string, int>>();
            var prof = new Dictionary<ParserHelper.Spec, string>();



            prof.Add(ParserHelper.Spec.Necromancer, "Nec");
            prof.Add(ParserHelper.Spec.Reaper, "Rea");
            prof.Add(ParserHelper.Spec.Scourge, "Scg");
            prof.Add(ParserHelper.Spec.Harbinger, "Har");

            prof.Add(ParserHelper.Spec.Elementalist, "Ele");
            prof.Add(ParserHelper.Spec.Tempest, "Tmp");
            prof.Add(ParserHelper.Spec.Weaver, "Wea");
            prof.Add(ParserHelper.Spec.Catalyst, "Cat");

            prof.Add(ParserHelper.Spec.Mesmer, "Mes");
            prof.Add(ParserHelper.Spec.Chronomancer, "Chr");
            prof.Add(ParserHelper.Spec.Mirage, "Mir");
            prof.Add(ParserHelper.Spec.Virtuoso, "Vir");

            prof.Add(ParserHelper.Spec.Warrior, "War");
            prof.Add(ParserHelper.Spec.Berserker, "Brs");
            prof.Add(ParserHelper.Spec.Spellbreaker, "Spb");
            prof.Add(ParserHelper.Spec.Bladesworn, "Bds");

            prof.Add(ParserHelper.Spec.Revenant, "Rev");
            prof.Add(ParserHelper.Spec.Herald, "Her");
            prof.Add(ParserHelper.Spec.Renegade, "Ren");
            prof.Add(ParserHelper.Spec.Vindicator, "Vin");

            prof.Add(ParserHelper.Spec.Guardian, "Gdn");
            prof.Add(ParserHelper.Spec.Dragonhunter, "Dgh");
            prof.Add(ParserHelper.Spec.Firebrand, "Fbd");
            prof.Add(ParserHelper.Spec.Willbender, "Wbd");

            prof.Add(ParserHelper.Spec.Thief, "Thf");
            prof.Add(ParserHelper.Spec.Daredevil, "Dar");
            prof.Add(ParserHelper.Spec.Deadeye, "Ded");
            prof.Add(ParserHelper.Spec.Specter, "Spe");

            prof.Add(ParserHelper.Spec.Ranger, "Rgr");
            prof.Add(ParserHelper.Spec.Druid, "Dru");
            prof.Add(ParserHelper.Spec.Soulbeast, "Slb");
            prof.Add(ParserHelper.Spec.Untamed, "Unt");

            prof.Add(ParserHelper.Spec.Engineer, "Eng");
            prof.Add(ParserHelper.Spec.Scrapper, "Scr");
            prof.Add(ParserHelper.Spec.Holosmith, "Hls");
            prof.Add(ParserHelper.Spec.Mechanist, "Mec");


            prof.Add(ParserHelper.Spec.NPC, "NPC");
            prof.Add(ParserHelper.Spec.Gadget, "Gadget");
            prof.Add(ParserHelper.Spec.Unknown, "Unknown");




            foreach (Player player in players)
            {
                string name;
                if (prof.ContainsKey(player.Spec))
                {
                    name = $"{player.Character} ({prof[player.Spec]})";
                }
                else
                {
                    name = $"{player.Character} ({player.Spec})";
                }
                int playePower = 0;
                int playerCounti = 0;

                foreach (PlayerNonSquad n in target)
                {
                    IReadOnlyList<AbstractHealthDamageEvent> dpsin = player.GetDamageTakenEvents(n, log, phases[0].Start, phases[0].End);
                    foreach (AbstractHealthDamageEvent d in dpsin)
                    {
                        if (d.GetType() == typeof(DirectHealthDamageEvent) && !d.ConditionDamageBased(log)) {
                            playePower += d.HealthDamage;
                        } else if (d.GetType() == typeof(NonDirectHealthDamageEvent) && d.ConditionDamageBased(log))                        {
                            playerCounti += d.HealthDamage;
                        }
                    }
                }
                dpsinpower.Add(new KeyValuePair<string, int>(name, playePower));
                dpsincounti.Add(new KeyValuePair<string, int>(name, playerCounti));

            }

            int length = (dpsinpower.Count >= 10) ? 10 : dpsinpower.Count;
            dpsinpower = dpsinpower.OrderByDescending(x => x.Value).ToList().GetRange(0, length);
            dpsincounti = dpsincounti.OrderByDescending(x => x.Value).ToList().GetRange(0, length);

            string dpsinPowerString = "";
            string dpsinCountiString = "";
            int c = 1;
            foreach (KeyValuePair<string, int> cc in dpsinpower)
            {
                dpsinPowerString += $"{c,2}.  {cc.Key,25}  {cc.Value.ToString("N0"),6}\n";
                c++;
            }
            c = 1;
            foreach (KeyValuePair<string, int> bs in dpsincounti)
            {
                dpsinCountiString += $"{c,2}.  {bs.Key,25}  {bs.Value.ToString("N0"),6}\n";
                c++;
            }

            builder.AddField("Direct Damage Summary", "```CSS\n" +
               $" #  Player                       Taken Damage\n" +
               $"--- --------------------------  ----------\n" +
                dpsinPowerString +
                "```");
            builder.AddField("Counti Damage Summary", "```CSS\n" +
               $" #  Player                       Taken Damage\n" +
               $"--- --------------------------  --------\n" +
                dpsinCountiString +
                "```");

            return builder.Build();
        }

        private Embed BuildEmbed(ParsedEvtcLog log, string dpsReportPermalink)
        {
            EmbedBuilder builder = GetEmbedBuilder();
            builder.WithThumbnailUrl(log.FightData.Logic.Icon);
            //
            builder.AddField("Encounter Duration", log.FightData.DurationString, true);
            //
            if (log.FightData.Logic.GetInstanceBuffs(log).Any())
            {
                builder.AddField("Instance Buffs", string.Join("\n", log.FightData.Logic.GetInstanceBuffs(log).Select(x => (x.stack > 1 ? x.stack + " " : "") + x.buff.Name)), true);
            }
            //
            /*var playerByGroup = log.PlayerList.Where(x => !x.IsFakeActor).GroupBy(x => x.Group).ToDictionary(x => x.Key, x => x.ToList());
            var hasGroups = playerByGroup.Count > 1;
            foreach (KeyValuePair<int, List<Player>> pair in playerByGroup)
            {
                var groupField = new List<string>();
                foreach (Player p in pair.Value)
                {
                    groupField.Add(p.Character + " - " + p.Prof);
                }
                builder.AddField(hasGroups ? "Group " + pair.Key : "Party Composition", String.Join("\n", groupField));
            }*/
            //
            builder.AddField("Game Data", "ARC: " + log.LogData.ArcVersion + " | " + "GW2 Build: " + log.LogData.GW2Build,true);
            //
            builder.WithTitle(log.FightData.FightName);
            //builder.WithTimestamp(DateTime.Now);
            AgentItem pov = log.LogData.PoV;
            AbstractSingleActor povActor = log.FindActor(pov);
            builder.WithFooter(povActor.Account + " - " + povActor.Spec.ToString() + "\n" + log.LogData.LogStartStd + " / " + log.LogData.LogEndStd, povActor.GetIcon());
            builder.WithColor(log.FightData.Success ? Color.Green : Color.Red);
            if (dpsReportPermalink.Length > 0)
            {
                builder.WithUrl(dpsReportPermalink);
            }
            return builder.Build();
        }

        private string[] UploadOperation(FileInfo fInfo, ParsedEvtcLog originalLog, OperationController originalController)
        {
            // Only upload supported 5 men, 10 men and golem logs, without anonymous players
            var isWingmanCompatible = !originalLog.ParserSettings.AnonymousPlayers && (
                            originalLog.FightData.Logic.ParseMode == GW2EIEvtcParser.EncounterLogic.FightLogic.ParseModeEnum.Instanced10 ||
                            originalLog.FightData.Logic.ParseMode == GW2EIEvtcParser.EncounterLogic.FightLogic.ParseModeEnum.Instanced5 ||
                            originalLog.FightData.Logic.ParseMode == GW2EIEvtcParser.EncounterLogic.FightLogic.ParseModeEnum.Benchmark
                            );
            //Upload Process
            string[] uploadresult = new string[2] { "", "" };
            if (Settings.UploadToDPSReports)
            {
                originalController.UpdateProgressWithCancellationCheck("DPSReport: Uploading");
                DPSReportUploadObject response = DPSReportController.UploadUsingEI(fInfo, str => originalController.UpdateProgress("DPSReport: " + str), Settings.DPSReportUserToken,
                originalLog.ParserSettings.AnonymousPlayers,
                originalLog.ParserSettings.DetailedWvWParse);
                uploadresult[0] = response != null ? response.Permalink : "Upload process failed";
                originalController.UpdateProgressWithCancellationCheck("DPSReport: " + uploadresult[0]);
                /*
                if (Properties.Settings.Default.UploadToWingman)
                {
                    if (isWingmanCompatible)
                    {
                        traces.Add("Uploading to Wingman using DPSReport url");
                        WingmanController.UploadToWingmanUsingImportLogQueued(uploadresult[0], traces, ParserVersion);
                    }
                    else
                    {
                        traces.Add("Can not upload to Wingman using DPSReport url: unsupported log");
                    }
                }
                */
            }
            if (Settings.UploadToWingman)
            {
#if !DEBUG
                if (!isWingmanCompatible)
                {
                    originalController.UpdateProgressWithCancellationCheck("Wingman: unsupported log");
                } 
                else
                {
                    string accName = originalLog.LogData.PoV != null ? originalLog.LogData.PoVAccount : null;

                    if (WingmanController.CheckUploadPossible(fInfo, accName, originalLog.FightData.TriggerID, str => originalController.UpdateProgress("Wingman: " + str)))
                    {
                        try
                        {
                            var expectedSettings = new EvtcParserSettings(Settings.Anonymous,
                                                            Settings.SkipFailedTries,
                                                            true,
                                                            true,
                                                            true,
                                                            Settings.CustomTooShort,
                                                            Settings.DetailledWvW);
                            ParsedEvtcLog logToUse = originalLog;
                            if (originalLog.ParserSettings.ComputeDamageModifiers != expectedSettings.ComputeDamageModifiers ||
                                originalLog.ParserSettings.ParsePhases != expectedSettings.ParsePhases ||
                                originalLog.ParserSettings.ParseCombatReplay != expectedSettings.ParseCombatReplay)
                            {
                                // We need to create a parser that matches Wingman's expected settings
                                var parser = new EvtcParser(expectedSettings, APIController);
                                originalController.UpdateProgressWithCancellationCheck("Wingman: Setting mismatch, creating a new ParsedEvtcLog, this will extend total processing duration if file generation is also requested");
                                logToUse = parser.ParseLog(originalController, fInfo, out GW2EIEvtcParser.ParserHelpers.ParsingFailureReason failureReason, !Settings.SingleThreaded);
                            }
                            byte[] jsonFile, htmlFile;
                            originalController.UpdateProgressWithCancellationCheck("Wingman: Creating JSON");
                            var uploadResult = new UploadResults();
                            {
                                var ms = new MemoryStream();
                                var sw = new StreamWriter(ms, NoBOMEncodingUTF8);
                                var builder = new RawFormatBuilder(logToUse, new RawFormatSettings(true), ParserVersion, uploadResult);

                                builder.CreateJSON(sw, false);
                                sw.Close();

                                jsonFile = ms.ToArray();
                            }
                            originalController.UpdateProgressWithCancellationCheck("Wingman: Creating HTML");
                            {
                                var ms = new MemoryStream();
                                var sw = new StreamWriter(ms, NoBOMEncodingUTF8);
                                var builder = new HTMLBuilder(logToUse, new HTMLSettings(false, false, null, null, true), htmlAssets, ParserVersion, uploadResult);

                                builder.CreateHTML(sw, null);
                                sw.Close();
                                htmlFile = ms.ToArray();
                            }
                            if (logToUse != originalLog)
                            {
                                originalController.UpdateProgressWithCancellationCheck("Wingman: new ParsedEvtcLog processing completed");
                            }
                            originalController.UpdateProgressWithCancellationCheck("Wingman: Preparing Upload");
                            string result = logToUse.FightData.Success ? "kill" : "fail";
                            WingmanController.UploadProcessed(fInfo, accName, jsonFile, htmlFile, $"_{logToUse.FightData.Logic.Extension}_{result}", str => originalController.UpdateProgress("Wingman: " + str), ParserVersion);
                        }
                        catch (Exception e)
                        {
                            originalController.UpdateProgressWithCancellationCheck("Wingman: operation failed " + e.Message);
                        }
                    } 
                    else
                    {
                        originalController.UpdateProgressWithCancellationCheck("Wingman: upload is not possible");
                    }
                }
                originalController.UpdateProgressWithCancellationCheck("Wingman: operation completed");
#endif

            }
            return uploadresult;
        }

        public void DoWork(OperationController operation)
        {
            System.Globalization.CultureInfo before = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture =
                    new System.Globalization.CultureInfo("en-US");
            operation.Reset();
            try
            {
                operation.Start();
                var fInfo = new FileInfo(operation.InputFile);

                var parser = new EvtcParser(new EvtcParserSettings(Settings.Anonymous,
                                                Settings.SkipFailedTries,
                                                Settings.ParsePhases,
                                                Settings.ParseCombatReplay,
                                                Settings.ComputeDamageModifiers,
                                                Settings.CustomTooShort,
                                                Settings.DetailledWvW),
                                            APIController);

                //Process evtc here
                ParsedEvtcLog log = parser.ParseLog(operation, fInfo, out GW2EIEvtcParser.ParserHelpers.ParsingFailureReason failureReason, !Settings.SingleThreaded && HasFormat());
                if (failureReason != null)
                {
                    failureReason.Throw();
                }
                operation.BasicMetaData = new OperationController.OperationBasicMetaData(log);
                string[] uploadStrings = UploadOperation(fInfo, log, operation);
                if (Settings.SendEmbedToWebhook && Settings.UploadToDPSReports)
                {
                    if (Settings.SendSimpleMessageToWebhook)
                    {
                        WebhookController.SendMessage(Settings.WebhookURL, uploadStrings[0], out string message);
                        operation.UpdateProgressWithCancellationCheck("Webhook: " + message);
                    }
                    else
                    {
                        WebhookController.SendMessage(Settings.WebhookURL, BuildEmbedLother(log, uploadStrings[0]), out string message);
                        operation.UpdateProgressWithCancellationCheck("Webhook: " + message);
                        
                        WebhookController.SendMessage(Settings.WebhookURL, BuildEmbedLother2(log), out string message2);
                        operation.UpdateProgressWithCancellationCheck("Webhook: " + message2);
                    }
                }
                if (uploadStrings[0].Contains("https"))
                {
                    operation.DPSReportLink = uploadStrings[0];
                }
                //Creating File
                GenerateFiles(log, operation, uploadStrings, fInfo);
            }
            catch (Exception ex)
            {
                throw new ProgramException(ex);
            }
            finally
            {
                operation.Stop();
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        private static void CompressFile(string file, MemoryStream str, OperationController operation)
        {
            // Create the compressed file.
            byte[] data = str.ToArray();
            string outputFile = file + ".gz";
            using (FileStream outFile =
                        File.Create(outputFile))
            {
                using (var Compress =
                    new GZipStream(outFile,
                    CompressionMode.Compress))
                {
                    // Copy the source file into 
                    // the compression stream.
                    Compress.Write(data, 0, data.Length);
                }
            }
            operation.AddFile(outputFile);
        }

        private DirectoryInfo GetSaveDirectory(FileInfo fInfo)
        {
            //save location
            DirectoryInfo saveDirectory;
            if (Settings.SaveAtOut || Settings.OutLocation == null)
            {
                //Default save directory
                saveDirectory = fInfo.Directory;
                if (!saveDirectory.Exists)
                {
                    throw new InvalidOperationException("Save directory does not exist");
                }
            }
            else
            {
                if (!Directory.Exists(Settings.OutLocation))
                {
                    throw new InvalidOperationException("Save directory does not exist");
                }
                saveDirectory = new DirectoryInfo(Settings.OutLocation);
            }
            return saveDirectory;
        }

        public void GenerateTraceFile(OperationController operation)
        {
            if (Settings.SaveOutTrace)
            {
                var fInfo = new FileInfo(operation.InputFile);

                string fName = fInfo.Name.Split('.')[0];
                if (!fInfo.Exists)
                {
                    fInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
                }

                DirectoryInfo saveDirectory = GetSaveDirectory(fInfo);

                string outputFile = Path.Combine(
                saveDirectory.FullName,
                $"{fName}.log"
                );
                operation.AddFile(outputFile);
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    operation.WriteLogMessages(sw);
                }
                operation.OutLocation = saveDirectory.FullName;
            }
        }

        private void GenerateFiles(ParsedEvtcLog log, OperationController operation, string[] uploadStrings, FileInfo fInfo)
        {
            operation.UpdateProgressWithCancellationCheck("Program: Creating File(s)");

            DirectoryInfo saveDirectory = GetSaveDirectory(fInfo);

            string result = log.FightData.Success ? "kill" : "fail";
            string encounterLengthTerm = Settings.AddDuration ? "_" + (log.FightData.FightDuration / 1000).ToString() + "s" : "";
            string PoVClassTerm = Settings.AddPoVProf ? "_" + log.LogData.PoV.Spec.ToString().ToLower() : "";
            string fName = fInfo.Name.Split('.')[0];
            fName = $"{fName}{PoVClassTerm}_{log.FightData.Logic.Extension}{encounterLengthTerm}_{result}";

            var uploadResults = new UploadResults(uploadStrings[0], uploadStrings[1]);
            operation.OutLocation = saveDirectory.FullName;
            if (Settings.SaveOutHTML)
            {
                operation.UpdateProgressWithCancellationCheck("Program: Creating HTML");
                string outputFile = Path.Combine(
                saveDirectory.FullName,
                $"{fName}.html"
                );
                operation.AddOpenableFile(outputFile);
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    var builder = new HTMLBuilder(log,
                        new HTMLSettings(
                            Settings.LightTheme,
                            Settings.HtmlExternalScripts,
                            Settings.HtmlExternalScriptsPath,
                            Settings.HtmlExternalScriptsCdn,
                            Settings.HtmlCompressJson
                        ), htmlAssets, ParserVersion, uploadResults);
                    builder.CreateHTML(sw, saveDirectory.FullName);
                }
                operation.UpdateProgressWithCancellationCheck("Program: HTML created");
            }
            if (Settings.SaveOutCSV)
            {
                operation.UpdateProgressWithCancellationCheck("Program: Creating CSV");
                string outputFile = Path.Combine(
                    saveDirectory.FullName,
                    $"{fName}.csv"
                );
                operation.AddOpenableFile(outputFile);
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
                {
                    var builder = new CSVBuilder(log, new CSVSettings(","), ParserVersion, uploadResults);
                    builder.CreateCSV(sw);
                }
                operation.UpdateProgressWithCancellationCheck("Program: CSV created");
            }
            if (Settings.SaveOutJSON || Settings.SaveOutXML)
            {
                var builder = new RawFormatBuilder(log, new RawFormatSettings(Settings.RawTimelineArrays), ParserVersion, uploadResults);
                if (Settings.SaveOutJSON)
                {
                    operation.UpdateProgressWithCancellationCheck("Program: Creating JSON");
                    string outputFile = Path.Combine(
                        saveDirectory.FullName,
                        $"{fName}.json"
                    );
                    Stream str;
                    if (Settings.CompressRaw)
                    {
                        str = new MemoryStream();
                    }
                    else
                    {
                        str = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                    }
                    using (var sw = new StreamWriter(str, NoBOMEncodingUTF8))
                    {
                        builder.CreateJSON(sw, Settings.IndentJSON);
                    }
                    if (str is MemoryStream msr)
                    {
                        CompressFile(outputFile, msr, operation);
                        operation.UpdateProgressWithCancellationCheck("Program: JSON compressed");
                    }
                    else
                    {
                        operation.AddFile(outputFile);
                    }
                    operation.UpdateProgressWithCancellationCheck("Program: JSON created");
                }
                if (Settings.SaveOutXML)
                {
                    operation.UpdateProgressWithCancellationCheck("Program: Creating XML");
                    string outputFile = Path.Combine(
                        saveDirectory.FullName,
                        $"{fName}.xml"
                    );
                    Stream str;
                    if (Settings.CompressRaw)
                    {
                        str = new MemoryStream();
                    }
                    else
                    {
                        str = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                    }
                    using (var sw = new StreamWriter(str, NoBOMEncodingUTF8))
                    {
                        builder.CreateXML(sw, Settings.IndentXML);
                    }
                    if (str is MemoryStream msr)
                    {
                        CompressFile(outputFile, msr, operation);
                        operation.UpdateProgressWithCancellationCheck("Program: XML compressed");
                    }
                    else
                    {
                        operation.AddFile(outputFile);
                    }
                    operation.UpdateProgressWithCancellationCheck("Program: XML created");
                }
            }
            operation.UpdateProgressWithCancellationCheck($"Completed for {result}ed {log.FightData.Logic.Extension}");
        }

    }
}
