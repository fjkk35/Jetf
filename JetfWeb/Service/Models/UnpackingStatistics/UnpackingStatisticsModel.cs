using System;

namespace Service.Models.UnpackingStatistics
{
    public class UnpackingStatisticsModel
    {
        public DateTime Date { get; set; }
        public string DataType { get; set; }
        public string Customer { get; set; }
        public int TotalCount { get; set; }
    }
}