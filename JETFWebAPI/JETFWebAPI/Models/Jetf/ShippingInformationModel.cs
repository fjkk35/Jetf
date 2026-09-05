using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Jetf
{
    public class ShippingInformationModel
    {
        /// <summary>
        /// 出仓日期
        /// </summary>
        public string Fdate { get; set; }

        /// <summary>
        /// 分单号
        /// </summary>
        public string AwbNo { get; set; }

        /// <summary>
        /// 配送单号
        /// </summary>
        public string TrackingNumber { get; set; }

        /// <summary>
        /// 外袋號碼
        /// </summary>
        public string BigbagId { get; set; }

        /// <summary>
        /// 收件人姓名
        /// </summary>
        public string ConsigneeName { get; set; }

        /// <summary>
        /// 收件人电话
        /// </summary>
        public string ConsigneePhone { get; set; }

        /// <summary>
        /// 收件人地址
        /// </summary>
        public string ConsigneeAddress { get; set; }

        /// <summary>
        /// 包裹件数
        /// </summary>
        public string PackagePic { get; set; }

        /// <summary>
        /// 包裹重量
        /// </summary>
        public string PackageWeight { get; set; }

        /// <summary>
        /// 长
        /// </summary>
        public string PackageLength { get; set; }

        /// <summary>
        /// 宽
        /// </summary>
        public string PackageWidth { get; set; }

        /// <summary>
        /// 高
        /// </summary>
        public string PackageHeight { get; set; }

        /// <summary>
        /// 代收款
        /// </summary>
        public string DaocuCash { get; set; }

        /// <summary>
        /// 承运方式(空，海)
        /// </summary>
        public string CarrierType { get; set; }

        /// <summary>
        /// 未程清关公司
        /// </summary>
        public string CustomsCop { get; set; }

        /// <summary>
        /// 配送公司客代号
        /// </summary>
        public string PsAccount { get; set; }

        /// <summary>
        /// 口岸
        /// </summary>
        public string LogisPort { get; set; }

        /// <summary>
        /// 业务模块
        /// </summary>
        public string BizType { get; set; }

        /// <summary>
        /// 状态码，空则表示正常
        /// </summary>
        public string Status { get; set; }
    }
}