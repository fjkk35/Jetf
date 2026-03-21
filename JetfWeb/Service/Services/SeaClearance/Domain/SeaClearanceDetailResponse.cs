using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance.Domain
{
    public class SeaClearanceDetailResponse
    {
        /// <summary>
        /// 收單通知日期(原單上傳日期)
        /// </summary>
        public DateTime OriginalCreateDate { get; set; }

        /// <summary>
        /// 倉別
        /// </summary>
        public string Modifyby { get; set; }

        /// <summary>
        /// Gb301-報單傳輸日
        /// </summary>
        public DateTime? ProDateTime { get; set; }

        /// <summary>
        /// 原單是否上傳
        /// </summary>
        public bool IsSeaOrderOriginal { get; set; }

        /// <summary>
        /// 報關方式
        /// </summary>
        public string Post_Entry { get; set; }

        /// <summary>
        /// 報單傳輸截止日
        /// </summary>
        public DateTime? ProDateTimeDeadline { get; set; }

        /// <summary>
        /// 建檔日期
        /// </summary>
        public DateTime CrtDateTime { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string Cust_Name { get; set; }

        /// <summary>
        /// 要求客戶截止日
        /// </summary>
        public DateTime? CustomerDeadline { get; set; }

        /// <summary>
        /// Gb301-報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 強制結案日
        /// </summary>
        public DateTime? CloseDate { get; set; }

        /// <summary>
        /// 滯報費
        /// </summary>
        public int LateDeclarationFee { get; set; }
    }
}
