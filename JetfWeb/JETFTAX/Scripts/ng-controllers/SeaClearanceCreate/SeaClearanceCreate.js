// <reference path="../../types/global.d.ts" />
mainApp.controller('SeaClearanceCreateController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            if (!value) {
                return '';
            }
            var date = new Date(value);
            var month = ('0' + (date.getMonth() + 1)).slice(-2);
            var day = ('0' + date.getDate()).slice(-2);
            return date.getFullYear() + '-' + month + '-' + day;
        }
        function openLoginPage() {
            window.location.href = Router.action('Account', 'Login');
        }
        function buildSearchRequest() {
            return {
                Page: $scope.currentPage,
                PageSize: parseInt($scope.pageSize, 10)
            };
        }
        function resetFileInput() {
            var fileElement = document.getElementById('fileTax');
            if (fileElement) {
                fileElement.value = '';
            }
        }
        $scope.data = [];
        $scope.uploadResults = [];
        $scope.dataDate = new Date();
        $scope.dataDatePopup = { opened: false };
        $scope.dateOptions = {
            formatYear: 'yyyy',
            maxDate: new Date(2099, 12, 31),
            minDate: new Date(2000, 1, 1),
            startingDay: 0,
            showWeeks: false
        };
        $scope.loading = false;
        $scope.currentPage = 1;
        $scope.pageSize = '10';
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.recordsInfo = '';
        $scope.loadData = function () {
            $scope.loading = true;
            $http.post(Router.action('SeaClearanceCreate', 'SearchData'), buildSearchRequest())
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
                $scope.data = response.data.Data || [];
                $scope.totalCount = response.data.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize, 10));
                $scope.updateRecordsInfo();
            })
                .catch(function () {
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
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            $scope.loadData();
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
            if ($scope.totalCount === 0) {
                $scope.recordsInfo = '目前無資料';
                return;
            }
            var pageSize = parseInt($scope.pageSize, 10);
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示第 ' + start + ' 到 ' + end + ' 筆資料，共 ' + $scope.totalCount + ' 筆';
        };
        $scope.openDataDatePopup = function () {
            $scope.dataDatePopup.opened = true;
        };
        $scope.uploadFile = function () {
            var form = document.getElementById('UploadFileForm');
            var formData = new FormData(form);
            formData.set('dataDate', formatDate($scope.dataDate));
            $scope.loading = true;
            $http.post(Router.action('SeaClearanceCreate', 'UploadFile'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            })
                .then(function (response) {
                if (response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'success') {
                    swal({
                        title: response.data.msg || '上傳成功',
                        icon: 'success'
                    });
                    resetFileInput();
                    $scope.currentPage = 1;
                    $scope.loadData();
                    return;
                }
                swal({
                    title: response.data.msg || '上傳失敗',
                    icon: 'error'
                });
                resetFileInput();
            })
                .catch(function () {
                swal({
                    title: '上傳失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.download = function (id) {
            $scope.loading = true;
            $http.post(Router.action('SeaClearanceCreate', 'SeaClearanceDetailExcel'), { id: id })
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
                link.download = response.data.fileName || 'SeaClearanceCreate.xlsx';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            })
                .catch(function () {
                swal({
                    title: '下載失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.uploadResult = function (id) {
            $scope.loading = true;
            $http.get(Router.action('SeaClearanceCreate', 'GetUploadResult'), {
                params: { id: id }
            })
                .then(function (response) {
                if (response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                $scope.uploadResults = Array.isArray(response.data) ? response.data : [];
                angular.element('#uploadResultModal').modal('show');
            })
                .catch(function () {
                swal({
                    title: '取得上傳結果失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        angular.element(function () {
            angular.element('#collapseSeaClearance').addClass('show');
            angular.element('#SeaClearanceCreate').addClass('active');
            $scope.loadData();
        });
    }]);
