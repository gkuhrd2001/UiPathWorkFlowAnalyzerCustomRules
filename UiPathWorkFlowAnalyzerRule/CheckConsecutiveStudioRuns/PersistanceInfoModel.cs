using System;

namespace CheckConsecutiveStudioRuns
{
    public class PersistanceInfoModel
    {
        public int Count { get; set; }
        public string Hash { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string FileName { get; set; }
    }
}
