namespace Service.Services.BatchSearchShenzhenCargo.Domain
{
    /// <summary>
    /// 批量查詢速派新遞物流貨號請求
    /// </summary>
    public class BatchSearchShenzhenCargoRequest
    {
        /// <summary>
        /// 分提單號列表（換行分隔）
        /// </summary>
        public string TrackingNoList { get; set; }
    }
}
