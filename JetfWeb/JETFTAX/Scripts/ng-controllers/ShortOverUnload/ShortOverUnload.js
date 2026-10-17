/// <reference path="../../types/global.d.ts" />
mainApp.controller('ShortOverUnloadController', ['$scope', '$http', '$window', function ($scope, $http, $window) {
        $scope.uploading = false;
        $scope.statusMessage = '';
        $scope.statusType = '';
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
            if ($scope.uploading) {
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
            $scope.uploading = true;
            $scope.statusMessage = '';
            $scope.statusType = '';
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
                if (result.status !== 'success' || !result.fileGuid) {
                    showStatus(result.msg || '匯出失敗，請重新上傳。', 'error');
                    return;
                }
                $window.location.href = Router.action('ShortOverUnload', 'Download')
                    + '?fileGuid=' + encodeURIComponent(result.fileGuid);
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
    }]);
