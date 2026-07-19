// 建立統一的主 Angular 模組
var mainApp = angular.module('mainApp', ['commonFilters', 'ui.bootstrap', 'ui.sortable']);

// Layout Controller
mainApp.controller('LayoutController', function ($scope, $http) {
    $scope.userName = window.UserCtx.userName;
    $scope.visibleMenus = [];

    var menus = [
        {
            id: 'tax', title: '稅金操作', icon: 'fas fa-fw fa-file-invoice-dollar', partner: 'Tax', children: [
                //{ id: 'Seatax', text: '1-1.海運稅金資料上傳', url: '~/Upload/Seatax', auth: ['UploadSeaTax'] },
                { id: 'SeaTaxUploadNew', text: '1-1.海運稅金資料上傳', url: '~/SeaTaxUpload/Index', auth: ['UploadSeaTax'] },
                { id: 'SeataxG', text: '2-1.G類資料上傳', url: '~/Upload/SeataxG', auth: ['UploadSeaTaxG'] },
                //{ id: 'DownloadSea', text: '3-1.物流代收檔下載-海運', url: '~/Download/DownloadSea', auth: ['DownloadSeaTax'] },
                { id: 'DownloadSeaNew', text: '3-1.物流代收檔下載-海運', url: '~/DownloadSeaNew/Index', auth: ['DownloadSeaTax'] },
                { id: 'CainiaoSevenElevenSeaTax', text: '3-1-1.菜鳥7-11海運稅金', url: '~/CainiaoSevenElevenSeaTax/Index', auth: ['CainiaoSevenElevenSeaTax'] },
                { id: 'CainiaoYtoSeaTax', text: '3-1-2.菜鳥圓通海運稅金', url: '~/CainiaoYtoSeaTax/Index', auth: ['CainiaoSevenElevenSeaTax'] },
                { id: 'CainiaoFamilySeaTax', text: '3-1-3.菜鳥全家海運稅金', url: '~/CainiaoFamilySeaTax/Index', auth: ['CainiaoFamilySeaTax'] },
                { id: 'CompanySeaTax', text: '3-1-4.物流公司海運稅金', url: '~/CompanySeaTax/Index', auth: ['DownloadSeaTax'] },
                { id: 'CainiaoTaixinStarSeaTax', text: '3-1-5.菜鳥海運超峰稅金', url: '~/CainiaoTaixinStarSeaTax/Index', auth: ['CainiaoTaixinStarSeaTax'] },
                //{ id: 'DownloadEtl', text: '3-2.物流代收檔下載-空運', url: '~/Download/DownloadEtl', auth: ['DownloadEtlTax'] },
                { id: 'DownloadEtlNew', text: '3-2.物流代收檔下載-空運', url: '~/DownloadEtlNew/Index', auth: ['DownloadEtlTax'] },
                { id: 'CainiaoHiLifeTax', text: '3-2-1.菜鳥空快萊爾富稅金', url: '~/CainiaoHiLifeTax/Index', auth: ['CainiaoHiLifeTax'] },
                { id: 'CainiaoFamilyTax', text: '3-2-2.菜鳥空快全家稅金', url: '~/CainiaoFamilyTax/Index', auth: ['CainiaoFamilyTax'] },
                { id: 'CainiaoTaixinStarTax', text: '3-2-3.菜鳥空快超峰稅金', url: '~/CainiaoTaixinStarTax/Index', auth: ['CainiaoTaixinStarSeaTax'] },
                { id: 'SevenElevenEtlTax', text: '3-2-4.空快7-11稅金', url: '~/SevenElevenEtlTax/Index', auth: ['SevenElevenEtlTax'] },
                { id: 'TCatEtlTax', text: '3-2-5.黑貓空快稅金', url: '~/TCatEtlTax/Index', auth: ['CainiaoTaixinStarSeaTax'] },
                { id: 'DownloadNoIncludeTax', text: '3-3.空快稅金-回桃園倉庫明細表', url: '~/DownloadNoIncludeTax/Index', auth: ['DownloadEtlWarehouse'] },
                { id: 'DownloadIncludeTax', text: '3-4.稅金總表及明細表', url: '~/Download/DownloadIncludeTax', auth: ['DownloadTaxReport'] },
                { id: 'CainiaoHiLifeTaxDetails', text: '3-4-1.萊爾富接收稅金明細表', url: '~/CainiaoHiLifeTaxDetails/Index', auth: ['CainiaoHiLifeTax'] },
                { id: 'DownloadSeaModifyG', text: '3-5.G類稅金調整明細表', url: '~/Download/DownloadSeaModifyG', auth: ['DownloadModifySeaTaxG'] },
                { id: 'DownloadSeaModify', text: '3-6.海快TPCT及TIPC稅金調整明細表', url: '~/Download/DownloadSeaModify', auth: ['DownloadModifySeaTax'] },
                { id: 'CainiaoTaxEdit', text: '3-7.菜鳥包稅稅金方式修改上傳', url: '~/Upload/CainiaoTaxEdit', auth: ['UploadCainiaoModifyTax'] },
                { id: 'HctEtlTax', text: '3-8.空快客戶託運新竹明細表', url: '~/HctEtlTax/Index', auth: ['HctEtlTax'] },
                { id: 'SeaCustomerShippingDetails', text: '3-9.海運客戶託運明細表', url: '~/SeaCustomerShippingDetails/Index', auth: ['SeaCustomerShippingDetails'] },
                { id: 'SeaMainNumberShippingDetails', text: '3-10.海運主號託運明細表(無稅金)', url: '~/SeaMainNumberShippingDetails/Index', auth: ['SeaMainNumberShippingDetails'] },
                //{ id: 'Receive', text: '4-1.物流代收金額上傳', url: '~/Upload/Receive', auth: ['UploadCollectibleAmount'] },
                //{ id: 'DownloadReceive', text: '4-2.物流代收金額差異表', url: '~/Download/DownloadReceive', auth: ['DownloadCollectibleAmount'] },
                //{ id: 'Transfer', text: '5-1.物流代收匯款上傳', url: '~/Upload/Transfer', auth: ['UploadCollectibleRemittance'] },
                //{ id: 'DownloadTransfer', text: '5-2.物流代收匯款明細表', url: '~/Download/DownloadTransfer', auth: ['DownloadCollectibleRemittanceDetails'] },
                //{ id: 'DownloadNoTransfer', text: '5-3.物流代收未匯款明細表', url: '~/Download/DownloadNoTransfer', auth: ['DownloadNotCollectibleRemittanceDetails'] },
                { id: 'InvoiceWorkNew', text: '6-1.開立電子發票作業', url: '~/InvoiceNew/Index', auth: ['InvoiceProcessing'] },
                { id: 'CustomerTaxSetting', text: '7-1.客戶稅金時間設定', url: '~/CustomerTaxSetting/Index', auth: ['CustomerTaxSetting'] },
                { id: 'CustomerTaxCalculate', text: '7-2.客戶稅金計算', url: '~/CustomerTaxCalculate/Index', auth: ['CustomerTaxCalculate'] },
                { id: 'CustomerTaxStatistics', text: '7-3.客戶稅金結算', url: '~/CustomerTaxStatistics/Index', auth: ['CustomerTaxStatistics'] },
                { id: 'MainTaxSearch', text: '7-4.主號稅金查詢', url: '~/MainTaxSearch/Index', auth: ['MainTaxSearch'] },
                { id: 'TaxPortalCustomer', text: '7-5.稅金單客戶查詢', url: '~/TaxPortalCustomer/Index', auth: ['TaxPortalCustomer'] },
                { id: 'SearchCustomer', text: '客戶查詢', url: '~/Customer/SearchCustomer', auth: ['SearchCustomer'] }
            ]
        },
        {
            id: 'seaShenzhen', title: '新遞', icon: 'fas fa-fw fa-truck', children: [
                { id: 'SeaShenzhenOriginal', text: '上傳託運資料', url: '~/SeaShenzhenOriginal/Index', auth: ['SeaShenzhenOriginalUpload'] },
                { id: 'SeaShenzhenTax', text: '上傳稅金資料', url: '~/SeaShenzhenTax/Index', auth: ['SeaShenzhenOriginalUpload'] },
                { id: 'SeaShenzhenFeeTransfer', text: '捷豐稅金轉檔', url: '~/SeaShenzhenFeeTransfer/Index', auth: ['SeaShenzhenOriginalUpload'] },
                { id: 'SeaShenzhenOriginalQuery', text: '託運資料查詢', url: '~/SeaShenzhenOriginalQuery/Index', auth: ['SeaShenzhenOriginalUpload'] },
                { id: 'SeaShenzhenFeeManualToDlvCod', text: '代收金額人工調整', url: '~/SeaShenzhenFeeManualToDlvCod/Index', auth: ['SeaShenzhenOriginalUpload'] },
                { id: 'SeaShenzhenFeeQuery', text: '稅金資料查詢', url: '~/SeaShenzhenFeeQuery/Index', auth: ['SeaShenzhenOriginalUpload'] },
                { id: 'SeaShenzhenFeeDownload', text: '物流代收檔下載', url: '~/SeaShenzhenFeeDownload/Index', auth: ['SeaShenzhenOriginalUpload'] }
            ]
        },
        {
            id: 'search', title: '查詢', icon: 'fas fa-fw fa-search', partner: 'Search', children: [
                { id: 'SearchCargo', text: '稅金查詢', url: '~/Cargo/SearchCargo', auth: ['SearchTax'] },
                { id: 'SearchCargo2', text: '貨況查詢', url: '~/Cargo/SearchCargo2', auth: ['SearchCargo'] },
                { id: 'SearchCargo', text: '貨況查詢V2', url: '~/SearchCargo/Index', auth: ['SearchCargo'] },
                { id: 'BatchSearchCargo2', text: '批量貨況查詢明細表', url: '~/BatchSearchCargo2/Index', auth: ['BatchSearchCargo'] },
                { id: 'CoupangReportForm', text: '批量查詢Coupang', url: '~/CoupangReportForm/Index', auth: ['CoupangReportForm'] },
                { id: 'BatchSearchShenzhenCargo', text: '批量查詢速派新遞物流貨號', url: '~/BatchSearchShenzhenCargo/Index', auth: ['BatchSearchShenzhenCargo'] },
                { id: 'BatchSearchTax', text: '批量稅金查詢', url: '~/BatchSearchTax/Index', auth: ['BatchSearchTax'] },
                { id: 'BatchSearchCargoShopee', text: '蝦皮預約入倉明細表', url: '~/Cargo/BatchSearchCargoShopee', auth: ['BatchSearchCargoShopee'] },
                { id: 'BatchUploadProcess', text: '處置說明批次上傳', url: '~/BatchUploadProcess/Index', auth: ['BatchUploadProcess'] },
                { id: 'DownloadProcess', text: '處置說明下載', url: '~/Cargo/DownloadProcess', auth: ['DownloadProcess'] },
                //{ id: 'EtlErrorWork', text: '空快錯單統計及明細下載', url: '~/WorkLoad/EtlErrorWork', auth: ['DownloadEtlErrorReport'] },
                { id: 'EtlErrorWork', text: '空快錯單統計及明細下載', url: '~/EtlErrorWork/Index', auth: ['DownloadEtlErrorReport'] },
                { id: 'WorkDay', text: '工作天', url: '~/WorkDay/Index' },
                { id: 'WorkDayArea', text: '工作天作業地區', url: '~/WorkDayArea/Index' },
                { id: 'BusinessRegistry', text: '營業登記', url: '~/BusinessRegistry/Index' },
                { id: 'BusinessRegistryNew', text: '新營業登記', url: '~/BusinessRegistryNew/Index' },
                { id: 'Importer', text: '申報人查詢', url: '~/Importer/Index', auth: ['Importer'] },
                { id: 'AccsShopee', text: 'ACCS關貿空運查詢', url: '~/AccsShopee/Index' },
                { id: 'AccsNew', text: 'ACCS關貿空運查詢(新)', url: '~/AccsNew/Index' },
                { id: 'Ftz', text: 'FTZ空運查詢', url: '~/Ftz/Index' },
                { id: 'Tact', text: 'TACT空運查詢', url: '~/Tact/Index' },
                { id: 'Ezway', text: 'Ezway電子商務通關平台(空運)', url: '~/Ezway/Index', auth: ['Ezway'] },
                { id: 'EzwaySea', text: 'Ezway電子商務通關平台(海運)', url: '~/EzwaySea/Index', auth: ['Ezway'] }
            ]
        },
        {
            id: 'income', title: '營收', icon: 'fas fa-fw fa-chart-line', partner: 'Income', children: [
                { id: 'IncomeReport', text: '營收報表', url: '~/Income/IncomeReport', auth: ['IncomeReport'] },
                { id: 'IncomeETAReport', text: '營收報表-到港日', url: '~/Income/IncomeETAReport', auth: ['IncomeEtaReport'] },
                { id: 'IncomeDetailsReport', text: '營收總表及明細表', url: '~/Income/IncomeDetailsReport', auth: ['IncomeDetails'] }
            ]
        },
        {
            id: 'workLoad', title: '作業量', icon: 'fas fa-fw fa-chart-bar', partner: 'WorkLoad', children: [
                { id: 'CCStatusReport', text: '海空快通關狀態彙總表', url: '~/WorkLoad/CCStatusReport', auth: ['ClearanceStatusReport'] },
                { id: 'EtlCustomerWorkLoadReport', text: '空快客戶作業量報表V2', url: '~/EtlCustomerWorkLoadReport/Index', auth: ['EtlCustomerWorkLoadReport'] },
                { id: 'UploadFile', text: '上傳檔案(A03、B6F、班機派件送達)', url: '~/WorkLoad/UploadFile', auth: ['UploadFlightArrival'] },
                { id: 'UploadFileEtlBagNo', text: '上傳空快錯單袋號', url: '~/WorkLoad/UploadFileEtlBagNo', auth: ['UploadEtlErrorBagNo'] },
                { id: 'UploadFileSeaManifest', text: '上傳海快艙單號碼', url: '~/WorkLoad/UploadFileSeaManifest', auth: ['UploadSeaManifest'] },
                { id: 'UploadFileSeaBagNo', text: '上傳海快錯單', url: '~/WorkLoad/UploadFileSeaBagNo', auth: ['UploadSeaErrorBagNo'] },
                { id: 'SeaBagNoWork', text: '海快錯單作業', url: '~/WorkLoad/SeaBagNoWork', auth: ['SeaErrorWorkLoad'] },
                { id: 'BatchEditOrderSearch', text: '批量製單申報資料查詢', url: '~/BatchEditOrder/Search', auth: ['BatchSearchEditOrder'] },
                { id: 'PostClearanceUploadFile', text: '上傳後段報關費用', url: '~/PostClearance/UploadFile', auth: ['UploadPostClearance'] },
                { id: 'cptTradeVan', text: 'CPT單一入口網站查詢', url: '~/CptTradeVan/Index', auth: ['CptTradeVan'] },
                { id: 'CptStatusReport', text: 'CPT收單錯單狀況表', url: '~/CptStatusReport/Index', auth: ['CptTradeVan'] },
                { id: 'Shenzhen', text: '上傳速派及新遞物流貨號資料', url: '~/Shenzhen/Upload' },
                { id: 'TpctContainer', text: 'TPCT貨櫃動態查詢', url: '~/TpctContainer/Index' }
            ]
        },
        {
            id: 'clearanceWork', title: '清關作業', icon: 'fas fa-fw fa-clipboard-check', partner: 'ClearanceWork', children: [
                //{ id: 'ScanCargoArrivalTime', text: '輸入外車交倉時間', url: '~/ScanCargoArrivalTime/Index' },
                { id: 'PdtScanCargoArrivalTime', text: '輸入外車交倉時間', url: '~/PdtScanCargoArrivalTime/Index' },
                { id: 'EtlClearanceDetails', text: '空快清關明細表', url: '~/EtlClearanceDetails/Index', auth: ['EtlClearanceDetails'] },
                { id: 'ETLCCLMainDetails', text: '空快清關主號明細表', url: '~/CCLWork/ETLCCLMainDetails', auth: ['EtlClearanceMainDetails'] },
                { id: 'EtlErrorG', text: '空快B6F錯單G報表', url: '~/EtlErrorG/Index', auth: ['EtlErrorG'] },
                { id: 'UploadFileB6F', text: '上傳拆袋資料', url: '~/CCLWork/UploadFileB6F', auth: ['UploadUnpackingBagNo'] },
                { id: 'EtlMergeBagNo', text: '上傳空快併袋袋號資料', url: '~/EtlMergeBagNo/Upload', auth: ['UploadEtlMergeBagNo'] },
                { id: 'EtlUnpackingZ', text: '上傳空快拆袋打Z資料', url: '~/EtlUnpackingZ/Upload', auth: ['UploadEtlMergeBagNo'] },
                { id: 'B6FUnpackingDetails', text: '已拆袋明細表', url: '~/CCLWork/B6FUnpackingDetails', auth: ['UnpackingBagNoDetails'] },
                { id: 'UnpackingDetails', text: '拆袋作業明細表', url: '~/CCLWork/UnpackingDetails', auth: ['UnpackingBagNoWorkDetails'] },
                { id: 'UnpackingStatistics', text: '拆袋統計表', url: '~/UnpackingStatistics/Index', auth: ['UnpackingStatistics'] },
                { id: 'ScanCargoDetails', text: '掃貨上車交接派件公司明細表', url: '~/CCLWork/ScanCargoDetails', auth: ['ScanCargoDetails'] },
                { id: 'ScanCargoCustomerDetails', text: '掃貨上車交接客戶派件公司明細表', url: '~/ScanCargoCustomer/ScanCargoCustomerDetails', auth: ['ScanCargoCustomerDetails'] },
                { id: 'ScanCargoCustomerDiff', text: '刷槍作業差異表', url: '~/ScanCargoCustomerDiff/Index', auth: ['ScanCargoCustomerDetails'] },
                { id: 'UploadExportFlight', text: '上傳空運出口航班及出入倉時間', url: '~/ExportClearance/UploadExportFlight', auth: ['UploadEtlExportFlight'] },
                { id: 'TransferBagReport', text: '接駁袋數統計表', url: '~/TransferBagReport/Index' }
            ]
        },
        {
            id: 'seaWork', title: '海快作業', icon: 'fas fa-fw fa-ship', children: [
                { id: 'SeaWorkErrorOrder', text: '海快作業錯單', url: '~/SeaWorkErrorOrder/Index', auth: ['SeaWorkErrorOrder'] },
                { id: 'SeaWorkErrorOrderReport', text: '海快作業錯單統計報表', url: '~/SeaWorkErrorOrderReport/Index', auth: ['SeaWorkErrorOrderReport'] },
                { id: 'SeaWorkRecognizance', text: '海快作業具結', url: '~/SeaWorkRecognizance/Index', auth: ['SeaWorkRecognizance'] },
                { id: 'SeaUnreceivedOrder', text: '海快未收單明細', url: '~/SeaUnreceivedOrder/Index', auth: ['SeaUnreceivedOrder'] },
                { id: 'SeaUnboxingRecord', text: '上傳拆櫃紀錄', url: '~/SeaUnboxingRecord/Index', auth: ['SeaUnboxingRecord'] },
                { id: 'SeaTransRecord', text: '上傳傳輸異動/紀錄', url: '~/SeaTransRecord/Index', auth: ['SeaTransRecord'] }
            ]
        },
        {
            id: 'seaClearance', title: '海快正式報關', icon: 'fas fa-fw fa-file-signature', children: [
                { id: 'SeaClearanceCreate', text: '海快後段建檔', url: '~/SeaClearanceCreate/Index', auth: ['SeaClearanceCreate'] },
                { id: 'SeaClearance', text: '海快後段報關系統', url: '~/SeaClearance/Index', auth: ['SeaClearance'] },
                { id: 'CustomsBroker', text: '報驗行建檔', url: '~/CustomsBroker/Index', auth: ['CustomsBroker'] },
                { id: 'SeaClearanceProcessor', text: '負責人建檔', url: '~/SeaClearanceProcessor/Index', auth: ['SeaClearanceProcessor'] },
                { id: 'SeaClearanceCustomer', text: '客戶管理', url: '~/SeaClearanceCustomer/Index', auth: ['SeaClearanceCustomer'] },
                { id: 'Step', text: '步驟管理', url: '~/Step/Index', auth: ['Step'] },
                { id: 'AbnormalState', text: '異常狀態', url: '~/AbnormalState/Index', auth: ['AbnormalState']},
                { id: 'ApprovalCategory', text: '簽審類別管理', url: '~/ApprovalCategory/Index', auth: ['ApprovalCategory'] },
                { id: 'AuthorizationForm', text: '文件名稱管理', url: '~/AuthorizationForm/Index', auth: ['AuthorizationForm'] },
                { id: 'SeaClearanceCustTaxPayment', text: '海快後段客戶收費方式', url: '~/SeaClearanceCustTaxPayment/Index', auth: ['SeaClearanceCustTaxPayment'] },
                { id: 'SeaClearanceSjlTaxPayment', text: '海快後段捷利收費方式', url: '~/SeaClearanceSjlTaxPayment/Index', auth: ['SeaClearanceSjlTaxPayment'] },
                { id: 'SeaClearanceFee', text: '海快後段報關費用', url: '~/SeaClearanceFee/Index', auth: ['SeaClearanceFee'] }
            ]
        },
        {
            id: 'shipmentInbound', title: '貨件回倉作業', icon: 'fas fa-fw fa-warehouse', children: [
                { id: 'ShipmentInboundBatchImport', text: '貨件入庫批量上傳', url: '~/ShipmentInboundBatchImport/Index', auth: ['ShipmentInboundBatchImport'] },
                //{ id: 'ShipmentInboundReturnImport', text: '貨件導入上傳', url: '~/ShipmentInboundReturnImport/Index', auth: ['ShipmentInboundBatchImport'] },
                { id: 'ShipmentInboundProcessStage', text: '預先登記處理', url: '~/ShipmentInboundProcessStage/Index', auth: ['ShipmentInboundProcess'] },
                { id: 'ShipmentInboundProcess', text: '貨件回倉處理', url: '~/ShipmentInboundProcess/Index', auth: ['ShipmentInboundProcess'] },
                { id: 'ShipmentInboundRecord', text: '貨件紀錄查詢', url: '~/ShipmentInboundRecord/Index', auth: ['ShipmentInboundRecord'] },
                { id: 'ShipmentInboundExceptionRecord', text: '異常紀錄查詢', url: '~/ShipmentInboundExceptionRecord/Index', auth: ['ShipmentInboundRecord'] },
                { id: 'ShipmentInboundPick', text: '撿貨明細', url: '~/ShipmentInboundPick/Index', auth: ['ShipmentInboundPick'] },
                { id: 'ShipmentOutboundBatchImport', text: '貨件出庫批量上傳', url: '~/ShipmentOutboundBatchImport/Index', auth: ['ShipmentOutboundBatchImport'] },
                { id: 'ShipmentOutboundBatchImportRevoke', text: '貨件出庫取消批量上傳', url: '~/ShipmentOutboundBatchImportRevoke/Index', auth: ['ShipmentOutboundBatchImport'] },
                { id: 'ShipmentInboundLocationTransfer', text: '儲位調撥', url: '~/ShipmentInboundLocationTransfer/Index', auth: ['ShipmentInboundLocationTransfer'] },
                { id: 'ShipmentInboundWarehouseProcess', text: '倉庫處理狀態', url: '~/ShipmentInboundWarehouseProcess/Index', auth: ['ShipmentInboundWarehouseProcess'] },
            ]
        },
        {
            id: 'jetft', title: '捷穩通', icon: 'fas fa-fw fa-truck-loading', children: [
                { id: 'DeliveryAssistant', text: '派送助理', url: '~/DeliveryAssistant/Index', },
                { id: 'SjlBatchImport', text: '捷利托運資料上傳', url: '~/SjlBatchImport/Index', auth: ['SjlBatchImport'] },
                { id: 'SjlBatchImportSearch', text: '捷利托運資料查詢', url: '~/SjlBatchImport/Search', auth: ['SjlBatchImport'] },
                { id: 'SjlBilling', text: '捷利帳單', url: '~/SjlBilling/Index', auth: ['SjlBilling'] },
            ]
        },
        {
            id: 'reconciliation', title: '代收銷帳作業', icon: 'fas fa-fw fa-file-invoice', children: [
                { id: 'ReconciliationUploadInvoice', text: '代收銷帳上傳發票', url: '~/ReconciliationInvoice/UploadInvoice', auth: ['ReconciliationUploadInvoice'] },
                { id: 'ReconciliationAir', text: '代收銷帳上傳空快', url: '~/ReconciliationAir/UploadAir', auth: ['ReconciliationAir'] },
                { id: 'ReconciliationCustomerGroup', text: '代收銷帳客戶群組', url: '~/ReconciliationCustomerGroup/Index', auth: ['ReconciliationCustomerGroup'] },
                { id: 'Receivable', text: '應收未收明細', url: '~/Receivable/Index', auth: ['Receivable'] },
              ]
        },
        {
            id: 'send', title: '發送訊息', icon: 'fas fa-fw fa-paper-plane', partner: 'Send', children: [
                { id: 'ErrorOrderSend', text: '錯單發送簡訊', url: '~/ErrorOrderSend/Index', auth: ['ErrorOrderSend'] },
                { id: 'ErrorOrderSendDetail', text: '錯單發送明細', url: '~/ErrorOrderSendDetail/Index' },
                { id: 'ErrorOrderSendCustomer', text: '客戶相對應平台', url: '~/ErrorOrderSendCustomer/Index' },
                { id: 'ErrorOrderSmsMessage', text: '罐頭簡訊', url: '~/ErrorOrderSmsMessage/Index' }
            ]
        },
        {
            id: 'line', title: 'LINE', icon: 'fas fa-fw fa-comments', partner: 'Line', children: [
                { id: 'TelegramGroup', text: 'Telegram群組', url: '~/TelegramGroup/Index' },
                { id: 'SearchWork', text: '轉檔查詢', url: '~/Cargo/SearchWork', auth: ['SearchWork'] }
            ]
        },
        {
            id: 'user', title: '會員', icon: 'fas fa-fw fa-users-cog', partner: 'User', children: [
                { id: 'UserMaster', text: '帳號管理', url: '~/UserMaster/Index', auth: ['UserMaster'] },
                { id: 'Authority', text: '權限', url: '~/Authority/Index', auth: ['UserMaster'] },
                { id: 'AuthorityGroup', text: '權限群組', url: '~/AuthorityGroup/Index', auth: ['UserMaster'] }
            ]
        }
    ];

    function hasPartner(p) {
        return !p || window.UserCtx.partners.indexOf(p) !== -1;
    }

    function hasAny(list) {
        if (!list || !list.length) return true;
        return list.some(function (a) {
            return window.UserCtx.authorities.indexOf(a) !== -1;
        });
    }

    function resolveUrl(u) {
        if (!u) return '#';
        try {
            if (window.Router) {
                var pattern = /^~\/(.+)$/;
                var m = pattern.exec(u);
                var segs;
                if (m) {
                    segs = m[1].split('/');
                    if (segs.length === 1) return Router.action(segs[0], 'Index');
                    if (segs.length >= 2) return Router.action(segs[0], segs[1]);
                }
            }
        } catch (e) { /* fallback */ }
        return u.replace('~', '');
    }

    // 計算當前活動項目
    var currentPath = (window.location.pathname || '').toLowerCase().replace(/\/$/, '');
    var activeGroupId = null;
    var activeItemId = null;

    // 處理選單資料
    menus.forEach(function (group) {
        if (hasPartner(group.partner)) {
            var visibleChildren = [];
            group.children.forEach(function (item) {
                if (hasAny(item.auth)) {
                    item.url = resolveUrl(item.url);
                    // 移除 wp 變數，直接處理特殊樣式
                    if (item.text.includes('明細表') || item.text.includes('上傳')) {
                        item.styleObj = { 'white-space': 'pre-wrap' };
                    } else {
                        item.styleObj = {};
                    }
                    var itemPath = item.url.toLowerCase().replace(/\/$/, '');
                    if (itemPath === currentPath) {
                        activeGroupId = group.id;
                        activeItemId = item.id;
                        item.isActive = true;
                    }
                    visibleChildren.push(item);
                }
            });
            if (visibleChildren.length > 0) {
                group.visibleChildren = visibleChildren;
                group.isActive = (group.id === activeGroupId);
                $scope.visibleMenus.push(group);
            }
        }
    });

    // 時鐘初始化
    $(function() { 
        $('.jclock').jclock({ format: '%Y-%m-%d %H:%M:%S' }); 
    });
});
