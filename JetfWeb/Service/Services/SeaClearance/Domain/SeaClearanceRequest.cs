using Dapper;
using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance
{
    public class SeaClearanceRequest
    {
        /// <summary>
        /// 上傳Id
        /// </summary>
        public int? SeaClearanceId { get; set; }

        /// <summary>
        /// 明細Id
        /// </summary>
        public int? SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 倉別
        /// </summary>
        public SeaWarehouseType? Type { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 倉別
        /// </summary>
        public string Modifyby { get; set; }

        /// <summary>
        /// 申報人
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// Gb301-報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 報關方式
        /// </summary>
        public PostEntryType? PostEntry { get; set; }

        /// <summary>
        /// 步驟Id
        /// </summary>
        public int? StepId { get; set; }

        /// <summary>
        /// 異常狀態Id
        /// </summary>
        public int? AbnormalStateId { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
