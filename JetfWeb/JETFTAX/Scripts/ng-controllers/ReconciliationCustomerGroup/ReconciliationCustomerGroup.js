// <reference path="../../types/global.d.ts" />
mainApp.controller('ReconciliationCustomerGroupController', ['$scope', '$http', function ($scope, $http) {
        function emptyForm() {
            return {
                Id: null,
                Type: '',
                GroupName: '',
                CustCodes: []
            };
        }
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        function showError(message) {
            swal({ title: message, icon: 'error' });
        }
        function loadGroupOptions() {
            $http.get(Router.action('ReconciliationCustomerGroup', 'GetGroupOptions'), {
                params: { type: $scope.searchForm.Type }
            }).then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '載入客戶群組失敗');
                    return;
                }
                $scope.groupOptions = response.data.ReturnObject || [];
            }).catch(function () {
                showError('載入客戶群組失敗');
            });
        }
        function loadCustomerOptions(type, id) {
            if (!type) {
                $scope.modal.customers = [];
                return;
            }
            $scope.modal.loading = true;
            $http.get(Router.action('ReconciliationCustomerGroup', 'GetCustomerOptions'), {
                params: { type: type, id: id }
            }).then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '載入客戶資料失敗');
                    return;
                }
                $scope.modal.customers = response.data.ReturnObject || [];
            }).catch(function () {
                showError('載入客戶資料失敗');
            }).finally(function () {
                $scope.modal.loading = false;
            });
        }
        function hideModal() {
            $('#reconciliationCustomerGroupModal').modal('hide');
        }
        function validateForm() {
            if (!$scope.modal.form.Type) {
                return '請選擇類型';
            }
            if (!$scope.modal.form.GroupName || !$scope.modal.form.GroupName.trim()) {
                return '請輸入群組名稱';
            }
            if ($scope.modal.form.GroupName.trim().length > 20) {
                return '群組名稱不可超過 20 個字元';
            }
            if ($scope.getSelectedCount() === 0) {
                return '請至少選擇一個客戶';
            }
            return '';
        }
        $scope.searchForm = { Type: '', GroupName: '' };
        $scope.groupOptions = [];
        $scope.rows = [];
        $scope.loading = false;
        $scope.modal = {
            form: emptyForm(),
            customers: [],
            customerKeyword: '',
            loading: false,
            saving: false
        };
        $scope.init = function () {
            angular.element('#ReconciliationCustomerGroup').addClass('active');
            loadGroupOptions();
            $scope.search();
        };
        $scope.search = function () {
            $scope.loading = true;
            $http.post(Router.action('ReconciliationCustomerGroup', 'Search'), $scope.searchForm)
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '查詢失敗');
                    return;
                }
                $scope.rows = response.data.ReturnObject || [];
            }).catch(function () {
                showError('查詢失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
        };
        $scope.clearSearch = function () {
            $scope.searchForm = { Type: '', GroupName: '' };
            loadGroupOptions();
            $scope.search();
        };
        $scope.onSearchTypeChange = function () {
            $scope.searchForm.GroupName = '';
            loadGroupOptions();
        };
        $scope.openCreateModal = function () {
            $scope.modal.form = emptyForm();
            $scope.modal.customers = [];
            $scope.modal.customerKeyword = '';
            $scope.modal.loading = false;
            $scope.modal.saving = false;
            $('#reconciliationCustomerGroupModal').modal('show');
        };
        $scope.openEditModal = function (id) {
            $scope.modal.loading = true;
            $http.get(Router.action('ReconciliationCustomerGroup', 'GetDetail'), {
                params: { id: id }
            }).then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    $scope.modal.loading = false;
                    return;
                }
                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    $scope.modal.loading = false;
                    showError(response.data.msg || '載入客戶群組失敗');
                    return;
                }
                $scope.modal.form = response.data.ReturnObject;
                $scope.modal.customers = [];
                $scope.modal.customerKeyword = '';
                $scope.modal.saving = false;
                $('#reconciliationCustomerGroupModal').modal('show');
                loadCustomerOptions($scope.modal.form.Type, $scope.modal.form.Id);
            }).catch(function () {
                $scope.modal.loading = false;
                showError('載入客戶群組失敗');
            });
        };
        $scope.onModalTypeChange = function () {
            $scope.modal.customers = [];
            $scope.modal.customerKeyword = '';
            loadCustomerOptions($scope.modal.form.Type, $scope.modal.form.Id);
        };
        $scope.getSelectedCount = function () {
            return $scope.modal.customers.filter(function (customer) {
                return customer.IsSelected && !customer.IsDisabled;
            }).length;
        };
        $scope.saveGroup = function () {
            var validationMessage = validateForm();
            if (validationMessage) {
                showError(validationMessage);
                return;
            }
            $scope.modal.form.CustCodes = $scope.modal.customers
                .filter(function (customer) {
                return customer.IsSelected && !customer.IsDisabled;
            })
                .map(function (customer) {
                return customer.CustCode;
            });
            $scope.modal.saving = true;
            $http.post(Router.action('ReconciliationCustomerGroup', 'Save'), $scope.modal.form)
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status !== 'success') {
                    showError(response.data.msg || '儲存失敗');
                    return;
                }
                swal({ title: response.data.msg || '儲存成功', icon: 'success' });
                $scope.searchForm.Type = $scope.modal.form.Type;
                $scope.searchForm.GroupName = $scope.modal.form.GroupName.trim();
                hideModal();
                loadGroupOptions();
                $scope.search();
            }).catch(function () {
                showError('儲存失敗，請稍後再試');
            }).finally(function () {
                $scope.modal.saving = false;
            });
        };
        $scope.deleteGroup = function (row) {
            if (!window.confirm('確定要刪除客戶群組「' + row.GroupName + '」？')) {
                return;
            }
            $http.post(Router.action('ReconciliationCustomerGroup', 'Delete'), { id: row.Id })
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status !== 'success') {
                    showError(response.data.msg || '刪除失敗');
                    return;
                }
                swal({ title: response.data.msg || '刪除成功', icon: 'success' });
                if ($scope.searchForm.GroupName === row.GroupName) {
                    $scope.searchForm.GroupName = '';
                }
                loadGroupOptions();
                $scope.search();
            }).catch(function () {
                showError('刪除失敗，請稍後再試');
            });
        };
        $('#reconciliationCustomerGroupModal').on('hidden.bs.modal', function () {
            $scope.$applyAsync(function () {
                $scope.modal.form = emptyForm();
                $scope.modal.customers = [];
                $scope.modal.customerKeyword = '';
                $scope.modal.loading = false;
                $scope.modal.saving = false;
            });
        });
    }]);
