using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.PostClearance
{
    public class PostClearanceUploadModel
    {
        /// <summary>
        /// 匯入日期
        /// </summary>
        public string ImportDate { get; set; }
        /// <summary>
        /// 分提單號
        /// </summary>
        public string BlNo { get; set; }
        /// <summary>
        /// 傳輸日
        /// </summary>
        public string TransferDate { get; set; }
        /// <summary>
        /// 出倉日
        /// </summary>
        public string SignOutDate { get; set; }
        /// <summary>
        /// MAIL
        /// </summary>
        public string Mail { get; set; }
        /// <summary>
        /// 報關類別
        /// </summary>
        public string ClearanceType { get; set; }
        /// <summary>
        /// 倉儲
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// 材積數
        /// </summary>
        public string Volume { get; set; }
        /// <summary>
        /// X類稅金 三聯稅單
        /// </summary>
        public string XTax { get; set; }
        /// <summary>
        /// G類稅金 四聯稅單
        /// </summary>
        public string GTax { get; set; }
        /// <summary>
        /// 滯報費減免
        /// </summary>
        public string FeeReduction { get; set; }

        /// <summary>
        /// 倉租天數減免
        /// </summary>
        public string WarehouseRentDaysReduction { get; set; }

        /// <summary>
        /// 報關費2
        /// </summary>
        public string ClearanceFee2 { get; set; }
        /// <summary>
        /// 報單收費方式
        /// </summary>
        public string ClearanceFeeType { get; set; }
        /// <summary>
        /// 稅金付款人
        /// </summary>
        public string TaxPayer { get; set; }
        /// <summary>
        /// 客戶
        /// </summary>
        public string Customer { get; set; }
        /// <summary>
        /// 實際交派日
        /// </summary>
        public string ActualDate { get; set; }
        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }
    }
}
