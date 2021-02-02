using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GBot;
using GCRBot;
using GCRBot.Data;
using MeetGBot;

namespace Full
{
    public class Meet : IDisposable
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LangConfig config;
        private readonly MeetBot meetBot;
        private readonly CancellationToken token;
        private readonly Stack<Message> messageStack;

        private readonly string DefaultMeetLink;

        public Meet(LangConfig config, string defaultMeetLink)
        {
            meetBot = new MeetBot(config);
            this.config = config;
            DefaultMeetLink = defaultMeetLink;
            messageStack = new();
            Login();
        }
        public Meet(LangConfig config,
                    string defaultMeetLink,
                    CancellationToken token) : this(config, defaultMeetLink)
        {
            this.token = token;
        }

        public async Task Start()
        {
            await MeetLoop();
        }

        public void ReceiveStartMessage(object sender, DataEventArgs<Message> eventArgs)
        {
            if (!messageStack.Contains(eventArgs.Data))
                messageStack.Push(new MeetMessage(eventArgs.Data));
            else
                logger.Fatal("Repeating message");
        }

        private async Task MeetLoop()
        {
            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger.Debug("Succesfully canceled");
                        break;
                    }
                    string link = DefaultMeetLink;

                    if (StartMessageReceived(out MeetMessage msg)
                        && !string.IsNullOrWhiteSpace(msg.MeetLink))
                    {
                        link = msg.MeetLink;
                        logger.Debug("Got link from message '{0}'", link);
                    }

                    if (MeetExists(link))
                    {
                        bool langClass = msg?.IsLanguageClass ?? false;

                        int peopleNeeded = GetPeopleNeeded(langClass);

                        if (meetBot.CanJoin())
                        {
                            int peopleInOverview = meetBot.PeopleInMeetOverview();

                            int seconds = 3;
                            // Wait two minutes
                            for (int i = 0; i < (60 * 2 / seconds) && peopleInOverview < peopleNeeded; i++)
                            {
                                await Task.Delay(new TimeSpan(0, 0, seconds), token);
                                peopleInOverview = meetBot.PeopleInMeetOverview();
                            }
                            // Only if people needed is met
                            // we enter meet
                            if (peopleInOverview >= peopleNeeded)
                            {
                                logger.Info("Entering meet");
                                logger.Debug(" with {0} people", peopleInOverview);
                                meetBot.EnterMeet();

                                await WaitPeopleToLeave(minimumPeople: peopleNeeded);

                                logger.Info("Leaving meet");
                                meetBot.LeaveMeet();
                            }
                            logger.Debug("Back to meet loop");
                        }
                    }

                    await Task.Delay(new TimeSpan(0, 0, seconds: 30), token);
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

        private bool StartMessageReceived(out MeetMessage result)
        {
            if (messageStack.Count > 0)
            {
                // Can crash bot if normal message has been pushed
                result = (MeetMessage)messageStack.Pop();
                return true;
            }
            result = null;
            return false;
        }

        private async Task WaitPeopleToLeave(int minimumPeople)
        {
            if (minimumPeople < 0) throw new ArgumentOutOfRangeException(nameof(minimumPeople));
            if (meetBot.State != MeetState.InCall) throw new Exception("Not in call");

            await Task.Delay(new TimeSpan(0, minutes: 15, 0), token);
            logger.Debug("Starting exit loop...");

            while (true)
            {
                int peopleInCall = meetBot.PeopleInMeet();

                if (peopleInCall < minimumPeople)
                {
                    logger.Debug("Leaving at {0} people", peopleInCall);
                    break;
                }

                await Task.Delay(new TimeSpan(0, 0, seconds: 5), token);
            }

        }

        private int GetPeopleNeeded(bool languageClass)
        {
            int peopleNeeded;
            //TODO REPLACE CONSTANTS
            if (languageClass)
            {
                peopleNeeded = 4;
            }
            else
            {
                peopleNeeded = 12;
            }

            return peopleNeeded;
        }

        private bool MeetExists(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                // throw new ArgumentNullException(nameof(link));
                return false;
            }
            if (meetBot.State != MeetState.OutsideMeet
               && meetBot.State != MeetState.InOverview)
            {
                logger.Error("Invalid state: {0}", meetBot.State);
                throw new InvalidOperationException("Invalid state");
            }

            try
            {
                meetBot.EnterMeetOverview(link);
                return true;
            }
            catch (OpenQA.Selenium.WebDriverTimeoutException)
            {
                // Most likely
                if (link.Contains("/lookup/"))
                {
                    logger.Debug("No meet in lookup link");
                    return false;
                }

                throw;
            }
        }

        private void Login()
        {
            if (meetBot.State != MeetState.NotLoggedIn) logger.Fatal("Wanting to login while logged");
            bool loggedIn = Utils.Retry(meetBot.Login, 3);
            if (!loggedIn)
            {
                throw new Exception("Not logged in");
            }
            logger.Debug("Logged in");
        }

        public void Dispose()
        {
            ((IDisposable)meetBot).Dispose();
        }

    }
}