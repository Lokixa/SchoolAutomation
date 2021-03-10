using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GBot;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MeetGBot
{
    public enum MeetState
    {
        NotLoggedIn,
        InCall,
        InOverview,
        OutsideMeet
    }
    public sealed partial class MeetBot : Bot
    {
        private readonly ReadOnlyDictionary<string, By> selectors;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public MeetState State { get; private set; }

        private static Config FixConfig(Config config)
        {
            // Needs to be visible
            if (config.Driver.Headless) config.Driver.Headless = false;
            return config;
        }
        public MeetBot(Config config) : base(FixConfig(config))
        {
            selectors = new MeetSelectorFactory().Get(config.Driver.Browser);
            State = MeetState.NotLoggedIn;
        }

        public bool Login()
        {
            bool loggedIn = base.Login(goToConfigLink: false);
            if (loggedIn)
            {
                ChangeState(MeetState.OutsideMeet);
            }
            return loggedIn;
        }

        private void Hangup()
        {
            IWebElement hangup = defaultWait.Until(driver =>
                driver.FindElement(
                    selectors[Elements.MeetHangupButton]
                )
            );
            logger.Debug("Hanging up");
            hangup.Click();
            ChangeState(MeetState.OutsideMeet);
        }

        public void EnterMeet()
        {
            if (State != MeetState.InOverview)
            {
                throw new InvalidOperationException("Not in meet overview");
            }

            IWebElement joinButton = firstLoad.Until(driver =>
                driver.FindElement(selectors[Elements.JoinButton])
            );
            firstLoad.Until(driver => joinButton.Displayed);
            logger.Debug("Joining meet");
            joinButton.Click();
            ChangeState(MeetState.InCall);
        }

        public void EnterMeetOverview(string link)
        {
            driver.Navigate().GoToUrl(link);
            if (link.Contains("/lookup/"))
            {
                logger.Debug("In lookup");
                Regex meetLinkReg = new Regex(@"https:\/\/meet.google.com\/([A-Za-z]{3}-?)([a-zA-Z]{4}-?)([A-Za-z]{3})");
                firstLoad.Until(driver => meetLinkReg.Match(driver.Url).Success);
            }
            else firstLoad.Until(driver => driver.Url == link);

            MuteElement(Elements.MicrophoneButton);
            MuteElement(Elements.CameraButton);

            ChangeState(MeetState.InOverview);
        }
        public int PeopleInMeet()
        {
            if (State != MeetState.InCall)
            {
                throw new InvalidOperationException("Not in meet call");
            }
            Regex reg = new Regex("[0-9]*");

            IWebElement el = firstLoad.Until(driver =>
                driver.FindElement(selectors[Elements.MeetChatButton])
            );
            firstLoad.Until(driver => el.Enabled && el.Displayed);

            Match match = reg.Match(el.Text);
            if (!match.Success || string.IsNullOrEmpty(match.Value))
            {
                throw new OpenQA.Selenium.NoSuchElementException("Couldn't parse chat button: " + match.Value);
            }

            return int.Parse(match.Value);
        }
        public int PeopleInMeetOverview()
        {
            if (State != MeetState.InOverview)
            {
                throw new InvalidOperationException("Not in meet overview");
            }
            string peopleInCall = defaultWait.Until(driver =>
                driver.FindElement(selectors[Elements.PeopleInCallOverview])
            ).Text;

            if (peopleInCall.Contains("No one")) return 0;
            else if (peopleInCall.Contains(" is ")) return 1;

            List<string> split = peopleInCall.Split(", ").ToList();
            string[] andSplit = split[split.Count - 1].Split("and");

            split.Remove(split[split.Count - 1]);

            split.Add(andSplit[0]);
            Regex reg = new Regex("[0-9]*");
            var match = reg.Match(andSplit[1].Trim());
            if (!match.Success || string.IsNullOrEmpty(match.Value))
            {
                split.Add(andSplit[1]);
                return split.Count;
            }
            else
            {
                // logger.Trace("Got {0} more people", match.Value);
                int val = int.Parse(match.Value);
                return split.Count + val;
            }
        }
        public bool CanJoin()
        {
            if (State != MeetState.InOverview)
            {
                throw new InvalidOperationException("Not in meet overview");
            }
            try
            {
                IWebElement joinButton = driver.FindElement(selectors[Elements.JoinButton]);
                firstLoad.Until(driver => joinButton.Displayed);
                string text = joinButton.Text.Trim();
                // logger.Debug("Join button text: '{0}'", text);
                return !(text.Contains("Ask") || text.Contains("Молба"));
            }
            catch (Exception ex)
            {
                if (ex is StaleElementReferenceException)
                    logger.Debug("Caught stale reference exception for joinButton");
                if (ex is NoSuchElementException)
                    logger.Debug("Caught such element exception for joinButton");
                return false;
            }
        }

        public void LeaveMeet()
        {
            if (State != MeetState.InCall)
            {
                throw new InvalidOperationException("Not in meet");
            }
            Hangup();
        }

        private bool TryFindElement(By selector)
        {
            try
            {
                IWebElement el = driver.FindElement(selector);
            }
            catch (NoSuchElementException)
            {
                return false;
            }
            return true;
        }

        private void MuteElement(string element)
        {
            IWebElement webElement = defaultWait.Until(driver =>
            {
                logger.Trace("Polling for {0}", element);
                return driver.FindElement(selectors[element]);
            });
            userWait.Until(driver =>
            {
                logger.Trace("Polling for {0} enabled and displayed", element);
                return webElement.Enabled && webElement.Displayed;
            });
            string muted = webElement.GetAttribute("data-is-muted");
            if (bool.TryParse(muted, out bool result) && !result)
            {
                webElement.Click();
                logger.Debug("{0} is muted", element);
            }
        }
        private void ChangeState(MeetState state)
        {
            if (state == State) return;
            logger.Debug("Changing state {0} => {1}", State, state);
            State = state;
        }
        public override void Dispose()
        {
            if (State == MeetState.InCall) Hangup();
            base.Dispose();
        }
    }
}
