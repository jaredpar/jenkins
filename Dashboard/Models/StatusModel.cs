using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class TestStatsModel
    {
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int StoreCount { get; set; }
        public int CurrentCount { get; set; }
    }
}