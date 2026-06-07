interface SeaShenzhenFeeManualToDlvCodUploadReturnObject {
    data?: SeaShenzhenFeeManualToDlvCodUploadFailRow[];
    message?: string;
}

interface SeaShenzhenFeeManualToDlvCodUploadResponse {
    status?: string;
    msg?: string;
    ReturnObject?: SeaShenzhenFeeManualToDlvCodUploadReturnObject;
}

interface SeaShenzhenFeeManualToDlvCodUploadFailRow {
    RowNo: number;
    FailFieldName: string;
    FailReason: string;
    TrackingNo: string;
    DlvInv: string;
    ToDlvCodText: string;
}

interface SeaShenzhenFeeManualToDlvCodScope extends ng.IScope {
    uploading: boolean;
    uploadFailData: SeaShenzhenFeeManualToDlvCodUploadFailRow[];
    uploadResult: {
        success: boolean;
        message: string;
    } | null;
    uploadFile: () => void;
}

mainApp.controller('SeaShenzhenFeeManualToDlvCodController', ['$scope', '$http', function (
    $scope: SeaShenzhenFeeManualToDlvCodScope,
    $http: ng.IHttpService
) {
    $scope.uploading = false;
    $scope.uploadFailData = [];
    $scope.uploadResult = null;

    $scope.uploadFile = function (): void {
        var fileInput = document.getElementById('seaShenzhenFeeManualToDlvCodFileInput') as HTMLInputElement;
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
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeManualToDlvCodUploadResponse>): void {
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
            .catch(function (): void {
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
            .finally(function (): void {
                $scope.uploading = false;
            });
    };

    function showError(message: string): void {
        swal({
            title: '錯誤',
            text: message,
            icon: 'error'
        });
    }
}]);
