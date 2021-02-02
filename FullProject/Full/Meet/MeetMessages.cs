using GCRBot.Data;

namespace Full
{
    internal record MeetMessage : Message
    {
        public bool IsLanguageClass { get; }
        public string MeetLink { get; }
        public MeetMessage(Message original) : base(original)
        {
            MeetLink = Utils.GetMeetLink(original.Information);
            IsLanguageClass = Utils.IsLanguageClass(original);
        }
    }
}