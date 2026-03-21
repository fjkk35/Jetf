using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearanceProcessor.Domain
{
    /// <summary>
    /// 負責人建檔模型
    /// </summary>
    public class SeaClearanceProcessorModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 步驟ID
        /// </summary>
        public int StepId { get; set; }

        /// <summary>
        /// 步驟名稱（用於顯示）
        /// </summary>
        public string StepName { get; set; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 客戶名稱（用於顯示）
        /// </summary>
        public string Cust_Name { get; set; }

        /// <summary>
        /// X2負責人
        /// </summary>
        public string X2 { get; set; }

        /// <summary>
        /// X3負責人
        /// </summary>
        public string X3 { get; set; }

        /// <summary>
        /// G1負責人
        /// </summary>
        public string G1 { get; set; }

        /// <summary>
        /// 移倉負責人
        /// </summary>
        public string MoveWarehouse { get; set; }

        /// <summary>
        /// 轉G1負責人
        /// </summary>
        public string TransferG1 { get; set; }

        /// <summary>
        /// 轉移倉負責人
        /// </summary>
        public string TransferWarehouse { get; set; }
    }

    /// <summary>
    /// 查詢請求模型
    /// </summary>
    public class SeaClearanceProcessorQueryModel
    {
        /// <summary>
        /// 步驟ID（篩選用）
        /// </summary>
        public int? StepId { get; set; }

        /// <summary>
        /// 客戶代碼（篩選用）
        /// </summary>
        public string Cust_Code { get; set; }
    }

    /// <summary>
    /// 新增/修改請求模型
    /// </summary>
    public class SeaClearanceProcessorRequestModel
    {
        /// <summary>
        /// ID（編輯時需要）
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 步驟ID
        /// </summary>
        public int StepId { get; set; }

        /// <summary>
        /// 客戶代碼
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// X2負責人
        /// </summary>
        public string X2 { get; set; }

        /// <summary>
        /// X3負責人
        /// </summary>
        public string X3 { get; set; }

        /// <summary>
        /// G1負責人
        /// </summary>
        public string G1 { get; set; }

        /// <summary>
        /// 移倉負責人
        /// </summary>
        public string MoveWarehouse { get; set; }

        /// <summary>
        /// 轉G1負責人
        /// </summary>
        public string TransferG1 { get; set; }

        /// <summary>
        /// 轉移倉負責人
        /// </summary>
        public string TransferWarehouse { get; set; }
    }
}
