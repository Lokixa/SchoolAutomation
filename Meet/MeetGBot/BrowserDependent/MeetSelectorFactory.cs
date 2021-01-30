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
        public ReadOnlyDictionary<string, By> ForFirefox()
        {
            Dictionary<string, By> selectors = new();

            selectors.Add(Elements.MeetChatButton,
                By.XPath("/html/body/div[1]/c-wiz/div[1]/div/div[7]/div[3]/div[6]/div[3]/div/div[2]/div[1]"));

            selectors.Add(Elements.MeetHangupButton,
                By.XPath("/html/body/div[1]/c-wiz/div[1]/div/div[7]/div[3]/div[9]/div[2]/div[2]/div"));

            const string common = "/html/body/div[1]/c-wiz/div/div/div[7]/div[3]/div/div/div[2]/div/div[1]";

            selectors.Add(Elements.CameraButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[2]/div/div"));

            selectors.Add(Elements.JoinButton,
                By.XPath($"{common}/div[2]/div/div[2]/div/div[1]/div[1]/span/span"));

            selectors.Add(Elements.MicrophoneButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[1]/div/div/div"));

            selectors.Add(Elements.PeopleInCallOverview,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[2]/div[2]"));

            selectors.Add(Elements.ReadyToJoinMessage,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[1]/div"));

            return new ReadOnlyDictionary<string, By>(selectors);
        }
        public ReadOnlyDictionary<string, By> ForChrome()
        {
            Dictionary<string, By> selectors = new();
            const string common = "/html/body/div[1]/c-wiz/div/div/div[8]/div[3]/div/div/div[2]/div/div[1]";

            selectors.Add(Elements.CameraButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[2]/div/div"));

            selectors.Add(Elements.MicrophoneButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[3]/div[1]/div/div/div"));

            selectors.Add(Elements.MeetChatButton,
                By.XPath("/html/body/div[1]/c-wiz/div[1]/div/div[8]/div[3]/div[1]/div[3]/div/div[2]/div[1]"));

            selectors.Add(Elements.MeetHangupButton,
                By.XPath("/html/body/div[1]/c-wiz/div[1]/div/div[8]/div[3]/div[9]/div[2]/div[2]/div"));

            selectors.Add(Elements.JoinButton,
                By.XPath($"{common}/div[2]/div/div[2]/div/div[1]/div[1]/span/span"));

            selectors.Add(Elements.PeopleInCallOverview,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[2]/div[2]"));

            selectors.Add(Elements.ReadyToJoinMessage,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[1]/div"));

            return new ReadOnlyDictionary<string, By>(selectors);
        }
        public ReadOnlyDictionary<string, By> Get(string browser)
        {
            switch (browser)
            {
                case "firefox":
                    return ForFirefox();
                case "chrome":
                    return ForChrome();
                default:
                    throw new NotSupportedException(nameof(browser));
            }
        }
    }
}