using System;

namespace Service.Services.ShipmentInboundBatchImport.Domain
{
    /// <summary>
    /// 表示一筆入庫貨件的資料模型，來自批次匯入的紀錄。
    /// </summary>
    public class ShipmentInboundModel
    {
        /// <summary>
        /// 主鍵識別 ID。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 資料型態（例如："海運"、"空運"）。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 入庫日期。
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 原單物流貨號
        /// </summary>
        public string OriginalJetfSerial { get; set; }

        /// <summary>
        /// 原單分提單號
        /// </summary>
        public string OriginalTrackingNo { get; set; }

        /// <summary>
        /// 流水號。
        /// </summary>
        public string SeqNo { get; set; }

        /// <summary>
        /// 儲位
        /// </summary>
        public string LocationCode { get; set; }

        /// <summary>
        /// 貨件來源。
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// 貨件來源顯示名稱（用於前端顯示）
        /// </summary>
        public string SourceTypeDisplay { get; set; }

        /// <summary>
        /// 退回的追蹤編號（若為退貨或重出時使用）。
        /// </summary>
        public string ReturnTrackingNo { get; set; }

        /// <summary>
        /// 尺寸或規格資訊（例如：S/M/L 或 長x寬x高）。
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 客戶代碼（CustCode），用於查詢客戶資料或對應客戶名稱。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 承運商代號（TransNo）。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 承運商名稱（可透過 TransNo 反查填入）。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 進口人姓名或收件人名稱。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 進口人或收件人電話。
        /// </summary>
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 進口人或收件人地址。
        /// </summary>
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 是否有原單資料
        /// </summary>
        public bool IsOrderOriginal { get; set; }

        /// <summary>
        /// 上傳操作人員帳號或識別。
        /// </summary>
        public string UploadOpe { get; set; }

        /// <summary>
        /// 建立時間（紀錄匯入或建立的時間）。
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 上傳狀態（成功/失敗）
        /// </summary>
        public string UploadStatus { get; set; }

        /// <summary>
        /// 上傳失敗原因
        /// </summary>
        public string FailReason { get; set; }

        /// <summary>
        /// 稅金
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 報關費
        /// </summary>
        public int Ccfee { get; set; }

        /// <summary>
        /// 到付款
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 手續費
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 退件原因
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Remark { get; set; }
    }
}
