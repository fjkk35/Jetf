using System;

namespace Service.Models.ShortOverUnload
{
    /// <summary>
    /// 短溢卸報告表頭使用的船掛及關港貿船舶資料。
    /// </summary>
    public class ShortOverUnloadVessel
    {
        /// <summary>海關通關號碼（船掛）。</summary>
        public string VslRegNo { get; set; }

        /// <summary>船（機）代碼（IMO NO）。</summary>
        public string ImoNo { get; set; }

        /// <summary>船舶航次。</summary>
        public string VoyageFlightNo { get; set; }

        /// <summary>船名。</summary>
        public string ShipName { get; set; }

        /// <summary>受理關別。</summary>
        public string ProcessCustCd { get; set; }

        /// <summary>預定到港日期。</summary>
        public DateTime EstimatedArrivalDate { get; set; }
    }
}
