using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞深圳稅單 Excel 欄位定義。
    /// </summary>
    public sealed class SeaShenzhenTaxUploadBrokerHeaderDefinition
    {
        /// <summary>
        /// 畫面與錯誤訊息使用的報關行名稱。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 主號欄位名稱集合。
        /// </summary>
        public string[] MainNumberHeaders { get; set; }

        /// <summary>
        /// 報單號碼欄位名稱集合。
        /// </summary>
        public string[] ClearanceNumberHeaders { get; set; }

        /// <summary>
        /// 分號欄位名稱集合。
        /// </summary>
        public string[] TrackingNoHeaders { get; set; }

        /// <summary>
        /// 稅單號碼欄位名稱集合。
        /// </summary>
        public string[] TaxNumberHeaders { get; set; }

        /// <summary>
        /// 稅單金額欄位名稱集合。
        /// </summary>
        public string[] TaxHeaders { get; set; }

        /// <summary>
        /// 納稅人欄位名稱集合。
        /// </summary>
        public string[] TaxPayerHeaders { get; set; }

        /// <summary>
        /// 統編欄位名稱集合。
        /// </summary>
        public string[] TaxRecIdHeaders { get; set; }

        /// <summary>
        /// 必要欄位群組，群組內任一欄位名稱命中即視為符合。
        /// </summary>
        public IEnumerable<string[]> RequiredHeaderGroups
        {
            get
            {
                yield return MainNumberHeaders;
                yield return ClearanceNumberHeaders;
                yield return TrackingNoHeaders;
                yield return TaxNumberHeaders;
                yield return TaxHeaders;
            }
        }
    }
}