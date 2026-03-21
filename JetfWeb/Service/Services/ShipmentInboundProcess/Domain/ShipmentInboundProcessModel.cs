using Service.EnumTax;
using Service.Extensions;
using System;

namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// 貨件入庫處理顯示模型
    /// 用於前端查詢列表顯示與後續處理作業
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
        /// 進口方式（例如海運、空運）
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
        /// 派件公司代碼(空運才有)
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 追蹤單號或追蹤號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 貨件來源類型
        /// </summary>
        public ShipmentInboundSourceType SourceType { get; set; }

        /// <summary>
        /// 貨件來源名稱
        /// </summary>
        public string SourceTypeName => SourceType.ToDescription();

        /// <summary>
        /// 退件原因（保留）
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 處理類型
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// 以易讀文字呈現的處理類型名稱
        /// </summary>
        public string ProcessTypeName => ProcessType?.ToDescription();

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
