using System;

namespace Service.Services.SeaClearance.Domain
{
    /// <summary>
    /// SeaClearance 列表查詢的資料項目，用於組成查詢結果清單。
    /// </summary>
    public sealed class SeaClearanceListQueryItem
    {
        /// <summary>
        /// 主鍵：SeaClearanceDetail Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 所屬的 SeaClearance 主表 Id（可為 null）
        /// </summary>
        public int? SeaClearanceId { get; set; }

        /// <summary>
        /// 資料日期（字串形式）
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 追蹤編號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 建立時間（原始映射）
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// 修改者
        /// </summary>
        public string Modifyby { get; set; }

        /// <summary>
        /// Post Entry（放行註記）
        /// </summary>
        public string PostEntry { get; set; }

        /// <summary>
        /// 到港時間（ETA）
        /// </summary>
        public DateTime? Eta { get; set; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public int? Piece { get; set; }

        /// <summary>
        /// 進口商
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 當前流程步驟 Id（可為 null）
        /// </summary>
        public int? CurrentStepId { get; set; }

        /// <summary>
        /// 當前異常狀態 Id（可為 null）
        /// </summary>
        public int? CurrentAbnormalStateId { get; set; }
    }
}
