// <reference path="../../types/global.d.ts" />
mainApp.controller('ReconciliationCustomerController', ['$scope', '$http', function ($scope, $http) {
        var lastQuery = null;
        function today() {
            var value = new Date();
            value.setHours(0, 0, 0, 0);
            return value;
        }
        function formatDate(value) {
            return value ? moment(value).format('YYYY-MM-DD') : null;
        }
        function showError(message) {
            swal({ title: message, icon: 'error' });
        }
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        function validateDates() {
            if (!$scope.searchForm.outDateStart || !$scope.searchForm.outDateEnd) {
                showError('日期為必填，請選擇開始日期與結束日期');
                return false;
            }
            if (moment($scope.searchForm.outDateStart).isAfter($scope.searchForm.outDateEnd, 'day')) {
                showError('開始日期不可晚於結束日期');
                return false;
            }
            return true;
        }
        function selectedCodes() {
            var codes = [];
            angular.forEach($scope.selectedCustomerMap, function (selected, code) {
                if (selected) {
                    codes.push(code);
                }
            });
            return codes.sort();
        }
        function buildRequest() {
            var customerCodes = selectedCodes();
            return {
                OutDateStart: formatDate($scope.searchForm.outDateStart),
                OutDateEnd: formatDate($scope.searchForm.outDateEnd),
                CustomerCodes: customerCodes.length ? customerCodes : null,
                DlvInvText: ($scope.searchForm.dlvInvText || '').trim() || null
            };
        }
        function loadData() {
            var request = buildRequest();
            $scope.loading = true;
            $http.post(Router.action('ReconciliationCustomer', 'Search'), request)
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '查詢失敗');
                    return;
                }
                $scope.result = response.data.ReturnObject;
                $scope.isSearched = true;
                lastQuery = angular.copy(request);
            }).catch(function () {
                showError('查詢失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
        }
        $scope.searchForm = {
            outDateStart: today(),
            outDateEnd: today(),
            dlvInvText: ''
        };
        $scope.selectedCustomerMap = {};
        $scope.dateOptions = {
            startingDay: 1,
            showWeeks: false
        };
        $scope.startDatePopup = { opened: false };
        $scope.endDatePopup = { opened: false };
        $scope.result = { ReceivableAmount: 0 };
        $scope.confirmAmount = null;
        $scope.loading = false;
        $scope.confirming = false;
        $scope.isSearched = false;
        $scope.init = function () {
            angular.element('#ReconciliationCustomer').addClass('active');
        };
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.search = function () {
            if (!validateDates()) {
                return;
            }
            loadData();
        };
        $scope.clearSearch = function () {
            $scope.searchForm = {
                outDateStart: today(),
                outDateEnd: today(),
                dlvInvText: ''
            };
            $scope.selectedCustomerMap = {};
            loadData();
        };
        $scope.openConfirmModal = function () {
            if (!$scope.isSearched || !lastQuery || $scope.result.ReceivableAmount <= 0) {
                showError('目前沒有可銷帳的應收金額');
                return;
            }
            $scope.confirmAmount = null;
            $('#reconciliationCustomerConfirmModal').modal('show');
        };
        $scope.confirm = function () {
            var amount = $scope.confirmAmount;
            if (!amount || amount <= 0 || Math.floor(amount) !== amount) {
                showError('請輸入大於 0 的整數銷帳金額');
                return;
            }
            if (amount !== $scope.result.ReceivableAmount) {
                showError('銷帳金額必須與應收金額相同');
                return;
            }
            $scope.confirming = true;
            $http.post(Router.action('ReconciliationCustomer', 'Confirm'), {
                Query: lastQuery,
                Amount: amount
            }).then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '銷帳失敗');
                    return;
                }
                $('#reconciliationCustomerConfirmModal').modal('hide');
                swal({ title: response.data.msg || '客戶銷帳完成', icon: 'success' });
                loadData();
            }).catch(function () {
                showError('銷帳失敗，請稍後再試');
            }).finally(function () {
                $scope.confirming = false;
            });
        };
    }]);
