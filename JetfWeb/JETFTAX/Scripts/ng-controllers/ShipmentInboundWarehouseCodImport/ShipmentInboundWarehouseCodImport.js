// <reference path="../../types/global.d.ts" />
mainApp.controller('ShipmentInboundWarehouseCodImportController', ['$scope', '$http', function ($scope, $http) {
        function clearSelectedFile(fileInput) {
            if (fileInput) {
                fileInput.value = '';
            }
        }
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        $scope.uploading = false;
        $scope.hasUploadFailure = false;
        $scope.uploadResult = null;
        $scope.upload = function () {
            var fileInput = document.getElementById('warehouseCodFile');
            var file = fileInput && fileInput.files && fileInput.files.length > 0
                ? fileInput.files[0]
                : null;
            if (!file) {
                swal({
                    title: '提醒',
                    text: '請選擇 xlsx 檔案',
                    icon: 'warning'
                });
                return;
            }
            var fileExtension = file.name.split('.').pop();
            if (!fileExtension || fileExtension.toLowerCase() !== 'xlsx') {
                clearSelectedFile(fileInput);
                swal({
                    title: '提醒',
                    text: '副檔名需為 xlsx',
                    icon: 'warning'
                });
                return;
            }
            var formData = new FormData();
            formData.append('file', file);
            $scope.uploading = true;
            $scope.hasUploadFailure = false;
            $scope.uploadResult = null;
            $http.post(Router.action('ShipmentInboundWarehouseCodImport', 'Upload'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                var data = response.data || {};
                if (redirectIfNeeded(data)) {
                    return;
                }
                $scope.uploadResult = data;
                var result = data.ReturnObject || {};
                var isSuccess = data.status === 'success';
                $scope.hasUploadFailure = (result.FailCount || 0) > 0;
                swal({
                    title: isSuccess
                        ? ($scope.hasUploadFailure ? '上傳完成' : '成功')
                        : '失敗',
                    text: result.Message || data.msg || (isSuccess ? '上傳成功' : '上傳失敗'),
                    icon: isSuccess
                        ? ($scope.hasUploadFailure ? 'warning' : 'success')
                        : 'error'
                });
            }).catch(function () {
                $scope.hasUploadFailure = false;
                $scope.uploadResult = {
                    status: 'error',
                    msg: '上傳失敗，請稍後再試或聯繫系統管理員'
                };
                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試或聯繫系統管理員',
                    icon: 'error'
                });
            }).finally(function () {
                $scope.uploading = false;
                clearSelectedFile(fileInput);
            });
        };
    }]);
