using System;

namespace CheckConsecutiveStudioRuns5
{
    public class PersistanceInfoModel
    {
        public int Count { get; set; }
        public string Hash { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string FileName { get; set; }
    }
}
