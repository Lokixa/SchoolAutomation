using System;
using System.Threading;
using System.Threading.Tasks;
using Full;
using GCRBot.Data;

namespace Tests
{
    class Program
    {
        private static NLog.Logger logger;

        static void Main(string[] args)
        {
            SetupLogger();
            LangConfig config = new LangConfig()
            {
                NemskaGrupa = false,
                Link = "https://classroom.google.com/u/1/c/MjEwMzIwNDY5MjYz",
            };
            config.Driver.Browser = "chrome";
            config.Driver.DriverFolder = "..\\..\\drivers";
            CancellationTokenSource source = new();

            using (Classroom cr = new Classroom(config, source.Token))
            {
                using (Meet meet = cr.InitMeetInstance(source.Token))
                {
                    try
                    {
                        Task crTask = cr.Start();
                        Task meetTask = meet.Start();
                        Console.ReadLine();
                        source.Cancel();
                        Task.WaitAll(crTask, meetTask);
                    }
                    catch (AggregateException agex)
                    {
                        foreach (Exception ex in agex.InnerExceptions)
                        {
                            if (ex is TaskCanceledException)
                            {
                                logger.Debug("Successful cancel");
                            }
                            else logger.Error(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }
        }
        private static void SetupLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            logconsole.Layout = new NLog.Layouts.SimpleLayout(
                "[${date:format=HH\\:mm\\:ss}][${level}]${logger:shortName=true}: ${message}"
            );
            logconsole.Encoding = System.Text.Encoding.UTF8;

            // Rules for mapping loggers to targets            
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logconsole, "*", final: true);

            // Apply config           
            NLog.LogManager.Configuration = config;
            logger = NLog.LogManager.GetCurrentClassLogger();
        }
    }
}
