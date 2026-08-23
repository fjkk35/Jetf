// <reference path="../../types/global.d.ts" />

interface ShipmentInboundWarehouseCodImportRow {
    RowNo: number;
    ShipmentNo: string;
    OrderNo: string;
    TrackingNo: string;
    DlvInv: string;
    Customer: string;
    Type: string;
    Cc: number | null;
    CcText: string;
    FailReason: string;
}

interface ShipmentInboundWarehouseCodImportResult {
    Count?: number;
    InsertedCount?: number;
    FailCount?: number;
    Message?: string;
    Data?: ShipmentInboundWarehouseCodImportRow[];
}

interface ShipmentInboundWarehouseCodImportScope extends ng.IScope {
    uploading: boolean;
    hasUploadFailure: boolean;
    uploadResult: ApiResponse<ShipmentInboundWarehouseCodImportResult> | null;
    upload: () => void;
}

mainApp.controller('ShipmentInboundWarehouseCodImportController', ['$scope', '$http', function (
    $scope: ShipmentInboundWarehouseCodImportScope,
    $http: ng.IHttpService
) {
    function clearSelectedFile(fileInput: HTMLInputElement | null): void {
        if (fileInput) {
            fileInput.value = '';
        }
    }

    function redirectIfNeeded(response: ApiResponse): boolean {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }

        return false;
    }

    $scope.uploading = false;
    $scope.hasUploadFailure = false;
    $scope.uploadResult = null;

    $scope.upload = function (): void {
        var fileInput = document.getElementById('warehouseCodFile') as HTMLInputElement;
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
        }).then(function (
            response: ng.IHttpResponse<ApiResponse<ShipmentInboundWarehouseCodImportResult>>
        ): void {
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
        }).catch(function (): void {
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
        }).finally(function (): void {
            $scope.uploading = false;
            clearSelectedFile(fileInput);
        });
    };
}]);
