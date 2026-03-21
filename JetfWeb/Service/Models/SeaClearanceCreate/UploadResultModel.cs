using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearanceCreate
{
    public class UploadResultModel
    {
        public string DataDate { get; set; }
        public string MainNumber { get; set; }
        public string TrackingNo { get; set; }
        public bool IsSucess { get; set; }
        public string Memo { get; set; }
    }
}