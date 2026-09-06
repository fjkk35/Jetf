mainApp.controller('SeaShenzhenTaxController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
        }
        function clearSelectedFile(fileInput) {
            if (fileInput) {
                fileInput.value = '';
            }
        }
        function formatTimestamp(value) {
            function pad(num) {
                return ('0' + num).slice(-2);
            }
            return value.getFullYear().toString()
                + pad(value.getMonth() + 1)
                + pad(value.getDate())
                + pad(value.getHours())
                + pad(value.getMinutes())
                + pad(value.getSeconds());
        }
        function toCsvValue(value) {
            var text = value === null || value === undefined ? '' : value.toString();
            return '"' + text.replace(/"/g, '""') + '"';
        }
        $scope.form = {
            dataDate: new Date(),
            dataType: ''
        };
        $scope.datePopup = { opened: false };
        $scope.dateOptions = {
            formatYear: 'yyyy',
            maxDate: new Date(2099, 11, 31),
            minDate: new Date(2000, 0, 1),
            startingDay: 0,
            showWeeks: false
        };
        $scope.uploading = false;
        $scope.downloadingTransferExceptions = false;
        $scope.taxDataTypeOptions = [{ Value: '', Text: '請選擇' }];
        $scope.uploadFailData = [];
        $scope.transferExceptions = [];
        $scope.uploadResult = null;
        $scope.uploadSummary = null;
        loadTaxDataTypeOptions();
        $scope.openDatePopup = function () {
            $scope.datePopup.opened = true;
        };
        $scope.downloadFailData = function () {
            if (!$scope.uploadFailData || $scope.uploadFailData.length === 0) {
                return;
            }
            var lines = [
            ['Excel列號', '失敗欄位', '失敗原因', '託運單號(條碼號)', '到付金額', '稅金金額', '稅金手續費'].map(toCsvValue).join(',')
            ];
            angular.forEach($scope.uploadFailData, function (item) {
                lines.push([
                    item.RowNo,
                    item.FailFieldName,
                    item.FailReason,
                item.TrackingNo,
                item.CodText,
                item.TaxText,
                item.FeeText
                ].map(toCsvValue).join(','));
            });
            var blob = new Blob(['\ufeff' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
            var downloadUrl = URL.createObjectURL(blob);
            var anchor = document.createElement('a');
            anchor.href = downloadUrl;
            anchor.download = '新遞深圳稅金上傳失敗明細_' + formatTimestamp(new Date()) + '.csv';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            URL.revokeObjectURL(downloadUrl);
        };
        $scope.downloadTransferExceptions = function () {
            if (!$scope.transferExceptions || $scope.transferExceptions.length === 0) {
                return;
            }
            $scope.downloadingTransferExceptions = true;
            $http.post(Router.action('SeaShenzhenTax', 'ExportTransferExceptions'), {
                Exceptions: $scope.transferExceptions
            })
                .then(function (response) {
                var data = response.data || {};
                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }
                if (data.msg) {
                    swal({
                        title: '錯誤',
                        text: data.msg,
                        icon: 'error'
                    });
                    return;
                }
                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') + '?fileGuid=' + data.fileGuid + '&fileName=' + encodeURIComponent(data.fileName);
                    var anchor = document.createElement('a');
                    anchor.href = downloadUrl;
                    anchor.download = data.fileName;
                    document.body.appendChild(anchor);
                    anchor.click();
                    document.body.removeChild(anchor);
                }
            })
                .catch(function () {
                swal({
                    title: '錯誤',
                    text: '下載異常明細失敗，請聯絡系統管理員',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.downloadingTransferExceptions = false;
            });
        };
        $scope.uploadFile = function () {
            var fileInput = document.getElementById('seaShenzhenTaxFileInput');
            var file = fileInput && fileInput.files && fileInput.files.length > 0
                ? fileInput.files[0]
                : null;
            if (!$scope.form.dataDate) {
                swal({
                    title: '錯誤',
                    text: '請選擇資料日期',
                    icon: 'error'
                });
                return;
            }
            if (!$scope.form.dataType) {
                swal({
                    title: '錯誤',
                    text: '請選擇報關行',
                    icon: 'error'
                });
                return;
            }
            if (!file) {
                swal({
                    title: '錯誤',
                    text: '請選擇檔案',
                    icon: 'error'
                });
                return;
            }
            var fileExtension = file.name.split('.').pop().toLowerCase();
            if (fileExtension !== 'xlsx') {
                clearSelectedFile(fileInput);
                swal({
                    title: '錯誤',
                    text: '副檔名需為 xlsx',
                    icon: 'error'
                });
                return;
            }
            $scope.uploading = true;
            $scope.uploadResult = null;
            $scope.uploadSummary = null;
            $scope.uploadFailData = [];
            $scope.transferExceptions = [];
            var formData = new FormData();
            formData.append('file', file);
            formData.append('dataDate', formatDate($scope.form.dataDate));
            formData.append('dataType', $scope.form.dataType);
            $http.post(Router.action('SeaShenzhenTax', 'Upload'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            })
                .then(function (response) {
                var data = response.data || {};
                var returnObj = data.ReturnObject || {};
                $scope.uploadSummary = returnObj;
                $scope.uploadFailData = returnObj.Data || [];
                $scope.transferExceptions = returnObj.Exceptions || [];
                if (data.status === 'success') {
                    $scope.uploadResult = {
                        type: $scope.uploadFailData.length > 0 ? 'warning' : 'success',
                        message: returnObj.Message || data.msg || '上傳成功'
                    };
                    swal({
                        title: $scope.uploadFailData.length > 0 ? '部分成功' : '成功',
                        text: $scope.uploadResult.message,
                        icon: $scope.uploadFailData.length > 0 ? 'warning' : 'success'
                    });
                    clearSelectedFile(fileInput);
                    return;
                }
                $scope.uploadResult = {
                    type: 'error',
                    message: returnObj.Message || data.msg || '上傳失敗'
                };
                clearSelectedFile(fileInput);
                swal({
                    title: '錯誤',
                    text: $scope.uploadResult.message,
                    icon: 'error'
                });
            })
                .catch(function () {
                $scope.uploadFailData = [];
                $scope.transferExceptions = [];
                $scope.uploadSummary = null;
                $scope.uploadResult = {
                    type: 'error',
                    message: '上傳失敗，請稍後再試'
                };
                clearSelectedFile(fileInput);
                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.uploading = false;
            });
        };
        function loadTaxDataTypeOptions() {
            $http.get(Router.action('SeaShenzhenTax', 'GetTaxDataTypeOptions'))
                .then(function (response) {
                $scope.taxDataTypeOptions = response.data || [];
                $scope.form.dataType = '';
            })
                .catch(function () {
                $scope.taxDataTypeOptions = [{ Value: '', Text: '請選擇' }];
            });
        }
    }]);
