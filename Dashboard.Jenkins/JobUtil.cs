namespace Dashboard.Jenkins
{
    public static class JobUtil
    {
        public static bool IsPullRequestJobName(JobId jobId)
        {
            return IsPullRequestJobName(jobId.Name);
        }

        public static bool IsPullRequestJobName(string jobName)
        {
            return jobName.Contains("_prtest");
        }

        public static bool IsCommitJobName(string jobName)
        {
            return !IsPullRequestJobName(jobName);
        }

        public static bool IsGCStressJob(JobId jobId)
        {
            return jobId.Name.Contains("_gcstress");
        }

        public static bool IsAuthNeededHeuristic(JobId jobId)
        {
            var name = jobId.Name;
            if (name.Contains("Private") ||
                name.Contains("perf_win10"))
            {
                return true;
            }

            return false;
        }
    }
}
