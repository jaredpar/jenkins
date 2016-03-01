using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Models
{
    public class TextStatSummary
    {
        public int Count { get; }
        public int MaxLength { get; }
        public int MinLength { get; }
        public int AverageLength { get; }

        private TextStatSummary(
            int count,
            int maxLength,
            int minLength,
            int averageLength)
        {
            Count = count;
            MaxLength = maxLength;
            MinLength = minLength;
            AverageLength = averageLength;
        }

        public static TextStatSummary Create(List<int> list)
        {
            return new TextStatSummary(
                count: list.Count,
                maxLength: list.Max(),
                minLength: list.Min(),
                averageLength: (int)list.Average());
        }
    }

    public class TestCacheStatSummary
    {
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int StoreCount { get; set; }
        public TextStatSummary OutputStandardSummary { get; set; }
        public TextStatSummary OutputErrorSummary { get; set; }
        public TextStatSummary ContentSummary { get; set; }
    }
}