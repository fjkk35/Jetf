interface SeaShenzhenFeeQueryOption {
    Value: string;
    Text: string;
}

interface SeaShenzhenFeeQuerySearchForm {
    dataDateStart: Date | null;
    dataDateEnd: Date | null;
    trackingNo: string;
    dlvInv: string;
    includeTax: string;
    dataType: string;
}

interface SeaShenzhenFeeQueryRow {
    Id: number;
    DataDateText: string;
    CustomerName: string;
    DataTypeDisplay: string;
    DlvCom: string;
    TrackingNo: string;
    DlvInv: string;
    IncludeTaxDisplay: string;
    Tax: number;
    Cod: number;
    Fee: number;
    ToDlvCod: number;
    ManualToDlvCod: number | null;
}

interface SeaShenzhenFeeQueryResponse {
    Data?: SeaShenzhenFeeQueryRow[];
    TotalCount?: number;
    error?: string;
}

interface SeaShenzhenFeeQueryExportResponse {
    fileGuid?: string;
    fileName?: string;
    msg?: string;
    Redirect?: boolean;
}

interface SeaShenzhenFeeQueryScope extends ng.IScope {
    data: SeaShenzhenFeeQueryRow[];
    taxPaymentOptions: SeaShenzhenFeeQueryOption[];
    dataTypeOptions: SeaShenzhenFeeQueryOption[];
    loading: boolean;
    exporting: boolean;
    isSearched: boolean;
    recordsInfo: string;
    currentPage: number;
    pageSize: string;
    totalCount: number;
    totalPages: number;
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    dateOptions: any;
    searchForm: SeaShenzhenFeeQuerySearchForm;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    search: () => void;
    clearSearch: () => void;
    loadData: () => void;
    exportExcel: () => void;
    changePageSize: () => void;
    changePage: (page: number) => void;
    previousPage: () => void;
    nextPage: () => void;
    getPages: () => number[];
    parsePageSize: () => number;
}

mainApp.controller('SeaShenzhenFeeQueryController', ['$scope', '$http', function (
    $scope: SeaShenzhenFeeQueryScope,
    $http: ng.IHttpService
) {
    $scope.data = [];
    $scope.taxPaymentOptions = [{ Value: '', Text: '全部' }];
    $scope.dataTypeOptions = [{ Value: '', Text: '全部' }];
    $scope.loading = false;
    $scope.exporting = false;
    $scope.isSearched = false;
    $scope.recordsInfo = '';
    $scope.currentPage = 1;
    $scope.pageSize = '10';
    $scope.totalCount = 0;
    $scope.totalPages = 0;

    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };

    $scope.searchForm = createSearchForm();

    $scope.openStartDatePopup = function (): void {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function (): void {
        $scope.endDatePopup.opened = true;
    };

    $scope.search = function (): void {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    $scope.clearSearch = function (): void {
        $scope.searchForm = createSearchForm();
        $scope.data = [];
        $scope.isSearched = false;
        $scope.recordsInfo = '';
        $scope.currentPage = 1;
        $scope.totalCount = 0;
        $scope.totalPages = 0;
    };

    $scope.loadData = function (): void {
        if (!isValidDateRange()) {
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('SeaShenzhenFeeQuery', 'SearchData'), buildRequest($scope.currentPage, $scope.parsePageSize()))
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeQueryResponse>): void {
                var result = response.data || {};
                if (result.error) {
                    showError('查詢失敗: ' + result.error);
                    return;
                }

                $scope.data = result.Data || [];
                $scope.totalCount = result.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / $scope.parsePageSize()) || 0;
                $scope.isSearched = true;
                updateRecordsInfo();
            })
            .catch(function (): void {
                showError('查詢失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.exportExcel = function (): void {
        if (!isValidDateRange()) {
            return;
        }

        $scope.exporting = true;

        $http.post(Router.action('SeaShenzhenFeeQuery', 'ExportExcel'), buildRequest($scope.currentPage, $scope.parsePageSize()))
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeQueryExportResponse>): void {
                var data = response.data || {};

                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }

                if (data.msg) {
                    showError(data.msg);
                    return;
                }

                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') + '?fileGuid=' + data.fileGuid + '&fileName=' + encodeURIComponent(data.fileName);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            })
            .catch(function (): void {
                showError('下載發生錯誤，請稍後再試');
            })
            .finally(function (): void {
                $scope.exporting = false;
            });
    };

    $scope.changePageSize = function (): void {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    $scope.changePage = function (page: number): void {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }

        $scope.currentPage = page;
        $scope.loadData();
    };

    $scope.previousPage = function (): void {
        if ($scope.currentPage > 1) {
            $scope.currentPage--;
            $scope.loadData();
        }
    };

    $scope.nextPage = function (): void {
        if ($scope.currentPage < $scope.totalPages) {
            $scope.currentPage++;
            $scope.loadData();
        }
    };

    $scope.getPages = function (): number[] {
        var pages: number[] = [];
        var startPage = Math.max(1, $scope.currentPage - 2);
        var endPage = Math.min($scope.totalPages, $scope.currentPage + 2);

        for (var i = startPage; i <= endPage; i++) {
            pages.push(i);
        }

        return pages;
    };

    $scope.parsePageSize = function (): number {
        return parseInt($scope.pageSize, 10);
    };

    loadTaxPaymentOptions();
    loadDataTypeOptions();

    function loadTaxPaymentOptions(): void {
        $http.get(Router.action('SeaShenzhenFeeQuery', 'GetTaxPaymentOptions'))
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeQueryOption[]>): void {
                $scope.taxPaymentOptions = response.data || [{ Value: '', Text: '全部' }];
            })
            .catch(function (): void {
                $scope.taxPaymentOptions = [{ Value: '', Text: '全部' }];
            });
    }

    function loadDataTypeOptions(): void {
        $http.get(Router.action('SeaShenzhenFeeQuery', 'GetDataTypeOptions'))
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeQueryOption[]>): void {
                $scope.dataTypeOptions = response.data || [{ Value: '', Text: '全部' }];
            })
            .catch(function (): void {
                $scope.dataTypeOptions = [{ Value: '', Text: '全部' }];
            });
    }

    function createSearchForm(): SeaShenzhenFeeQuerySearchForm {
        return {
            dataDateStart: null,
            dataDateEnd: null,
            trackingNo: '',
            dlvInv: '',
            includeTax: '',
            dataType: ''
        };
    }

    function buildRequest(pageIndex: number, pageSize: number): any {
        return {
            DataDateStart: formatDate($scope.searchForm.dataDateStart),
            DataDateEnd: formatDate($scope.searchForm.dataDateEnd),
            TrackingNo: $scope.searchForm.trackingNo,
            DlvInv: $scope.searchForm.dlvInv,
            IncludeTax: $scope.searchForm.includeTax,
            DataType: $scope.searchForm.dataType,
            PageIndex: pageIndex,
            PageSize: pageSize
        };
    }

    function isValidDateRange(): boolean {
        if ($scope.searchForm.dataDateStart && $scope.searchForm.dataDateEnd &&
            $scope.searchForm.dataDateStart > $scope.searchForm.dataDateEnd) {
            showError('開始日期不可大於結束日期');
            return false;
        }

        return true;
    }

    function updateRecordsInfo(): void {
        if ($scope.totalCount === 0) {
            $scope.recordsInfo = '共 0 筆';
            return;
        }

        var pageSize = $scope.parsePageSize();
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo = '顯示第 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    }

    function formatDate(date: Date | null): string {
        if (!date) {
            return '';
        }

        var currentDate = new Date(date);
        var month = '' + (currentDate.getMonth() + 1);
        var day = '' + currentDate.getDate();
        var year = currentDate.getFullYear();

        if (month.length < 2) {
            month = '0' + month;
        }

        if (day.length < 2) {
            day = '0' + day;
        }

        return [year, month, day].join('-');
    }

    function showError(message: string): void {
        if (typeof swal === 'function') {
            swal({
                title: '錯誤',
                text: message,
                icon: 'error'
            });
            return;
        }

        alert(message);
    }
}]);