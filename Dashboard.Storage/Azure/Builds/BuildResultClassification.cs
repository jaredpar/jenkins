namespace Dashboard.Azure.Builds
{
    /// <summary>
    /// 
    /// </summary>
    public enum ClassificationKind 
    {
        /// <summary>
        /// Specifically represents the abscence of a classification from Jenkins. 
        /// </summary>
        Unknown,
        Succeeded,
        BuildFailure,
        TestFailure,
        Infrastructure,
        MergeConflict,
        Aborted,

        /// <summary>
        /// A classification exists but it is not one of the well known ones.
        /// </summary>
        Custom,
    }

    public struct BuildResultClassification
    {
        public static readonly BuildResultClassification Succeeded = new BuildResultClassification(ClassificationKind.Succeeded, "Succeeded", "");
        public static readonly BuildResultClassification Unknown = new BuildResultClassification(ClassificationKind.Unknown, "Unknown", "");
        public static readonly BuildResultClassification TestFailure = new BuildResultClassification(ClassificationKind.TestFailure, "TestFailure", "");
        public static readonly BuildResultClassification MergeConflict = new BuildResultClassification(ClassificationKind.MergeConflict, "MergeConflict", "");
        public static readonly BuildResultClassification Infrastructure = new BuildResultClassification(ClassificationKind.Infrastructure, "Infrastructure", "");
        public static readonly BuildResultClassification BuildFailure = new BuildResultClassification(ClassificationKind.BuildFailure, "Build", "");
        public static readonly BuildResultClassification Aborted = new BuildResultClassification(ClassificationKind.Aborted, "Aborted", "");

        public ClassificationKind Kind { get; }
        public string Name { get; }
        public string DetailedName { get; }

        public BuildResultClassification(ClassificationKind kind, string name, string detailedName)
        {
            Kind = kind;
            Name = name;
            DetailedName = detailedName ?? "";
        }
    }
}
