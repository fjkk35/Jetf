/// <reference path="../../types/global.d.ts" />

interface CoupangReportFormResponse extends ApiResponse {
    fileGuid?: string;
    fileName?: string;
}

interface CoupangReportFormScope extends ng.IScope {
    isLoading: boolean;
    upload: () => void;
}

function getCoupangReportForm(): HTMLFormElement {
    return document.getElementById('coupangReportUploadForm') as HTMLFormElement;
}

function getCoupangReportFileInput(): HTMLInputElement {
    return document.getElementById('coupangReportFile') as HTMLInputElement;
}

function buildCoupangReportDownloadUrl(fileGuid: string, fileName: string): string {
    return Router.action('Download', 'DownloadFile')
        + '?fileGuid=' + encodeURIComponent(fileGuid)
        + '&fileName=' + encodeURIComponent(fileName);
}

function resetCoupangReportFileInput(): void {
    var fileInput = getCoupangReportFileInput();
    if (fileInput) {
        fileInput.value = '';
    }
}

function hasCoupangReportFile(): boolean {
    var fileInput = getCoupangReportFileInput();
    return !!(fileInput && fileInput.files && fileInput.files.length > 0);
}

function showCoupangReportNoFileMessage(): void {
    swal({
        title: '未選擇檔案',
        icon: 'error'
    });
}

function showCoupangReportProcessError(): void {
    swal({
        title: '檔案處理失敗',
        icon: 'error'
    });
}

mainApp.controller('CoupangReportFormController', ['$scope', '$http', '$window', function (
    $scope: CoupangReportFormScope,
    $http: ng.IHttpService,
    $window: ng.IWindowService
) {
    function setActiveMenu(): void {
        angular.element('#collapseCCLWork').addClass('show');
        angular.element('#CoupangReportForm').addClass('active');
    }

    function buildDownloadUrl(fileGuid: string, fileName: string): string {
        return buildCoupangReportDownloadUrl(fileGuid, fileName);
    }

    function resetFileInput(): void {
        resetCoupangReportFileInput();
    }

    $scope.isLoading = false;
    setActiveMenu();

    $scope.upload = function (): void {
        var form = getCoupangReportForm();
        if (!hasCoupangReportFile()) {
            showCoupangReportNoFileMessage();
            return;
        }

        // 使用 AngularJS $http 搭配 FormData 上傳檔案，Content-Type 交給瀏覽器自動補 multipart boundary。
        var postData = new FormData(form);
        $scope.isLoading = true;

        $http.post(Router.action('CoupangReportForm', 'Upload'), postData, {
            headers: { 'Content-Type': undefined },
            transformRequest: angular.identity
        }).then(function (response: ng.IHttpResponse<CoupangReportFormResponse>): void {
            var result = response.data || {};

            // Session 過期時後端會回 Redirect，直接導回登入頁。
            if (result.Redirect) {
                $window.location.href = Router.action('Account', 'Login');
                return;
            }

            // 後端檔案驗證或 Excel 處理失敗時，顯示錯誤訊息並停留原頁。
            if (result.msg) {
                swal({
                    title: result.msg,
                    icon: 'error'
                });
                return;
            }

            // 成功時用既有 DownloadFile action 取回補值後的 Excel。
            if (result.fileGuid && result.fileName) {
                $window.open(buildDownloadUrl(result.fileGuid, result.fileName));
                resetFileInput();
            }
        }).catch(function (): void {
            showCoupangReportProcessError();
        }).finally(function (): void {
            $scope.isLoading = false;
        });
    };
}]);
