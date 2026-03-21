namespace Service.Services.ShipmentInboundRecord.Domain
{
    public class ShipmentInboundRecordExportExcelResult
    {
        /// <summary>
        /// 檔名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 檔案內容(bytes)
        /// </summary>
        public byte[] FileBytes { get; set; }
    }
}
