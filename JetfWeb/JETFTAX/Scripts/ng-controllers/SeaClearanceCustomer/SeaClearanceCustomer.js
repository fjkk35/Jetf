// Controller
mainApp.controller('SeaClearanceCustomerController', function ($scope, $http) {
    // 初始化資料
    $scope.availableCustomers = [];
    $scope.selectedCustomers = [];
    $scope.loadingAvailable = true;
    $scope.loadingSelected = true;
    $scope.adding = false;
    $scope.deleting = false;
    $scope.availableSearchText = '';
    $scope.selectedSearchText = '';
    $scope.selectAllAvailable = false;
    $scope.selectAllSelected = false;

    // 搜尋過濾器
    $scope.availableFilter = function(customer) {
        if (!$scope.availableSearchText) return true;
        var searchText = $scope.availableSearchText.toLowerCase();
        return customer.Cust_Code.toLowerCase().indexOf(searchText) !== -1 ||
               customer.Cust_Name.toLowerCase().indexOf(searchText) !== -1;
    };

    $scope.selectedFilter = function(customer) {
        if (!$scope.selectedSearchText) return true;
        var searchText = $scope.selectedSearchText.toLowerCase();
        return customer.Cust_Code.toLowerCase().indexOf(searchText) !== -1 ||
               customer.Cust_Name.toLowerCase().indexOf(searchText) !== -1;
    };

    // 載入可用客戶
    $scope.loadAvailableCustomers = function () {
        $scope.loadingAvailable = true;
        $http.get(Router.action('SeaClearanceCustomer', 'GetAvailableCustomers'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.availableCustomers = response.data.map(function(customer) {
                        customer.IsInSystem = customer.IsSelected;
                        customer.IsSelected = false; // 重置選擇狀態，用於批量操作
                        return customer;
                    });
                } else if (response.data && response.data.msg) {
                    $scope.availableCustomers = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.availableCustomers = [];
                }
            })
            .catch(function (error) {
                console.error('載入可用客戶失敗:', error);
                $scope.availableCustomers = [];
                swal({
                    title: "錯誤",
                    text: "載入可用客戶失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loadingAvailable = false;
            });
    };

    // 載入已選擇的客戶
    $scope.loadSelectedCustomers = function () {
        $scope.loadingSelected = true;
        $http.get(Router.action('SeaClearanceCustomer', 'GetSelectedCustomers'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.selectedCustomers = response.data.map(function(customer) {
                        customer.IsSelectedForDelete = false; // 用於批量刪除
                        return customer;
                    });
                } else if (response.data && response.data.msg) {
                    $scope.selectedCustomers = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.selectedCustomers = [];
                }
            })
            .catch(function (error) {
                console.error('載入已選擇客戶失敗:', error);
                $scope.selectedCustomers = [];
                swal({
                    title: "錯誤",
                    text: "載入已選擇客戶失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loadingSelected = false;
            });
    };

    // 檢查是否有未選擇的客戶可新增
    $scope.hasUnselectedCustomers = function () {
        return $scope.availableCustomers.some(function(customer) {
            return customer.IsSelected && !customer.IsInSystem;
        });
    };

    // 檢查是否有選擇要刪除的客戶
    $scope.hasSelectedCustomersToDelete = function () {
        return $scope.selectedCustomers.some(function(customer) {
            return customer.IsSelectedForDelete;
        });
    };

    // 取得未選擇客戶數量
    $scope.getUnselectedCount = function () {
        return $scope.availableCustomers.filter(function(customer) {
            return customer.IsSelected && !customer.IsInSystem;
        }).length;
    };

    // 取得選擇刪除客戶數量
    $scope.getSelectedForDeleteCount = function () {
        return $scope.selectedCustomers.filter(function(customer) {
            return customer.IsSelectedForDelete;
        }).length;
    };

    // 切換全選/全不選 (可用客戶)
    $scope.toggleAllAvailable = function () {
        var filteredCustomers = $scope.availableCustomers.filter($scope.availableFilter);
        filteredCustomers.forEach(function(customer) {
            if (!customer.IsInSystem) {
                customer.IsSelected = $scope.selectAllAvailable;
            }
        });
    };

    // 切換全選/全不選 (已選擇客戶)
    $scope.toggleAllSelected = function () {
        var filteredCustomers = $scope.selectedCustomers.filter($scope.selectedFilter);
        filteredCustomers.forEach(function(customer) {
            customer.IsSelectedForDelete = $scope.selectAllSelected;
        });
    };

    // 新增選擇的客戶
    $scope.addSelectedCustomers = function () {
        var selectedCodes = $scope.availableCustomers
            .filter(function(customer) { 
                return customer.IsSelected && !customer.IsInSystem; 
            })
            .map(function(customer) { 
                return customer.Cust_Code; 
            });

        if (selectedCodes.length === 0) {
            swal({
                title: "提示",
                text: "請選擇要新增的客戶",
                icon: "warning"
            });
            return;
        }

        swal({
            title: "確認新增",
            text: `確定要新增 ${selectedCodes.length} 個客戶嗎？`,
            icon: "question",
            buttons: {
                cancel: {
                    text: "取消",
                    value: null,
                    visible: true,
                    className: "btn-secondary",
                    closeModal: true,
                },
                confirm: {
                    text: "確定新增",
                    value: true,
                    visible: true,
                    className: "btn-primary",
                    closeModal: true
                }
            }
        }).then(function (willAdd) {
            if (willAdd) {
                $scope.adding = true;
                $scope.$apply();

                $http.post(Router.action('SeaClearanceCustomer', 'AddCustomers'), selectedCodes)
                    .then(function (response) {
                        if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                            var result = response.data.ReturnObject || {};
                            swal({
                                title: "新增成功",
                                text: result.Message || "客戶新增完成",
                                icon: "success",
                                timer: 2000
                            });
                            
                            // 重新載入資料
                            $scope.loadAvailableCustomers();
                            $scope.loadSelectedCustomers();
                            $scope.selectAllAvailable = false;
                        } else {
                            swal({
                                title: "新增失敗",
                                text: response.data.msg || "新增失敗，請稍後再試",
                                icon: "error"
                            });
                        }
                    })
                    .catch(function (error) {
                        console.error('新增客戶失敗:', error);
                        swal({
                            title: "錯誤",
                            text: "新增失敗，請稍後再試",
                            icon: "error"
                        });
                    })
                    .finally(function () {
                        $scope.adding = false;
                    });
            }
        });
    };

    // 刪除選擇的客戶
    $scope.deleteSelectedCustomers = function () {
        var selectedCodes = $scope.selectedCustomers
            .filter(function(customer) { 
                return customer.IsSelectedForDelete; 
            })
            .map(function(customer) { 
                return customer.Cust_Code; 
            });

        if (selectedCodes.length === 0) {
            swal({
                title: "提示",
                text: "請選擇要刪除的客戶",
                icon: "warning"
            });
            return;
        }

        swal({
            title: "確認刪除",
            text: `確定要刪除 ${selectedCodes.length} 個客戶嗎？\n※ 此操作無法復原`,
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    value: null,
                    visible: true,
                    className: "btn-secondary",
                    closeModal: true,
                },
                confirm: {
                    text: "確定刪除",
                    value: true,
                    visible: true,
                    className: "btn-danger",
                    closeModal: true
                }
            },
            dangerMode: true,
        }).then(function (willDelete) {
            if (willDelete) {
                $scope.deleting = true;
                $scope.$apply();

                $http.post(Router.action('SeaClearanceCustomer', 'DeleteCustomers'), selectedCodes)
                    .then(function (response) {
                        if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                            var result = response.data.ReturnObject || {};
                            swal({
                                title: "刪除成功",
                                text: result.Message || "客戶刪除完成",
                                icon: "success",
                                timer: 2000
                            });
                            
                            // 重新載入資料
                            $scope.loadAvailableCustomers();
                            $scope.loadSelectedCustomers();
                            $scope.selectAllSelected = false;
                        } else {
                            swal({
                                title: "刪除失敗",
                                text: response.data.msg || "刪除失敗，請稍後再試",
                                icon: "error"
                            });
                        }
                    })
                    .catch(function (error) {
                        console.error('刪除客戶失敗:', error);
                        swal({
                            title: "錯誤",
                            text: "刪除失敗，請稍後再試",
                            icon: "error"
                        });
                    })
                    .finally(function () {
                        $scope.deleting = false;
                    });
            }
        });
    };

    // 初始化載入
    $scope.loadAvailableCustomers();
    $scope.loadSelectedCustomers();
});