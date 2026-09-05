using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.SimpleDeclaration
{
    public class SimpleDeclarationModel
    {
        /// <summary>
        /// 報關人
        /// </summary>
        public string CustomsDeclarer { get; set; }

        /// <summary>
        /// 稅費帳號
        /// </summary>
        public string TaxAccount { get; set; }

        public CesMainOrderModel CesMainOrder { get; set; }

        public ClearacceInfoModel ClearacceInfo { get; set; }

        public List<ClearacceTaxModel> ClearacceTaxList { get; set; }

        public List<SeaOrderEdit> SeaOrderEditList { get; set; }
    }

    public class SeaOrderEdit
    {
        /// <summary>
        /// 分提單號
        /// </summary>
        public string Bl_No { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 統一編號
        /// </summary>
        public string Importer_Id { get; set; }

        /// <summary>
        /// 收貨人名稱
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Im_Add { get; set; }

        /// <summary>
        /// 寄件人名稱
        /// </summary>
        public string Exporter { get; set; }

        /// <summary>
        /// 起運國別
        /// </summary>
        public string Ex_CounrtyCode { get; set; }

        /// <summary>
        /// 總件數
        /// </summary>
        public int Piece { get; set; }

        /// <summary>
        /// 單位
        /// </summary>
        public string Piece_Unit { get; set; }

        /// <summary>
        /// 總毛重
        /// </summary>
        public decimal Gw { get; set; }

        /// <summary>
        /// 總淨重
        /// </summary>
        public decimal Nw { get; set; }

        /// <summary>
        /// 項次
        /// </summary>
        public int Item_No { get; set; }

        /// <summary>
        /// 貨物名稱商標(牌名)
        /// </summary>
        public string Item_Name { get; set; }

        /// <summary>
        /// 規格等
        /// </summary>
        public string Trademark { get; set; }

        /// <summary>
        /// 生產國別
        /// </summary>
        public string MadeIn { get; set; }

        /// <summary>
        /// 產品稅金代碼
        /// </summary>
        public string Ccc_Code { get; set; }

        /// <summary>
        /// 進口稅率
        /// </summary>
        public string Tax1 { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 單位
        /// </summary>
        public string Quantity_Unit { get; set; }

        /// <summary>
        /// 完稅價格
        /// </summary>
        public decimal Invoice_Amount { get; set; }
    }


    public class CesMainOrderModel  
    { 
        /// <summary>
        /// 到港日
        /// </summary>
        public DateTime? Field_Date { get; set; }

        /// <summary>
        /// 航機班次
        /// </summary>
        public string Field_B { get; set; }

        /// <summary>
        /// 存放處所
        /// </summary>
        public string Field_E { get; set; }
    }


    public class ClearacceInfoModel 
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string Main_Number { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string Clearance_Number { get; set; }

        /// <summary>
        /// 報單類別
        /// </summary>
        public string Clearance_Type { get; set; }
    }

    public class ClearacceTaxModel
    {
        public string Data_Type { get; set; }

        /// <summary>
        /// 營業稅稅基
        /// </summary>
        public int Tax_Base { get; set; }

        /// <summary>
        /// 稅費合計
        /// </summary>
        public int Tax_Amount { get; set; }

        /// <summary>
        /// 進口稅
        /// </summary>
        public double ImportTax { get; set; }

        /// <summary>
        /// 營業稅
        /// </summary>
        public double BusinessTax { get; set; }

    }
}