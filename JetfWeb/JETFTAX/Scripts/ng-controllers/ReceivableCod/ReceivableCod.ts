// <reference path="../../types/global.d.ts" />

interface ReceivableCodRow {
    Id: number;
    PostingDate: string;
    Source: string;
    Type: string;
    CustomerCode: string;
    CustomerName: string;
    OutDateTime: string;
    TrackingNo: string;
    DlvInv: string;
    CodAmount: number;
    FreightFee: number;
    Fee: number;
    ReceivableAmount: number;
    ReceivedAmount: number;
    UnreceivedAmount: number;
}

interface ReceivableCodQueryResponse {
    TotalCount: number;
    Data: ReceivableCodRow[];
}

interface ReceivableCodSelectionMap {
    [custCode: string]: boolean;
}

interface ReceivableCodScope extends ng.IScope {
    searchForm: {
        signOutDateStart: Date | null;
        signOutDateEnd: Date | null;
        trackingNo: string;
        dlvInv: string;
        status: string;
    };
    dateOptions: any;
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    rows: ReceivableCodRow[];
    loading: boolean;
    exporting: boolean;
    isSearched: boolean;
    currentPage: number;
    pageSize: string;
    totalCount: number;
    totalPages: number;
    recordsInfo: string;
    selectedCustomerMap: ReceivableCodSelectionMap;
    init: () => void;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    search: () => void;
    clearSearch: () => void;
    changePageSize: () => void;
    goToPage: (page: number) => void;
    previousPage: () => void;
    nextPage: () => void;
    getPageNumbers: () => number[];
    exportExcel: () => void;
}

mainApp.controller('ReceivableCodController', ['$scope', '$http', function (
    $scope: ReceivableCodScope,
    $http: ng.IHttpService
) {
    function redirectIfNeeded(response: ApiResponse): boolean {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }

        return false;
    }

    function showError(message: string): void {
        swal({ title: message, icon: 'error' });
    }

    function parseNullableNumber(value: string): number | null {
        return value ? parseInt(value, 10) : null;
    }

    function today(): Date {
        var value = new Date();
        value.setHours(0, 0, 0, 0);
        return value;
    }

    function formatDate(value: Date | null): string | null {
        return value ? moment(value).format('YYYY-MM-DD') : null;
    }

    function validateDates(): boolean {
        if (!$scope.searchForm.signOutDateStart || !$scope.searchForm.signOutDateEnd) {
            showError('日期為必填，請選擇開始日期與結束日期');
            return false;
        }

        if (moment($scope.searchForm.signOutDateStart)
            .isAfter($scope.searchForm.signOutDateEnd, 'day')) {
            showError('開始日期不可晚於結束日期');
            return false;
        }

        return true;
    }

    function selectedCodes(selectionMap: ReceivableCodSelectionMap): string[] {
        var codes: string[] = [];
        angular.forEach(selectionMap, function (
            selected: boolean,
            code: string
        ): void {
            if (selected) {
                codes.push(code);
            }
        });
        return codes.sort();
    }

    function buildRequest(includePaging: boolean): any {
        var codes = selectedCodes($scope.selectedCustomerMap);
        var request: any = {
            SignOutDateStart: formatDate($scope.searchForm.signOutDateStart),
            SignOutDateEnd: formatDate($scope.searchForm.signOutDateEnd),
            CustomerCodes: codes.length ? codes : null,
            TrackingNo: $scope.searchForm.trackingNo,
            DlvInv: $scope.searchForm.dlvInv,
            Status: parseNullableNumber($scope.searchForm.status)
        };

        if (includePaging) {
            request.Page = $scope.currentPage;
            request.PageSize = parseInt($scope.pageSize, 10);
        }

        return request;
    }

    function updateRecordsInfo(): void {
        if ($scope.totalCount === 0) {
            $scope.recordsInfo = '共 0 筆';
            return;
        }

        var pageSize = parseInt($scope.pageSize, 10);
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo = '顯示 ' + start + ' 至 ' + end +
            ' 筆，共 ' + $scope.totalCount + ' 筆';
    }

    function loadData(): void {
        $scope.loading = true;
        $http.post(
            Router.action('ReceivableCod', 'Search'),
            buildRequest(true)
        ).then(function (
            response: ng.IHttpResponse<ApiResponse<ReceivableCodQueryResponse>>
        ): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error' || !response.data.ReturnObject) {
                showError(response.data.msg || '查詢失敗');
                return;
            }

            var result = response.data.ReturnObject;
            $scope.rows = result.Data || [];
            $scope.totalCount = result.TotalCount || 0;
            $scope.totalPages = Math.ceil(
                $scope.totalCount / parseInt($scope.pageSize, 10)) || 0;
            $scope.isSearched = true;
            updateRecordsInfo();

            if ($scope.totalPages > 0 && $scope.currentPage > $scope.totalPages) {
                $scope.currentPage = $scope.totalPages;
                loadData();
            }
        }).catch(function (): void {
            showError('查詢失敗，請稍後再試');
        }).finally(function (): void {
            $scope.loading = false;
        });
    }

    $scope.searchForm = {
        signOutDateStart: today(),
        signOutDateEnd: today(),
        trackingNo: '',
        dlvInv: '',
        status: ''
    };
    $scope.dateOptions = {
        startingDay: 1,
        showWeeks: false
    };
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.rows = [];
    $scope.loading = false;
    $scope.exporting = false;
    $scope.isSearched = false;
    $scope.currentPage = 1;
    $scope.pageSize = '20';
    $scope.totalCount = 0;
    $scope.totalPages = 0;
    $scope.recordsInfo = '';
    $scope.selectedCustomerMap = {};

    $scope.init = function (): void {
        angular.element('#ReceivableCod').addClass('active');
    };

    $scope.openStartDatePopup = function (): void {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function (): void {
        $scope.endDatePopup.opened = true;
    };

    $scope.search = function (): void {
        if (!validateDates()) {
            return;
        }

        $scope.currentPage = 1;
        loadData();
    };

    $scope.clearSearch = function (): void {
        $scope.searchForm = {
            signOutDateStart: today(),
            signOutDateEnd: today(),
            trackingNo: '',
            dlvInv: '',
            status: ''
        };
        $scope.selectedCustomerMap = {};
        $scope.currentPage = 1;
        loadData();
    };

    $scope.changePageSize = function (): void {
        $scope.currentPage = 1;
        loadData();
    };

    $scope.goToPage = function (page: number): void {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }

        $scope.currentPage = page;
        loadData();
    };

    $scope.previousPage = function (): void {
        $scope.goToPage($scope.currentPage - 1);
    };

    $scope.nextPage = function (): void {
        $scope.goToPage($scope.currentPage + 1);
    };

    $scope.getPageNumbers = function (): number[] {
        var pages: number[] = [];
        var maxVisible = 10;
        var start = Math.max(
            1,
            $scope.currentPage - Math.floor(maxVisible / 2));
        var end = Math.min($scope.totalPages, start + maxVisible - 1);
        if (end - start < maxVisible - 1) {
            start = Math.max(1, end - maxVisible + 1);
        }

        for (var page = start; page <= end; page++) {
            pages.push(page);
        }

        return pages;
    };

    $scope.exportExcel = function (): void {
        if (!validateDates()) {
            return;
        }

        var request = buildRequest(false);
        $scope.exporting = true;
        $http.post(Router.action('ReceivableCod', 'ExportExcel'), request)
            .then(function (response: ng.IHttpResponse<any>): void {
                var data = response.data || {};
                if (redirectIfNeeded(data)) {
                    return;
                }

                if (data.msg) {
                    showError(data.msg);
                    return;
                }

                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile')
                        + '?fileGuid=' + data.fileGuid
                        + '&fileName=' + encodeURIComponent(data.fileName);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            }).catch(function (): void {
                showError('下載失敗，請稍後再試');
            }).finally(function (): void {
                $scope.exporting = false;
            });
    };
}]);
