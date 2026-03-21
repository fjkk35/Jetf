mainApp.controller('ShipmentOutboundBatchImportRevokeController', function ($scope, $http) {
    $scope.uploading = false;
    $scope.uploadResult = null;
    $scope.uploadData = [];

    $scope.uploadFile = function () {
        var fileInput = document.getElementById('fileInput');
        var file = fileInput.files[0];

        if (!file) {
            swal({
                title: "錯誤",
                text: "請選擇檔案",
                icon: "error"
            });
            return;
        }

        var fileExtension = file.name.split('.').pop().toLowerCase();
        if (fileExtension !== 'xlsx') {
            swal({
                title: "錯誤",
                text: "副檔名需為 xlsx",
                icon: "error"
            });
            return;
        }

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadData = [];

        var formData = new FormData();
        formData.append('file', file);

        $.ajax({
            type: "POST",
            url: Router.action('ShipmentOutboundBatchImportRevoke', 'Upload'),
            data: formData,
            cache: false,
            dataType: 'json',
            processData: false,
            contentType: false,
            success: function (data) {
                $scope.$apply(function () {
                    $scope.uploading = false;

                    if (data.status === 'success') {
                        var returnObj = data.ReturnObject || {};
                        $scope.uploadData = returnObj.data || [];

                        var hasFailure = returnObj.failCount > 0;

                        $scope.uploadResult = {
                            success: !hasFailure,
                            message: returnObj.message || '上傳成功'
                        };

                        swal({
                            title: hasFailure ? "失敗" : "成功",
                            text: $scope.uploadResult.message,
                            icon: hasFailure ? "error" : "success"
                        });

                        if (!hasFailure) {
                            fileInput.value = '';
                        }
                    } else {
                        $scope.uploadResult = {
                            success: false,
                            message: data.msg || '上傳失敗'
                        };
                        swal({
                            title: "錯誤",
                            text: $scope.uploadResult.message,
                            icon: "error"
                        });
                    }
                });

                if (fileInput)
                    fileInput.value = '';
            },
            error: function (xhr, status, error) {
                $scope.$apply(function () {
                    $scope.uploading = false;
                    $scope.uploadResult = {
                        success: false,
                        message: '上傳失敗：' + error
                    };
                    $scope.uploadData = [];
                });
                swal({
                    title: "錯誤",
                    text: '上傳失敗，請稍後再試',
                    icon: "error"
                });
            }
        });
    };
});
