/// <reference path="../../types/global.d.ts" />
/**
 * 初始化檔案上傳狀態與送出流程。
 * @param $scope 上傳頁面的狀態與操作。
 * @param $http 傳送匯入請求的 HTTP 服務。
 */
mainApp.controller('FlightArrivalUploadController', ['$scope', '$http', function ($scope, $http) {
        $scope.uploading = false;
        /**
         * 檢查 xlsx 檔案、避免重複送出，並在請求結束後清除所選檔案與上傳狀態。
         * @returns 無回傳值。
         */
        $scope.upload = function () {
            if ($scope.uploading) {
                return;
            }
            var fileInput = document.getElementById('fileTax');
            var file = fileInput && fileInput.files && fileInput.files.length > 0
                ? fileInput.files[0]
                : null;
            if (!file || file.size === 0) {
                swal({ title: '未選擇檔案', icon: 'error' });
                return;
            }
            if (!/\.xlsx$/i.test(file.name)) {
                swal({ title: '副檔名需為xlsx', icon: 'error' });
                return;
            }
            var formData = new FormData();
            formData.append('file', file);
            $scope.uploading = true;
            $http.post(Router.action('FlightArrivalUpload', 'UploadFile'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined, 'X-Requested-With': 'XMLHttpRequest' }
            }).then(function (response) {
                // 顯示匯入結果，登入逾時時導向登入頁。
                var data = response.data || {};
                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }
                swal({
                    title: data.msg || '上傳失敗，請稍後再試',
                    icon: data.status === 'success' ? 'success' : 'error'
                });
            }).catch(function (response) {
                // 處理請求失敗，未授權時導向登入頁。
                if (response.status === 401 || (response.data && response.data.Redirect)) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }
                swal({ title: '上傳失敗，請稍後再試', icon: 'error' });
            }).finally(function () {
                // 無論請求成功或失敗，都恢復可上傳狀態並清空檔案。
                $scope.uploading = false;
                fileInput.value = '';
            });
        };
    }]);
