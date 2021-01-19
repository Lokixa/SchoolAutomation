﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GoogleCRBot;
using GoogleCRBot.Data;
using Newtonsoft.Json;

namespace Automation
{
    class Program
    {
        const bool NemskaGrupa = false;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            CRConfig config = JsonConvert.DeserializeObject<CRConfig>(File.ReadAllText("config.json"));
            if (config == null)
            {
                Console.WriteLine("Invalid config file");
            }
            using Bot bot = new Bot(config, HelloMsg);
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
        static Task HelloMsg(ClassroomBot bot, CancellationToken token)
        {
            // Post lastPost = null;
            Message lastMessage = null;
            return Task.Run(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested) break;
                    Message latestMsg = bot.GetMessage(0);
                    if (latestMsg != lastMessage)
                    {
                        Console.WriteLine(latestMsg);
                        if (latestMsg.Information.ContainsGreeting()
                            || latestMsg.Information.IsMeetLink())
                        {
                            if (!AreLangClass(latestMsg, lastMessage))
                            {
                                latestMsg = LangGroupFilter(bot, latestMsg);
                                bot.SendOnMessage(latestMsg, "Добър ден.");
                                Console.WriteLine("Добър ден " + latestMsg.Teacher);
                            }
                        }
                    }
                    lastMessage = latestMsg;


                    await Task.Delay(new TimeSpan(0, minutes: 1, 0), token);
                }
            });
        }
        // Post latestPost = bot.GetPost(0);
        // if (latestPost != lastPost)
        // {
        //     if (latestPost.Teacher.Contains("Йовчева"))
        //     {
        //         // Console.WriteLine(latestPost);
        //         bot.GoToPost(latestPost);
        //         bot.SendOnCurrentPost("Добър ден.");
        //         bot.GoHome();
        //         Console.WriteLine("\nДобър ден " + latestPost.Teacher);
        //     }
        // }
        // lastPost = latestPost;

        private static bool AreLangClass(Message latestMsg, Message lastMessage)
        {
            if (lastMessage == null)
            {
                return false;
            }
            if (latestMsg.Teacher.Contains("Чапанова"))
            {
                return lastMessage.Teacher.Contains("Вихрогонова");
            }
            else if (latestMsg.Teacher.Contains("Вихрогонова"))
            {
                return lastMessage.Teacher.Contains("Чапанова");
            }
            return false;
        }

        private static Message LangGroupFilter(ClassroomBot bot, Message latest)
        {
            if (NemskaGrupa && latest.Teacher.Contains("Чапанова"))
            {
                Message msgAfter = bot.GetMessageAfter(latest, 0);
                if (msgAfter.Teacher.Contains("Вихрогонова"))
                {
                    latest = msgAfter;
                }
                else
                {
                    latest = null;
                }
            }
            else if (!NemskaGrupa && latest.Teacher.Contains("Вихрогонова"))
            {
                Message msgAfter = bot.GetMessageAfter(latest, 1);
                Console.WriteLine(msgAfter);
                if (msgAfter.Teacher.Contains("Чапанова"))
                {
                    latest = msgAfter;
                }
                else
                {
                    latest = null;
                }
            }

            return latest;
        }
    }
    public class Bot : IDisposable
    {
        protected ClassroomBot bot;
        protected CancellationTokenSource source = new CancellationTokenSource();
        public Task Task { get; }
        public Bot(CRConfig config, Func<ClassroomBot, CancellationToken, Task> loop)
        {
            bot = new ClassroomBot(config);
            Login();
            Task = loop(bot, source.Token);
        }
        void Login()
        {
            bool loggedIn = bot.Login();
            Console.WriteLine("Logged in: " + loggedIn);
            if (!loggedIn)
            {
                for (int i = 0; i < 3 && !loggedIn; i++)
                {
                    Console.WriteLine("Retrying login");
                    loggedIn = bot.Login();
                }
                if (!loggedIn) throw new Exception("Can't login");
                Console.WriteLine("Logged in");
            }
        }

        public void Dispose()
        {
            source.Cancel();
            ((IDisposable)bot).Dispose();
            source.Dispose();
            Task.Dispose();
        }
    }
    public static class Extensions
    {
        public static bool ContainsGreeting(this string text)
        {
            string[] greetings = new string[] {
                "добър ден","привет","здравейте","hello","очаквам ви","guten tag","good morning","добро утро"
            };
            foreach (string greeting in greetings)
            {
                if (text.Contains(greeting, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsMeetLink(this string text)
        {
            return text.Contains("meet.google.com");
        }
    }
}