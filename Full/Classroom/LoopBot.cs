using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GBot;
using GCRBot;

namespace Full
{
    internal class LoopBot : IDisposable
    {
        protected ClassroomBot bot;
        private readonly CancellationToken token;

        public List<Task> Tasks { get; private set; }
        public LoopBot(Config config, CancellationToken token)
        {
            this.bot = new ClassroomBot(config);
            this.Tasks = new List<Task>();
            this.token = token;
        }
        public void Add(Func<ClassroomBot, CancellationToken, Task> loop)
        {
            Tasks.Add(Task.Run(() => loop(bot, token)));
        }
        public void Login()
        {
            bool loggedIn = bot.Login();
            // Console.WriteLine("Logged in: " + loggedIn);
            if (!loggedIn)
            {
                for (int i = 0; i < 3 && !loggedIn; i++)
                {
                    // Console.WriteLine("Retrying login");
                    loggedIn = bot.Login();
                }
                if (!loggedIn) throw new Exception("Can't login");
                // Console.WriteLine("Logged in");
            }
        }

        public void Dispose()
        {
            ((IDisposable)bot).Dispose();
            foreach (Task task in Tasks)
            {
                task.Dispose();
            }
        }
    }
}