mainApp.controller('SeaShenzhenOriginalQueryController', ['$scope', '$http', function ($scope, $http) {
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
        $scope.loadData = function () {
            if (!isValidDateRange()) {
                return;
            }
            $scope.loading = true;
            $http.post(Router.action('SeaShenzhenOriginalQuery', 'SearchData'), buildRequest($scope.currentPage, $scope.parsePageSize()))
                .then(function (response) {
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
                .catch(function () {
                alert('查詢失敗，請稍後再試');
            })
                .finally(function () {
                $scope.loading = false;
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
        $scope.parsePageSize = function () {
            return parseInt($scope.pageSize, 10);
        };
        function buildRequest(pageIndex, pageSize) {
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
        function loadTaxPaymentOptions() {
            $http.get(Router.action('SeaShenzhenOriginalQuery', 'GetTaxPaymentOptions'))
                .then(function (response) {
                $scope.taxPaymentOptions = response.data || [{ Value: '', Text: '全部' }];
            })
                .catch(function () {
                $scope.taxPaymentOptions = [{ Value: '', Text: '全部' }];
            });
        }
        function loadDataTypeOptions() {
            $http.get(Router.action('SeaShenzhenOriginalQuery', 'GetDataTypeOptions'))
                .then(function (response) {
                $scope.dataTypeOptions = response.data || [{ Value: '', Text: '全部' }];
            })
                .catch(function () {
                $scope.dataTypeOptions = [{ Value: '', Text: '全部' }];
            });
        }
        function isValidDateRange() {
            if ($scope.searchForm.dataDateStart && $scope.searchForm.dataDateEnd &&
                $scope.searchForm.dataDateStart > $scope.searchForm.dataDateEnd) {
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
            var pageSize = $scope.parsePageSize();
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示第 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
        }
        function formatDate(date) {
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
