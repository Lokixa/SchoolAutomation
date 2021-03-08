using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GBot;
using GCRBot;
using GCRBot.Data;

namespace Full
{
    public class Classroom : IDisposable
    {
        public event EventHandler<DataEventArgs<Message>>? OnMessageReceived;
        public event EventHandler<DataEventArgs<Message>>? OnGreetingReceived;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private CancellationToken? token;
        private ClassroomBot crBot;
        private readonly FullConfig config;

        public Classroom(FullConfig config)
        {
            config.Driver.Headless = true;
            crBot = new ClassroomBot(config);
            OnMessageReceived += Greet;
            this.config = config;
            Login();
        }
        public Classroom(FullConfig config, CancellationToken token) : this(config)
        {
            this.token = token;
            token.Register(() => logger.Debug("Cancellation requested for classroom"));
        }
        public Meet InitMeetInstance(CancellationToken token)
        {
            Meet meet = new Meet(config, token);

            OnGreetingReceived += meet.ReceiveStartMessage;
            return meet;
        }

        public async Task Start()
        {
            await GetMessageLoop();
        }
        public async Task GetMessageLoop()
        {
            try
            {
                Message? last = null;
                while (true)
                {
                    if (token?.IsCancellationRequested ?? false)
                    {
                        logger.Debug("Succesfully canceled");
                        break;
                    }
                    Message latest = crBot.GetMessage(0);
                    if ((Message)latest !=  last)
                    {
                        logger.Debug("Received message from {0}", latest.Teacher);
                        logger.Trace(latest);
                        OnMessageReceived?.Invoke(crBot, new DataEventArgs<Message>(latest, last));
                        last = latest;
                    }
                    if (token == null)
                        await Task.Delay(new TimeSpan(0, minutes: 3, 0));
                    else
                        await Task.Delay(new TimeSpan(0, minutes: 3, 0), (CancellationToken)token);
                }
            }
            catch (TaskCanceledException)
            {
                logger.Debug("Successfully canceled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
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
        private void Greet(object? sender, DataEventArgs<Message> eventArgs)
        {
            ClassroomBot? bot = sender as ClassroomBot;
            if (bot == null) throw new NullReferenceException();
            if (eventArgs.Data == null)
            {
                throw new NullReferenceException("Message is null on receive");
            }

            Message latest = eventArgs.Data;

            if (latest.Information.ContainsGreeting()
                || latest.Information.HasMeetLink())
            {
                if (config.SplitClass.ReplacesTeachers.Contains(latest.Teacher))
                {
                    logger.Debug("Received greeting from different group teacher: {0}",
                                 latest.Teacher);
                }
                else
                {
                    logger.Debug("Trying to greet");

                    if (!bot.WrittenCommentOn(latest))
                    {
                        logger.Info("Saying hello to {0}", eventArgs.Data.Teacher);
                        bot.SendOnMessage(eventArgs.Data, "Добър ден.");
                    }
                    else logger.Debug("There's a comment on it");
                }

                OnGreetingReceived?.Invoke(bot, eventArgs);
            }
        }

        public void Dispose()
        {
            ((IDisposable)crBot).Dispose();
        }
    }
}