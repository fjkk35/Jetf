mainApp.controller('SeaShenzhenFeeDownloadController', ['$scope', '$http', function ($scope, $http) {
        function formatDataDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + month + day;
        }
        $scope.form = {
            dataDate: new Date()
        };
        $scope.datePopup = { opened: false };
        $scope.dateOptions = {
            formatYear: 'yyyy',
            maxDate: new Date(2099, 11, 31),
            minDate: new Date(2000, 0, 1),
            startingDay: 0,
            showWeeks: false
        };
        $scope.downloading = false;
        $scope.resultMessage = '';
        $scope.openDatePopup = function () {
            $scope.datePopup.opened = true;
        };
        $scope.downloadExcel = function () {
            if (!$scope.form.dataDate) {
                showError('請選擇日期');
                return;
            }
            $scope.downloading = true;
            $scope.resultMessage = '';
            $http.post(Router.action('SeaShenzhenFeeDownload', 'ExportExcel'), {
                DataDate: formatDataDate($scope.form.dataDate)
            })
                .then(function (response) {
                var data = response.data || {};
                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }
                if (data.msg) {
                    showError(data.msg);
                    return;
                }
                if (data.fileGuid && data.fileName) {
                    $scope.resultMessage = '下載檔案已產生，請確認瀏覽器下載。';
                    var downloadUrl = Router.action('Download', 'DownloadFile') + '?fileGuid=' + data.fileGuid + '&fileName=' + encodeURIComponent(data.fileName);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            })
                .catch(function () {
                showError('下載發生錯誤，請稍後再試');
            })
                .finally(function () {
                $scope.downloading = false;
            });
        };
        function showError(message) {
            $scope.resultMessage = message;
            if (typeof swal === 'function') {
                swal({
                    title: '錯誤',
                    text: message,
                    icon: 'error'
                });
                return;
            }
            alert(message);
        }
    }]);
