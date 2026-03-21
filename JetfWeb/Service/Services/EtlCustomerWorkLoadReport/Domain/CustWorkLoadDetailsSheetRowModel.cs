using System;

namespace Service.Services.EtlCustomerWorkLoadReport.Domain
{
    /// <summary>
    /// 袋號明細頁簽列資料模型
    /// </summary>
    public class CustWorkLoadDetailsSheetRowModel
    {
        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string Mainnumber { get; set; }

        /// <summary>
        /// 袋號
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 通關方式
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 交倉時間
        /// </summary>
        public string ArrivalTime { get; set; }

        /// <summary>
        /// 異常類別
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 渠道代碼
        /// </summary>
        public string LineCode { get; set; }
    }
}
