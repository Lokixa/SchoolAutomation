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
        public event EventHandler<DataEventArgs<Message>> OnMessageReceived;
        public event EventHandler<DataEventArgs<Message>> OnGreetingReceived;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private CancellationToken token;
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
            try
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
        private void Greet(object sender, DataEventArgs<Message> eventArgs)
        {
            ClassroomBot bot = (ClassroomBot)sender;
            if (eventArgs.Data == null)
            {
                throw new NullReferenceException("Message is null on receive");
            }

            Message latest = LanguageClassTrim(eventArgs);
            if (latest == null)
            {
                logger.Info("Received message but not in language group.");
                logger.Info("Moving on...");
                return;
            }

            if (latest.Information.ContainsGreeting()
                || latest.Information.HasMeetLink())
            {
                logger.Debug("Trying to greet", latest);

                if (!bot.WrittenCommentOn(latest))
                {
                    logger.Info("Saying hello to {0}", eventArgs.Data.Teacher);
                    bot.SendOnMessage(eventArgs.Data, "Добър ден.");
                }
                else logger.Debug("There's a comment on it");

                OnGreetingReceived?.Invoke(bot, eventArgs);
            }
        }

        private Message LanguageClassTrim(DataEventArgs<Message> eventArgs)
        {
            string[] teachers = config.SplitClass.Teachers;
            if (teachers.Contains(eventArgs.Data.Teacher))
            {
                if (config.SplitClass.Teacher == eventArgs.Data.Teacher)
                {
                    return eventArgs.Data;
                }
                else if (teachers.Contains(eventArgs.PreviousData.Teacher))
                {
                    if (config.SplitClass.Teacher == eventArgs.PreviousData.Teacher)
                    {
                        return eventArgs.PreviousData;
                    }
                }
                else return null;
            }
            return eventArgs.Data;
        }

        public void Dispose()
        {
            ((IDisposable)crBot).Dispose();
        }
    }
}