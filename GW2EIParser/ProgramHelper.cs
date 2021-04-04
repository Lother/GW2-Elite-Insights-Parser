using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using GW2EIBuilders;
using GW2EIDiscord;
using GW2EIDPSReport;
using GW2EIDPSReport.DPSReportJsons;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using GW2EIGW2API;
using GW2EIParser.Exceptions;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIParser
{
    internal static class ProgramHelper
    {
        internal static HTMLAssets htmlAssets { get; set; }

        internal static Version ParserVersion { get; } = new Version(Application.ProductVersion);

        private static readonly UTF8Encoding NoBOMEncodingUTF8 = new UTF8Encoding(false);

        internal static readonly string SkillAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/SkillList.json";
        internal static readonly string SpecAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/SpecList.json";
        internal static readonly string TraitAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/TraitList.json";
        internal static readonly string EILogPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Logs/";

        internal static readonly GW2APIController APIController = new GW2APIController(SkillAPICacheLocation, SpecAPICacheLocation, TraitAPICacheLocation);

        internal static int GetMaxParallelRunning()
        {
            if (Properties.Settings.Default.SendEmbedToWebhook && Properties.Settings.Default.UploadToDPSReports)
            {
                return Math.Max(Environment.ProcessorCount / 2, 1);
            }
            else
            {
                return Environment.ProcessorCount;
            }
        }

        internal static EmbedBuilder GetEmbedBuilder()
        {
            var builder = new EmbedBuilder();
            builder.WithAuthor("Elite Insights " + ParserVersion.ToString(), "https://github.com/baaron4/GW2-Elite-Insights-Parser/blob/master/GW2EIParser/Content/LI.png?raw=true", "https://github.com/baaron4/GW2-Elite-Insights-Parser");
            return builder;
        }

        private static Embed BuildEmbed(ParsedEvtcLog log, string dpsReportPermalink)
        {
            EmbedBuilder builder = GetEmbedBuilder();
            builder.WithThumbnailUrl(log.FightData.Logic.Icon);
            //
            builder.AddField("Encounter Duration", log.FightData.DurationString);
            //
            if (log.StatisticsHelper.PresentFractalInstabilities.Any())
            {
                builder.AddField("Instabilities", string.Join("\n", log.StatisticsHelper.PresentFractalInstabilities.Select(x => x.Name)));
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
            builder.AddField("Game Data", "ARC: " + log.LogData.ArcVersion + " | " + "GW2 Build: " + log.LogData.GW2Build);
            //
            builder.WithTitle(log.FightData.GetFightName(log));
            //builder.WithTimestamp(DateTime.Now);
            builder.WithFooter(log.LogData.LogStartStd + " / " + log.LogData.LogEndStd);
            builder.WithColor(log.FightData.Success ? Color.Green : Color.Red);
            if (dpsReportPermalink.Length > 0)
            {
                builder.WithUrl(dpsReportPermalink);
            }
            return builder.Build();
        }
        internal static EmbedBuilder GetEmbedBuilderLother()
        {
            var builder = new EmbedBuilder();
            builder.WithAuthor("AEG WvW Report", "https://wiki.guildwars2.com/images/5/51/Catmander_tag_%28white%29.png", "https://lother.dev/wvw/");
            return builder;
        }

        private static Embed BuildEmbedLother(ParsedEvtcLog log, string dpsReportPermalink)
        {
            EmbedBuilder builder = GetEmbedBuilderLother();
            //
            builder.AddField("Encounter Duration", log.FightData.DurationString);
            var players = new List<AbstractSingleActor>(log.PlayerList.Where(x => !x.IsFakeActor));
            IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
            string s = "";
            int totalDamage = 0;
            int totalDps = 0;
            int totalDowns = 0;
            int totalDeaths = 0;
            var boonStrips = new List<KeyValuePair<string,int>>();
            var condiCleanse = new List<KeyValuePair<string, int>>();
            var dps = new List<KeyValuePair<string, (int,int)>>();
            foreach (Player player in players) {
                string name = $"{player.Character} ({player.Prof.Substring(0,3)})";
                FinalDPS dpss = player.GetDPSStats(log, phases[0].Start, phases[0].End);
                totalDamage += dpss.Damage;
                totalDps += dpss.Dps;
                dps.Add(new KeyValuePair<string, (int, int)>(name, (dpss.Damage, dpss.Dps)));
                FinalDefensesAll def = player.GetDefenseStats(log, phases[0].Start, phases[0].End);
                totalDowns += def.DownCount;
                totalDeaths += def.DeadCount;
                FinalPlayerSupport sp = player.GetPlayerSupportStats(log, phases[0].Start, phases[0].End);
                condiCleanse.Add(new KeyValuePair<string, int>(name, sp.CondiCleanse));
                boonStrips.Add(new KeyValuePair<string, int >(name, sp.BoonStrips));
            }
            dps = dps.OrderByDescending(x => x.Value.Item1).ToList().GetRange(0, 10);
            condiCleanse = condiCleanse.OrderByDescending(x => x.Value).ToList().GetRange(0, 10);
            boonStrips = boonStrips.OrderByDescending(x => x.Value).ToList().GetRange(0, 10);

            builder.AddField("Squad Summary", "```CSS\n" +
               $" Player   Damage       DPS      Downs    Deaths\n"+
               $"--------  -----------  -------  -------  -------\n" +
               $"   {players.Count,-2}     {totalDamage.ToString("N0"),11}  {totalDps.ToString("N0"),7}     {totalDowns,-2}       {totalDeaths,-2}\n" +
                "```");
            string dpsString = "";
            string condiCleanseString = "";
            string boonStripsString = "";
            int c;
            c = 1;
            foreach (KeyValuePair<string, (int, int)> d in dps) {
                dpsString += $"{c,2}.  {d.Key,24}  {d.Value.Item1.ToString("N0"),9}  {d.Value.Item2.ToString("N0"),7}\n";
                c++;
            }
            c = 1;
            foreach (KeyValuePair<string, int> cc in condiCleanse)
            {
                condiCleanseString += $"{c,2}.  {cc.Key,24}  {cc.Value, 6}\n";
                c++;
            }
            c = 1;
            foreach (KeyValuePair<string, int> bs in boonStrips)
            {
                boonStripsString += $"{c,2}.  {bs.Key,24}  {bs.Value,5}\n";
                c++;
            }

            builder.AddField("Damage Summary", "```CSS\n" +
               $" #  Player                     Damage     DPS    \n" +
               $"--- -------------------------  ---------  -------\n" +
                dpsString +
                "```");
            builder.AddField("Cleanse Summary", "```CSS\n" +
               $" #  Player                     Cleanses\n" +
               $"--- -------------------------  ----------\n" +
                condiCleanseString +
                "```");
            builder.AddField("Strips Summary", "```CSS\n" +
               $" #  Player                     Strips\n" +
               $"--- -------------------------  --------\n" +
                boonStripsString +
                "```");
            /*
            var playerByGroup = log.PlayerList.Where(x => !x.IsFakeActor).GroupBy(x => x.Group).ToDictionary(x => x.Key, x => x.ToList());
            var hasGroups = playerByGroup.Count > 1;
            foreach (KeyValuePair<int, List<Player>> pair in playerByGroup)
            {
                var groupField = new List<string>();
                foreach (Player p in pair.Value)
                {
                    groupField.Add(p.Character + " - " + p.Prof);
                }
                builder.AddField(hasGroups ? "Group " + pair.Key : "Party Composition", String.Join("\n", groupField));
            }
            //*/
            //
            builder.WithTitle(log.FightData.GetFightName(log));
            //builder.WithTimestamp(DateTime.Now);
            builder.WithFooter(log.LogData.LogStartStd + " / " + log.LogData.LogEndStd);
            builder.WithColor(log.FightData.Success ? Color.Green : Color.Red);
            if (dpsReportPermalink.Length > 0)
            {
                builder.WithUrl(dpsReportPermalink);
            }
            return builder.Build();
        }

        private static bool HasFormat()
        {
            return Properties.Settings.Default.SaveOutCSV || Properties.Settings.Default.SaveOutHTML || Properties.Settings.Default.SaveOutXML || Properties.Settings.Default.SaveOutJSON;
        }

        private static string[] UploadOperation(List<string> traces, FileInfo fInfo)
        {
            var controller = new DPSReportController(Properties.Settings.Default.DPSReportUserToken,
                Properties.Settings.Default.Anonymous,
                Properties.Settings.Default.DetailledWvW
                );
            //Upload Process
            string[] uploadresult = new string[3] { "", "", "" };
            if (Properties.Settings.Default.UploadToDPSReports)
            {
                traces.Add("Uploading to DPSReports using EI");
                DPSReportUploadObject response = controller.UploadUsingEI(fInfo, traces);
                uploadresult[0] = response != null ? response.Permalink : "Upload process failed";
                traces.Add("DPSReports using EI: " + uploadresult[0]);
            }
            if (Properties.Settings.Default.UploadToDPSReportsRH)
            {
                traces.Add("Uploading to DPSReports using RH");
                DPSReportUploadObject response = controller.UploadUsingRH(fInfo, traces);
                uploadresult[1] = response != null ? response.Permalink : "Upload process failed";
                traces.Add("DPSReports using RH: " + uploadresult[1]);
            }
            /*if (settings.UploadToRaidar)
            {
                traces.Add("Uploading to Raidar");
                uploadresult[2] = UploadController.UploadRaidar();
                traces.Add("Raidar: " + uploadresult[2]);
            }*/
            return uploadresult;
        }

        public static void DoWork(OperationController operation)
        {
            System.Globalization.CultureInfo before = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture =
                    new System.Globalization.CultureInfo("en-US");
            operation.Reset();
            var sw = new Stopwatch();
            try
            {
                sw.Start();
                var fInfo = new FileInfo(operation.InputFile);

                var parser = new EvtcParser(new EvtcParserSettings(Properties.Settings.Default.Anonymous,
                                                Properties.Settings.Default.SkipFailedTries,
                                                Properties.Settings.Default.ParsePhases,
                                                Properties.Settings.Default.ParseCombatReplay,
                                                Properties.Settings.Default.ComputeDamageModifiers,
                                                Properties.Settings.Default.CustomTooShort,
                                                Properties.Settings.Default.DetailledWvW),
                                            APIController);

                //Process evtc here
                ParsedEvtcLog log = parser.ParseLog(operation, fInfo, out GW2EIEvtcParser.ParserHelpers.ParsingFailureReason failureReason);
                if (failureReason != null)
                {
                    failureReason.Throw();
                }
                operation.BasicMetaData = new OperationController.OperationBasicMetaData(log);
                var externalTraces = new List<string>();
                string[] uploadStrings = UploadOperation(externalTraces, fInfo);
                foreach (string trace in externalTraces)
                {
                    operation.UpdateProgressWithCancellationCheck(trace);
                }
                string result = log.FightData.Success ? "kill" : "fail";
                string encounterLengthTerm = Properties.Settings.Default.AddDuration ? "_" + (log.FightData.FightEnd / 1000).ToString() + "s" : "";
                string PoVClassTerm = Properties.Settings.Default.AddPoVProf ? "_" + log.LogData.PoV.Prof.ToLower() : "";
                string fName = fInfo.Name.Split('.')[0];
                fName = $"{fName}{PoVClassTerm}_{log.FightData.Logic.Extension}{encounterLengthTerm}_{result}.html";
                string URI = "https://lother.dev/wvw/" + fName;
                if (Properties.Settings.Default.SendEmbedToWebhook){
                    operation.UpdateProgressWithCancellationCheck(new WebhookController(Properties.Settings.Default.WebhookURL, BuildEmbedLother(log, URI)).SendMessage());
                }
                /*if (Properties.Settings.Default.SendEmbedToWebhook && Properties.Settings.Default.UploadToDPSReports)
                {
                    if (Properties.Settings.Default.SendSimpleMessageToWebhook)
                    {
                        operation.UpdateProgressWithCancellationCheck(new WebhookController(Properties.Settings.Default.WebhookURL, uploadStrings[0]).SendMessage());
                    } 
                    else
                    {
                        operation.UpdateProgressWithCancellationCheck(new WebhookController(Properties.Settings.Default.WebhookURL, BuildEmbed(log, uploadStrings[0])).SendMessage());
                    }
                }
                if (uploadStrings[0].Contains("https"))
                {
                    operation.DPSReportLink = uploadStrings[0];
                }*/
                //Creating File
                GenerateFiles(log, operation, uploadStrings, fInfo);
            }
            catch (Exception ex)
            {
                throw new ProgramException(ex);
            }
            finally
            {
                sw.Stop();
                GC.Collect();
                Thread.CurrentThread.CurrentCulture = before;
                operation.Elapsed = ("Elapsed " + sw.ElapsedMilliseconds + " ms");
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
            operation.GeneratedFiles.Add(outputFile);
        }

        private static DirectoryInfo GetSaveDirectory(FileInfo fInfo)
        {
            //save location
            DirectoryInfo saveDirectory;
            if (Properties.Settings.Default.SaveAtOut || Properties.Settings.Default.OutLocation == null)
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
                if (!Directory.Exists(Properties.Settings.Default.OutLocation))
                {
                    throw new InvalidOperationException("Save directory does not exist");
                }
                saveDirectory = new DirectoryInfo(Properties.Settings.Default.OutLocation);
            }
            return saveDirectory;
        }

        public static void GenerateTraceFile(OperationController operation)
        {
            if (Properties.Settings.Default.SaveOutTrace)
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
                operation.GeneratedFiles.Add(outputFile);
                operation.OutLocation = saveDirectory.FullName;
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    operation.WriteLogMessages(sw);
                }
            }
        }

        private static void GenerateFiles(ParsedEvtcLog log, OperationController operation, string[] uploadStrings, FileInfo fInfo)
        {
            operation.UpdateProgressWithCancellationCheck("Creating File(s)");

            DirectoryInfo saveDirectory = GetSaveDirectory(fInfo);

            string result = log.FightData.Success ? "kill" : "fail";
            string encounterLengthTerm = Properties.Settings.Default.AddDuration ? "_" + (log.FightData.FightEnd / 1000).ToString() + "s" : "";
            string PoVClassTerm = Properties.Settings.Default.AddPoVProf ? "_" + log.LogData.PoV.Prof.ToLower() : "";
            string fName = fInfo.Name.Split('.')[0];
            fName = $"{fName}{PoVClassTerm}_{log.FightData.Logic.Extension}{encounterLengthTerm}_{result}";

            // parallel stuff
            if (Properties.Settings.Default.MultiThreaded && HasFormat())
            {
                IReadOnlyList<PhaseData> phases = log.FightData.GetPhases(log);
                operation.UpdateProgressWithCancellationCheck("Multi threading");
                var playersAndTargets = new List<AbstractSingleActor>(log.PlayerList);
                playersAndTargets.AddRange(log.FightData.Logic.Targets);
                foreach (AbstractSingleActor actor in playersAndTargets)
                {
                    // that part can't be //
                    actor.GetTrackedBuffs(log);
                }
                Parallel.ForEach(playersAndTargets, actor => actor.GetStatus(log));
                if (log.CanCombatReplay)
                {
                    var playersAndTargetsAndMobs = new List<AbstractSingleActor>(log.FightData.Logic.TrashMobs);
                    playersAndTargetsAndMobs.AddRange(playersAndTargets);
                    // init all positions
                    Parallel.ForEach(playersAndTargetsAndMobs, actor => actor.GetCombatReplayPolledPositions(log));
                }
                else if (log.CombatData.HasMovementData)
                {
                    Parallel.ForEach(log.PlayerList, player => player.GetCombatReplayPolledPositions(log));
                }
                Parallel.ForEach(playersAndTargets, actor => actor.GetBuffGraphs(log));
                Parallel.ForEach(playersAndTargets, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffDistribution(log, phase.Start, phase.End);
                    }
                });
                Parallel.ForEach(playersAndTargets, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffPresence(log, phase.Start, phase.End);
                    }
                });
                //
                Parallel.ForEach(log.PlayerList, player => player.GetDamageModifierStats(log, null));
                Parallel.ForEach(log.PlayerList, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffs(BuffEnum.Self, log, phase.Start, phase.End);
                    }
                });
                Parallel.ForEach(log.PlayerList, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffs(BuffEnum.Group, log, phase.Start, phase.End);
                    }
                });
                Parallel.ForEach(log.PlayerList, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffs(BuffEnum.OffGroup, log, phase.Start, phase.End);
                    }
                });
                Parallel.ForEach(log.PlayerList, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffs(BuffEnum.Squad, log, phase.Start, phase.End);
                    }
                });
                Parallel.ForEach(log.FightData.Logic.Targets, actor =>
                {
                    foreach (PhaseData phase in phases)
                    {
                        actor.GetBuffs(log, phase.Start, phase.End);
                    }
                });
            }
            var uploadResults = new UploadResults(uploadStrings[0], uploadStrings[1], uploadStrings[2]);
            if (Properties.Settings.Default.SaveOutHTML)
            {
                operation.UpdateProgressWithCancellationCheck("Creating HTML");
                string outputFile = Path.Combine(
                saveDirectory.FullName,
                $"{fName}.html"
                );
                operation.GeneratedFiles.Add(outputFile);
                operation.OpenableFiles.Add(outputFile);
                operation.OutLocation = saveDirectory.FullName;
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    var builder = new HTMLBuilder(log, new HTMLSettings(Properties.Settings.Default.LightTheme, Properties.Settings.Default.HtmlExternalScripts, Properties.Settings.Default.HtmlExternalScriptsPath, Properties.Settings.Default.HtmlExternalScriptsCdn), htmlAssets, ParserVersion, uploadResults);
                    builder.CreateHTML(sw, saveDirectory.FullName);
                }
                operation.UpdateProgressWithCancellationCheck("HTML created");
            }
            if (Properties.Settings.Default.SaveOutCSV)
            {
                operation.UpdateProgressWithCancellationCheck("Creating CSV");
                string outputFile = Path.Combine(
                    saveDirectory.FullName,
                    $"{fName}.csv"
                );
                operation.GeneratedFiles.Add(outputFile);
                operation.OpenableFiles.Add(outputFile);
                operation.OutLocation = saveDirectory.FullName;
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
                {
                    var builder = new CSVBuilder(log, new CSVSettings(","), ParserVersion, uploadResults);
                    builder.CreateCSV(sw);
                }
                operation.UpdateProgressWithCancellationCheck("CSV created");
            }
            if (Properties.Settings.Default.SaveOutJSON || Properties.Settings.Default.SaveOutXML)
            {
                var builder = new RawFormatBuilder(log, new RawFormatSettings(Properties.Settings.Default.RawTimelineArrays), ParserVersion, uploadResults);
                if (Properties.Settings.Default.SaveOutJSON)
                {
                    operation.UpdateProgressWithCancellationCheck("Creating JSON");
                    string outputFile = Path.Combine(
                        saveDirectory.FullName,
                        $"{fName}.json"
                    );
                    operation.OutLocation = saveDirectory.FullName;
                    Stream str;
                    if (Properties.Settings.Default.CompressRaw)
                    {
                        str = new MemoryStream();
                    }
                    else
                    {
                        str = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                    }
                    using (var sw = new StreamWriter(str, NoBOMEncodingUTF8))
                    {
                        builder.CreateJSON(sw, Properties.Settings.Default.IndentJSON);
                    }
                    if (str is MemoryStream msr)
                    {
                        CompressFile(outputFile, msr, operation);
                        operation.UpdateProgressWithCancellationCheck("JSON compressed");
                    }
                    else
                    {
                        operation.GeneratedFiles.Add(outputFile);
                    }
                    operation.UpdateProgressWithCancellationCheck("JSON created");
                }
                if (Properties.Settings.Default.SaveOutXML)
                {
                    operation.UpdateProgressWithCancellationCheck("Creating XML");
                    string outputFile = Path.Combine(
                        saveDirectory.FullName,
                        $"{fName}.xml"
                    );
                    operation.OutLocation = saveDirectory.FullName;
                    Stream str;
                    if (Properties.Settings.Default.CompressRaw)
                    {
                        str = new MemoryStream();
                    }
                    else
                    {
                        str = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                    }
                    using (var sw = new StreamWriter(str, NoBOMEncodingUTF8))
                    {
                        builder.CreateXML(sw, Properties.Settings.Default.IndentXML);
                    }
                    if (str is MemoryStream msr)
                    {
                        CompressFile(outputFile, msr, operation);
                        operation.UpdateProgressWithCancellationCheck("XML compressed");
                    }
                    else
                    {
                        operation.GeneratedFiles.Add(outputFile);
                    }
                    operation.UpdateProgressWithCancellationCheck("XML created");
                }
            }
            operation.UpdateProgressWithCancellationCheck($"Completed parsing for {result}ed {log.FightData.Logic.Extension}");
        }

    }
}
