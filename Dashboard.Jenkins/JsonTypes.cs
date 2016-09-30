namespace Dashboard.Jenkins.Json
{
    public class Build
    {
        public int Number { get; set; }
        public string Url { get; set; }

        public override string ToString() => $"Build {Number}";
    }
}
