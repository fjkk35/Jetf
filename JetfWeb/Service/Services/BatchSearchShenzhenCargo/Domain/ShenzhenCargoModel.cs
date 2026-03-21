namespace Service.Services.BatchSearchShenzhenCargo.Domain
{
    /// <summary>
    /// 速派新遞物流貨號查詢結果
    /// </summary>
    public class ShenzhenCargoModel
    {
        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號
        /// </summary>
        public string DeliveryNo { get; set; }
    }
}
