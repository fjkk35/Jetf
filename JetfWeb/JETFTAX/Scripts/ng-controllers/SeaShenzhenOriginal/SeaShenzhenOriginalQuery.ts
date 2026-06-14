interface SeaShenzhenOriginalQueryOption {
    Value: string;
    Text: string;
}

interface SeaShenzhenOriginalQuerySearchForm {
    dataDateStart: Date | null;
    dataDateEnd: Date | null;
    trackingNo: string;
    blNo: string;
    orderNo: string;
    jetfSerial: string;
    importer: string;
    importerPhone: string;
    taxPayment: string;
    dataType: string;
}

interface SeaShenzhenOriginalQueryRow {
    Id: number;
    DataDateText: string;
    DataTypeDisplay: string;
    TrackingNo: string;
    BlNo: string;
    OrderNo: string;
    JetfSerial: string;
    TransTimeText: string;
    TransName: string;
    Importer: string;
    ImporterAddress: string;
    ImporterPhone: string;
    ItemName: string;
    CcText: string;
    QuantityText: string;
    GwText: string;
    Memo: string;
    Claimant: string;
    TaxPayment: string;
}

interface SeaShenzhenOriginalQueryResponse {
    Data?: SeaShenzhenOriginalQueryRow[];
    TotalCount?: number;
    error?: string;
}

interface SeaShenzhenOriginalQueryScope extends ng.IScope {
    data: SeaShenzhenOriginalQueryRow[];
    taxPaymentOptions: SeaShenzhenOriginalQueryOption[];
    dataTypeOptions: SeaShenzhenOriginalQueryOption[];
    loading: boolean;
    isSearched: boolean;
    recordsInfo: string;
    currentPage: number;
    pageSize: string;
    totalCount: number;
    totalPages: number;
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    dateOptions: any;
    searchForm: SeaShenzhenOriginalQuerySearchForm;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    search: () => void;
    clearSearch: () => void;
    loadData: () => void;
    changePageSize: () => void;
    changePage: (page: number) => void;
    previousPage: () => void;
    nextPage: () => void;
    getPages: () => number[];
    parsePageSize: () => number;
}

mainApp.controller('SeaShenzhenOriginalQueryController', ['$scope', '$http', function (
    $scope: SeaShenzhenOriginalQueryScope,
    $http: ng.IHttpService
) {
    $scope.data = [];
    $scope.taxPaymentOptions = [{ Value: '', Text: '全部' }];
    $scope.dataTypeOptions = [{ Value: '', Text: '全部' }];
    $scope.loading = false;
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

    $scope.searchForm = {
        dataDateStart: null,
        dataDateEnd: null,
        trackingNo: '',
        blNo: '',
        orderNo: '',
        jetfSerial: '',
        importer: '',
        importerPhone: '',
        taxPayment: '',
        dataType: ''
    };

    loadTaxPaymentOptions();
    loadDataTypeOptions();

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
        $scope.searchForm = {
            dataDateStart: null,
            dataDateEnd: null,
            trackingNo: '',
            blNo: '',
            orderNo: '',
            jetfSerial: '',
            importer: '',
            importerPhone: '',
            taxPayment: '',
            dataType: ''
        };
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

        $http.post(Router.action('SeaShenzhenOriginalQuery', 'SearchData'), buildRequest($scope.currentPage, $scope.parsePageSize()))
            .then(function (response: ng.IHttpResponse<SeaShenzhenOriginalQueryResponse>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢失敗: ' + result.error);
                    return;
                }

                $scope.data = result.Data || [];
                $scope.totalCount = result.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / $scope.parsePageSize()) || 0;
                $scope.isSearched = true;
                updateRecordsInfo();
            })
            .catch(function (): void {
                alert('查詢失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.loading = false;
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

    function buildRequest(pageIndex: number, pageSize: number): any {
        return {
            DataDateStart: formatDate($scope.searchForm.dataDateStart),
            DataDateEnd: formatDate($scope.searchForm.dataDateEnd),
            TrackingNo: $scope.searchForm.trackingNo,
            BlNo: $scope.searchForm.blNo,
            OrderNo: $scope.searchForm.orderNo,
            JetfSerial: $scope.searchForm.jetfSerial,
            Importer: $scope.searchForm.importer,
            ImporterPhone: $scope.searchForm.importerPhone,
            TaxPayment: $scope.searchForm.taxPayment,
            DataType: $scope.searchForm.dataType,
            PageIndex: pageIndex,
            PageSize: pageSize
        };
    }

    function loadTaxPaymentOptions(): void {
        $http.get(Router.action('SeaShenzhenOriginalQuery', 'GetTaxPaymentOptions'))
            .then(function (response: ng.IHttpResponse<SeaShenzhenOriginalQueryOption[]>): void {
                $scope.taxPaymentOptions = response.data || [{ Value: '', Text: '全部' }];
            })
            .catch(function (): void {
                $scope.taxPaymentOptions = [{ Value: '', Text: '全部' }];
            });
    }

    function loadDataTypeOptions(): void {
        $http.get(Router.action('SeaShenzhenOriginalQuery', 'GetDataTypeOptions'))
            .then(function (response: ng.IHttpResponse<SeaShenzhenOriginalQueryOption[]>): void {
                $scope.dataTypeOptions = response.data || [{ Value: '', Text: '全部' }];
            })
            .catch(function (): void {
                $scope.dataTypeOptions = [{ Value: '', Text: '全部' }];
            });
    }

    function isValidDateRange(): boolean {
        if ($scope.searchForm.dataDateStart && $scope.searchForm.dataDateEnd &&
            $scope.searchForm.dataDateStart > $scope.searchForm.dataDateEnd) {
            alert('開始日期不可大於結束日期');
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
}]);