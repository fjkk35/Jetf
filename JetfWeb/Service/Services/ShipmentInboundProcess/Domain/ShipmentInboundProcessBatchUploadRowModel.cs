namespace Service.Services.ShipmentInboundProcess.Domain
{
    public class ShipmentInboundProcessBatchUploadRowModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 處理方式(中文)
        /// </summary>
        public string ProcessTypeText { get; set; }

        /// <summary>
        /// 退件原因
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Excel 列號
        /// </summary>
        public int RowNo { get; set; }
    }
}
