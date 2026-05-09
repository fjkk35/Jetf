using Service.Extensions;
using Service.Models.CptTradeVan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Service.Models.SeaClearanceCreate
{

    public class SeaClearanceDetailQueryModel
    {
        public int Id { get; set; }

        public string DataDate { get; set; }

        public string MainNumber { get; set; }

        /// <summary>
        /// 掛號
        /// </summary>
        public string MftNo { get; set; }

        public string TrackingNo { get; set; }

        public bool IsSucess { get; set; }

        public string Memo { get; set; }

        /// <summary>
        /// 步驟Id
        /// </summary>
        public int? CurrentStepId { get; set; }

        /// <summary>
        /// 步驟名稱
        /// </summary>
        public string CurrentStepName { get; set; }

        /// <summary>
        /// 異常狀態Id
        /// </summary>
        public int? CurrentAbnormalStateId { get; set; }

        /// <summary>
        /// 異常狀態名稱
        /// </summary>
        public string CurrentAbnormalStateName { get; set; }

        /// <summary>
        /// 原單是否上傳
        /// </summary>
        public bool IsSeaOrderOriginal { get; set; }

        public List<SeaOrderOriginalModel> SeaOrderOriginals { get; set; }

        /// <summary>
        /// Gb326-進口日期
        /// </summary>
        public string ImportDate { get; set; }

        /// <summary>
        /// Gb301-報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// Gb301、GB321-報單傳輸日
        /// </summary>
        public DateTime? ProDateTime { get; set; }

        /// <summary>
        /// 要求客戶截止日
        /// </summary>
        public DateTime? CustomerDeadline { get; set; }

        /// <summary>
        /// 強制結案日
        /// </summary>
        public DateTime? CloseDate { get; set; }

        /// <summary>
        /// 報單傳輸截止日
        /// </summary>
        public DateTime? ProDateTimeDeadline { get; set; }

        /// <summary>
        /// 滯報費
        /// </summary>
        public int LateDeclarationFee { get; set; }

        /// <summary>
        /// 到倉天數
        /// </summary>
        public int? WarehouseDays { get; set; }

        /// <summary>
        /// 建檔日期
        /// </summary>
        public DateTime CrtDateTime { get; set; }

        /// <summary>
        /// 報關費用
        /// </summary>
        public int ClearanceFee { get; set; }

        /// <summary>
        /// 稅金
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 報驗公司ID
        /// </summary>
        public int? CustomsBrokerId { get; set; }

        /// <summary>
        /// 報驗公司名稱
        /// </summary>
        public string CustomsBrokerName { get; set; }

        /// <summary>
        /// 代理報驗ID
        /// </summary>
        public int? CustomsBrokerageId { get; set; }

        /// <summary>
        /// 代理報驗名稱
        /// </summary>
        public string CustomsBrokerageName { get; set; }

        /// <summary>
        /// 簽審類別
        /// </summary>
        public string ApprovalCategoryName { get; set; }

        /// <summary>
        /// 處理人
        /// </summary>
        public string ProcessingPersonnel { get; set; }

        /// <summary>
        /// 收到正本選單
        /// </summary>
        public string ReceivedOriginalMenu { get; set; }

        /// <summary>
        /// 寄文件選單
        /// </summary>
        public string DocumentDeliveryMenu { get; set; }

        /// <summary>
        /// 聯繫人異動資料
        /// </summary>
        public string ContactChangeData { get; set; }

        /// <summary>
        /// 聯繫人信箱
        /// </summary>
        public string ContactEmail { get; set; }

        /// <summary>
        /// 入倉日期
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉日期
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 扣倉
        /// </summary>
        public bool IsCustomsHold { get; set; }

        /// <summary>
        /// 扣倉項次
        /// </summary>
        public string CustomsHold { get; set; }
    }

    public class SeaOrderOriginalModel
    {
        public int SeaClearanceDetailId { get; set; }

        public int SeaOrderOriginalId { get; set; }

        public string MainNumber { get; set; }

        public string Bl_No { get; set; }

        /// <summary>
        /// 預計到港日
        /// </summary>
        public DateTime? Eta { get; set; }

        /// <summary>
        /// 原單上傳日期
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

        /// <summary>
        /// 客戶
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string Cust_Name { get; set; }

        /// <summary>
        /// 到付款
        /// </summary>
        public double? CC { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public int? Piece { get; set; }

        /// <summary>
        /// 原單申報人
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 原單申報人電話
        /// </summary>
        public string Im_Phoneno { get; set; }

        /// <summary>
        /// 進口人統一編號
        /// </summary>
        public string Importer_Id { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string Item_Name { get; set; }

        /// <summary>
        /// 派件
        /// </summary>
        public string Jetf_Serial { get; set; }

        public string Merge_Over_Flag { get; set; }

        public decimal Gw { get; set; }

        /// <summary>
        /// 收費方式
        /// </summary>
        public string Tax_Payment { get; set; }

        /// <summary>
        /// 報關費用-G1
        /// </summary>
        public int? G1Fee { get; set; }

        /// <summary>
        /// 報關費用-移倉
        /// </summary>
        public int? MoveWarehouseFee { get; set; }

        /// <summary>
        /// 報關費用-轉G1
        /// </summary>
        public int? TransferG1Fee { get; set; }

        /// <summary>
        /// 報關費用-轉移倉
        /// </summary>
        public int? TransferWarehouseFee { get; set; }

        /// <summary>
        /// 報關費用-X2X3
        /// </summary>
        public int? X2Fee { get; set; }
    }

    /// <summary>
    /// 海運通關當前步驟模型
    /// </summary>
    public class SeaClearanceCurrentStepModel
    {
        /// <summary>
        /// 海運通關步驟ID
        /// </summary>
        public int SeaClearanceStepId { get; set; }

        /// <summary>
        /// 步驟ID
        /// </summary>
        public int StepId { get; set; }

        /// <summary>
        /// 步驟名稱
        /// </summary>
        public string StepName { get; set; }

        /// <summary>
        /// 資料日期
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 建立人員
        /// </summary>
        public string CrtUser { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 步驟詳細列表
        /// </summary>
        public List<SeaClearanceStepDetailModel> StepDetails { get; set; }
    }

    /// <summary>
    /// 海運通關步驟詳細模型
    /// </summary>
    public class SeaClearanceStepDetailModel
    {
        /// <summary>
        /// 步驟詳細ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 步驟詳細名稱
        /// </summary>
        public string StepDetailName { get; set; }
    }

    public class TaxResult
    {
        public string TrackingNo { get; set; }
        public int? Tax { get; set; }
    }


    public class SeaClearanceDetailModel
    {
        /// <summary>
        /// Gb301-報單號碼
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// Gb301、GB321-報單傳輸日
        /// </summary>
        public DateTime? ProDateTime { get; set; }

        /// <summary>
        /// 報關方式
        /// </summary>
        public string Post_Entry { get; set; }
    }

}
