// <reference path="../../types/global.d.ts" />
mainApp.controller('DownloadIncludeTaxController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            return moment(value).format('YYYY-MM-DD');
        }
        function showError(message) {
            swal({ title: message, icon: 'error' });
        }
        function openDownloadFile(response) {
            if (!response.fileGuid || !response.fileName) {
                return;
            }
            var downloadUrl = Router.action('Download', 'DownloadFile')
                + '?fileGuid=' + encodeURIComponent(response.fileGuid)
                + '&fileName=' + encodeURIComponent(response.fileName);
            var link = document.createElement('a');
            link.href = downloadUrl;
            link.download = response.fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        $scope.form = { startDate: today, endDate: today, source: '1' };
        $scope.startDatePopup = { opened: false };
        $scope.endDatePopup = { opened: false };
        $scope.dateOptions = {
            formatYear: 'yyyy',
            maxDate: new Date(2099, 11, 31),
            minDate: new Date(2000, 0, 1),
            startingDay: 0,
            showWeeks: false
        };
        $scope.exporting = false;
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.exportExcel = function () {
            if (!$scope.form.startDate || !$scope.form.endDate) {
                showError('日期為必填，請選擇開始日期與結束日期');
                return;
            }
            if (moment($scope.form.startDate).isAfter($scope.form.endDate, 'day')) {
                showError('開始日期不可晚於結束日期');
                return;
            }
            $scope.exporting = true;
            $http.post(Router.action('DownloadIncludeTax', 'ExportExcel'), {
                StartDate: formatDate($scope.form.startDate),
                EndDate: formatDate($scope.form.endDate),
                Source: $scope.form.source
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
                openDownloadFile(data);
            }).catch(function () {
                showError('檔案下載失敗，請稍後再試');
            }).finally(function () {
                $scope.exporting = false;
            });
        };
    }]);
