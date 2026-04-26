using Service.EnumTax;
using Service.Extensions;
using System;
using System.Collections.Generic;

namespace Service.Services.ShipmentInboundRecord.Domain
{
    public class ShipmentInboundRecordModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 資料類型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 入庫日期
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 派件公司代碼
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
        /// 退件原因
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 貨件來源
        /// </summary>
        public ShipmentInboundSourceType SourceType { get; set; }

        /// <summary>
        /// 貨件來源名稱
        /// </summary>
        public string SourceTypeName => SourceType.ToDescription();

        /// <summary>
        /// 流水編號
        /// </summary>
        public string SeqNo { get; set; }

        /// <summary>
        /// 儲位
        /// </summary>
        public string LocationCode { get; set; }

        /// <summary>
        /// 處理方式
        /// </summary>
        public ShipmentInboundProcessType? ProcessType { get; set; }

        /// <summary>
        /// 處理方式名稱
        /// </summary>
        public string ProcessTypeName => ProcessType?.ToDescription();

        /// <summary>
        /// 重出單號
        /// </summary>
        public string ReturnTrackingNo { get; set; }

        /// <summary>
        /// 重出派件公司
        /// </summary>
        public ShipmentInboundProcessTransNo? ProcessTransNo { get; set; }

        /// <summary>
        /// 重出派件公司名稱
        /// </summary>
        public string ProcessTransName => ProcessTransNo?.ToDescription();

        /// <summary>
        /// 收件人
        /// </summary>
        public string ProcessImporter { get; set; }

        /// <summary>
        /// 電話
        /// </summary>
        public string ProcessImporterPhone { get; set; }

        /// <summary>
        /// 地址
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
        /// 運費支付方
        /// </summary>
        public ShipmentInboundFreightPayerNo? FreightPayerNo { get; set; }

        /// <summary>
        /// 運費支付方名稱
        /// </summary>
        public string FreightPayerName => FreightPayerNo?.ToDescription();

        /// <summary>
        /// 稅金
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 手續費
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 報關費
        /// </summary>
        public int Ccfee { get; set; }

        /// <summary>
        /// 到付款
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 重出運費
        /// </summary>
        public int FreightFee { get; set; }

        /// <summary>
        /// 車牌號碼
        /// </summary>
        public string CarNo { get; set; }

        /// <summary>
        /// 預計自取日期
        /// </summary>
        public DateTime? PickupTime { get; set; }

        /// <summary>
        /// 客服處理人
        /// </summary>
        public string ProcessOpe { get; set; }

        /// <summary>
        /// 出庫日期
        /// </summary>
        public DateTime? OutboundDate { get; set; }

        /// <summary>
        /// 出庫操作日
        /// </summary>
        public DateTime? OutboundTime { get; set; }

        /// <summary>
        /// 出庫操作人
        /// </summary>
        public string OutboundOpe { get; set; }

        /// <summary>
        /// 倉庫狀態
        /// </summary>
        public WarehouseProcessType? WarehouseProcessType { get; set; }

        /// <summary>
        /// 倉庫狀態名稱
        /// </summary>
        public string WarehouseProcessName => WarehouseProcessType?.ToDescription();

        /// <summary>
        /// 倉庫狀態操作日
        /// </summary>
        public DateTime? WarehouseProcessTime { get; set; }

        /// <summary>
        /// 倉庫狀態操作人
        /// </summary>
        public string WarehouseProcessOpe { get; set; }

        /// <summary>
        /// 客服處理日期
        /// </summary>
        public DateTime? ProcessTime { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 出庫單號
        /// </summary>
        public string OutboundTrackingNo { get; set; }

        /// <summary>
        /// 總金額
        /// </summary>
        public int TotalAmount
        {
            get
            {
                // 開新單號
                if (ProcessType.HasValue && ProcessType == ShipmentInboundProcessType.NewTrackingNo)
                {
                    return Tax + Ccfee + Cod + FreightFee + Fee;
                }

                return Tax + Ccfee + Cod + Fee;
            }
        }

        /// <summary>
        /// 明細頁顯示的手續費。
        /// </summary>
        public int DisplayFee => Fee;

        /// <summary>
        /// 最新異常原因。
        /// </summary>
        public string ExceptionReason { get; set; }

        /// <summary>
        /// 異常圖片列表。
        /// </summary>
        public List<string> ExceptionFilePaths { get; set; } = new List<string>();
    }
}
