using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum Authority
    {
        /// <summary>
        /// 1-1.海運稅金資料上傳
        /// </summary>
        UploadSeaTax,

        /// <summary>
        /// 2-1.G類資料上傳
        /// </summary>
        UploadSeaTaxG,

        /// <summary>
        /// 3-1.物流代收檔下載-海運
        /// </summary>
        DownloadSeaTax,

        /// <summary>
        /// 3-1-1.菜鳥7-11海運稅金
        /// </summary>
        CainiaoSevenElevenSeaTax,

        /// <summary>
        /// 3-1-2.菜鳥圓通海運稅金
        /// </summary>
        CainiaoYtoSeaTax,

        /// <summary>
        /// 3-1-3.菜鳥全家海運稅金
        /// </summary>
        CainiaoFamilySeaTax,

        /// <summary>
        /// 3-1-5.菜鳥海運超峰稅金
        /// </summary>
        CainiaoTaixinStarSeaTax,

        /// <summary>
        /// 3-2.物流代收檔下載-空運
        /// </summary>
        DownloadEtlTax,

        /// <summary>
        /// 3-2-1.菜鳥萊爾富稅金
        /// </summary>
        CainiaoHiLifeTax,

        /// <summary>
        /// 3-2-2.菜鳥空快全家稅金
        /// </summary>
        CainiaoFamilyTax,

        /// <summary>
        /// 3-2-3.菜鳥空快超峰稅金
        /// </summary>
        CainiaoTaixinStarTax,

        /// <summary>
        /// 3-2-4.菜鳥空快7-11稅金
        /// </summary>
        SevenElevenEtlTax,

        /// <summary>
        /// 3-3.空快稅金-回桃園倉庫明細表
        /// </summary>
        DownloadEtlWarehouse,

        /// <summary>
        /// 3-4.稅金總表及明細表
        /// </summary>
        DownloadTaxReport,

        /// <summary>
        /// 3-5.G類稅金調整明細表
        /// </summary>
        DownloadModifySeaTaxG,

        /// <summary>
        /// 3-6.海快TPCT及TIPC稅金調整明細表
        /// </summary>
        DownloadModifySeaTax,

        /// <summary>
        /// 3-7.菜鳥包稅稅金方式修改上傳
        /// </summary>
        UploadCainiaoModifyTax,

        /// <summary>
        /// 3-8.空快客戶託運新竹明細表
        /// </summary>
        HctEtlTax,

        /// <summary>
        /// 4-1.物流代收金額上傳
        /// </summary>
        UploadCollectibleAmount,

        /// <summary>
        /// 4-2.物流代收金額差異表
        /// </summary>
        DownloadCollectibleAmount,

        /// <summary>
        /// 5-1.物流代收匯款上傳
        /// </summary>
        UploadCollectibleRemittance,

        /// <summary>
        /// 5-2.物流代收匯款明細表
        /// </summary>
        DownloadCollectibleRemittanceDetails,

        /// <summary>
        /// 5-3.物流代收未匯款明細表
        /// </summary>
        DownloadNotCollectibleRemittanceDetails,

        /// <summary>
        /// 6-1.開立電子發票作業
        /// </summary>
        InvoiceProcessing,

        /// <summary>
        /// 客戶查詢
        /// </summary>
        SearchCustomer,

        /// <summary>
        /// 稅金查詢
        /// </summary>
        SearchTax,

        /// <summary>
        /// 批量稅金查詢
        /// </summary>
        BatchSearchTax,

        /// <summary>
        /// 貨況查詢
        /// </summary>
        SearchCargo,

        /// <summary>
        /// 批量貨況查詢明細表
        /// </summary>
        BatchSearchCargo,

        /// <summary>
        /// 蝦皮預約入倉明細表
        /// </summary>
        BatchSearchCargoShopee,

        /// <summary>
        /// 處置說明批次上傳
        /// </summary>
        BatchUploadProcess,

        /// <summary>
        /// 處置說明下載
        /// </summary>
        DownloadProcess,

        /// <summary>
        /// 空快錯單統計及明細下載
        /// </summary>
        DownloadEtlErrorReport,

        /// <summary>
        /// 營收報表
        /// </summary>
        IncomeReport,

        /// <summary>
        /// 營收報表-到港日
        /// </summary>
        IncomeEtaReport,

        /// <summary>
        /// 營收總表及明細表
        /// </summary>
        IncomeDetails,

        /// <summary>
        /// 海空快通關狀態彙總表
        /// </summary>
        ClearanceStatusReport,

        /// <summary>
        /// 空快客戶作業量報表
        /// </summary>
        EtlCustomerWorkLoadReport,

        /// <summary>
        /// 上傳檔案(A03、B6F、班機派件送達)
        /// </summary>
        UploadFlightArrival,

        /// <summary>
        /// 上傳空快錯單袋號
        /// </summary>
        UploadEtlErrorBagNo,

        /// <summary>
        /// 上傳海快艙單號碼
        /// </summary>
        UploadSeaManifest,

        /// <summary>
        /// 上傳海快錯單
        /// </summary>
        UploadSeaErrorBagNo,

        /// <summary>
        /// 海快錯單作業
        /// </summary>
        SeaErrorWorkLoad,

        /// <summary>
        /// 批量製單申報資料查詢
        /// </summary>
        BatchSearchEditOrder,

        /// <summary>
        /// 上傳後段報關費用
        /// </summary>
        UploadPostClearance,

        /// <summary>
        /// 空快清關明細表
        /// </summary>
        EtlClearanceDetails,

        /// <summary>
        /// 空快清關主號明細表
        /// </summary>
        EtlClearanceMainDetails,

        /// <summary>
        /// 上傳拆袋資料
        /// </summary>
        UploadUnpackingBagNo,

        /// <summary>
        /// 上傳空快併袋袋號資料
        /// </summary>
        UploadEtlMergeBagNo,

        /// <summary>
        /// 已拆袋明細表
        /// </summary>
        UnpackingBagNoDetails,

        /// <summary>
        /// 拆袋作業明細表
        /// </summary>
        UnpackingBagNoWorkDetails,

        /// <summary>
        /// 掃貨上車交接派件公司明細表
        /// </summary>
        ScanCargoDetails,

        /// <summary>
        /// 掃貨上車交接客戶派件公司明細表
        /// </summary>
        ScanCargoCustomerDetails,

        /// <summary>
        /// 上傳空運出口航班及出入倉時間
        /// </summary>
        UploadEtlExportFlight,

        /// <summary>
        /// LINE群組建立
        /// </summary>
        CreateLineGroup,

        /// <summary>
        /// LINE群組查詢
        /// </summary>
        SearchLineGroup,

        /// <summary>
        /// 轉檔查詢
        /// </summary>
        SearchWork,

        /// <summary>
        /// CPT單一入口網站查詢
        /// </summary>
        CptTradeVan,

        /// <summary>
        /// 帳號管理
        /// </summary>
        UserMaster,

        /// <summary>
        /// 空快B6F錯單G表
        /// </summary>
        EtlErrorG,

        /// <summary>
        /// 工作天
        /// </summary>
        WorkDay,

        /// <summary>
        /// 工作天作業地區
        /// </summary>
        WorkDayArea,

        /// <summary>
        /// 錯單發送簡訊
        /// </summary>
        ErrorOrderSend,

        /// <summary>
        /// 申報人查詢
        /// </summary>
        Importer,

        /// <summary>
        /// 權限
        /// </summary>
        Authority,

        /// <summary>
        /// 權限群組
        /// </summary>
        AuthorityGroup,

        /// <summary>
        /// 拆袋統計表
        /// </summary>
        UnpackingStatistics,

        /// <summary>
        /// 海快後段建檔
        /// </summary>
        SeaClearanceCreate,

        /// <summary>
        /// 海快後段報關系統
        /// </summary>
        SeaClearance,

        /// <summary>
        /// 報驗行建檔
        /// </summary>
        CustomsBroker,

        /// <summary>
        /// 客戶管理
        /// </summary>
        SeaClearanceCustomer,

        /// <summary>
        /// 步驟管理
        /// </summary>
        Step,

        /// <summary>
        /// 簽審類別管理
        /// </summary>
        ApprovalCategory,

        /// <summary>
        /// 文件名稱管理
        /// </summary>
        AuthorizationForm,

        /// <summary>
        /// 海快後段客戶收費方式
        /// </summary>
        SeaClearanceCustTaxPayment,

        /// <summary>
        /// 海快後段捷利收費方式
        /// </summary>
        SeaClearanceSjlTaxPayment,

        /// <summary>
        /// 海快後段報關費用
        /// </summary>
        SeaClearanceFee,

        /// <summary>
        /// 海快作業錯單
        /// </summary>
        SeaWorkErrorOrder,

        /// <summary>
        /// 海快作業錯單統計報表
        /// </summary>
        SeaWorkErrorOrderReport,

        /// <summary>
        /// 海快作業具結
        /// </summary>
        SeaWorkRecognizance,

        /// <summary>
        /// 海快未收單明細
        /// </summary>
        SeaUnreceivedOrder,

        /// <summary>
        /// 上傳拆櫃紀錄
        /// </summary>
        SeaUnboxingRecord,

        /// <summary>
        /// 上傳傳輸異動/紀錄
        /// </summary>
        SeaTransRecord,

        /// <summary>
        /// 7-1客戶稅金時間設定
        /// </summary>
        CustomerTaxSetting,

        /// <summary>
        /// 7-2客戶稅金計算
        /// </summary>
        CustomerTaxCalculate,

        /// <summary>
        /// 7-3客戶稅金結算
        /// </summary>
        CustomerTaxStatistics,

        /// <summary>
        /// 異常狀態
        /// </summary>
        AbnormalState,

        /// <summary>
        /// 負責人建檔
        /// </summary>
        SeaClearanceProcessor,

        /// <summary>
        /// 貨件入庫批量上傳
        /// </summary>
        ShipmentInboundBatchImport,

        /// <summary>
        /// 貨件退件處理
        /// </summary>
        ShipmentInboundProcess,

        /// <summary>
        /// 貨件紀錄查詢
        /// </summary>
        ShipmentInboundRecord,

        /// <summary>
        /// 撿貨明細
        /// </summary>
        ShipmentInboundPick,

        /// <summary>
        /// 貨件出庫批量上傳
        /// </summary>
        ShipmentOutboundBatchImport,

        /// <summary>
        /// 儲位調撥
        /// </summary>
        ShipmentInboundLocationTransfer,

        /// <summary>
        /// 倉庫處理狀態
        /// </summary>
        ShipmentInboundWarehouseProcess,

        /// <summary>
        /// 批量查詢速派新遞物流貨號
        /// </summary>
        BatchSearchShenzhenCargo,

        /// <summary>
        /// 派送助理
        /// </summary>
        DeliveryAssistant,

        /// <summary>
        /// 主號稅金查詢
        /// </summary>
        MainTaxSearch,

        /// <summary>
        /// 捷利托運資料上傳
        /// </summary>
        SjlBatchImport,

        /// <summary>
        /// 新遞上傳託運資料
        /// </summary>
        SeaShenzhenOriginalUpload,

        /// <summary>
        /// 捷利帳單
        /// </summary>
        SjlBilling,

        /// <summary>
        /// 7-5稅金單客戶查詢
        /// </summary>
        TaxPortalCustomer,

        /// <summary>
        /// Ezway電子商務通關平台
        /// </summary>
        Ezway,

        /// <summary>
        /// Coupang回報表單
        /// </summary>
        CoupangReportForm,
    }
}
