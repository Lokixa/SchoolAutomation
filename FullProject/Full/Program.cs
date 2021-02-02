using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Full
{
    class Program
    {
        static NLog.Logger logger;

        static void Main(string[] args)
        {
            SetupLogger();
            CancellationTokenSource source = new();
            LangConfig config = GetConfig();
            if (config == null) return;

            Classroom classroom = new Classroom(config, source.Token);
            Meet meet = null;
            try
            {
                meet = classroom.InitMeetInstance(source.Token);
                Task crTask = classroom.Start();
                Task meetTask = meet.Start();
                Console.ReadLine();
                source.Cancel();
                Task.WaitAll(crTask, meetTask);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                    logger.Info("Successfully canceled");
                else logger.Error(ex);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
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

            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            var layout = new NLog.Layouts.SimpleLayout(
                "[${date:format=HH\\:mm\\:ss}][${level}]${logger:shortName=true}: ${message}"
            );
            logconsole.Layout = layout;
            logconsole.Encoding = System.Text.Encoding.UTF8;
            var logfile = new NLog.Targets.FileTarget("logfile");
            logfile.Layout = layout;
            logfile.Encoding = logconsole.Encoding;

            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logconsole, "*", final: true);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile, "*", final: true);

            NLog.LogManager.Configuration = config;
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        private static LangConfig GetConfig()
        {
            if (!System.IO.File.Exists("config.json"))
            {
                GCRBot.ClassroomBot.CreateEmpty<LangConfig>();
                logger.Info("Created empty config file");
                return null;
            }
            return JsonConvert.DeserializeObject<LangConfig>(System.IO.File.ReadAllText("config.json"));
        }
    }
}
