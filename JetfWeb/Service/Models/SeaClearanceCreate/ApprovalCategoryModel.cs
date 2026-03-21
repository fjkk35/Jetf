using System;

namespace Service.Models.SeaClearanceCreate
{
    /// <summary>
    /// 簽審類別模型
    /// </summary>
    public class ApprovalCategoryModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 類別名稱
        /// </summary>
        public string CategoryName { get; set; }
    }

    /// <summary>
    /// 海關明細簽審類別關聯模型
    /// </summary>
    public class SeaClearanceDetailApprovalCategoryModel
    {
        /// <summary>
        /// 海關明細ID
        /// </summary>
        public int SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 簽審類別ID
        /// </summary>
        public int ApprovalCategoryId { get; set; }

        /// <summary>
        /// 類別名稱
        /// </summary>
        public string CategoryName { get; set; }
    }
}