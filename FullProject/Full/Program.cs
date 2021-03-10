using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Full
{
    class Program
    {
        static NLog.Logger? logger;

        static void Main(string[] args)
        {
            SetupLogger();
            FullConfig? config = GetConfig();
            if (config == null) return;

            CancellationTokenSource source = new();

            Classroom classroom = new Classroom(config, source.Token);
            Meet? meet = null;
            try
            {
                meet = classroom.InitMeetInstance(source.Token);
                logger?.Debug("Starting classroom");
                Task crTask = classroom.Start();
                logger?.Debug("Starting meet");
                Task meetTask = meet.Start();
                logger?.Debug("Started all");
                Console.ReadLine();
                source.Cancel();
                Task.WaitAll(crTask, meetTask);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                    logger?.Info("Successfully canceled");
                else logger?.Error(ex);
            }
            catch (Exception ex)
            {
                logger?.Error(ex);
            }
            finally
            {
                source.Dispose();
                meet?.Dispose();
                classroom.Dispose();
            }
        }
        private static void SetupLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole");
            var layout = new NLog.Layouts.SimpleLayout(
                "[${date:format=HH\\:mm\\:ss}][${level}]${logger:shortName=true}: ${message}"
            );
            logconsole.Layout = layout;
            logconsole.Encoding = System.Text.Encoding.UTF8;
            var logfile = new NLog.Targets.FileTarget("logfile");
            logfile.FileName = "./logs/log.txt";
            logfile.ArchiveFileName = "log.{#}.txt";
            logfile.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
            logfile.ArchiveDateFormat = "dd-MM-yyyy";
            logfile.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
            logfile.CreateDirs = true;
            logfile.Layout = layout;
            logfile.Encoding = System.Text.Encoding.UTF8;

            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logconsole, "*");
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile, "*");

            NLog.LogManager.Configuration = config;
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        private static FullConfig? GetConfig()
        {
            if (!System.IO.File.Exists("config.json"))
            {
                GCRBot.ClassroomBot.CreateEmpty<FullConfig>();
                logger?.Info("Created empty config file");
                return null;
            }
            return JsonConvert.DeserializeObject<FullConfig>(System.IO.File.ReadAllText("config.json"));
        }
    }
}
