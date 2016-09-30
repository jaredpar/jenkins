using Dashboard.Jenkins;

namespace Dashboard.Azure.Builds
{
    // TODO: Should use BoundBuildId
    public struct BuildKey
    {
        public BuildId BuildId { get; }
        public string Key { get; }

        public BuildKey(BuildId buildId)
        {
            BuildId = buildId;
            Key = GetKey(buildId);
        }

        public static string GetKey(BuildId buildId) => $"{buildId.Number}-{AzureUtil.NormalizeKey(buildId.JobName, '_')}";
    }
}
