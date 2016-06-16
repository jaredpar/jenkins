using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
