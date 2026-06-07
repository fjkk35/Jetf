mainApp.controller('SeaShenzhenFeeManualToDlvCodController', ['$scope', '$http', function ($scope, $http) {
    $scope.uploading = false;
    $scope.uploadFailData = [];
    $scope.uploadResult = null;
    $scope.uploadFile = function () {
        var fileInput = document.getElementById('seaShenzhenFeeManualToDlvCodFileInput');
        var file = fileInput && fileInput.files && fileInput.files.length > 0
            ? fileInput.files[0]
            : null;
        if (!file) {
            showError('請選擇檔案');
            return;
        }
        var fileExtension = file.name.split('.').pop().toLowerCase();
        if (fileExtension !== 'xlsx') {
            showError('副檔名需為 xlsx');
            return;
        }
        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadFailData = [];
        var formData = new FormData();
        formData.append('file', file);
        $http.post(Router.action('SeaShenzhenFeeManualToDlvCod', 'Upload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response) {
            var data = response.data || {};
            var returnObj = data.ReturnObject || {};
            $scope.uploadFailData = returnObj.data || [];
            if (data.status === 'success') {
                $scope.uploadResult = {
                    success: true,
                    message: returnObj.message || data.msg || '上傳成功'
                };
                swal({
                    title: '成功',
                    text: $scope.uploadResult.message,
                    icon: 'success'
                });
                fileInput.value = '';
                return;
            }
            $scope.uploadResult = {
                success: false,
                message: returnObj.message || data.msg || '上傳失敗'
            };
            swal({
                title: '錯誤',
                text: $scope.uploadResult.message,
                icon: 'error'
            });
        })
            .catch(function () {
            $scope.uploadFailData = [];
            $scope.uploadResult = {
                success: false,
                message: '上傳失敗，請稍後再試'
            };
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
    function showError(message) {
        swal({
            title: '錯誤',
            text: message,
            icon: 'error'
        });
    }
}]);
