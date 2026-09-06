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
        /// 託運單號欄位名稱集合。
        /// </summary>
        public string[] TrackingNoHeaders { get; set; }

        /// <summary>
        /// 到付金額欄位名稱集合。
        /// </summary>
        public string[] CodHeaders { get; set; }

        /// <summary>
        /// 稅金金額欄位名稱集合。
        /// </summary>
        public string[] TaxHeaders { get; set; }

        /// <summary>
        /// 稅金手續費欄位名稱集合。
        /// </summary>
        public string[] FeeHeaders { get; set; }

        /// <summary>
        /// 必要欄位群組，群組內任一欄位名稱命中即視為符合。
        /// </summary>
        public IEnumerable<string[]> RequiredHeaderGroups
        {
            get
            {
                yield return TrackingNoHeaders;
                yield return CodHeaders;
                yield return TaxHeaders;
                yield return FeeHeaders;
            }
        }
    }
}
