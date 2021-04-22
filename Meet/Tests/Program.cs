using System;
using System.IO;
using System.Threading.Tasks;
using GBot;
using MeetGBot;
using Newtonsoft.Json;

namespace Automation
{
    class Program
    {
        static NLog.Logger logger;
        static void Main(string[] args)
        {
            SetupLogger();
            MeetTest();
        }

        private static void MeetTest()
        {
            if (!File.Exists("config.json"))
            {
                MeetBot.CreateEmpty<Config>();
                Console.WriteLine("Created sample config.");
                return;
            }
            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.GetFullPath(".") + "/config.json"));
            MeetBot bot = new MeetBot(config);
            try
            {
                bool loggedIn = bot.Login();
                if (!loggedIn)
                {
                    Console.WriteLine("Retrying login");
                    for (int i = 0; i < 3 && !loggedIn; i++)
                    {
                        loggedIn = bot.Login();
                    }
                    Console.WriteLine("Logged in: " + loggedIn);
                }
                bot.EnterMeetOverview("https://meet.google.com/rje-zpyi-jcg");
                Console.WriteLine(bot.PeopleInMeetOverview());
                bot.EnterMeet();
                Task wait = WaitFor(10);
                while (!wait.IsCompleted)
                {
                    Console.WriteLine(bot.PeopleInMeet());
                }
                Console.WriteLine("Press enter to quit...");
                Console.ReadLine();
                bot.LeaveMeet();
            }
            finally
            {
                bot.Dispose();
            }
        }
        static async Task WaitFor(float seconds)
        {
            Console.WriteLine($"Waiting for {seconds} seconds");
            await Task.Delay((int)(seconds * 1000));
            Console.WriteLine($"Done");
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

            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logconsole, "*");

            NLog.LogManager.Configuration = config;
            logger = NLog.LogManager.GetCurrentClassLogger();
        }
    }
}
