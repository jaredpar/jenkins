namespace Dashboard.Helpers.Json
{
    public sealed class TestRunData
    {
        public string Cache { get; set; }
        public int ElapsedSeconds { get; set; }
        public bool Succeeded { get; set; }
        public bool IsJenkins { get; set; }
        public string JenkinsUrl { get; set; }
        public bool Is32Bit { get; set; }
        public int AssemblyCount { get; set; }
        public int CacheCount { get; set; }
        public int ChunkCount { get; set; }
        public bool HasErrors { get; set; }
    }
}