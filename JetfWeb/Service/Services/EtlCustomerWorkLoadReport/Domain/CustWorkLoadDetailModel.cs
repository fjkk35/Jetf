using System;

namespace Service.Services.EtlCustomerWorkLoadReport.Domain
{
    /// <summary>
    /// 客戶作業量明細資料模型
    /// </summary>
    public class CustWorkLoadDetailModel
    {
        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 派件公司編號
        /// </summary>
        public int? TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string Mainnumber { get; set; }

        /// <summary>
        /// 袋號
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 渠道代碼
        /// </summary>
        public string LineCode { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 出倉日期 (格式: yyyyMMdd)
        /// </summary>
        public string SignOutDate { get; set; }

        /// <summary>
        /// 貨物件數
        /// </summary>
        public int? CargoPiece { get; set; }

        /// <summary>
        /// 貨物重量
        /// </summary>
        public double? CargoWeight { get; set; }

        /// <summary>
        /// 交倉時間
        /// </summary>
        public string ArrivalTime { get; set; }

        /// <summary>
        /// 異常類別 (C3/未見/A03/B6F)
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 格式化袋號 (含袋數)
        /// </summary>
        public string FormatBlNo { get; set; }
    }
}
