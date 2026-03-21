using Newtonsoft.Json;
using Service.Models.SeaUnreceivedOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaWorkErrorOrderReport
{
    public class SeaWorkErrorOrderReportModel
    {
        public string DATADATE { get; set; }
        public string DESPATCH_NAME { get; set; }
        public string MAINNUMBER { get; set; }
        public string BAGNUMBER { get; set; }
        public string MANIFEST { get; set; }
        public string ETA { get; set; }
        /// <summary>
        /// 上傳錯誤原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 上傳錯誤原因時間
        /// </summary>
        public string ReasonUploadTime { get; set; }


        public string Gb353RejReason { get; set; }

        /// <summary>
        /// Gb326進口日期
        /// </summary>
        public string Gb326ImportDate { get; set; }

        /// <summary>
        /// Gb353錯誤代碼
        /// </summary>
        public string Gb353RejReasonCode { get; set; }

        public List<Gb353RejReasonModel> Gb353RejReasonList
        {
            get
            {
                return !string.IsNullOrEmpty(Gb353RejReason)
                     ? JsonConvert.DeserializeObject<List<Gb353RejReasonModel>>(Gb353RejReason) 
                     : GetUploadGb353RejReason();
            }
        }

        //錯誤原因代碼(最新)，明細
        public string ReasonCodeByDetail =>
            !string.IsNullOrEmpty(Gb353RejReasonCode)
            ? Gb353RejReasonCode
            : Reason;

        /// <summary>
        /// 最新的錯誤代碼，最新的Gb353代碼，時間相同取第一筆
        /// </summary>
        public string ReasonCodeByReport =>
          Gb353RejReasonList ?
         .GroupBy(x => x.IssueDateTime)
         .OrderByDescending(x => x.Key)
         .FirstOrDefault()
         ?.Select(x => x.RejReasonCode)
         ?.OrderBy(x =>
         {
             // 如果能解析成 ReasonCodeEnum，回傳enum值；否則回傳最大值(排在最後)
             return Enum.TryParse<ReasonCodeEnum>(x, out ReasonCodeEnum code)
                 ? (int)code
                 : int.MaxValue;
         })
         ?.FirstOrDefault();

        /// <summary>
        /// Gb353次數，時間相同只算一次
        /// </summary>
        public int Gb353Count =>
            Gb353RejReasonList?
                .GroupBy(x => x.IssueDateTime)
                .Count() ?? 0;

        /// <summary>
        /// 是否收單
        /// </summary>
        public bool IsReceiveOrder { get; set; }

        public string VESSEL { get; set; }
        public string GW { get; set; }
        public string PIECE { get; set; }
        public string ITEM_NO { get; set; }
        public string ITEM_NAME { get; set; }
        public string NW { get; set; }
        public string UNIT_PRICE { get; set; }
        public string INVOICE_AMOUNT { get; set; }
        public string IMPORTER_ID { get; set; }
        public string IMPORTER { get; set; }
        public string IM_PHONENO { get; set; }
        public string IM_ADD { get; set; }
        public string TRANS_NAME { get; set; }
        public string JETF_SERIAL { get; set; }
        public string LPNO { get; set; }
        public string MODIFYBY { get; set; }

        /// <summary>
        /// 取得上傳的GB353錯單
        /// </summary>
        /// <returns></returns>
        List<Gb353RejReasonModel> GetUploadGb353RejReason() 
        {
            if(string.IsNullOrEmpty(this.Reason))
                return new List<Gb353RejReasonModel>();

            return new List<Gb353RejReasonModel>()
            {
                new Gb353RejReasonModel()
                { 
                    RejReasonCode = Reason,
                    IssueDateTime = ReasonUploadTime,
                }
            };
        }
    }
}
