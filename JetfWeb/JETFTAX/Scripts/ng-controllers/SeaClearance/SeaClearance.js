// <reference path="../../types/global.d.ts" />
mainApp.controller('SeaClearanceController', ['$scope', '$http', function ($scope, $http) {
        function createDefaultSearchForm() {
            return {
                trackingNo: '',
                customerCode: '',
                type: '',
                postEntry: '',
                stepId: '',
                abnormalStateId: '',
                declNo: '',
                importer: ''
            };
        }
        function buildSearchRequest() {
            return {
                TrackingNo: $scope.searchForm.trackingNo,
                DeclNo: $scope.searchForm.declNo,
                CustCode: $scope.searchForm.customerCode,
                Type: $scope.searchForm.type,
                Importer: $scope.searchForm.importer,
                PostEntry: $scope.searchForm.postEntry,
                StepId: $scope.searchForm.stepId,
                AbnormalStateId: $scope.searchForm.abnormalStateId,
                Page: $scope.currentPage,
                PageSize: parseInt($scope.pageSize, 10)
            };
        }
        function openLoginPage() {
            window.location.href = Router.action('Account', 'Login');
        }
        function getListResponseData(response) {
            if (Array.isArray(response.data)) {
                return response.data;
            }
            return response.data.ReturnObject || [];
        }
        $scope.data = [];
        $scope.loading = false;
        $scope.isSearched = false;
        $scope.currentPage = 1;
        $scope.pageSize = '10';
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.searchForm = createDefaultSearchForm();
        $scope.customers = [];
        $scope.types = [];
        $scope.postEntries = [];
        $scope.steps = [];
        $scope.abnormalStates = [];
        $scope.loadCustomers = function () {
            $http.get(Router.action('SeaClearance', 'GetSeaCustomerList'))
                .then(function (response) {
                $scope.customers = Array.isArray(response.data) ? response.data : [];
            })
                .catch(function (error) {
                console.error('載入客戶清單失敗:', error);
            });
        };
        $scope.loadTypes = function () {
            $http.get(Router.action('DropDownList', 'GetSeaWarehouseTypeList'))
                .then(function (response) {
                $scope.types = Array.isArray(response.data) ? response.data : [];
            })
                .catch(function (error) {
                console.error('載入倉別清單失敗:', error);
            });
        };
        $scope.loadPostEntries = function () {
            $http.get(Router.action('DropDownList', 'GetPostEntryTypeList'))
                .then(function (response) {
                $scope.postEntries = Array.isArray(response.data) ? response.data : [];
            })
                .catch(function (error) {
                console.error('載入報關方式清單失敗:', error);
            });
        };
        $scope.loadSteps = function () {
            $http.get(Router.action('Step', 'GetAllSteps'))
                .then(function (response) {
                $scope.steps = getListResponseData(response);
            })
                .catch(function (error) {
                console.error('載入步驟清單失敗:', error);
            });
        };
        $scope.loadAbnormalStates = function () {
            $http.get(Router.action('AbnormalState', 'GetAllAbnormalStates'))
                .then(function (response) {
                $scope.abnormalStates = getListResponseData(response);
            })
                .catch(function (error) {
                console.error('載入異常狀態清單失敗:', error);
            });
        };
        $scope.search = function () {
            $scope.currentPage = 1;
            $scope.isSearched = true;
            $scope.loadData();
        };
        $scope.loadData = function () {
            $scope.loading = true;
            $http.post(Router.action('SeaClearance', 'SearchData'), buildSearchRequest())
                .then(function (response) {
                if (response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.error) {
                    swal({
                        title: '查詢失敗',
                        text: response.data.error,
                        icon: 'error'
                    });
                    return;
                }
                $scope.data = response.data.Data || response.data.data || [];
                $scope.totalCount = response.data.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize, 10));
                $scope.updateRecordsInfo();
            })
                .catch(function (error) {
                console.error('查詢失敗:', error);
                swal({
                    title: '查詢失敗',
                    text: '請稍後再試或聯繫系統管理員',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.clearSearch = function () {
            $scope.searchForm = createDefaultSearchForm();
            $scope.data = [];
            $scope.isSearched = false;
            $scope.currentPage = 1;
            $scope.totalCount = 0;
            $scope.totalPages = 0;
            $scope.recordsInfo = '';
        };
        $scope.exportExcel = function () {
            $scope.loading = true;
            $http.post(Router.action('SeaClearance', 'Excel'), buildSearchRequest())
                .then(function (response) {
                if (response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.msg) {
                    swal({
                        title: response.data.msg,
                        icon: 'error'
                    });
                    return;
                }
                var path = Router.action('Download', 'DownloadFile') +
                    '?fileGuid=' + encodeURIComponent(response.data.fileGuid || '') +
                    '&filename=' + encodeURIComponent(response.data.fileName || '');
                var link = document.createElement('a');
                link.href = path;
                link.download = response.data.fileName || 'SeaClearance.xlsx';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            })
                .catch(function (error) {
                console.error('匯出失敗:', error);
                swal({
                    title: '匯出失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            if ($scope.isSearched) {
                $scope.loadData();
            }
        };
        $scope.goToPage = function (page) {
            if (page >= 1 && page <= $scope.totalPages && page !== $scope.currentPage) {
                $scope.currentPage = page;
                $scope.loadData();
            }
        };
        $scope.previousPage = function () {
            if ($scope.currentPage > 1) {
                $scope.goToPage($scope.currentPage - 1);
            }
        };
        $scope.nextPage = function () {
            if ($scope.currentPage < $scope.totalPages) {
                $scope.goToPage($scope.currentPage + 1);
            }
        };
        $scope.getPageNumbers = function () {
            var pages = [];
            var maxVisible = 10;
            var startPage = Math.max(1, $scope.currentPage - Math.floor(maxVisible / 2));
            var endPage = Math.min($scope.totalPages, startPage + maxVisible - 1);
            if (endPage - startPage < maxVisible - 1) {
                startPage = Math.max(1, endPage - maxVisible + 1);
            }
            for (var index = startPage; index <= endPage; index++) {
                pages.push(index);
            }
            return pages;
        };
        $scope.updateRecordsInfo = function () {
            var pageSize = parseInt($scope.pageSize, 10);
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示第 ' + start + ' 到 ' + end + ' 筆資料，共 ' + $scope.totalCount + ' 筆';
        };
        $scope.openDetail = function (id) {
            var url = Router.action('SeaClearance', 'Detail') + '?id=' + id;
            window.open(url, '_blank');
        };
        $scope.loadCustomers();
        $scope.loadTypes();
        $scope.loadPostEntries();
        $scope.loadSteps();
        $scope.loadAbnormalStates();
    }]);
