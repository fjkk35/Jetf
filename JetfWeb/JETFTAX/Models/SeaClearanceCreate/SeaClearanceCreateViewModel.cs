using Service.Models.ErrorOrderSend;
using Service.Models.SeaClearanceCreate;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.SeaClearanceCreate
{
    public class SeaClearanceCreateViewModel
    {
        [Display(Name = "日　　期")]
        public string DataDate { get; set; }

        public List<SeaClearanceModel> SeaClearanceList { get; set; }
    }
}