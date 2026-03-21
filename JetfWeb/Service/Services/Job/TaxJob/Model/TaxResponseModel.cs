using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.TaxJob.Model
{
    public class TaxResponseModel
    {
        public int FeeMasterId { get; set; }
        public string Code { get; set; }
        public bool Data { get; set; }
        public string Message { get; set; }
        public bool Status { get; set; }
        public string Time { get; set; }
    }
}
