mainApp.controller('SeaShenzhenOriginalController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
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
        $scope.uploading = false;
        $scope.uploadFailData = [];
        $scope.uploadResult = null;
        $scope.openDatePopup = function () {
            $scope.datePopup.opened = true;
        };
        $scope.uploadFile = function () {
            var fileInput = document.getElementById('seaShenzhenOriginalFileInput');
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
            var fileExtension = file.name.split('.').pop().toLowerCase();
            if (fileExtension !== 'xlsx') {
                swal({
                    title: '錯誤',
                    text: '副檔名需為 xlsx',
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
            $http.post(Router.action('SeaShenzhenOriginal', 'Upload'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            })
                .then(function (response) {
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
                    fileInput.value = '';
                    return;
                }
                $scope.uploadResult = {
                    success: false,
                    message: returnObj.message || data.msg || '上傳失敗'
                };
                swal({
                    title: '錯誤',
                    text: $scope.uploadResult.message,
                    icon: 'error'
                });
            })
                .catch(function () {
                $scope.uploadFailData = [];
                $scope.uploadResult = {
                    success: false,
                    message: '上傳失敗，請稍後再試'
                };
                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.uploading = false;
            });
        };
    }]);
