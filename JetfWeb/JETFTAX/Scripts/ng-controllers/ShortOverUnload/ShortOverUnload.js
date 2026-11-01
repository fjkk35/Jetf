/// <reference path="../../types/global.d.ts" />
mainApp.controller('ShortOverUnloadController', ['$scope', '$http', '$window', function ($scope, $http, $window) {
        $scope.dataSourceOptions = [];
        $scope.selectedFormat = '';
        $scope.uploading = false;
        $scope.statusMessage = '';
        $scope.statusType = '';
        $scope.missingData = [];
        function showStatus(message, type) {
            $scope.statusMessage = message;
            $scope.statusType = type;
        }
        function redirectToLogin(response) {
            $window.location.href = response.headers('X-Login-Redirect')
                || (response.data && response.data.Redirect)
                || Router.action('Account', 'Login');
        }
        $scope.upload = function () {
            if ($scope.uploading || !$scope.selectedFormat) {
                return;
            }
            var form = document.getElementById('shortOverUnloadForm');
            if (!form) {
                return;
            }
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }
            var postData = new FormData(form);
            // ng-options 的原生選項值包含型別前綴，改用實際列舉值上傳。
            postData.set('format', $scope.selectedFormat);
            $scope.uploading = true;
            $scope.statusMessage = '';
            $scope.statusType = '';
            $scope.missingData = [];
            $http.post(Router.action('ShortOverUnload', 'Upload'), postData, {
                headers: {
                    'Content-Type': undefined,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                transformRequest: angular.identity
            }).then(function (response) {
                var result = response.data || {};
                if (result.Redirect) {
                    redirectToLogin(response);
                    return;
                }
                if (result.status !== 'success' || !result.fileGuid || !result.fileName) {
                    showStatus(result.msg || '匯出失敗，請重新上傳。', 'error');
                    return;
                }
                $scope.missingData = result.missingData || [];
                if ($scope.missingData.length > 0) {
                    showStatus('匯出完成，以下 ' + $scope.missingData.length + ' 項資料未取得，對應欄位保留空白。', 'warning');
                }
                else {
                    showStatus('匯出完成，已開始下載。', 'success');
                }
                $window.location.href = Router.action('Download', 'DownloadFile')
                    + '?fileGuid=' + encodeURIComponent(result.fileGuid)
                    + '&fileName=' + encodeURIComponent(result.fileName);
            }, function (response) {
                if (response.status === 401 || (response.data && response.data.Redirect)
                    || response.headers('X-Login-Redirect')) {
                    redirectToLogin(response);
                    return;
                }
                showStatus('上傳失敗，請確認登入狀態與 Excel 檔案後重試。', 'error');
            }).finally(function () {
                $scope.uploading = false;
            });
        };
        $http.get(Router.action('ShortOverUnload', 'GetDataSourceOptions'), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(function (response) {
            if (!Array.isArray(response.data)) {
                if (response.data && response.data.Redirect) {
                    redirectToLogin(response);
                    return;
                }
                showStatus('無法取得資料來源，請重新整理頁面。', 'error');
                return;
            }
            $scope.dataSourceOptions = response.data;
            $scope.selectedFormat = response.data.length > 0 ? response.data[0].Value : '';
            if (!$scope.selectedFormat) {
                showStatus('無可用的資料來源。', 'error');
            }
        }, function (response) {
            if (response.status === 401 || (response.data && response.data.Redirect)
                || response.headers('X-Login-Redirect')) {
                redirectToLogin(response);
                return;
            }
            showStatus('無法取得資料來源，請重新整理頁面。', 'error');
        });
    }]);
