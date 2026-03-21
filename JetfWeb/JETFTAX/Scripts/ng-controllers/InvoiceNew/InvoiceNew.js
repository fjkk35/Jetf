mainApp.controller('InvoiceNewController', ['$scope', '$http', function ($scope, $http) {

    // 初始化
    $scope.init = function () {
        $scope.isLoading = false;
        $scope.selectedFile = null;
        $scope.fileName = '';
    };

    // 檔案選擇
    $scope.onFileSelect = function (files) {
        if (files && files.length > 0) {
            $scope.selectedFile = files[0];
            $scope.fileName = files[0].name;
            $scope.$apply();
        }
    };

    // 上傳檔案
    $scope.uploadFile = function () {
        var fileInput = document.getElementById('fileTax');
        
        if (!fileInput.files || fileInput.files.length === 0) {
            swal({
                title: "請選擇檔案",
                icon: "warning"
            });
            return;
        }

        var file = fileInput.files[0];
        
        // 檢查副檔名
        var fileName = file.name;
        var fileExtension = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
        if (fileExtension !== '.xlsx') {
            swal({
                title: "副檔名需為xlsx",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var formData = new FormData();
        formData.append('file', file);

        $http.post(Router.action('InvoiceNew', 'InvoiceWorkNew'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (response) {
            // 成功回調
            var data = response.data;
            
            // 立即停止 loading
            $scope.isLoading = false;

            if (data.Redirect) {
                window.location = Router.action('Account', 'Login');
            }
            else {
                if (data.status == "success") {
                    // 重置檔案選擇
                    fileInput.value = '';
                    $scope.selectedFile = null;
                    $scope.fileName = '';

                    // 直接下載檔案
                    if (data.ReturnObject && data.ReturnObject.fileGuid && data.ReturnObject.fileName) {
                        var path = Router.action('Download', 'DownloadFile') 
                            + '?fileGuid=' + data.ReturnObject.fileGuid
                            + '&filename=' + data.ReturnObject.fileName;
                        
                        // 建立隱藏 <a> 並觸發下載
                        var link = document.createElement('a');
                        link.href = path;
                        link.download = response.data.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                    } else {
                        swal({
                            title: data.msg,
                            icon: "success"
                        });
                    }
                }
                else {
                    // 重置檔案選擇
                    fileInput.value = '';
                    $scope.selectedFile = null;
                    $scope.fileName = '';

                    swal({
                        title: data.msg,
                        icon: "error"
                    });
                }
            }
        }, function (error) {
            // 錯誤回調
            $scope.isLoading = false;

            swal({
                title: "處理失敗",
                text: "請檢查檔案格式或稍後再試",
                icon: "error"
            });
            console.error(error);
            
            // 重置檔案選擇
            fileInput.value = '';
            $scope.selectedFile = null;
            $scope.fileName = '';
        });
    };

    // 初始化
    $scope.init();
}]);
