interface SearchTaxRow {
    Id: number;
    DataDate: string;
    Source: string;
    Type: string;
    CustName: string;
    MainNumber: string;
    BagNumber: string;
    DlvInv: string;
    TaxNumber: string;
    TransName: string;
    Recipient: string;
    RecPhone: string;
    RecAddress: string;
    InDateTimeText: string;
    OutDateText: string;
    OutDateTimeText: string;
    IncludeTax: string;
    Tax1: number;
    Tax2: number;
    TotalTax: number;
    CcFee: number | null;
    Fee: number | null;
    Cod: number | null;
    ToDlvCod: string;
}

interface SearchTaxCargoStatus {
    TransModifyTimeText: string;
    TransStatusDesc: string;
}

interface SearchTaxDetail {
    Fee: SearchTaxRow | null;
    CargoStatuses: SearchTaxCargoStatus[];
}

interface SearchTaxLog {
    DlvInv: string;
    SearchTime: string;
    UserId: string;
    UserIp: string;
}

interface SearchTaxResponse<T> {
    Data?: T;
    error?: string;
}

interface SearchTaxScope extends ng.IScope {
    keyword: string;
    data: SearchTaxRow[];
    loading: boolean;
    isSearched: boolean;
    detail: SearchTaxDetail;
    detailDlvInv: string;
    logs: SearchTaxLog[];
    search: () => void;
    openDetail: (dlvInv: string) => void;
    openLogs: () => void;
}

mainApp.controller('SearchTaxController', ['$scope', '$http', function (
    $scope: SearchTaxScope,
    $http: ng.IHttpService
) {
    $scope.keyword = '';
    $scope.data = [];
    $scope.loading = false;
    $scope.isSearched = false;
    $scope.detail = { Fee: null, CargoStatuses: [] };
    $scope.detailDlvInv = '';
    $scope.logs = [];

    $scope.search = function (): void {
        $scope.loading = true;

        $http.post(Router.action('SearchTax', 'Search'), { keyword: normalizeText($scope.keyword) })
            .then(function (response: ng.IHttpResponse<SearchTaxResponse<SearchTaxRow[]>>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢失敗: ' + result.error);
                    return;
                }

                $scope.data = result.Data || [];
                $scope.isSearched = true;
            })
            .catch(function (): void {
                alert('查詢失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    // 物流貨號按鈕：載入明細後開啟視窗
    $scope.openDetail = function (dlvInv: string): void {
        dlvInv = normalizeText(dlvInv);
        if (!dlvInv) {
            return;
        }

        $http.post(Router.action('SearchTax', 'GetDetail'), { dlvInv: dlvInv })
            .then(function (response: ng.IHttpResponse<SearchTaxResponse<SearchTaxDetail>>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢明細失敗: ' + result.error);
                    return;
                }

                $scope.detailDlvInv = dlvInv;
                $scope.detail = result.Data || { Fee: null, CargoStatuses: [] };
                $('#searchTaxDetailModal').modal('show');
            })
            .catch(function (): void {
                alert('查詢明細失敗，請稍後再試');
            });
    };

    // 查詢紀錄按鈕：載入紀錄後開啟第二層視窗
    $scope.openLogs = function (): void {
        $http.post(Router.action('SearchTax', 'GetSearchLogs'), { dlvInv: $scope.detailDlvInv })
            .then(function (response: ng.IHttpResponse<SearchTaxResponse<SearchTaxLog[]>>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢紀錄失敗: ' + result.error);
                    return;
                }

                $scope.logs = result.Data || [];
                $('#searchTaxLogModal').modal('show');
            })
            .catch(function (): void {
                alert('查詢紀錄失敗，請稍後再試');
            });
    };

    function normalizeText(value: string): string {
        return value ? value.toString().trim() : '';
    }

    // 查詢紀錄疊在明細視窗上，關閉後保留明細視窗的捲動狀態
    $('#searchTaxLogModal').on('hidden.bs.modal', function (): void {
        if ($('#searchTaxDetailModal').hasClass('show')) {
            $('body').addClass('modal-open');
        }
    });
}]);
