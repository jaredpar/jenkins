using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Jenkins
{
    public static class JobUtil
    {
        public static bool IsPullRequestJobName(string jobName)
        {
            // TODO: This is super hacky.  But for now it's a correct hueristic and is workable.
            return jobName.Contains("_pr");
        }

        public static bool IsCommitJobName(string jobName)
        {
            return !IsPullRequestJobName(jobName);
        }
    }
}
