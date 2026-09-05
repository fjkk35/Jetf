interface SeaShenzhenOriginalTaxDataTypeOption {
    Value: string;
    Text: string;
}

interface SeaShenzhenOriginalUploadReturnObject {
    data?: SeaShenzhenOriginalUploadFailRow[];
    message?: string;
}

interface SeaShenzhenOriginalUploadResponse {
    status?: string;
    msg?: string;
    ReturnObject?: SeaShenzhenOriginalUploadReturnObject;
}

interface SeaShenzhenOriginalUploadFailRow {
    RowNo: number;
    FailFieldName: string;
    FailReason: string;
    TrackingNo: string;
    BlNo: string;
    OrderNo: string;
    JetfSerial: string;
    TransTimeText: string;
    TransName: string;
    Importer: string;
    ImporterAddress: string;
    ImporterPhone: string;
    ItemName: string;
    CcText: string;
    QuantityText: string;
    Volume: number | null;
    GwText: string;
    Memo: string;
    Claimant: string;
    TaxPayment: string;
}

interface SeaShenzhenOriginalUploadScope extends ng.IScope {
    form: {
        dataDate: Date;
        dataType: string;
    };
    datePopup: { opened: boolean };
    dateOptions: any;
    uploading: boolean;
    taxDataTypeOptions: SeaShenzhenOriginalTaxDataTypeOption[];
    uploadFailData: SeaShenzhenOriginalUploadFailRow[];
    uploadResult: {
        success: boolean;
        message: string;
    } | null;
    openDatePopup: () => void;
    uploadFile: () => void;
}

mainApp.controller('SeaShenzhenOriginalController', ['$scope', '$http', function (
    $scope: SeaShenzhenOriginalUploadScope,
    $http: ng.IHttpService
) {
    function formatDate(value: Date): string {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + '-' + month + '-' + day;
    }

    function clearSelectedFile(fileInput: HTMLInputElement): void {
        if (fileInput) {
            fileInput.value = '';
        }
    }

    function getSelectedBrokerName(): string {
        for (var i = 0; i < $scope.taxDataTypeOptions.length; i++) {
            if ($scope.taxDataTypeOptions[i].Value === $scope.form.dataType) {
                return $scope.taxDataTypeOptions[i].Text;
            }
        }

        return '';
    }

    $scope.form = {
        dataDate: new Date(),
        dataType: ''
    };
    $scope.datePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.uploading = false;
    $scope.taxDataTypeOptions = [{ Value: '', Text: '請選擇' }];
    $scope.uploadFailData = [];
    $scope.uploadResult = null;

    loadTaxDataTypeOptions();

    $scope.openDatePopup = function (): void {
        $scope.datePopup.opened = true;
    };

    $scope.uploadFile = function (): void {
        var fileInput = document.getElementById('seaShenzhenOriginalFileInput') as HTMLInputElement;
        var file = fileInput && fileInput.files && fileInput.files.length > 0
            ? fileInput.files[0]
            : null;

        if (!$scope.form.dataDate) {
            swal({
                title: '錯誤',
                text: '請選擇資料日期',
                icon: 'error'
            });
            return;
        }

        if (!file) {
            swal({
                title: '錯誤',
                text: '請選擇檔案',
                icon: 'error'
            });
            return;
        }

        if (!$scope.form.dataType) {
            swal({
                title: '錯誤',
                text: '請選擇報關行',
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

        var brokerName = getSelectedBrokerName();
        var fileNameWithoutExtension = file.name.replace(/\.[^.]+$/, '');
        if (brokerName && fileNameWithoutExtension.indexOf(brokerName) < 0) {
            clearSelectedFile(fileInput);
            swal({
                title: '錯誤',
                text: '檔名需包含報關行「' + brokerName + '」',
                icon: 'error'
            });
            return;
        }

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadFailData = [];

        var formData = new FormData();
        formData.append('file', file);
        formData.append('dataDate', formatDate($scope.form.dataDate));
        formData.append('dataType', $scope.form.dataType);

        $http.post(Router.action('SeaShenzhenOriginal', 'Upload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response: ng.IHttpResponse<SeaShenzhenOriginalUploadResponse>): void {
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

                    clearSelectedFile(fileInput);
                    return;
                }

                $scope.uploadResult = {
                    success: false,
                    message: returnObj.message || data.msg || '上傳失敗'
                };

                clearSelectedFile(fileInput);

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

                clearSelectedFile(fileInput);

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

    function loadTaxDataTypeOptions(): void {
        $http.get(Router.action('SeaShenzhenOriginal', 'GetTaxDataTypeOptions'))
            .then(function (response: ng.IHttpResponse<SeaShenzhenOriginalTaxDataTypeOption[]>): void {
                $scope.taxDataTypeOptions = response.data || [{ Value: '', Text: '請選擇' }];
            })
            .catch(function (): void {
                $scope.taxDataTypeOptions = [{ Value: '', Text: '請選擇' }];
            });
    }
}]);
