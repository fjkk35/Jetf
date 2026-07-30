interface ReconciliationTaxDlvInvAdjustmentRow {
    RowNo: number;
    TrackingNo: string;
    OldDlvInv: string;
    NewDlvInv: string;
    IsSuccess: boolean;
    Status: string;
}

interface ReconciliationTaxDlvInvAdjustmentResult {
    Message: string;
    Data: ReconciliationTaxDlvInvAdjustmentRow[];
}

interface ReconciliationTaxDlvInvAdjustmentResponse {
    status?: string;
    msg?: string;
    ReturnObject?: ReconciliationTaxDlvInvAdjustmentResult;
    Redirect?: boolean;
}

interface ReconciliationTaxDlvInvAdjustmentScope extends ng.IScope {
    uploading: boolean;
    uploadSummary: ReconciliationTaxDlvInvAdjustmentResult | null;
    uploadData: ReconciliationTaxDlvInvAdjustmentRow[];
    uploadFile: () => void;
}

mainApp.controller(
    'ReconciliationTaxDlvInvAdjustmentController',
    ['$scope', '$http', function (
        $scope: ReconciliationTaxDlvInvAdjustmentScope,
        $http: ng.IHttpService
    ): void {
        function getFileInput(): HTMLInputElement {
            return document.getElementById(
                'taxDlvInvAdjustmentFileInput'
            ) as HTMLInputElement;
        }

        function clearSelectedFile(): void {
            var fileInput = getFileInput();
            if (fileInput) {
                fileInput.value = '';
            }
        }

        $scope.uploading = false;
        $scope.uploadSummary = null;
        $scope.uploadData = [];

        $scope.uploadFile = function (): void {
            var fileInput = getFileInput();
            var file = fileInput &&
                fileInput.files &&
                fileInput.files.length > 0
                ? fileInput.files[0]
                : null;

            if (!file) {
                swal({
                    title: '錯誤',
                    text: '請選擇上傳檔案。',
                    icon: 'error'
                });
                return;
            }

            var fileExtension = file.name.split('.').pop().toLowerCase();
            if (fileExtension !== 'xlsx') {
                clearSelectedFile();
                swal({
                    title: '錯誤',
                    text: '檔案格式必須為 .xlsx。',
                    icon: 'error'
                });
                return;
            }

            var formData = new FormData();
            formData.append('file', file);
            $scope.uploading = true;
            $scope.uploadSummary = null;
            $scope.uploadData = [];

            $http.post(
                Router.action(
                    'ReconciliationTaxDlvInvAdjustment',
                    'Upload'
                ),
                formData,
                {
                    transformRequest: angular.identity,
                    headers: { 'Content-Type': undefined }
                }
            ).then(function (
                response:
                    ng.IHttpResponse<ReconciliationTaxDlvInvAdjustmentResponse>
            ): void {
                var responseData = response.data || {};
                if (responseData.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }

                var result = responseData.ReturnObject;
                if (result) {
                    $scope.uploadSummary = result;
                    $scope.uploadData = result.Data || [];
                }

                if (responseData.status !== 'success' || !result) {
                    swal({
                        title: '錯誤',
                        text: responseData.msg || '上傳失敗。',
                        icon: 'error'
                    });
                    return;
                }

                swal({
                    title: '成功',
                    text: result.Message || '上傳完成',
                    icon: 'success'
                });
            }).catch(function (): void {
                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試。',
                    icon: 'error'
                });
            }).finally(function (): void {
                $scope.uploading = false;
                clearSelectedFile();
            });
        };
    }]
);
