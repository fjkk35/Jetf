/// <reference path="../../types/global.d.ts" />

interface ShortOverUnloadResponse {
    Redirect?: string;
    status?: string;
    msg?: string;
    fileGuid?: string;
}

interface ShortOverUnloadScope extends ng.IScope {
    uploading: boolean;
    statusMessage: string;
    statusType: 'success' | 'error' | '';
    upload: () => void;
}

mainApp.controller('ShortOverUnloadController', ['$scope', '$http', '$window', function (
    $scope: ShortOverUnloadScope,
    $http: ng.IHttpService,
    $window: ng.IWindowService
) {
    $scope.uploading = false;
    $scope.statusMessage = '';
    $scope.statusType = '';

    function showStatus(message: string, type: 'success' | 'error' | ''): void {
        $scope.statusMessage = message;
        $scope.statusType = type;
    }

    function redirectToLogin(response: ng.IHttpResponse<ShortOverUnloadResponse>): void {
        $window.location.href = response.headers('X-Login-Redirect')
            || (response.data && response.data.Redirect)
            || Router.action('Account', 'Login');
    }

    $scope.upload = function (): void {
        if ($scope.uploading) {
            return;
        }

        var form = document.getElementById('shortOverUnloadForm') as HTMLFormElement | null;
        if (!form) {
            return;
        }
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        var postData = new FormData(form);
        $scope.uploading = true;
        $scope.statusMessage = '';
        $scope.statusType = '';

        $http.post<ShortOverUnloadResponse>(Router.action('ShortOverUnload', 'Upload'), postData, {
            headers: {
                'Content-Type': undefined,
                'X-Requested-With': 'XMLHttpRequest'
            },
            transformRequest: angular.identity
        }).then(function (response: ng.IHttpResponse<ShortOverUnloadResponse>): void {
            var result = response.data || {};
            if (result.Redirect) {
                redirectToLogin(response);
                return;
            }
            if (result.status !== 'success' || !result.fileGuid) {
                showStatus(result.msg || '匯出失敗，請重新上傳。', 'error');
                return;
            }

            $window.location.href = Router.action('ShortOverUnload', 'Download')
                + '?fileGuid=' + encodeURIComponent(result.fileGuid);
        }, function (response: ng.IHttpResponse<ShortOverUnloadResponse>): void {
            if (response.status === 401 || (response.data && response.data.Redirect)
                || response.headers('X-Login-Redirect')) {
                redirectToLogin(response);
                return;
            }
            showStatus('上傳失敗，請確認登入狀態與 Excel 檔案後重試。', 'error');
        }).finally(function (): void {
            $scope.uploading = false;
        });
    };
}]);
