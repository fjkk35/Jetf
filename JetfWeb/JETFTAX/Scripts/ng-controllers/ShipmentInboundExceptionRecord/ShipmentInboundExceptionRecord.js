mainApp.controller('ShipmentInboundExceptionRecordController', ['$scope', '$http', function ($scope, $http) {
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
        $scope.searchForm = {
            inboundDateStart: null,
            inboundDateEnd: null,
            mainNumber: '',
            trackingNo: '',
            exceptionReason: ''
        };
        $scope.customerSelectAll = true;
        $scope.selectedCustCodes = [];
        $scope.customerDisplayText = '全選';
        $scope.customerDisplayFullText = '全選';
        $scope.exceptionReasonList = [];
        $scope.init = function () {
            $scope.loadExceptionReasonList();
        };
        $scope.loadExceptionReasonList = function () {
            $http.get(Router.action('ShipmentInboundExceptionRecord', 'GetExceptionReasonList'))
                .then(function (response) {
                var result = response.data || [];
                if (result.error) {
                    alert(result.error);
                    return;
                }
                $scope.exceptionReasonList = result;
            })
                .catch(function () {
                alert('載入異常原因失敗');
            });
        };
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.search = function () {
            $scope.currentPage = 1;
            $scope.loadData();
        };
        $scope.clearSearch = function () {
            $scope.searchForm = {
                inboundDateStart: null,
                inboundDateEnd: null,
                mainNumber: '',
                trackingNo: '',
                exceptionReason: ''
            };
            $scope.customerSelectAll = true;
            $scope.selectedCustCodes = [];
            $scope.customerDisplayText = '全選';
            $scope.customerDisplayFullText = '全選';
            $scope.data = [];
            $scope.isSearched = false;
            $scope.recordsInfo = '';
            $scope.totalCount = 0;
            $scope.totalPages = 0;
        };
        $scope.loadData = function () {
            if (!isValidDateRange()) {
                return;
            }
            $scope.loading = true;
            $http.post(Router.action('ShipmentInboundExceptionRecord', 'SearchData'), buildRequest($scope.currentPage, parseInt($scope.pageSize, 10)))
                .then(function (response) {
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
                .catch(function () {
                alert('查詢失敗，請稍後再試');
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.exportExcel = function () {
            if (!isValidDateRange()) {
                return;
            }
            $scope.exporting = true;
            $http.post(Router.action('ShipmentInboundExceptionRecord', 'ExportExcel'), buildRequest(1, 10))
                .then(function (response) {
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
                .catch(function () {
                alert('匯出失敗，請稍後再試');
            })
                .finally(function () {
                $scope.exporting = false;
            });
        };
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            $scope.loadData();
        };
        $scope.changePage = function (page) {
            if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
                return;
            }
            $scope.currentPage = page;
            $scope.loadData();
        };
        $scope.previousPage = function () {
            if ($scope.currentPage > 1) {
                $scope.currentPage--;
                $scope.loadData();
            }
        };
        $scope.nextPage = function () {
            if ($scope.currentPage < $scope.totalPages) {
                $scope.currentPage++;
                $scope.loadData();
            }
        };
        $scope.getPages = function () {
            var pages = [];
            var startPage = Math.max(1, $scope.currentPage - 2);
            var endPage = Math.min($scope.totalPages, $scope.currentPage + 2);
            for (var i = startPage; i <= endPage; i++) {
                pages.push(i);
            }
            return pages;
        };
        function buildRequest(page, pageSize) {
            return {
                InboundDateStart: formatDate($scope.searchForm.inboundDateStart),
                InboundDateEnd: formatDate($scope.searchForm.inboundDateEnd),
                MainNumber: $scope.searchForm.mainNumber,
                TrackingNo: $scope.searchForm.trackingNo,
                CustCodes: $scope.customerSelectAll ? [] : ($scope.selectedCustCodes || []),
                ExceptionReason: $scope.searchForm.exceptionReason,
                Page: page,
                PageSize: pageSize
            };
        }
        function isValidDateRange() {
            if ($scope.searchForm.inboundDateStart && $scope.searchForm.inboundDateEnd &&
                $scope.searchForm.inboundDateStart > $scope.searchForm.inboundDateEnd) {
                alert('開始日期不可大於結束日期');
                return false;
            }
            return true;
        }
        function updateRecordsInfo() {
            if ($scope.totalCount === 0) {
                $scope.recordsInfo = '共 0 筆';
                return;
            }
            var pageSize = parseInt($scope.pageSize, 10);
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示第 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
        }
        function formatDate(date) {
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
