using NPOI.HSSF.Record.CF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.PostClearance
{
    public class PostClearanceModel
    {
        /// <summary>
        /// 匯入日期
        /// </summary>
        public DateTime? ImportDate { get; set; }
        /// <summary>
        /// 分提單號
        /// </summary>
        public string BlNo { get; set; }
        /// <summary>
        /// 派件單號
        /// </summary>
        public string JetfSerial { get; set; }
        /// <summary>
        /// 派件公司
        /// </summary>
        public string TransName { get; set; }
        /// <summary>
        /// 拆櫃日期
        /// </summary>
        public DateTime? UnboxingDate { get; set; }
        /// <summary>
        /// 傳輸日
        /// </summary>
        public DateTime? TransferDate { get; set; }
        /// <summary>
        /// 到港日
        /// </summary>
        public DateTime? Eta { get; set; }
        /// <summary>
        /// 出倉日
        /// </summary>
        public DateTime? SignOutDate { get; set; }
        /// <summary>
        /// MAIL
        /// </summary>
        public string Mail { get; set; }
        /// <summary>
        /// 報關類別
        /// </summary>
        public string ClearanceType { get; set; }
        /// <summary>
        /// 客戶代號
        /// </summary>
        public string CustCode { get; set; }
        /// <summary>
        /// 客戶
        /// </summary>
        public string CustName { get; set; }
        /// <summary>
        /// 倉儲
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// 倉儲(代收檔)
        /// </summary>
        public string CollectibleDataType { get; set; }
        /// <summary>
        /// 材積數
        /// </summary>
        public int Volume { get; set; }

        /// <summary>
        /// 倉租天數
        /// </summary>
        public int WarehouseRentDays { get; set; }

        /// <summary>
        /// 倉租天數減免
        /// </summary>
        public int WarehouseRentDaysReduction { get; set; }

        /// <summary>
        /// 倉租
        /// </summary>
        public int WarehouseRent { get; set; }
        /// <summary>
        /// 倉租數量
        /// </summary>
        public int WarehouseRentCount { get; set; }

        /// <summary>
        /// 移倉費
        /// </summary>
        public int RelocationFee { get; set; }
        /// <summary>
        /// 數量(移倉)
        /// </summary>
        public int RelocationCount { get; set; }

        /// <summary>
        /// EDI傳輸費
        /// </summary>
        public int EdiShippingFee { get; set; }

        /// <summary>
        /// 數量(EDI傳輸)
        /// </summary>
        public int EdiShippingCount { get; set; }

        /// <summary>
        /// 處理費
        /// </summary>
        public int HandlingFee { get; set; }

        /// <summary>
        /// 數量(處理費)
        /// </summary>
        public int HandlingCount { get; set; }



        /// <summary>
        /// X類稅金
        /// </summary>
        public int XTax { get; set; }
        /// <summary>
        /// G類稅金
        /// </summary>
        public int GTax { get; set; }
        /// <summary>
        /// 滯報費減免
        /// </summary>
        public int FeeReduction { get; set; }
        /// <summary>
        /// 稅金單編號
        /// </summary>
        public string TaxNumber { get; set; }
        /// <summary>
        /// 稅金單檔案
        /// </summary>
        public string TaxNumberFile { get; set; }
        /// <summary>
        /// 總計(未含代收手續費)
        /// </summary>
        public double Total { get; set; }
        /// <summary>
        /// 總計(含代收手續費)
        /// </summary>
        public double Total2 { get; set; }
        /// <summary>
        /// 總計(含代收+加值)
        /// </summary>
        public double Total3 { get; set; }
        /// <summary>
        /// 派送手續費
        /// </summary>
        public int DeliveryFee { get; set; }

        /// <summary>
        /// 派送加值1%
        /// </summary>
        public double DeliverySurcharge { get; set; }

        /// <summary>
        /// 派送手續費總額
        /// </summary>
        public double TotalDeliveryAmount { get; set; }
        /// <summary>
        /// 報關費
        /// </summary>
        public int? ClearanceFee { get; set; }
        /// <summary>
        /// 報關費2
        /// </summary>
        public int ClearanceFee2 { get; set; }
        /// <summary>
        /// 機械使用費
        /// </summary>
        public int MachineryUsageFee { get; set; }
        
        /// <summary>
        /// 報單收費方式
        /// </summary>
        public string ClearanceFeeType { get; set; }
        /// <summary>
        /// 稅金付款人
        /// </summary>
        public string TaxPayer { get; set; }
        /// <summary>
        /// 稅金類別
        /// </summary>
        public string TaxType { get; set; }
        /// <summary>
        /// 稅金類別備註
        /// </summary>
        public string TaxTypeRemark { get; set; }
        /// <summary>
        /// 客戶
        /// </summary>
        public string Customer { get; set; }
        /// <summary>
        /// 實際交派日
        /// </summary>
        public DateTime? ActualDate { get; set; }
        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }

        //發票金額
        public int InvoiceAmount { get; set; }
        //發票金額+代收
        public int InvoiceAndCollectibleAmount { get; set; }

        //發票稅額
        public double InvoiceTax { get; set; }

        //收據金額
        public int ReceiptAmount { get; set; }

        /// <summary>
        /// 代收款
        /// </summary>
        public double? CC { get; set; }

        /// <summary>
        /// 系統註記
        /// </summary>
        public string SystemMemo { get; set; }

        /// <summary>
        /// 判斷是否需要改為「匯款」
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsChangeToRemittance()
        {
            var custNames = new[]
            {
                "超峰", "深圳超峰", "牽禮馬", "萬達", "天馬", "新遞",
                "速派", "騰揚","巧巧郎", "台星", "穩達達", "攜誠", "網訊"
            };

            return custNames.Contains(CustName) 
                && ClearanceFeeType == "客戶" 
                && TaxPayer == "客戶";
        }

    }


}
