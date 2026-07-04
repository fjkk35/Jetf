mainApp.controller('ReconciliationUploadInvoiceController', ['$scope', '$http', function ($scope, $http) {
        function clearSelectedFile(fileInput) {
            if (fileInput) {
                fileInput.value = '';
            }
        }
        $scope.uploading = false;
        $scope.uploadResult = null;
        $scope.uploadData = [];
        $scope.uploadSummary = null;
        $scope.uploadFile = function () {
            var fileInput = document.getElementById('uploadInvoiceFileInput');
            var file = fileInput && fileInput.files && fileInput.files.length > 0
                ? fileInput.files[0]
                : null;
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
            var formData = new FormData();
            formData.append('file', file);
            $scope.uploading = true;
            $scope.uploadResult = null;
            $scope.uploadData = [];
            $scope.uploadSummary = null;
            $http.post(Router.action('ReconciliationInvoice', 'Upload'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                var data = response.data || {};
                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }
                var result = data.ReturnObject || {};
                $scope.uploadSummary = result;
                $scope.uploadData = result.Data || [];
                if (data.status === 'success') {
                    $scope.uploadResult = {
                        success: true,
                        message: result.Message || data.msg || '上傳成功'
                    };
                    swal({
                        title: '成功',
                        text: $scope.uploadResult.message,
                        icon: 'success'
                    });
                }
                else {
                    $scope.uploadResult = {
                        success: false,
                        message: result.Message || data.msg || '上傳失敗'
                    };
                    swal({
                        title: '錯誤',
                        text: $scope.uploadResult.message,
                        icon: 'error'
                    });
                }
            }).catch(function () {
                $scope.uploadResult = {
                    success: false,
                    message: '上傳失敗，請稍後再試'
                };
                $scope.uploadData = [];
                $scope.uploadSummary = null;
                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試',
                    icon: 'error'
                });
            }).finally(function () {
                $scope.uploading = false;
                clearSelectedFile(fileInput);
            });
        };
    }]);
