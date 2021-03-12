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
            OnMessageReceived += ToGreetCheck;
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

            meet.ActiveMeetLink = crBot.GetClassroomMeetLink();
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
                    Message? latest = crBot.GetMessage(0);
                    if ((Message)latest != last)
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
        private void ToGreetCheck(object? sender, DataEventArgs<Message> eventArgs)
        {
            ClassroomBot? bot = sender as ClassroomBot;
            if (bot == null) throw new NullReferenceException();
            if (eventArgs.Data == null)
            {
                throw new NullReferenceException("Message is null on receive");
            }

            Message latest = eventArgs.Data;

            logger.Debug("Trying to greet");
            if (latest.Information.ContainsGreeting()
                || latest.Information.HasMeetLink())
            {
                if (config.SplitClass.ReplacesTeachers.Contains(latest.Teacher))
                {
                    logger.Debug("Received greeting from different group teacher: {0}",
                                 latest.Teacher);
                    logger.Debug("Checking message after if missed...");
                    Message nextMessage = bot.GetMessageAfter(latest);
                    if (config.SplitClass.Teacher != nextMessage.Teacher)
                    {
                        return;
                    }
                    latest = nextMessage;
                }

                // Greet latest message
                if (bot.Greet(latest))
                {
                    logger.Info("Said 'добър ден.' to {0}", latest.Teacher);
                }
                else logger.Debug("Already greeted {0}", latest.Teacher);

                OnGreetingReceived?.Invoke(bot, new DataEventArgs<Message>(latest, eventArgs.PreviousData));
            }
        }

        public void Dispose()
        {
            ((IDisposable)crBot).Dispose();
        }
    }
}