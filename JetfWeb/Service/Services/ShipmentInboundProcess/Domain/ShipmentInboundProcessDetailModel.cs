using Service.EnumTax;
using System;

namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// 貨件入庫處理詳細資料模型
    /// 包含已處理或欲處理時所需記錄的細節
    /// </summary>
    public class ShipmentInboundProcessDetailModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 處理類型
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// 重出派件公司代碼
        /// </summary>
        public ShipmentInboundProcessTransNo? ProcessTransNo { get; set; }

        /// <summary>
        /// 收件人姓名
        /// </summary>
        public string ProcessImporter { get; set; }

        /// <summary>
        /// 收件人電話
        /// </summary>
        public string ProcessImporterPhone { get; set; }

        /// <summary>
        /// 收件人地址
        /// </summary>
        public string ProcessImporterAddr { get; set; }

        /// <summary>
        /// 門市店號
        /// </summary>
        public string StoreCode { get; set; }

        /// <summary>
        /// 門市名稱
        /// </summary>
        public string StoreName { get; set; }

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

        /// <summary>
        /// 重出運費支付方代碼
        /// </summary>
        public ShipmentInboundFreightPayerNo? FreightPayerNo { get; set; }

        /// <summary>
        /// 重出運費
        /// </summary>
        public decimal? FreightFee { get; set; }

        /// <summary>
        /// 手續費
        /// </summary>
        public decimal? Fee { get; set; }

        /// <summary>
        /// 車牌號碼
        /// </summary>
        public string CarNo { get; set; }

        /// <summary>
        /// 預計自取時間
        /// </summary>
        public DateTime? PickupTime { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }
    }
}
