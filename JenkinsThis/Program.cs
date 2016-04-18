using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.JenkinsThis
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var userName = args[0];
            var tokenFile = args[1];
            var repoUrl = args[2];
            var branchOrCommit = args[3];

            var str = ConfigurationManager.AppSettings[SharedConstants.GithubConnectionStringName];
            var token = str.Split(':')[1];

            var util = new JenkinsThisUtil(userName, token, new Uri(repoUrl), branchOrCommit);
            util.Go();
        }
    }
}
