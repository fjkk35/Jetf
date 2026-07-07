interface ReconciliationUploadAirRow {
    RowNo: number;
    Type: string;
    MainNumber: string;
    TrackingNo: string;
    Recipient: string;
    TaxRecId: string;
    TaxBaseText: string;
    TaxText: string;
    FailReason: string;
}

interface ReconciliationUploadAirResult {
    Count?: number;
    FailCount?: number;
    CreatedCount?: number;
    UpdatedCount?: number;
    Message?: string;
    Data?: ReconciliationUploadAirRow[];
}

interface ReconciliationUploadAirResponse {
    status?: string;
    msg?: string;
    ReturnObject?: ReconciliationUploadAirResult;
    Redirect?: boolean;
}

interface ReconciliationUploadAirScope extends ng.IScope {
    selectedType: string;
    uploading: boolean;
    uploadResult: {
        success: boolean;
        message: string;
    } | null;
    uploadData: ReconciliationUploadAirRow[];
    uploadSummary: ReconciliationUploadAirResult | null;
    getAcceptedExtensions: () => string;
    getAllowedExtensionText: () => string;
    uploadFile: () => void;
}

mainApp.controller('ReconciliationUploadAirController', ['$scope', '$http', function (
    $scope: ReconciliationUploadAirScope,
    $http: ng.IHttpService
) {
    function clearSelectedFile(fileInput: HTMLInputElement | null): void {
        if (fileInput) {
            fileInput.value = '';
        }
    }

    function getAllowedExtensions(type: string): string[] {
        return type === 'TACT'
            ? ['csv']
            : ['xls', 'xlsx'];
    }

    function getAllowedExtensionText(type: string): string {
        return getAllowedExtensions(type).join('、');
    }

    $scope.selectedType = 'FTZ';
    $scope.uploading = false;
    $scope.uploadResult = null;
    $scope.uploadData = [];
    $scope.uploadSummary = null;
    $scope.getAcceptedExtensions = function (): string {
        return $scope.selectedType === 'TACT'
            ? '.csv'
            : '.xls,.xlsx';
    };
    $scope.getAllowedExtensionText = function (): string {
        return getAllowedExtensionText($scope.selectedType);
    };

    $scope.$watch('selectedType', function (): void {
        clearSelectedFile(document.getElementById('uploadAirFileInput') as HTMLInputElement);
    });

    $scope.uploadFile = function (): void {
        var fileInput = document.getElementById('uploadAirFileInput') as HTMLInputElement;
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
        if (getAllowedExtensions($scope.selectedType).indexOf(fileExtension) < 0) {
            clearSelectedFile(fileInput);
            swal({
                title: '錯誤',
                text: $scope.selectedType + ' 上傳檔案副檔名需為 ' + getAllowedExtensionText($scope.selectedType),
                icon: 'error'
            });
            return;
        }

        var formData = new FormData();
        formData.append('file', file);
        formData.append('type', $scope.selectedType);

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadData = [];
        $scope.uploadSummary = null;

        $http.post(Router.action('ReconciliationAir', 'Upload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (response: ng.IHttpResponse<ReconciliationUploadAirResponse>): void {
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
            } else {
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
        }).catch(function (): void {
            $scope.uploadResult = {
                success: false,
                message: '系統發生錯誤，請稍後再試'
            };
        }).finally(function (): void {
            $scope.uploading = false;
            clearSelectedFile(fileInput);
        });
    };
}]);
