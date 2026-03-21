using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance
{
    public class SeaClearanceResponse
    {
        public List<SeaClearanceModel> Data { get; set; }

        public int TotalCount { get; set; }
    }


    public class SeaClearanceModel
    {
        public int Id { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// Gb301-報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 建檔日期
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// 倉別
        /// </summary>
        public string Modifyby { get; set; }

        /// <summary>
        /// 報關方式
        /// </summary>
        public string Post_Entry { get; set; }

        public DateTime? Eta { get; set; }

        public string Cust_Code { get; set; }

        public string Cust_Name { get; set; }

        public int? Piece { get; set; }

        /// <summary>
        /// 進口人
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        public string Jetf_Serial { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string Item_Name { get; set; }

        /// <summary>
        /// 出倉日期
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 步驟名稱
        /// </summary>
        public string StepName { get; set; }
    }
}
