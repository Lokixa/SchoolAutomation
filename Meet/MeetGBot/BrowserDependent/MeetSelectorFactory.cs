using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GBot;
using OpenQA.Selenium;

namespace MeetGBot
{
    internal class MeetSelectorFactory
    {
        public Dictionary<string, By> ForFirefox()
        {
            Dictionary<string, By> selectors = new();

            string meetCommon = "/html/body/div[1]/c-wiz/div[1]/div/div[9]/div[3]";

            selectors.Add(Elements.MeetChatButton,
                By.XPath($"{meetCommon}/div[1]/div[3]/div/div[2]/div[1]"));

            selectors.Add(Elements.MeetHangupButton,
                By.XPath($"{meetCommon}/div[9]/div[2]/div[2]/div"));

            const string common = "/html/body/div[1]/c-wiz/div/div/div[9]/div[3]/div/div/div[2]/div/div[1]";

            selectors.Add(Elements.CameraButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[2]/div/div"));

            selectors.Add(Elements.JoinButton,
                By.XPath($"{common}/div[2]/div/div[2]/div/div[1]/div[1]"));

            selectors.Add(Elements.MicrophoneButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[1]/div/div/div"));

            selectors.Add(Elements.PeopleInCallOverview,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[2]/div[2]"));

            selectors.Add(Elements.ReadyToJoinMessage,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[1]/div"));

            return selectors;
        }
        public Dictionary<string, By> ForChrome()
        {
            Dictionary<string, By> selectors = ForFirefox();
            // Elements.MeetChatButton,
            // Elements.MeetHangupButton,

            const string common = "/html/body/div[1]/c-wiz/div[1]/div/div[9]/div[3]/div/div/div[2]/div/div[1]";

            selectors[Elements.CameraButton] =
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[2]/div/div");

            selectors[Elements.MicrophoneButton] =
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[1]/div/div/div");

            selectors[Elements.JoinButton] =
                By.XPath($"{common}/div[2]/div/div[2]/div/div[1]/div[1]");

            selectors[Elements.PeopleInCallOverview] =
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[2]/div[2]");

            selectors[Elements.ReadyToJoinMessage] =
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[1]/div");

            return selectors;
        }
        public ReadOnlyDictionary<string, By> Get(string browser)
        {
            switch (browser)
            {
                case "firefox":
                    return new ReadOnlyDictionary<string, By>(ForFirefox());
                case "chrome":
                    return new ReadOnlyDictionary<string, By>(ForChrome());
                default:
                    throw new NotSupportedException(nameof(browser));
            }
        }
    }
}