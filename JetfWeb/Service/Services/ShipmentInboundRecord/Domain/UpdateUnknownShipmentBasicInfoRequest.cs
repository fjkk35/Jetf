namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 更新不明貨件基本資料的請求。
    /// </summary>
    public class UpdateUnknownShipmentBasicInfoRequest
    {
        /// <summary>
        /// 貨件入庫資料 Id。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 進口方式，例如海運或空運。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// 空運資料可能為空值。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 貨件來源代碼。
        /// </summary>
        public byte? SourceType { get; set; }
    }
}