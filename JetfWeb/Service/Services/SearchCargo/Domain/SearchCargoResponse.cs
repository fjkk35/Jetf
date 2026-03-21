using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 貨況查詢回應
    /// </summary>
    public class SearchCargoResponse
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }

   /// <summary>
      /// 稅金作業日
  /// </summary>
        public string F_DataDate { get; set; }

  /// <summary>
        /// 倉儲類型
  /// </summary>
        public string I_DATA_TYPE { get; set; }

        /// <summary>
 /// 出倉日期
        /// </summary>
        public string Format_OUT_DATETIME { get; set; }

        /// <summary>
   /// 客戶名稱
        /// </summary>
 public string CUSTOMER { get; set; }

        /// <summary>
   /// 主提單號
   /// </summary>
        public string MAINNUMBER { get; set; }

     /// <summary>
/// 清關袋號
        /// </summary>
        public string BL_NO { get; set; }

        /// <summary>
   /// 件數
        /// </summary>
   public string PIECE { get; set; }

        /// <summary>
        /// 物流貨號
    /// </summary>
     public string DELIVERYNO { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
    public string ITEM_NAME { get; set; }
    }
}
