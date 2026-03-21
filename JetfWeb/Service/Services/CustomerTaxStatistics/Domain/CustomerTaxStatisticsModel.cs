using Service.Models.CustomerTaxCalculate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CustomerTaxStatistics.Domain
{
    /// <summary>
    /// 客戶資料模型
    /// </summary>
    public class CustomerTaxStatisticsCustomerModel
    {
        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CUST_NAME { get; set; }
    }

    /// <summary>
    /// 稅金差異項目模型
    /// </summary>
    public class TaxStatisticsDifferenceItemModel
    {
        /// <summary>
        /// 客戶原始資料
        /// </summary>
        public CustomerTaxFeeMasterDataModel CustomerData { get; set; }

        /// <summary>
        /// 稅金總表資料
        /// </summary>
        public CustomerTaxFeeMasterDataModel FeeMasterData { get; set; }

        /// <summary>
        /// 客戶原始資料稅額總計
        /// </summary>
        public int CustomerTotalTax { get; set; }

        /// <summary>
        /// 稅金總表稅額總計
        /// </summary>
        public int FeeMasterTotalTax { get; set; }

        /// <summary>
        /// 差異金額 (稅金總表 - 客戶原始資料)
        /// </summary>
        public int DifferenceAmount { get; set; }
    }

    /// <summary>
    /// 匯出結果模型
    /// </summary>
    public class CustomerTaxStatisticsExportResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 檔案名稱
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 檔案資料
        /// </summary>
        public byte[] FileData { get; set; }

        /// <summary>
        /// 記錄數量
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; }
    }
}