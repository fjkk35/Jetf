using System.ComponentModel.DataAnnotations;

namespace JETFTAX.Models.EtlClearanceDetails
{
    public class EtlClearanceDetailsViewModel
    {
        [Display(Name = "日　　期")]
        public string sDate { get; set; }

        [Display(Name = "日　　期")]
        public string eDate { get; set; }

        [Display(Name = "資料時間")]
        public string dataTime { get; set; }
    }
}