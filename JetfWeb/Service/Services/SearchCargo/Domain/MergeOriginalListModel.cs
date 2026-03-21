using System;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 合併原始清單模型
    /// </summary>
    internal class MergeOriginalListModel
    {
        public string Id { get; set; }
   public string ORIGINAL { get; set; }
    public DateTime? ETA { get; set; }
        public string GW { get; set; }
        public string PIECE { get; set; }
    public string F_DataDate { get; set; }
        public string I_DATA_TYPE { get; set; }
        public string I_CLEARANCE_TYPE { get; set; }
        public string DESPATCH_NAME { get; set; }
      public string CUSTOMER { get; set; }
      public DateTime? I_SIGN_IN_TIME { get; set; }
        public DateTime? I_SIGN_OUT_TIME { get; set; }
        public string MAINNUMBER { get; set; }
    public string BL_NO { get; set; }
public string JETF_SERIAL { get; set; }
        public string F_TAX_NUMBER { get; set; }
        public string TRANS_NAME { get; set; }
        public string IMPORTER { get; set; }
        public string IM_PHONENO { get; set; }
        public string IM_ADD { get; set; }
 public string F_INCLUDE_TAX { get; set; }
        public string F_CCFEE { get; set; }
        public string F_FEE { get; set; }
   public string F_COD { get; set; }
        public string F_TAX1 { get; set; }
        public string F_TAX2 { get; set; }
        public string F_TO_DLV_COD { get; set; }
     public string ITEM_NAME { get; set; }
        public string CC { get; set; }
        public string DELIVERYNO { get; set; }
        public string FIELD_X { get; set; }
    public string TRANS_TAXPAYMENT { get; set; }
      public string TRANS_NAME_NEW { get; set; }
     public string ORDER_NO { get; set; }
        public string EXPRESS_NO { get; set; }
        public string TRACKINGNO { get; set; }
   public string Format_OUT_DATETIME { get; set; }
        public string STATUS { get; set; }
    }
}
