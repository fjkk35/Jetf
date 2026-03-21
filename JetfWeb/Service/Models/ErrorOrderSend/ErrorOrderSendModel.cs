using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ErrorOrderSend
{
    public class ErrorOrderSendModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int TotalCount { get; set; }
        public int PhoneCount { get; set; }
        public int LineCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string IsSend { get; set; }
        public string UploadOpe { get; set; }
        public DateTime CrtDateTime { get; set; }

        public string IsSendDisplay
        {
            get
            {
                return IsSend == "1" ? "已發送" : "未發送";
            }
        }
    }
}
