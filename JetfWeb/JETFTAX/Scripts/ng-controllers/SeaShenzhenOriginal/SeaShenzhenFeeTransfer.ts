interface SeaShenzhenFeeTransferExceptionRow {
    Reason: string;
    MainNumber: string;
    TrackingNo: string;
    DlvInv: string;
    Recipient: string;
    RecPhone: string;
    RecAddress: string;
    Tax1: number;
    Tax2: number;
}

interface SeaShenzhenFeeTransferResult {
    DataDate: string;
    SourceCount: number;
    DeletedCount: number;
    CreatedCount: number;
    ExceptionCount: number;
    Exceptions: SeaShenzhenFeeTransferExceptionRow[];
    message?: string;
}

interface SeaShenzhenFeeTransferResponse {
    status?: string;
    msg?: string;
    ReturnObject?: SeaShenzhenFeeTransferResult;
}

interface SeaShenzhenFeeTransferExportResponse {
    fileGuid?: string;
    fileName?: string;
    msg?: string;
    Redirect?: boolean;
}

interface SeaShenzhenFeeTransferScope extends ng.IScope {
    form: {
        dataDate: Date;
    };
    datePopup: { opened: boolean };
    dateOptions: any;
    transferring: boolean;
    exportingExceptions: boolean;
    hasResult: boolean;
    resultMessage: string;
    result: SeaShenzhenFeeTransferResult;
    openDatePopup: () => void;
    transfer: () => void;
    downloadExceptions: () => void;
}

mainApp.controller('SeaShenzhenFeeTransferController', ['$scope', '$http', function (
    $scope: SeaShenzhenFeeTransferScope,
    $http: ng.IHttpService
) {
    function formatDataDate(value: Date): string {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + month + day;
    }

    $scope.form = {
        dataDate: new Date()
    };
    $scope.datePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.transferring = false;
    $scope.exportingExceptions = false;
    $scope.hasResult = false;
    $scope.resultMessage = '';
    $scope.result = createEmptyResult();

    $scope.openDatePopup = function (): void {
        $scope.datePopup.opened = true;
    };

    $scope.transfer = function (): void {
        if (!$scope.form.dataDate) {
            showMessage('error', '請選擇日期');
            return;
        }

        if (!window.confirm('確定要執行稅金轉檔？')) {
            return;
        }

        $scope.transferring = true;
        $scope.resultMessage = '';

        $http.post(Router.action('SeaShenzhenFeeTransfer', 'Transfer'), {
            DataDate: formatDataDate($scope.form.dataDate)
        })
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeTransferResponse>): void {
                var responseData = response.data || {};
                if (responseData.status !== 'success' || !responseData.ReturnObject) {
                    showMessage('error', responseData.msg || '轉檔失敗');
                    return;
                }

                $scope.result = responseData.ReturnObject;
                $scope.result.Exceptions = $scope.result.Exceptions || [];
                $scope.resultMessage = $scope.result.message || responseData.msg || '轉檔完成';
                $scope.hasResult = true;
                showMessage('success', $scope.resultMessage);
            })
            .catch(function (): void {
                showMessage('error', '轉檔失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.transferring = false;
            });
    };

    $scope.downloadExceptions = function (): void {
        if (!$scope.result.Exceptions || $scope.result.Exceptions.length === 0) {
            showMessage('error', '查無異常明細');
            return;
        }

        $scope.exportingExceptions = true;

        $http.post(Router.action('SeaShenzhenFeeTransfer', 'ExportExceptions'), {
            DataDate: $scope.result.DataDate || formatDataDate($scope.form.dataDate),
            Exceptions: $scope.result.Exceptions
        })
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeTransferExportResponse>): void {
                var data = response.data || {};

                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }

                if (data.msg) {
                    showMessage('error', data.msg);
                    return;
                }

                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') + '?fileGuid=' + data.fileGuid + '&fileName=' + encodeURIComponent(data.fileName);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            })
            .catch(function (): void {
                showMessage('error', '下載發生錯誤，請稍後再試');
            })
            .finally(function (): void {
                $scope.exportingExceptions = false;
            });
    };

    function createEmptyResult(): SeaShenzhenFeeTransferResult {
        return {
            DataDate: '',
            SourceCount: 0,
            DeletedCount: 0,
            CreatedCount: 0,
            ExceptionCount: 0,
            Exceptions: []
        };
    }

    function showMessage(type: string, text: string): void {
        if (typeof swal === 'function') {
            swal({
                title: type === 'success' ? '成功' : '錯誤',
                text: text,
                icon: type
            });
            return;
        }

        alert(text);
    }
}]);
