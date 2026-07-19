// <reference path="../../types/global.d.ts" />
mainApp.controller('DownloadSeaNewController', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
        }
        function openLoginPage() {
            window.location.href = Router.action('Account', 'Login');
        }
        function setActiveMenu() {
            angular.element('#collapseUpload').addClass('show');
            angular.element('#DownloadSeaNew').addClass('active');
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
                console.error('載入稅金種類失敗:', error);
            });
        }
        function createFormData() {
            var form = document.getElementById('DownloadSeaNewForm');
            var formData = new FormData(form);
            formData.set('date', formatDate($scope.form.date));
            formData.set('taxType', $scope.form.taxType);
            return formData;
        }
        function postAction(actionName) {
            return $http.post(Router.action('DownloadSeaNew', actionName), createFormData(), {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                return response.data;
            });
        }
        function openDownloadFile(response) {
            if (!response.fileGuid || !response.fileName) {
                return;
            }
            var path = Router.action('Download', 'DownloadFile')
                + '?fileGuid=' + encodeURIComponent(response.fileGuid)
                + '&filename=' + encodeURIComponent(response.fileName);
            window.open(path);
        }
        function handleResponse(response) {
            if (response.Redirect) {
                openLoginPage();
                return $q.reject('redirect');
            }
            if (response.msg) {
                swal({
                    title: response.msg,
                    icon: 'error'
                });
            }
            if (response.fileName) {
                openDownloadFile(response);
            }
            return $q.when();
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
        $scope.downloadAll = function () {
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
            $scope.loading = true;
            postAction('SeaExcel')
                .then(handleResponse)
                .then(function () {
                return postAction('SeaSpecialDExcel');
            })
                .then(handleResponse)
                .then(function () {
                return postAction('SeaErrorExcel');
            })
                .then(handleResponse)
                .catch(function (error) {
                if (error === 'redirect') {
                    return;
                }
                swal({
                    title: '下載失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
    }]);
