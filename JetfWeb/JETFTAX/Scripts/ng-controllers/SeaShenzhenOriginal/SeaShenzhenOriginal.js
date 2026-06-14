mainApp.controller('SeaShenzhenOriginalController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
        }
        function clearSelectedFile(fileInput) {
            if (fileInput) {
                fileInput.value = '';
            }
        }
        function getSelectedBrokerName() {
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
                .catch(function () {
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
                .finally(function () {
                $scope.uploading = false;
            });
        };
        function loadTaxDataTypeOptions() {
            $http.get(Router.action('SeaShenzhenOriginal', 'GetTaxDataTypeOptions'))
                .then(function (response) {
                $scope.taxDataTypeOptions = response.data || [{ Value: '', Text: '請選擇' }];
            })
                .catch(function () {
                $scope.taxDataTypeOptions = [{ Value: '', Text: '請選擇' }];
            });
        }
    }]);
