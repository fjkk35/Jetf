// <reference path="../../types/global.d.ts" />
mainApp.controller('DownloadEtlNewController', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        var downloadTasks = [
            { action: 'EtlExcel', company: '新竹物流' },
            { action: 'EtlExcel', company: '新瑞宅配' },
            { action: 'EtlExcel', company: '圓通自取' },
            { action: 'EtlSpecialDExcel', company: '新竹物流' },
            { action: 'EtlErrorExcel', company: '新竹物流' }
        ];
        function formatDate(value) {
            var month = ('0' + (value.getMonth() + 1)).slice(-2);
            var day = ('0' + value.getDate()).slice(-2);
            return value.getFullYear() + '-' + month + '-' + day;
        }
        function openLoginPage() {
            window.location.href = Router.action('Account', 'Login');
        }
        function openDownloadFile(response) {
            if (!response.fileGuid || !response.fileName) {
                return;
            }
            var path = Router.action('Download', 'DownloadFile') + '?fileGuid=' + response.fileGuid + '&filename=' + response.fileName;
            window.open(path);
        }
        function showError(message) {
            if (!message) {
                return;
            }
            swal({
                title: message,
                icon: 'error'
            });
        }
        function createFormData(company) {
            var form = document.getElementById('DownloadEtlNewForm');
            var formData = new FormData(form);
            formData.set('date', formatDate($scope.form.date));
            formData.set('timeBetween', $scope.form.timeBetween);
            formData.set('sTime', $scope.form.sTime);
            formData.set('eTime', $scope.form.eTime);
            formData.set('company', company);
            return formData;
        }
        function postAction(actionName, company) {
            return $http.post(Router.action('DownloadEtlNew', actionName), createFormData(company), {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                return response.data || {};
            });
        }
        function runDownloadTasks(index) {
            if (index >= downloadTasks.length) {
                return $q.when();
            }
            var task = downloadTasks[index];
            return postAction(task.action, task.company)
                .then(function (response) {
                if (response.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.msg) {
                    showError(response.msg);
                }
                openDownloadFile(response);
            })
                .catch(function () {
                showError('檔案下載失敗，請稍後再試');
            })
                .then(function () {
                return runDownloadTasks(index + 1);
            });
        }
        $scope.timeBetweenList = [
            { value: '1', text: '前一天22:00-當日08:00' },
            { value: '2', text: '當日08:00-當日16:00' },
            { value: '3', text: '當日21:00-當日22:00' }
        ];
        $scope.form = {
            date: new Date(),
            timeBetween: '1',
            sTime: '2200',
            eTime: '0800',
            company: ''
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
        $scope.openDatePopup = function () {
            $scope.datePopup.opened = true;
        };
        $scope.onTimeBetweenChange = function () {
            if ($scope.form.timeBetween === '1') {
                $scope.form.sTime = '2200';
                $scope.form.eTime = '0800';
            }
            else if ($scope.form.timeBetween === '2') {
                $scope.form.sTime = '0800';
                $scope.form.eTime = '1600';
            }
            else if ($scope.form.timeBetween === '3') {
                $scope.form.sTime = '2100';
                $scope.form.eTime = '2200';
            }
        };
        $scope.downloadAll = function () {
            if (!$scope.form.date) {
                showError('請選擇日期');
                return;
            }
            if (!$scope.form.sTime || !$scope.form.eTime || $scope.form.sTime.length !== 4 || $scope.form.eTime.length !== 4) {
                showError('時間區間錯誤，請確認');
                return;
            }
            $scope.loading = true;
            postAction('UploadEtl', '')
                .then(function (response) {
                if (response.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.msg) {
                    showError(response.msg);
                    return;
                }
                return runDownloadTasks(0);
            })
                .catch(function () {
                showError('轉檔失敗，請稍後再試');
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
    }]);