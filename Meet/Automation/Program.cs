﻿using System;
using System.IO;
using System.Threading.Tasks;
using MeetGBot;
using Newtonsoft.Json;
using OpenQA.Selenium.Firefox;

namespace Automation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await MeetTest();
        }

        private static async Task MeetTest()
        {
            if (!File.Exists("config.json"))
            {
                MeetBot.CreateSampleConfig();
                Console.WriteLine("Created sample config.");
                return;
            }
            MeetConfig config = JsonConvert.DeserializeObject<MeetConfig>(File.ReadAllText(Path.GetFullPath(".") + "/config.json"));
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
                bot.EnterMeet("https://meet.google.com/rje-zpyi-jcg");
                await WaitFor(5);
                bot.LeaveMeet();

                await WaitFor(10);

                bot.EnterMeet("https://meet.google.com/ewd-kemi-rny");
                await WaitFor(5);
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
    }
}