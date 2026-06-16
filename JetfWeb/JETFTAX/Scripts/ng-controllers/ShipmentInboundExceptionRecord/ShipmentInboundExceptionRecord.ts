interface ShipmentInboundExceptionRecordSearchForm {
    inboundDateStart: Date | null;
    inboundDateEnd: Date | null;
    mainNumber: string;
    trackingNo: string;
    exceptionReasons: string[];
}

interface ShipmentInboundExceptionReasonOption {
    Value: string;
    Text: string;
}

interface ShipmentInboundExceptionReasonModalState {
    selectedMap: { [key: string]: boolean; };
}

interface ShipmentInboundExceptionRecordDownloadResponse {
    Redirect?: boolean;
    msg?: string;
    fileGuid?: string;
    fileName?: string;
}

interface ShipmentInboundExceptionRecordRequestPayload {
    InboundDateStart: string;
    InboundDateEnd: string;
    MainNumber: string;
    TrackingNo: string;
    CustCodes: string[];
    ExceptionReasons: string[];
    Page: number;
    PageSize: number;
}

mainApp.controller('ShipmentInboundExceptionRecordController', ['$scope', '$http', function ($scope: any, $http: ng.IHttpService) {
    $scope.data = [];
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

    $scope.searchForm = <ShipmentInboundExceptionRecordSearchForm>{
        inboundDateStart: null,
        inboundDateEnd: null,
        mainNumber: '',
        trackingNo: '',
        exceptionReasons: []
    };

    $scope.customerSelectAll = true;
    $scope.selectedCustCodes = [];
    $scope.customerDisplayText = '全選';
    $scope.customerDisplayFullText = '全選';
    $scope.exceptionReasonList = [];
    $scope.exceptionReasonDisplayText = '全部';
    $scope.exceptionReasonDisplayFullText = '全部';
    $scope.exceptionReasonSelectAll = true;
    $scope.exceptionReasonModal = <ShipmentInboundExceptionReasonModalState>{ selectedMap: {} };

    $scope.init = function (): void {
        $scope.loadExceptionReasonList();
    };

    $scope.loadExceptionReasonList = function (): void {
        $http.get(Router.action('ShipmentInboundExceptionRecord', 'GetExceptionReasonList'))
            .then(function (response: ng.IHttpPromiseCallbackArg<any>): void {
                var result = response.data || [];
                if (result.error) {
                    alert(result.error);
                    return;
                }

                $scope.exceptionReasonList = result;
                updateExceptionReasonDisplay();
            })
            .catch(function (): void {
                alert('載入異常原因失敗');
            });
    };

    $scope.openExceptionReasonModal = function (): void {
        syncExceptionReasonModalState();
        (<any>angular.element('#exceptionReasonSelectModal')).modal('show');
    };

    $scope.closeExceptionReasonModal = function (): void {
        (<any>angular.element('#exceptionReasonSelectModal')).modal('hide');
    };

    $scope.selectAllExceptionReasons = function (): void {
        var selectedMap: { [key: string]: boolean; } = {};
        var allValues = getAllExceptionReasonValues();

        for (var i = 0; i < allValues.length; i++) {
            selectedMap[allValues[i]] = true;
        }

        $scope.exceptionReasonModal.selectedMap = selectedMap;
    };

    $scope.toggleAllExceptionReasons = function ($event?: Event): void {
        var target = $event ? <HTMLInputElement>$event.target : null;
        var isSelected = target ? target.checked : !!$scope.exceptionReasonSelectAll;
        var selectedMap: { [key: string]: boolean; } = {};
        var allValues = getAllExceptionReasonValues();

        for (var i = 0; i < allValues.length; i++) {
            selectedMap[allValues[i]] = isSelected;
        }

        $scope.exceptionReasonSelectAll = isSelected;
        $scope.exceptionReasonModal.selectedMap = selectedMap;
        $scope.searchForm.exceptionReasons = [];
        updateExceptionReasonDisplay();
    };

    $scope.onExceptionReasonItemChanged = function (): void {
        commitExceptionReasonSelection();
    };

    $scope.confirmExceptionReasons = function (): void {
        var allValues = getAllExceptionReasonValues();
        var selectedValues = getSelectedExceptionReasonValues($scope.exceptionReasonModal.selectedMap);

        if (selectedValues.length === 0 || selectedValues.length === allValues.length) {
            $scope.searchForm.exceptionReasons = [];
        } else {
            $scope.searchForm.exceptionReasons = selectedValues;
        }

        updateExceptionReasonDisplay();
        $scope.closeExceptionReasonModal();
    };

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
        $scope.searchForm = <ShipmentInboundExceptionRecordSearchForm>{
            inboundDateStart: null,
            inboundDateEnd: null,
            mainNumber: '',
            trackingNo: '',
            exceptionReasons: []
        };

        $scope.customerSelectAll = true;
        $scope.selectedCustCodes = [];
        $scope.customerDisplayText = '全選';
        $scope.customerDisplayFullText = '全選';
        $scope.exceptionReasonSelectAll = true;
        $scope.exceptionReasonModal = <ShipmentInboundExceptionReasonModalState>{ selectedMap: {} };
        $scope.data = [];
        $scope.isSearched = false;
        $scope.recordsInfo = '';
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        updateExceptionReasonDisplay();
    };

    $scope.loadData = function (): void {
        if (!isValidDateRange()) {
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('ShipmentInboundExceptionRecord', 'SearchData'), buildRequest($scope.currentPage, parseInt($scope.pageSize, 10)))
            .then(function (response: ng.IHttpPromiseCallbackArg<any>): void {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢失敗: ' + result.error);
                    return;
                }

                $scope.data = result.Data || [];
                $scope.totalCount = result.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize, 10));
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

    $scope.exportExcel = function (): void {
        if (!isValidDateRange()) {
            return;
        }

        $scope.exporting = true;

        $http.post(Router.action('ShipmentInboundExceptionRecord', 'ExportExcel'), buildRequest(1, 10))
            .then(function (response: ng.IHttpPromiseCallbackArg<ShipmentInboundExceptionRecordDownloadResponse>): void {
                var result = response.data || {};

                if (result.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }

                if (result.msg) {
                    alert(result.msg);
                    return;
                }

                if (result.fileGuid && result.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') +
                        '?fileGuid=' + encodeURIComponent(result.fileGuid) +
                        '&fileName=' + encodeURIComponent(result.fileName);

                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = result.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            })
            .catch(function (): void {
                alert('匯出失敗，請稍後再試');
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

    function buildRequest(page: number, pageSize: number): ShipmentInboundExceptionRecordRequestPayload {
        var exceptionReasons = ($scope.searchForm.exceptionReasons || []).slice();
        var isSelectAll = !!$scope.exceptionReasonSelectAll;

        return {
            InboundDateStart: formatDate($scope.searchForm.inboundDateStart),
            InboundDateEnd: formatDate($scope.searchForm.inboundDateEnd),
            MainNumber: $scope.searchForm.mainNumber,
            TrackingNo: $scope.searchForm.trackingNo,
            CustCodes: $scope.customerSelectAll ? [] : ($scope.selectedCustCodes || []),
            ExceptionReasons: isSelectAll ? [] : exceptionReasons,
            Page: page,
            PageSize: pageSize
        };
    }

    function getAllExceptionReasonValues(): string[] {
        var values: string[] = [];
        var reasonList = <ShipmentInboundExceptionReasonOption[]>($scope.exceptionReasonList || []);

        for (var i = 0; i < reasonList.length; i++) {
            if (reasonList[i] && reasonList[i].Value) {
                values.push(reasonList[i].Value);
            }
        }

        return values;
    }

    function getCommittedExceptionReasonValues(): string[] {
        var allValues = getAllExceptionReasonValues();
        var selectedLookup: { [key: string]: boolean; } = {};
        var selectedValues = <string[]>($scope.searchForm.exceptionReasons || []);
        var normalizedValues: string[] = [];

        if ($scope.exceptionReasonSelectAll) {
            return allValues.slice();
        }

        for (var i = 0; i < selectedValues.length; i++) {
            var value = (selectedValues[i] || '').trim();
            if (value) {
                selectedLookup[value] = true;
            }
        }

        for (var j = 0; j < allValues.length; j++) {
            if (selectedLookup[allValues[j]]) {
                normalizedValues.push(allValues[j]);
            }
        }

        return normalizedValues;
    }

    function getSelectedExceptionReasonValues(selectedMap: { [key: string]: boolean; }): string[] {
        var values: string[] = [];
        var allValues = getAllExceptionReasonValues();

        for (var i = 0; i < allValues.length; i++) {
            if (selectedMap && selectedMap[allValues[i]]) {
                values.push(allValues[i]);
            }
        }

        return values;
    }

    function syncExceptionReasonModalState(): void {
        var selectedMap: { [key: string]: boolean; } = {};
        var selectedValues = getCommittedExceptionReasonValues();

        for (var i = 0; i < selectedValues.length; i++) {
            selectedMap[selectedValues[i]] = true;
        }

        $scope.exceptionReasonModal = <ShipmentInboundExceptionReasonModalState>{ selectedMap: selectedMap };
    }

    function commitExceptionReasonSelection(): void {
        var allValues = getAllExceptionReasonValues();
        var selectedValues = getSelectedExceptionReasonValues($scope.exceptionReasonModal.selectedMap);
        var isSelectAll = allValues.length > 0 && selectedValues.length === allValues.length;

        $scope.exceptionReasonSelectAll = isSelectAll;
        $scope.searchForm.exceptionReasons = isSelectAll ? [] : selectedValues;
        updateExceptionReasonDisplay();
    }

    function updateExceptionReasonDisplay(): void {
        var selectedValues = getCommittedExceptionReasonValues();
        var allValues = getAllExceptionReasonValues();

        if (allValues.length === 0 || $scope.exceptionReasonSelectAll) {
            $scope.exceptionReasonDisplayText = '全部';
            $scope.exceptionReasonDisplayFullText = '全部';
            return;
        }

        if (selectedValues.length === 0) {
            $scope.exceptionReasonDisplayText = '全部';
            $scope.exceptionReasonDisplayFullText = '全部';
            return;
        }

        var selectedTexts: string[] = [];
        var reasonList = <ShipmentInboundExceptionReasonOption[]>($scope.exceptionReasonList || []);
        var selectedLookup: { [key: string]: boolean; } = {};

        for (var i = 0; i < selectedValues.length; i++) {
            selectedLookup[selectedValues[i]] = true;
        }

        for (var j = 0; j < reasonList.length; j++) {
            if (selectedLookup[reasonList[j].Value]) {
                selectedTexts.push(reasonList[j].Text);
            }
        }

        $scope.exceptionReasonDisplayText = '已選擇 ' + selectedValues.length + ' 項';
        $scope.exceptionReasonDisplayFullText = selectedTexts.join('、');
    }

    function isValidDateRange(): boolean {
        if ($scope.searchForm.inboundDateStart && $scope.searchForm.inboundDateEnd &&
            $scope.searchForm.inboundDateStart > $scope.searchForm.inboundDateEnd) {
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

        var pageSize = parseInt($scope.pageSize, 10);
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo = '顯示第 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    }

    function formatDate(date: Date | null): string {
        if (!date) {
            return '';
        }

        var d = new Date(date);
        var month = '' + (d.getMonth() + 1);
        var day = '' + d.getDate();
        var year = d.getFullYear();

        if (month.length < 2) {
            month = '0' + month;
        }

        if (day.length < 2) {
            day = '0' + day;
        }

        return [year, month, day].join('-');
    }

    $scope.init();
}]);
