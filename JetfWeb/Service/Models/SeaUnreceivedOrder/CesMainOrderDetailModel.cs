using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaUnreceivedOrder
{
    public class CesMainOrderDetailModel
    {
        /// <summary>
        /// 分提單號碼
        /// </summary>
        public string BL_NO { get; set; }

        /// <summary>
        /// 航班主號
        /// </summary>
        public string MAINNUMBER { get; set; }

        /// <summary>
        /// 艙單號碼
        /// </summary>
        public string MANIFEST { get; set; }

        /// <summary>
        /// 項次
        /// </summary>
        public string ITEM_NO { get; set; }

        public int ITEM_NO_SORT 
        {
            get
            {
               return int.TryParse(ITEM_NO, out var itemNo) 
                        ? itemNo : 99;
            }
        }


        /// <summary>
        /// 快遞業者統一編號
        /// </summary>
        public string JETF_ID { get; set; }

        /// <summary>
        /// 單價條件
        /// </summary>
        public string TERMSOFPRICE { get; set; }

        /// <summary>
        /// 單價幣別代碼
        /// </summary>
        public string CURRENCY { get; set; }

        /// <summary>
        /// 毛重
        /// </summary>
        public double GW { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public int PIECE { get; set; }

        /// <summary>
        /// 件數單位
        /// </summary>
        public string PIECE_UNIT { get; set; }

        /// <summary>
        /// 標記
        /// </summary>
        public string MARKS { get; set; }

        /// <summary>
        /// 貨物名稱
        /// </summary>
        public string ITEM_NAME { get; set; }

        /// <summary>
        /// 貨品分類號列
        /// </summary>
        public string CCC_CODE { get; set; }

        /// <summary>
        /// 商標(牌名)
        /// </summary>
        public string TRADEMARK { get; set; }

        /// <summary>
        /// 成分及規格
        /// </summary>
        public string II_SPEC { get; set; }

        /// <summary>
        /// 淨重
        /// </summary>
        public double NW { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public int QUANTITY { get; set; }

        /// <summary>
        /// 數量單位
        /// </summary>
        public string QUANTITY_UNIT { get; set; }

        /// <summary>
        /// 單價金額
        /// </summary>
        public double UNIT_PRICE { get; set; }

        /// <summary>
        /// 發票總金額
        /// </summary>
        public double INVOICE_AMOUNT { get; set; }

        /// <summary>
        /// 體積
        /// </summary>
        public string MEASUREMENT { get; set; }

        /// <summary>
        /// 體積單位
        /// </summary>
        public string CBM { get; set; }

        /// <summary>
        /// 生產國別
        /// </summary>
        public string MADEIN { get; set; }

        /// <summary>
        /// 出口人英文名稱
        /// </summary>
        public string EXPORTER { get; set; }

        /// <summary>
        /// 出口人國家代碼
        /// </summary>
        public string EX_COUNRTYCODE { get; set; }

        /// <summary>
        /// 出口人英文地址
        /// </summary>
        public string EX_ADD { get; set; }

        /// <summary>
        /// 進口人身分識別碼
        /// </summary>
        public string PARTY_IDENTIFIER { get; set; }

        /// <summary>
        /// 進口人統一編號
        /// </summary>
        public string IMPORTER_ID { get; set; }

        /// <summary>
        /// 進口人名稱
        /// </summary>
        public string IMPORTER { get; set; }

        /// <summary>
        /// 進口人電話
        /// </summary>
        public string IM_PHONENO { get; set; }

        /// <summary>
        /// 進口人英文地址
        /// </summary>
        public string IM_ADD { get; set; }

        /// <summary>
        /// 貨櫃種類 (製單資料)
        /// </summary>
        public string E_CONT_TYPE { get; set; }

        /// <summary>
        /// 貨櫃號碼 (製單資料)
        /// </summary>
        public string E_CONT_NO { get; set; }

        /// <summary>
        /// 封條號碼 (製單資料)
        /// </summary>
        public string E_SEALNO { get; set; }

        /// <summary>
        /// 其他申報事項2
        /// </summary>
        public string DECLARATION_2 { get; set; }

        /// <summary>
        /// 主動申報繳納稅款註記
        /// </summary>
        public string TAXFEE_DECLARED { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string TRANS_NAME { get; set; }

        /// <summary>
        /// 配送單號
        /// </summary>
        public string JETF_SERIAL { get; set; }

        /// <summary>
        /// 尺寸（單位：CM）
        /// </summary>
        public string SIZE { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string DESPATCH_NAME { get; set; }

        /// <summary>
        /// 貨櫃種類 (原單資料)
        /// </summary>
        public string O_CONT_TYPE { get; set; }

        /// <summary>
        /// 貨櫃號碼 (原單資料)
        /// </summary>
        public string O_CONT_NO { get; set; }

        /// <summary>
        /// 封條號碼 (原單資料)
        /// </summary>
        public string O_SEALNO { get; set; }

        /// <summary>
        /// 電商或集運商編號
        /// </summary>
        public string CONSOL_CODE { get; set; }

        /// <summary>
        /// 貨物識別代碼
        /// </summary>
        public string CONSOL_TYPE { get; set; }

        /// <summary>
        /// 電商或集運商名稱
        /// </summary>
        public string CONSOL_NAME { get; set; }

        /// <summary>
        /// 電商或集運商網址
        /// </summary>
        public string CONSOL_URL { get; set; }

        public string CorrectImporterName { get; set; }

        public string CorrectImporterId { get; set; }

        public string CorrectImporterPhone { get; set; }

        public string UploadOpe { get; set; }
    }
}
