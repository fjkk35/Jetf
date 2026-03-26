namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料派件公司修改條件。
    /// </summary>
    public class SjlShippingDataUpdateTransNameRequest
    {
        public int SjlShippingDataId { get; set; }

        public string TransName { get; set; }
    }
}