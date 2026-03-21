using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CustomerTaxCalculate
{
    /// <summary>
    /// 稅金時間模型
    /// </summary>
    public class TaxTimeModel
    {
        /// <summary>
        /// 時間ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 稅金時間 (例如 11:00, 13:00)
        /// </summary>
        public string TaxTime { get; set; }
    }

    /// <summary>
    /// 客戶模型
    /// </summary>
    public class CustomerTaxCalculateCustomerModel
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
    /// 稅金計算資料模型
    /// </summary>
    public class CustomerTaxCalculateDataModel
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string MAINNUMBER { get; set; }

        /// <summary>
        /// 清關袋號
        /// </summary>
        public string BL_NO { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string DESPATCH_NAME { get; set; }

        /// <summary>
        /// 運輸稅金付款方式
        /// </summary>
        public string TRANS_TAXPAYMENT { get; set; }

        /// <summary>
        /// 納稅義務人
        /// </summary>
        public string IMPORTER { get; set; }

        /// <summary>
        /// 電話
        /// </summary>
        public string IM_PHONENO { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string IM_ADD { get; set; }

        /// <summary>
        /// 申報人身份證
        /// </summary>
        public string IMPORTER_ID { get; set; }

        /// <summary>
        /// 分提單號/運單號
        /// </summary>
        public string JETF_SERIAL { get; set; }

        /// <summary>
        /// 到付款
        /// </summary>
        public string CC { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string MEMO { get; set; }

        /// <summary>
        /// 到貨時間
        /// </summary>
        public string ARRIVAL { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string TRANS_NAME { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        public string TAX_NUMBER { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string CLEARANCE_NUMBER { get; set; }

        /// <summary>
        /// 稅基
        /// </summary>
        public int? TAX_BASE { get; set; }

        /// <summary>
        /// 稅金
        /// </summary>
        public int? TAX_AMOUNT { get; set; }

        /// <summary>
        /// 資料來源
        /// </summary>
        public string DATA_TYPE { get; set; }

        /// <summary>
        /// 報關類別
        /// </summary>
        public string CLEARANCE_TYPE { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public DateTime? SIGN_IN_TIME { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? SIGN_OUT_TIME { get; set; }
    }

    /// <summary>
    /// 稅金總表資料模型
    /// </summary>
    public class CustomerTaxFeeMasterDataModel
    {
        /// <summary>
        /// 作業日
        /// </summary>
        public string DATADATE { get; set; }
        /// <summary>
        /// 主號
        /// </summary>
        public string MAIN_NUMBER { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TRACKINGNO { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string CUSTOMER { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string DESPATCH_NAME { get; set; }

        /// <summary>
        /// 運單號
        /// </summary>
        public string DLV_INV { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string DLV_COM { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        public string TAX_NUMBER { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string CLEARANCE_NUMBER { get; set; }

        /// <summary>
        /// 稅基
        /// </summary>
        public int? TAX_BASE { get; set; }

        /// <summary>
        /// 稅金1
        /// </summary>
        public int? TAX1 { get; set; }

        /// <summary>
        /// 稅金2
        /// </summary>
        public int? TAX2 { get; set; }

        /// <summary>
        /// 稅金合計
        /// </summary>
        public int TotalTax => (TAX1 ?? 0) + (TAX2 ?? 0);

        /// <summary>
        /// 資料來源
        /// </summary>
        public string SOURCE { get; set; }

        /// <summary>
        /// 報關類別
        /// </summary>
        public string TYPE { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public DateTime? IN_DATETIME { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? OUT_DATETIME { get; set; }

        /// <summary>
        /// 納稅義務人
        /// </summary>
        public string RECIPIENT { get; set; }

        /// <summary>
        /// 電話
        /// </summary>
        public string RECPHONE { get; set; }

        /// <summary>
        /// 差異金額
        /// </summary>
        public int DIFF_AMOUNT { get; set; }
    }

    /// <summary>
    /// 稅金差異項目模型
    /// </summary>
    public class TaxDifferenceItemModel
    {
        /// <summary>
        /// 稅金總表資料
        /// </summary>
        public CustomerTaxFeeMasterDataModel FeeMasterData { get; set; }

        /// <summary>
        /// 稅金總表稅額總計
        /// </summary>
        public decimal FeeMasterTotalTax { get; set; }

        /// <summary>
        /// 原始資料稅額總計
        /// </summary>
        public decimal DataListTotalTax { get; set; }

        /// <summary>
        /// 差異金額 (稅金總表 - 原始資料)
        /// </summary>
        public decimal DifferenceAmount { get; set; }
    }

    /// <summary>
    /// 匯出結果模型
    /// </summary>
    public class CustomerTaxCalculateExportResult
    {
        public bool Success { get; set; }
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public int RecordCount { get; set; }
        public string Message { get; set; }
    }
}