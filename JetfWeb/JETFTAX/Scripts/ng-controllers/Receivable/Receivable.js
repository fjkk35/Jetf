// <reference path="../../types/global.d.ts" />
mainApp.controller('ReceivableController', ['$scope', '$http', function ($scope, $http) {
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        function showError(message) {
            swal({ title: message, icon: 'error' });
        }
        function parseNullableNumber(value) {
            return value ? parseInt(value, 10) : null;
        }
        function today() {
            var value = new Date();
            value.setHours(0, 0, 0, 0);
            return value;
        }
        function formatDate(value) {
            return value ? moment(value).format('YYYY-MM-DD') : null;
        }
        function validateDates() {
            if (!$scope.searchForm.outDateStart || !$scope.searchForm.outDateEnd) {
                showError('日期為必填，請選擇開始日期與結束日期');
                return false;
            }
            if (moment($scope.searchForm.outDateStart).isAfter($scope.searchForm.outDateEnd, 'day')) {
                showError('開始日期不可晚於結束日期');
                return false;
            }
            return true;
        }
        function selectedCodes(selectionMap) {
            var codes = [];
            angular.forEach(selectionMap, function (selected, code) {
                if (selected) {
                    codes.push(code);
                }
            });
            return codes.sort();
        }
        function buildRequest(includePaging) {
            var codes = selectedCodes($scope.selectedCustomerMap);
            var request = {
                OutDateStart: formatDate($scope.searchForm.outDateStart),
                OutDateEnd: formatDate($scope.searchForm.outDateEnd),
                CustomerCodes: codes.length ? codes : null,
                Status: parseNullableNumber($scope.searchForm.status),
                CollectionType: parseNullableNumber($scope.searchForm.collectionType)
            };
            if (includePaging) {
                request.Page = $scope.currentPage;
                request.PageSize = parseInt($scope.pageSize, 10);
            }
            return request;
        }
        function updateRecordsInfo() {
            if ($scope.totalCount === 0) {
                $scope.recordsInfo = '共 0 筆';
                return;
            }
            var pageSize = parseInt($scope.pageSize, 10);
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
        }
        function loadData() {
            $scope.loading = true;
            $http.post(Router.action('Receivable', 'Search'), buildRequest(true))
                .then(function (response) {
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
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize, 10)) || 0;
                $scope.isSearched = true;
                updateRecordsInfo();
                if ($scope.totalPages > 0 && $scope.currentPage > $scope.totalPages) {
                    $scope.currentPage = $scope.totalPages;
                    loadData();
                }
            }).catch(function () {
                showError('查詢失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
        }
        $scope.searchForm = {
            outDateStart: today(),
            outDateEnd: today(),
            status: '',
            collectionType: ''
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
        $scope.init = function () {
            angular.element('#Receivable').addClass('active');
        };
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.search = function () {
            if (!validateDates()) {
                return;
            }
            $scope.currentPage = 1;
            loadData();
        };
        $scope.clearSearch = function () {
            $scope.searchForm = {
                outDateStart: today(),
                outDateEnd: today(),
                status: '',
                collectionType: ''
            };
            $scope.selectedCustomerMap = {};
            $scope.currentPage = 1;
            loadData();
        };
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            loadData();
        };
        $scope.goToPage = function (page) {
            if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
                return;
            }
            $scope.currentPage = page;
            loadData();
        };
        $scope.previousPage = function () {
            $scope.goToPage($scope.currentPage - 1);
        };
        $scope.nextPage = function () {
            $scope.goToPage($scope.currentPage + 1);
        };
        $scope.getPageNumbers = function () {
            var pages = [];
            var maxVisible = 10;
            var start = Math.max(1, $scope.currentPage - Math.floor(maxVisible / 2));
            var end = Math.min($scope.totalPages, start + maxVisible - 1);
            if (end - start < maxVisible - 1) {
                start = Math.max(1, end - maxVisible + 1);
            }
            for (var page = start; page <= end; page++) {
                pages.push(page);
            }
            return pages;
        };
        $scope.exportExcel = function () {
            if (!validateDates()) {
                return;
            }
            var request = buildRequest(false);
            $scope.exporting = true;
            $http.post(Router.action('Receivable', 'ExportExcel'), request)
                .then(function (response) {
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
            }).catch(function () {
                showError('下載失敗，請稍後再試');
            }).finally(function () {
                $scope.exporting = false;
            });
        };
    }]);
