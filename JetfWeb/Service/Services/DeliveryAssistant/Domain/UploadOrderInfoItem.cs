namespace Service.Services.DeliveryAssistant.Domain
{
    public class UploadOrderInfoItem
    {
        /// <summary>
        /// 車次編號
        /// </summary>
        public string dcShip { get; set; }

        /// <summary>
        /// 客戶單號
        /// </summary>
        public string cusOrder { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string cusOwnerName { get; set; }

        /// <summary>
        /// 到貨日期
        /// </summary>
        public string arriveDate { get; set; }

        /// <summary>
        /// 駕駛
        /// </summary>
        public string driverName { get; set; }

        /// <summary>
        /// 聯絡人
        /// </summary>
        public string contactPerson { get; set; }

        /// <summary>
        /// 連絡電話
        /// </summary>
        public string contactTel { get; set; }

        /// <summary>
        /// 應收款
        /// </summary>
        public string accountsReceivable { get; set; }

        /// <summary>
        /// 住址
        /// </summary>
        public string addr { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public string cases { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public string wgt { get; set; }
    }
}
