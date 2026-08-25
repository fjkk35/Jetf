using Service.Data;
using Service.Models;
using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace Service.Services.TaxTransferGuard
{
    /// <summary>
    /// 稅金轉檔前的作業日銷帳檢查服務。
    /// </summary>
    public sealed class TaxTransferGuardService
    {
        private readonly JetfDbContext _jetfDb;

        /// <summary>
        /// 建立稅金轉檔作業日銷帳檢查服務。
        /// </summary>
        /// <param name="jetfDb">JETF 資料庫內容。</param>
        public TaxTransferGuardService(JetfDbContext jetfDb)
        {
            _jetfDb = jetfDb;
        }

        /// <summary>
        /// 驗證指定作業日是否允許執行稅金轉檔。
        /// </summary>
        /// <param name="dataDate">作業日期，格式為 yyyyMMdd。</param>
        /// <returns>驗證結果。</returns>
        public ResponseModel ValidateCanTransfer(string dataDate)
        {
            DateTime parsedDate;
            if (!DateTime.TryParseExact(
                dataDate,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate))
            {
                return new ResponseModel("稅金轉檔日期格式錯誤");
            }

            // 同一作業日只要有任一客戶或物流銷帳時間，就禁止所有來源重新執行稅金轉檔。
            var hasReconciledDetail = _jetfDb.FeeMasterDetails
                .AsNoTracking()
                .Any(detail =>
                    detail.FeeMaster.DataDate == dataDate &&
                    (detail.ReceivedCustomerCodTime.HasValue ||
                     detail.ReceivedToDlvCodTime.HasValue));
            if (hasReconciledDetail)
            {
                return new ResponseModel
                {
                    IsSuccess = false,
                    status = Status.error,
                    msg = $"{parsedDate:yyyy/MM/dd} 已有客戶或物流銷帳資料，不能重新執行稅金轉檔。"
                };
            }

            return new ResponseModel();
        }
    }
}
