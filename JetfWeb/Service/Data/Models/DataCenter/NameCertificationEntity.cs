using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 進口人姓名與證號驗證資料。
    /// </summary>
    [Table("NAME_CERTIFICATION", Schema = "dbo")]
    public sealed class NameCertificationEntity
    {
        /// <summary>
        /// 資料主鍵。
        /// </summary>
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 系統端分提單號。
        /// </summary>
        [Column("E_BL_NO")]
        public string EBlNo { get; set; }

        /// <summary>
        /// 系統端進口人證號。
        /// </summary>
        [Column("E_IMPORTER_ID")]
        public string EImporterId { get; set; }

        /// <summary>
        /// 系統端進口人姓名。
        /// </summary>
        [Column("E_IMPORTER")]
        public string EImporter { get; set; }

        /// <summary>
        /// 系統端進口人電話。
        /// </summary>
        [Column("E_IM_PHONENO")]
        public string EImporterPhone { get; set; }

        /// <summary>
        /// 原始進口人證號。
        /// </summary>
        [Column("O_IMPORTER_ID")]
        public string OImporterId { get; set; }

        /// <summary>
        /// 原始進口人姓名。
        /// </summary>
        [Column("O_IMPORTER")]
        public string OImporter { get; set; }

        /// <summary>
        /// 原始進口人電話。
        /// </summary>
        [Column("O_IM_PHONENO")]
        public string OImporterPhone { get; set; }

        /// <summary>
        /// 格式化後電話一。
        /// </summary>
        [Column("O_IM_PHONENO_FORMAT")]
        public string OImporterPhoneFormat { get; set; }

        /// <summary>
        /// 格式化後電話二。
        /// </summary>
        [Column("O_IM_PHONENO_FORMAT2")]
        public string OImporterPhoneFormat2 { get; set; }

        /// <summary>
        /// 身分證檢核結果一。
        /// </summary>
        [Column("ID_RESULT1")]
        public string IdResult1 { get; set; }

        /// <summary>
        /// 身分證檢核結果二。
        /// </summary>
        [Column("ID_RESULT2")]
        public string IdResult2 { get; set; }

        /// <summary>
        /// 身分證檢核結果三。
        /// </summary>
        [Column("ID_RESULT3")]
        public string IdResult3 { get; set; }

        /// <summary>
        /// 身分證檢核結果四。
        /// </summary>
        [Column("ID_RESULT4")]
        public string IdResult4 { get; set; }

        /// <summary>
        /// 身分證檢核時間。
        /// </summary>
        [Column("ID_DATE")]
        public DateTime? IdDate { get; set; }

        /// <summary>
        /// 電話檢核結果一。
        /// </summary>
        [Column("TEL_RESULT1")]
        public string TelResult1 { get; set; }

        /// <summary>
        /// 電話檢核結果二。
        /// </summary>
        [Column("TEL_RESULT2")]
        public string TelResult2 { get; set; }

        /// <summary>
        /// 電話檢核結果三。
        /// </summary>
        [Column("TEL_RESULT3")]
        public string TelResult3 { get; set; }

        /// <summary>
        /// 電話檢核結果四。
        /// </summary>
        [Column("TEL_RESULT4")]
        public string TelResult4 { get; set; }

        /// <summary>
        /// 電話檢核時間。
        /// </summary>
        [Column("TEL_DATE")]
        public DateTime? TelDate { get; set; }

        /// <summary>
        /// 最終確認進口人證號。
        /// </summary>
        [Column("CHECK_IMPORTER_ID")]
        public string CheckImporterId { get; set; }

        /// <summary>
        /// 最終確認進口人姓名。
        /// </summary>
        [Column("CHECK_IMPORTER")]
        public string CheckImporter { get; set; }

        /// <summary>
        /// 最終確認進口人電話。
        /// </summary>
        [Column("CHECK_IM_PHONENO")]
        public string CheckImporterPhone { get; set; }

        /// <summary>
        /// 系統端資料修改時間。
        /// </summary>
        [Column("E_MODIFTYDATE")]
        public DateTime? EModifyDate { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CRTDATETIME")]
        public DateTime? CrtDateTime { get; set; }
    }
}