mainApp.controller('SjlBatchImportController', function ($scope) {
    $scope.uploading = false;
    $scope.uploadFailData = [];
    $scope.uploadResult = null;

    $scope.uploadFile = function () {
        var fileInput = document.getElementById('fileInput');
        var file = fileInput.files[0];

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
            swal({
                title: '錯誤',
                text: '副檔名需為 xlsx',
                icon: 'error'
            });
            return;
        }

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadFailData = [];

        var formData = new FormData();
        formData.append('file', file);

        $.ajax({
            type: 'POST',
            url: Router.action('SjlBatchImport', 'Upload'),
            data: formData,
            cache: false,
            dataType: 'json',
            processData: false,
            contentType: false,
            success: function (data) {
                $scope.$apply(function () {
                    var returnObj = data.ReturnObject || {};
                    $scope.uploading = false;
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
                    } else {
                        $scope.uploadResult = {
                            success: false,
                            message: returnObj.message || data.msg || '上傳失敗'
                        };

                        swal({
                            title: '錯誤',
                            text: $scope.uploadResult.message,
                            icon: 'error'
                        });
                    }
                });
            },
            error: function () {
                $scope.$apply(function () {
                    $scope.uploading = false;
                    $scope.uploadFailData = [];
                    $scope.uploadResult = {
                        success: false,
                        message: '上傳失敗，請稍後再試'
                    };
                });

                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試',
                    icon: 'error'
                });
            }
        });
    };
});
