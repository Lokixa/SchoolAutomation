using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly FullConfig config;
        private readonly MeetBot meetBot;
        private readonly CancellationToken token;
        private readonly string DefaultMeetLink;

        private Message lastMessage;


        public Meet(FullConfig config, string defaultMeetLink)
        {
            meetBot = new MeetBot(config);
            this.config = config;
            DefaultMeetLink = defaultMeetLink;
            Login();
        }
        public Meet(FullConfig config,
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
            if (lastMessage != null)
                lastMessage = eventArgs.Data;
            else
                logger.Fatal("Received new message while last one isn't done: {0}", eventArgs.Data);
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

                    if (lastMessage != null
                        && !string.IsNullOrWhiteSpace(lastMessage.MeetLink()))
                    {
                        link = lastMessage.MeetLink();
                        logger.Debug("Got link: '{0}'", link);
                    }

                    if (MeetExists(link))
                    {
                        bool langClass = lastMessage != null ? IsLanguageClass(lastMessage) : false;

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

                            if (peopleInOverview >= peopleNeeded)
                            {
                                // Pop
                                lastMessage = null;

                                logger.Info("Entering meet");
                                logger.Debug("with {0} people", peopleInOverview);
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

        private bool IsLanguageClass(Message lastMessage)
        {
            if (lastMessage == null)
                throw new ArgumentNullException(nameof(lastMessage));

            if (config.SplitClass.Teachers.Contains(lastMessage.Teacher))
            {
                if (config.SplitClass.Teacher != lastMessage.Teacher)
                {
                    throw new ArgumentException("Received a wrong language class");
                }
                return true;
            }

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
            if (languageClass)
            {
                peopleNeeded = config.SplitClass.MinimumPeopleToEnter;
            }
            else
            {
                peopleNeeded = config.MinimumPeopleToEnter;
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
                    logger.Debug("No meet in lookup link (timeout)");
                    return false;
                }

                throw;
            }
            catch (OpenQA.Selenium.StaleElementReferenceException)
            {
                if (link.Contains("/lookup/"))
                {
                    logger.Debug("No meet in lookup link (stale element)");
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