using System.ComponentModel.DataAnnotations;

namespace JETFTAX.Models.UnpackingStatistics
{
    public class UnpackingStatisticsViewModel
    {
        [Display(Name = "起始日期")]
        public string StartDate { get; set; }

        [Display(Name = "結束日期")]
        public string EndDate { get; set; }
    }
}