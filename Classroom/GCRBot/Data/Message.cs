using GBot.Extensions;
using OpenQA.Selenium;

namespace GCRBot.Data
{
    [FromXPath("/html/body/div[2]/div/div/div[2]/main/section/div/div[2]/div[{index}]")]
    public record Message
    {
        [FromXPath("/div[1]/div[1]/div[1]/div/div/span")]
        public string Teacher { get; init; }
        [FromXPath("/div[1]/div[1]/div[1]/span/span[2]")]
        public string Timestamp { get; init; }
        [FromXPath("/div[1]/div[2]/div[1]/html-blob")]
        public string Information { get; init; }
        public IWebElement WebElement { get; init; }
    };
}