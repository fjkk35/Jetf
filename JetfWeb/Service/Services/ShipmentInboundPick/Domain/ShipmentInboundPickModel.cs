using Service.EnumTax;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.ShipmentInboundPick.Domain
{
    /// <summary>
    /// 貨件回倉揀貨資料。
    /// </summary>
    public class ShipmentInboundPickModel
    {
        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 序號。
        /// </summary>
        public string SeqNo { get; set; }

        /// <summary>
        /// 儲位。
        /// </summary>
        public string LocationCode { get; set; }

        /// <summary>
        /// 貨到付款金額。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 處理方式。
        /// </summary>
        public ShipmentInboundProcessType ProcessType { get; set; }

        /// <summary>
        /// 處理方式名稱。
        /// </summary>
        public string ProcessTypeName => ProcessType.ToDescription();

        /// <summary>
        /// 重出派件公司。
        /// </summary>
        public ShipmentInboundProcessTransNo ProcessTransNo { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string ProcessImporter { get; set; }

        /// <summary>
        /// 電話。
        /// </summary>
        public string ProcessImporterPhone { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        public string ProcessImporterAddr { get; set; }

        /// <summary>
        /// 門市店號。
        /// </summary>
        public string StoreCode { get; set; }

        /// <summary>
        /// 門市名稱。
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int Ccfee { get; set; }

        /// <summary>
        /// 重出運費。
        /// </summary>
        public int FreightFee { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int ProcessFee { get; set; }

        /// <summary>
        /// 總代收款項。
        /// </summary>
        public int TotalAmount
        {
            get
            {
                // 開新單號才需要計算代收款項。
                if ( ProcessType == ShipmentInboundProcessType.NewTrackingNo)
                {
                    return Cod + FreightFee + Tax + Ccfee + ProcessFee;
                }
                return 0;
            }
        }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 進口方式。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Remark { get; set; }
    }
}
