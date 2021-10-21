using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GBot;
using OpenQA.Selenium;

namespace MeetGBot
{
    internal static class MeetSelectorFactory
    {
        private static Dictionary<string, By> ForChrome()
        {
            Dictionary<string, By> selectors = new();


            string meetCommon = "/html/body/div[1]/c-wiz/div[1]/div/div[9]/div[3]";

            selectors.Add(Elements.PeopleInMeetCount,
                By.XPath($"{meetCommon}/div[10]/div[3]/div[2]/div/div/div[2]/div"));

            selectors.Add(Elements.MeetHangupButton,
                By.XPath($"{meetCommon}/div[10]/div[2]/div/div[6]"));


            const string common = "/html/body/div[1]/c-wiz/div/div/div[9]/div[3]/div/div/div[3]/div/div";

            selectors.Add(Elements.MicrophoneButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[4]/div[1]/div/div/div"));

            selectors.Add(Elements.CameraButton,
                By.XPath($"{common}/div[1]/div[1]/div/div[4]/div[2]/div/div"));

            selectors.Add(Elements.JoinButton,
                By.XPath($"{common}/div[2]/div/div[2]/div/div[1]/div[1]"));

            selectors.Add(Elements.PeopleInCallOverview,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[2]/div[2]"));

            selectors.Add(Elements.ReadyToJoinMessage,
                By.XPath($"{common}/div[2]/div/div[1]/div[1]/div[1]/div"));

            return selectors;
        }
        public static ReadOnlyDictionary<string, By> Get(string browser)
        {
            switch (browser)
            {
                case "chrome":
                    return new ReadOnlyDictionary<string, By>(ForChrome());
                default:
                    throw new NotSupportedException(nameof(browser));
            }
        }
    }
}