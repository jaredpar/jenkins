using Dashboard.Jenkins;
using System;
using System.Collections.Generic;
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
            var token = args[1];
            var repoUrl = args[2];
            var branchName = args[3];
            var sha1 = args[4];
        }

        private static void Go(
            string userName,
            string token,
            Uri repoUri,
            string branchName,
            string sha1)
        {

        }

        private static int PosToJenkins()
        {

        }
    }
}
