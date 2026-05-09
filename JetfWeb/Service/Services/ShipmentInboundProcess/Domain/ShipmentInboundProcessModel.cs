using Service.EnumTax;
using Service.Extensions;
using System;

namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// 貨件回倉處理顯示模型
    /// 用於前端查詢列表顯示與單筆處理作業
    /// </summary>
    public class ShipmentInboundProcessModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 入庫日期
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// 進口方式(例如海運、空運)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 派件公司代碼(空運常用)
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 貨件來源
        /// </summary>
        public ShipmentInboundSourceType SourceType { get; set; }

        /// <summary>
        /// 貨件來源名稱
        /// </summary>
        public string SourceTypeName => SourceType.ToDescription();

        /// <summary>
        /// 退件原因(保留)
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 重出單號
        /// </summary>
        public string ReturnTrackingNo { get; set; }

        /// <summary>
        /// 處理方式
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// 處理方式名稱
        /// </summary>
        public string ProcessTypeName => ProcessType?.ToDescription();

        /// <summary>
        /// 開始處理時間。
        /// 若超過系統鎖定逾時時間，後端會回傳空值。
        /// </summary>
        public DateTime? ProcessStartTime { get; set; }

        /// <summary>
        /// 開始處理人員。
        /// 若超過系統鎖定逾時時間，後端會回傳空值。
        /// </summary>
        public string ProcessStartOpe { get; set; }

        /// <summary>
        /// 稅金
        /// </summary>
        public decimal? Tax { get; set; }

        /// <summary>
        /// 報關費
        /// </summary>
        public decimal? Ccfee { get; set; }

        /// <summary>
        /// 到付款
        /// </summary>
        public decimal? Cod { get; set; }
    }
}
