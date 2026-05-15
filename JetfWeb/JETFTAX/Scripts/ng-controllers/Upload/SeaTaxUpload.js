// <reference path="../../types/global.d.ts" />
mainApp.controller('SeaTaxUploadController', ['$scope', '$http', function ($scope, $http) {
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
        }
        function resetFileInput() {
            var fileElement = document.getElementById('fileTax');
            if (fileElement) {
                fileElement.value = '';
            }
        }
        function openLoginPage() {
            window.location.href = Router.action('Account', 'Login');
        }
        function setActiveMenu() {
            angular.element('#collapseUpload').addClass('show');
            angular.element('#SeaTaxUploadNew').addClass('active');
        }
        function loadTaxTypeList() {
            $http.get(Router.action('SeaTaxUpload', 'GetSeaTaxTypeList'))
                .then(function (response) {
                $scope.taxTypeList = response.data || [];
                if (!$scope.form.taxType && $scope.taxTypeList.length > 0) {
                    $scope.form.taxType = $scope.taxTypeList[0].Value;
                }
            })
                .catch(function (error) {
                console.error('載入稅金類型失敗:', error);
            });
        }
        $scope.taxTypeList = [];
        $scope.form = {
            date: new Date(),
            taxType: ''
        };
        $scope.datePopup = { opened: false };
        $scope.dateOptions = {
            formatYear: 'yyyy',
            maxDate: new Date(2099, 11, 31),
            minDate: new Date(2000, 0, 1),
            startingDay: 0,
            showWeeks: false
        };
        $scope.loading = false;
        setActiveMenu();
        loadTaxTypeList();
        $scope.openDatePopup = function () {
            $scope.datePopup.opened = true;
        };
        $scope.uploadFile = function () {
            var fileElement = document.getElementById('fileTax');
            if (!$scope.form.date) {
                swal({
                    title: '請選擇日期',
                    icon: 'error'
                });
                return;
            }
            if (!$scope.form.taxType) {
                swal({
                    title: '請選擇稅金種類',
                    icon: 'error'
                });
                return;
            }
            if (!fileElement || !fileElement.files || fileElement.files.length === 0) {
                swal({
                    title: '未選擇檔案',
                    icon: 'error'
                });
                return;
            }
            var form = document.getElementById('SeaTaxUploadForm');
            var formData = new FormData(form);
            formData.set('date', formatDate($scope.form.date));
            formData.set('taxType', $scope.form.taxType);
            $scope.loading = true;
            $http.post(Router.action('SeaTaxUpload', 'UploadFile'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            })
                .then(function (response) {
                if (response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'success') {
                    swal({
                        title: response.data.msg || '上傳成功',
                        icon: 'success'
                    });
                    resetFileInput();
                    return;
                }
                swal({
                    title: response.data.msg || '上傳失敗',
                    icon: 'error'
                });
                resetFileInput();
            })
                .catch(function () {
                swal({
                    title: '上傳失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
    }]);
