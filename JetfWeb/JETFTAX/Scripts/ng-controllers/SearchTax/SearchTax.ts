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
    detail: SearchTaxDetail | null;
    detailDlvInv: string;
    detailLoading: boolean;
    logs: SearchTaxLog[];
    logsLoading: boolean;
    showLogs: boolean;
    search: () => void;
    clearSearch: () => void;
    openDetail: (dlvInv: string) => void;
    toggleLogs: () => void;
}

mainApp.controller('SearchTaxController', ['$scope', '$http', function (
    $scope: SearchTaxScope,
    $http: ng.IHttpService
) {
    $scope.keyword = '';
    $scope.data = [];
    $scope.loading = false;
    $scope.isSearched = false;
    $scope.detail = null;
    $scope.detailDlvInv = '';
    $scope.detailLoading = false;
    $scope.logs = [];
    $scope.logsLoading = false;
    $scope.showLogs = false;

    $scope.search = function (): void {
        var keyword = normalizeText($scope.keyword);
        if (!keyword) {
            alert('請輸入物流貨號、分提單號或袋號');
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('SearchTax', 'Search'), { keyword: keyword })
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

    $scope.clearSearch = function (): void {
        $scope.keyword = '';
        $scope.data = [];
        $scope.isSearched = false;
    };

    $scope.openDetail = function (dlvInv: string): void {
        dlvInv = normalizeText(dlvInv);
        if (!dlvInv) {
            return;
        }

        $scope.detailDlvInv = dlvInv;
        $scope.detail = null;
        $scope.detailLoading = true;
        $scope.logs = [];
        $scope.showLogs = false;
        $('#searchTaxDetailModal').modal('show');

        $http.post(Router.action('SearchTax', 'GetDetail'), { dlvInv: dlvInv })
            .then(function (response: ng.IHttpResponse<SearchTaxResponse<SearchTaxDetail>>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢明細失敗: ' + result.error);
                    return;
                }

                $scope.detail = result.Data || { Fee: null, CargoStatuses: [] };
            })
            .catch(function (): void {
                alert('查詢明細失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.detailLoading = false;
            });
    };

    // 明細視窗內展開/收合查詢紀錄；每次展開重新讀取
    $scope.toggleLogs = function (): void {
        if ($scope.showLogs) {
            $scope.showLogs = false;
            return;
        }

        if (!$scope.detailDlvInv) {
            return;
        }

        $scope.showLogs = true;
        $scope.logs = [];
        $scope.logsLoading = true;

        $http.post(Router.action('SearchTax', 'GetSearchLogs'), { dlvInv: $scope.detailDlvInv })
            .then(function (response: ng.IHttpResponse<SearchTaxResponse<SearchTaxLog[]>>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢紀錄失敗: ' + result.error);
                    return;
                }

                $scope.logs = result.Data || [];
            })
            .catch(function (): void {
                alert('查詢紀錄失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.logsLoading = false;
            });
    };

    function normalizeText(value: string): string {
        return value ? value.toString().trim() : '';
    }
}]);
