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
#nullable enable
    public class Meet : IDisposable
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly FullConfig config;
        private readonly MeetBot meetBot;
        private readonly CancellationToken token;

        public string? ActiveMeetLink { get; set; }
        private Message? activeMessage;
        private Message? lastMessage;


        public Meet(FullConfig config)
        {
            meetBot = new MeetBot(config);
            this.config = config;
            Login();
        }
        public Meet(FullConfig config,
                    CancellationToken token) : this(config)
        {
            this.token = token;
        }

        public async Task Start()
        {
            await MeetLoop();
        }

        public void ReceiveStartMessage(object? sender, DataEventArgs<Message> eventArgs)
        {
            if (activeMessage != null
                && !config.SplitClass.ReplacesTeachers.Contains(activeMessage.Teacher))
            {
                logger.Warn("Received new message while last one isn't done");
                logger.Debug("Last message: {0}", activeMessage);
                logger.Debug("New message: {0}", eventArgs.Data);
                logger.Debug("Changing to new message.");
            }
            if (eventArgs.Data != lastMessage)
            {
                activeMessage = eventArgs.Data;
            }

            var bot = sender as ClassroomBot;
            if (bot == null) throw new NullReferenceException("Null classroom bot");

            string link = bot.GetClassroomMeetLink();
            if (link != ActiveMeetLink)
            {
                ActiveMeetLink = link;
                logger.Debug("New meet link");
            }
        }
        private async Task MeetLoop()
        {
            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        logger?.Debug("Succesfully canceled");
                        break;
                    }
                    string? teacher = activeMessage?.Teacher;
                    string? link = GetLink(activeMessage);
                    logger?.Debug("Got teacher {0}", teacher);
                    // No entry until right message
                    while (link == null && activeMessage != null)
                    {
                        // Wait for lastMessage update
                        while (teacher == activeMessage.Teacher)
                            await Task.Delay(new TimeSpan(0, 0, 1), token);

                        link = GetLink(activeMessage);

                        teacher = activeMessage.Teacher;
                    }
                    logger?.Debug("Got link {0}", link);

                    if (link == null) logger?.Debug("Link is null");

                    await TryEnterMeet(link);

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

        private string? GetLink(Message? message)
        {
            if (message != null)
            {
                if (message.Teacher == config.SplitClass.Teacher)
                {
                    string? msgLink = message.MeetLink();

                    if (msgLink != null)
                    {
                        return msgLink;
                    }
                }
                else if (config.SplitClass.ReplacesTeachers.Contains(message.Teacher))
                {
                    return null;
                }
            }

            return ActiveMeetLink;
        }

        private async Task TryEnterMeet(string? link)
        {
            if (MeetExists(link))
            {
                bool langClass = activeMessage != null ? IsLanguageClass(activeMessage) : false;

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
                        lastMessage = activeMessage;
                        activeMessage = null;

                        logger.Info("Entering meet");
                        logger.Debug("with {0} people", peopleInOverview);
                        meetBot.EnterMeet();

                        await WaitForPeopleToLeave(minimumPeople: peopleNeeded);

                        logger.Info("Leaving meet");
                        meetBot.LeaveMeet();
                    }
                    logger.Debug("Back to meet loop");
                }
            }
        }

        private bool IsLanguageClass(Message lastMessage)
        {
            if (lastMessage == null)
                throw new ArgumentNullException(nameof(lastMessage));

            if (config.SplitClass.ReplacesTeachers.Contains(lastMessage.Teacher))
            {
                if (config.SplitClass.Teacher != lastMessage.Teacher)
                {
                    throw new ArgumentException("Received a wrong language class");
                }
                return true;
            }

            return false;
        }

        private async Task WaitForPeopleToLeave(int minimumPeople)
        {
            if (minimumPeople < 0) throw new ArgumentOutOfRangeException(nameof(minimumPeople));
            if (meetBot.State != MeetState.InCall) throw new Exception("Not in call");

            await Task.Delay(new TimeSpan(0, minutes: 5, 0), token);
            logger?.Debug("Starting exit loop...");

            while (true)
            {
                int peopleInCall = -1;
                while (peopleInCall == -1)
                {
                    try
                    {
                        peopleInCall = meetBot.PeopleInMeet();
                    }
                    catch (OpenQA.Selenium.NoSuchElementException)
                    {
                        logger?.Debug("Failed to fetch people in meet");
                    }
                    catch (OpenQA.Selenium.WebDriverTimeoutException)
                    {
                        break;
                    }
                }

                if (peopleInCall < minimumPeople)
                {
                    logger?.Debug("Leaving at {0} people", peopleInCall);
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

        private bool MeetExists(string? link)
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