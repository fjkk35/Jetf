mainApp.controller('TaxPortalCustomerController', function ($scope, $http) {
    $scope.customerGroups = {
        SeaCustomers: [],
        AirCustomers: []
    };
    $scope.modalMode = 'create';
    $scope.searchForm = {
        UserName: ''
    };
    $scope.createForm = {
        UserName: '',
        Memo: ''
    };
    $scope.createSelectionMap = {};
    $scope.editForm = {
        Id: 0,
        UserName: '',
        Memo: '',
        NewPassword: ''
    };
    $scope.editSelectionMap = {};
    $scope.userList = [];
    $scope.credentialResult = null;

    $scope.loadingCustomers = false;
    $scope.searching = false;
    $scope.creating = false;
    $scope.loadingDetail = false;
    $scope.generatingPassword = false;
    $scope.updating = false;

    $scope.init = function () {
        $scope.loadCustomerGroups();
        $scope.searchUsers();
    };

    $scope.loadCustomerGroups = function () {
        $scope.loadingCustomers = true;
        $http.get(Router.action('TaxPortalCustomer', 'GetCustomerGroups'))
            .then(function (response) {
                if (response.data && response.data.status === 'success' && response.data.ReturnObject) {
                    $scope.customerGroups = response.data.ReturnObject;
                } else {
                    swal({
                        title: '載入失敗',
                        text: response.data.msg || '無法載入客戶資料',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                swal({
                    title: '載入失敗',
                    text: '無法載入客戶資料，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loadingCustomers = false;
            });
    };

    $scope.searchUsers = function () {
        $scope.searching = true;
        $http.get(Router.action('TaxPortalCustomer', 'QueryUsers'), {
            params: {
                UserName: $scope.searchForm.UserName || ''
            }
        })
            .then(function (response) {
                if (response.data && response.data.status === 'success') {
                    $scope.userList = response.data.ReturnObject || [];
                } else {
                    $scope.userList = [];
                    swal({
                        title: '查詢失敗',
                        text: response.data.msg || '查詢失敗',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                $scope.userList = [];
                swal({
                    title: '查詢失敗',
                    text: '查詢發生錯誤，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.searching = false;
            });
    };

    $scope.getSelectedCustCodes = function (selectionMap) {
        var selectedCustCodes = [];
        angular.forEach(selectionMap, function (selected, custCode) {
            if (selected) {
                selectedCustCodes.push(custCode);
            }
        });

        return selectedCustCodes;
    };

    $scope.getSelectedCount = function (selectionMap) {
        return $scope.getSelectedCustCodes(selectionMap).length;
    };

    $scope.clearCreateForm = function () {
        $scope.createForm = {
            UserName: '',
            Memo: ''
        };
        $scope.createSelectionMap = {};
    };

    $scope.createUser = function () {
        if (!$scope.createForm.UserName || $scope.createForm.UserName.trim() === '') {
            swal({
                title: '錯誤',
                text: '請輸入帳號',
                icon: 'error'
            });
            return;
        }

        if ($scope.getSelectedCount($scope.createSelectionMap) === 0) {
            swal({
                title: '錯誤',
                text: '請至少選擇一位客戶',
                icon: 'error'
            });
            return;
        }

        $scope.creating = true;
        $http.post(Router.action('TaxPortalCustomer', 'CreateUser'), {
            UserName: $scope.createForm.UserName,
            Memo: $scope.createForm.Memo,
            SelectedCustCodes: $scope.getSelectedCustCodes($scope.createSelectionMap)
        })
            .then(function (response) {
                if (response.data && response.data.status === 'success') {
                    $scope.credentialResult = angular.extend({ Mode: 'create' }, response.data.ReturnObject || {});
                    swal({
                        title: '新增成功',
                        text: '帳號已建立',
                        icon: 'success'
                    });
                    $('#taxPortalCustomerModal').modal('hide');
                    $scope.clearCreateForm();
                    $scope.searchUsers();
                } else {
                    swal({
                        title: '新增失敗',
                        text: response.data.msg || '新增失敗',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                swal({
                    title: '新增失敗',
                    text: '新增發生錯誤，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.creating = false;
            });
    };

    $scope.openCreateModal = function () {
        $scope.modalMode = 'create';
        $scope.loadingDetail = false;
        $scope.clearCreateForm();
        $('#taxPortalCustomerModal').modal('show');
    };

    $scope.openEditModal = function (item) {
        $scope.modalMode = 'edit';
        $scope.loadingDetail = true;
        $scope.editSelectionMap = {};
        $scope.editForm = {
            Id: item.Id,
            UserName: '',
            Memo: '',
            NewPassword: ''
        };

        $http.get(Router.action('TaxPortalCustomer', 'GetUserDetail'), {
            params: {
                id: item.Id
            }
        })
            .then(function (response) {
                if (response.data && response.data.status === 'success' && response.data.ReturnObject) {
                    var detail = response.data.ReturnObject;
                    $scope.editForm.Id = detail.Id;
                    $scope.editForm.UserName = detail.UserName;
                    $scope.editForm.Memo = detail.Memo;
                    $scope.editForm.NewPassword = '';
                    angular.forEach(detail.SelectedCustomers || [], function (customer) {
                        $scope.editSelectionMap[customer.CustCode] = true;
                    });
                    $('#taxPortalCustomerModal').modal('show');
                } else {
                    swal({
                        title: '載入失敗',
                        text: response.data.msg || '無法載入帳號明細',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                swal({
                    title: '載入失敗',
                    text: '無法載入帳號明細，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loadingDetail = false;
            });
    };

    $scope.generatePassword = function () {
        $scope.generatingPassword = true;
        $http.post(Router.action('TaxPortalCustomer', 'GeneratePassword'))
            .then(function (response) {
                if (response.data && response.data.status === 'success' && response.data.ReturnObject) {
                    $scope.editForm.NewPassword = response.data.ReturnObject.Password || '';
                } else {
                    swal({
                        title: '產生失敗',
                        text: response.data.msg || '無法產生密碼',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                swal({
                    title: '產生失敗',
                    text: '無法產生密碼，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.generatingPassword = false;
            });
    };

    $scope.updateUser = function () {
        if ($scope.getSelectedCount($scope.editSelectionMap) === 0) {
            swal({
                title: '錯誤',
                text: '請至少選擇一位客戶',
                icon: 'error'
            });
            return;
        }

        $scope.updating = true;
        $http.post(Router.action('TaxPortalCustomer', 'UpdateUser'), {
            Id: $scope.editForm.Id,
            Memo: $scope.editForm.Memo,
            SelectedCustCodes: $scope.getSelectedCustCodes($scope.editSelectionMap),
            NewPassword: $scope.editForm.NewPassword
        })
            .then(function (response) {
                if (response.data && response.data.status === 'success') {
                    if (response.data.ReturnObject && response.data.ReturnObject.Password) {
                        $scope.credentialResult = angular.extend({ Mode: 'update' }, response.data.ReturnObject);
                    }

                    swal({
                        title: '修改成功',
                        text: response.data.ReturnObject && response.data.ReturnObject.Password ? '資料與密碼已更新' : '資料已更新',
                        icon: 'success'
                    });
                    $('#taxPortalCustomerModal').modal('hide');
                    $scope.searchUsers();
                } else {
                    swal({
                        title: '修改失敗',
                        text: response.data.msg || '修改失敗',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                swal({
                    title: '修改失敗',
                    text: '修改發生錯誤，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.updating = false;
            });
    };

    $scope.init();
});