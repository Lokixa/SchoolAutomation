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
        static void Main(string[] args)
        {
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
                Console.WriteLine(bot.PeopleInMeet());
                bot.LeaveMeet();

                // await WaitFor(10);

                // bot.EnterMeetOverview("https://meet.google.com/ewd-kemi-rny");
                // Console.WriteLine(bot.PeopleInMeetOverview());
                // bot.EnterMeet();
                // await WaitFor(5);
                // bot.LeaveMeet();
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
    }
}
