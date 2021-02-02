using System;
using System.Threading;
using System.Threading.Tasks;
using GBot;
using GCRBot;
using GCRBot.Data;

namespace Full
{
    public class Classroom : IDisposable
    {
        public event EventHandler<DataEventArgs<Message>> OnMessageReceived;
        public event EventHandler<DataEventArgs<Message>> OnGreetingReceived;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private CancellationToken token;
        private ClassroomBot crBot;
        private readonly LangConfig config;

        public Classroom(LangConfig config)
        {
            config.Driver.Headless = true;
            crBot = new ClassroomBot(config);
            OnMessageReceived += Greet;
            this.config = config;
            Login();
        }
        public Classroom(LangConfig config, CancellationToken token) : this(config)
        {
            this.token = token;
            token.Register(() => logger.Debug("Cancellation requested for classroom"));
        }
        public Meet InitMeetInstance(CancellationToken token)
        {
            string meetLink = crBot.GetClassroomMeetLink();

            Meet meet = new Meet(config, meetLink, token);

            OnGreetingReceived += meet.ReceiveStartMessage;
            return meet;
        }

        public async Task Start()
        {
            await GetMessageLoop();
        }
        public async Task GetMessageLoop()
        {
            Message last = null;
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    logger.Debug("Succesfully canceled");
                    break;
                }
                Message latest = crBot.GetMessage(0);
                if ((Message)latest != last)
                {
                    logger.Debug("Received message from {0}", latest.Teacher);
                    logger.Trace(latest);
                    OnMessageReceived?.Invoke(crBot, new DataEventArgs<Message>(latest, last));
                    last = latest;
                }
                await Task.Delay(new TimeSpan(0, minutes: 3, 0), token);
            }
        }
        private void Login()
        {
            bool loggedIn = Utils.Retry(crBot.Login, times: 3);
            if (!loggedIn)
            {
                throw new Exception("Couldn't login");
            }
            logger.Debug("Logged in.");
        }
        private void Greet(object sender, DataEventArgs<Message> eventArgs)
        {
            ClassroomBot bot = (ClassroomBot)sender;
            Message latest = eventArgs.Data;
            Message previous = eventArgs.PreviousData;
            if (latest.Information.ContainsGreeting()
                || latest.Information.HasMeetLink())
            {
                if (!(Utils.IsLangClass(latest) && Utils.IsLangClass(previous)))
                {
                    logger.Trace("Greeting on message {0}", latest);
                    latest = LangGroupFilter(bot, latest);
                    if (latest == null)
                        logger.Error("Can't find language group's teacher's message");

                    if (!bot.WrittenCommentOn(latest))
                    {
                        logger.Info("Saying hello to {0}", eventArgs.Data.Teacher);
                        bot.SendOnMessage(eventArgs.Data, "Добър ден.");
                    }

                    OnGreetingReceived?.Invoke(bot, eventArgs);
                }
            }
        }
        private Message LangGroupFilter(ClassroomBot bot, Message latest)
        {
            if (config.NemskaGrupa && latest.Teacher.Contains("Чапанова"))
            {
                Message msgAfter = bot.GetMessageAfter(latest);
                if (msgAfter.Teacher.Contains("Вихрогонова"))
                {
                    logger.Trace("Nemska grupa found teacher");
                    latest = msgAfter;
                }
                else
                {
                    latest = null;
                }
            }
            else if (!config.NemskaGrupa && latest.Teacher.Contains("Вихрогонова"))
            {
                Message msgAfter = bot.GetMessageAfter(latest);
                if (msgAfter.Teacher.Contains("Чапанова"))
                {
                    logger.Trace("Ruska grupa found teacher");
                    latest = msgAfter;
                }
                else
                {
                    latest = null;
                }
            }

            return latest;
        }

        public void Dispose()
        {
            ((IDisposable)crBot).Dispose();
        }
    }
}