using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using GCRBot.Data;
using GBot;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using NLog;

namespace GCRBot
{
    public partial class ClassroomBot : Bot
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private ReadOnlyDictionary<string, By> selectors;

        private SelectorFetcher selFetcher { get; }

        public ClassroomBot(Config config) : base(config)
        {
            selFetcher = new SelectorFetcher(driver);
            selectors = ClassroomSelectorFactory.Get(config.Driver.Browser);
        }
        public new bool Login()
        {
            string path = Path.Combine(config.Driver.CookieFolder, Cookies.GetName(config.Driver.Browser));
            logger.Debug("Cookie path: " + path);
            if (!File.Exists(path))
            {
                logger.Trace("Launching login bot");
                Config loginConf = new Config();
                loginConf.Driver.Browser = config.Driver.Browser;
                loginConf.Driver.Headless = false;
                loginConf.Driver.DriverFolder = config.Driver.DriverFolder;
                loginConf.Driver.CookieFolder = config.Driver.CookieFolder;
                LoginBot loginBot = null;
                try
                {
                    loginBot = new LoginBot(loginConf);

                    // Assumes user login
                    bool login = loginBot.Login();
                    if (!login) logger.Debug("LoginBot failed");
                }
                finally
                {
                    loginBot?.Dispose();
                }
            }
            logger.Trace("On to base login");
            // Assumes cookies exist
            bool loggedIn = base.Login();
            if (loggedIn)
            {
                GoHome();
            }
            return loggedIn;
        }
        public string GetClassroomMeetLink()
        {
            try
            {
                IWebElement link = firstLoad.Until(driver =>
                    driver.FindElement(selectors[Elements.ClassroomMeetLink]));
                firstLoad.Until(driver => link.Enabled && link.Displayed);
                return link.Text;
            }
            catch (WebDriverTimeoutException)
            {
                logger.Warn("No default meet link in classroom");
                return string.Empty;
            }
        }
        void UpdateFeed()
        {
            try
            {
                // logger.Trace("Updating feed...");
                // firstLoad.Until(driver =>
                //     driver.Navigate()
                // ).Refresh();

                IWebElement el = driver.FindElement(selectors[Elements.ShowMoreButton]);
                if (el.Displayed)
                {
                    logger.Trace("Updated feed");
                    el.Click();
                }
            }
            catch (Exception ex)
            {
                // Do nothing.
                logger.Debug(ex, "Couldn't update feed...");
            }
        }
    }
    class LoginBot : Bot
    {
        public LoginBot(Config config) : base(config)
        {

        }
        public new bool Login() => base.Login();
    }
}