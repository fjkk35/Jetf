// <reference path="../../types/global.d.ts" />

interface ReconciliationIncludeTaxDownloadFormat {
    Id: number;
    FormatName: string;
}

interface ReconciliationIncludeTaxDownloadScope extends ng.IScope {
    searchForm: {
        outDateStart: Date | null;
        outDateEnd: Date | null;
        formatId: number | null;
    };
    formats: ReconciliationIncludeTaxDownloadFormat[];
    selectedCustomerMap: { [custCode: string]: boolean };
    dateOptions: any;
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    loading: boolean;
    exporting: boolean;
    init: () => void;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    exportExcel: () => void;
}

mainApp.controller('ReconciliationIncludeTaxDownloadController', ['$scope', '$http', function (
    $scope: ReconciliationIncludeTaxDownloadScope,
    $http: ng.IHttpService
) {
    function today(): Date {
        var value = new Date();
        value.setHours(0, 0, 0, 0);
        return value;
    }

    function formatDate(value: Date | null): string | null {
        return value ? moment(value).format('YYYY-MM-DD') : null;
    }

    function selectedCodes(): string[] {
        var codes: string[] = [];
        angular.forEach($scope.selectedCustomerMap, function (selected: boolean, code: string): void {
            if (selected) {
                codes.push(code);
            }
        });
        return codes.sort();
    }

    function showError(message: string): void {
        swal({ title: message, icon: 'error' });
    }

    function redirectIfNeeded(response: ApiResponse): boolean {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }

        return false;
    }

    function openDownloadFile(response: any): void {
        if (!response.fileGuid || !response.fileName) {
            return;
        }

        var path = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(response.fileGuid)
            + '&filename=' + encodeURIComponent(response.fileName);
        window.open(path);
    }

    $scope.searchForm = {
        outDateStart: today(),
        outDateEnd: today(),
        formatId: null
    };
    $scope.formats = [];
    $scope.selectedCustomerMap = {};
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.loading = false;
    $scope.exporting = false;

    $scope.init = function (): void {
        $scope.loading = true;
        $http.get(Router.action('ReconciliationIncludeTaxDownload', 'GetFormats'))
            .then(function (response: ng.IHttpResponse<ApiResponse<ReconciliationIncludeTaxDownloadFormat[]>>): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '載入格式失敗');
                    return;
                }

                $scope.formats = response.data.ReturnObject || [];
            })
            .catch(function (): void {
                showError('載入格式失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.openStartDatePopup = function (): void {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function (): void {
        $scope.endDatePopup.opened = true;
    };

    $scope.exportExcel = function (): void {
        if (!$scope.searchForm.outDateStart || !$scope.searchForm.outDateEnd) {
            showError('日期為必填，請選擇開始日期與結束日期');
            return;
        }

        if (moment($scope.searchForm.outDateStart).isAfter($scope.searchForm.outDateEnd, 'day')) {
            showError('開始日期不可晚於結束日期');
            return;
        }

        if (!$scope.searchForm.formatId) {
            showError('請選擇格式');
            return;
        }

        $scope.exporting = true;
        $http.post(Router.action('ReconciliationIncludeTaxDownload', 'ExportExcel'), {
            OutDateStart: formatDate($scope.searchForm.outDateStart),
            OutDateEnd: formatDate($scope.searchForm.outDateEnd),
            CustomerCodes: selectedCodes(),
            FormatId: $scope.searchForm.formatId
        }).then(function (response: ng.IHttpResponse<any>): void {
            var data = response.data || {};
            if (redirectIfNeeded(data)) {
                return;
            }

            if (data.msg) {
                showError(data.msg);
                return;
            }

            openDownloadFile(data);
        }).catch(function (): void {
            showError('檔案下載失敗，請稍後再試');
        }).finally(function (): void {
            $scope.exporting = false;
        });
    };
}]);
