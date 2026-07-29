// <reference path="../../types/global.d.ts" />
mainApp.controller('ReconciliationIncludeTaxDownloadController', ['$scope', '$http', function ($scope, $http) {
        function today() {
            var value = new Date();
            value.setHours(0, 0, 0, 0);
            return value;
        }
        function formatDate(value) {
            return value ? moment(value).format('YYYY-MM-DD') : null;
        }
        function selectedCodes() {
            var codes = [];
            angular.forEach($scope.selectedCustomerMap, function (selected, code) {
                if (selected) {
                    codes.push(code);
                }
            });
            return codes.sort();
        }
        function showError(message) {
            swal({ title: message, icon: 'error' });
        }
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        function openDownloadFile(response) {
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
        $scope.init = function () {
            $scope.loading = true;
            $http.get(Router.action('ReconciliationIncludeTaxDownload', 'GetFormats'))
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '載入格式失敗');
                    return;
                }
                $scope.formats = response.data.ReturnObject || [];
            })
                .catch(function () {
                showError('載入格式失敗，請稍後再試');
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.exportExcel = function () {
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
            }).then(function (response) {
                var data = response.data || {};
                if (redirectIfNeeded(data)) {
                    return;
                }
                if (data.msg) {
                    showError(data.msg);
                    return;
                }
                openDownloadFile(data);
            }).catch(function () {
                showError('檔案下載失敗，請稍後再試');
            }).finally(function () {
                $scope.exporting = false;
            });
        };
    }]);
