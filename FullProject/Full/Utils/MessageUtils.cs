using GCRBot.Data;

namespace Full
{
    public static class MessageUtils
    {
        public static string MeetLink(this Message msg)
        {
            return msg.Information.MeetLink();
        }
    }
}