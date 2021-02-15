namespace Full
{
    public class FullConfig : GBot.Config
    {
        public SplitClass SplitClass { get; } = new SplitClass();
        public int MinimumPeopleToEnter { get; init; } = 11;
    }
    public class SplitClass
    {
        public string Teacher { get; init; } = string.Empty;
        public string[] Teachers { get; init; } = new string[] { };
        public int MinimumPeopleToEnter { get; init; } = 5;
    }
}