// <reference path="../../types/global.d.ts" />
mainApp.controller('DownloadNoIncludeTaxController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
        }
        function showError(message) {
            swal({
                title: message,
                icon: 'error'
            });
        }
        function createFormData() {
            // 將 datepicker 的 Date 轉成後端可直接解析的日期字串。
            var form = document.getElementById('DownloadNoIncludeTaxForm');
            var formData = new FormData(form);
            formData.set('sDate', formatDate($scope.form.sDate));
            formData.set('eDate', formatDate($scope.form.eDate));
            formData.set('source', $scope.form.source);
            return formData;
        }
        function openDownloadFile(response) {
            if (!response.fileGuid || !response.fileName) {
                return;
            }
            // 使用後端回傳的一次性 TempData handle 開啟既有下載 action。
            var path = Router.action('Download', 'DownloadFile')
                + '?fileGuid=' + encodeURIComponent(response.fileGuid)
                + '&filename=' + encodeURIComponent(response.fileName);
            window.open(path);
        }
        var today = new Date();
        $scope.form = {
            sDate: today,
            eDate: today,
            source: '1'
        };
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
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.download = function () {
            // step1: 前端先檢查必要條件，避免送出無效查詢。
            if (!$scope.form.sDate || !$scope.form.eDate) {
                showError('請選擇日期');
                return;
            }
            if ($scope.form.sDate > $scope.form.eDate) {
                showError('結束日期不可早於開始日期');
                return;
            }
            $scope.loading = true;
            // step2: 後端產生檔案並回傳 TempData handle。
            $http.post(Router.action('DownloadNoIncludeTax', 'NoIncludeTaxExcel'), createFormData(), {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                var data = response.data || {};
                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }
                if (data.msg) {
                    showError(data.msg);
                    return;
                }
                // step3: 使用 handle 開啟下載視窗，實際檔案不經過 JSON 傳輸。
                openDownloadFile(data);
            }).catch(function () {
                showError('檔案下載失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
        };
    }]);
