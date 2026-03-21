// 客戶稅金時間設定控制器
mainApp.controller('CustomerTaxSettingController', function ($scope, $http) {
    // 初始化資料
    $scope.customers = [];
    $scope.taxTimes = [];
    $scope.customerSettings = [];
    $scope.selectedCustomer = null;
    $scope.selectedTaxTimes = {};
    $scope.hasExistingSetting = false;
    
    // Modal相關
    $scope.modalMode = 'add'; // 'add' 或 'edit'
    $scope.availableCustomers = []; // 可選擇的客戶列表（過濾後）
    
    // 載入狀態
    $scope.loadingCustomers = true;
    $scope.loadingTaxTimes = true;
    $scope.loadingSettings = true;
    $scope.saving = false;
    $scope.deleting = false;

    // 初始化
    $scope.init = function() {
        $scope.loadCustomers();
        $scope.loadTaxTimes();
        $scope.loadCustomerTaxSettings();
    };

    // 載入SEA客戶列表
    $scope.loadCustomers = function() {
        $scope.loadingCustomers = true;
        $http.get(Router.action('CustomerTaxSetting', 'GetSeaCustomers'))
            .then(function(response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.customers = response.data;
                    $scope.updateAvailableCustomers();
                } else if (response.data && response.data.msg) {
                    $scope.customers = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.customers = [];
                }
            })
            .catch(function(error) {
                console.error('載入客戶列表失敗:', error);
                $scope.customers = [];
                swal({
                    title: "錯誤",
                    text: "載入客戶列表失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.loadingCustomers = false;
            });
    };

    // 載入稅金時間列表
    $scope.loadTaxTimes = function() {
        $scope.loadingTaxTimes = true;
        $http.get(Router.action('CustomerTaxSetting', 'GetTaxTimes'))
            .then(function(response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.taxTimes = response.data;
                    // 初始化選擇狀態
                    $scope.selectedTaxTimes = {};
                    angular.forEach($scope.taxTimes, function(taxTime) {
                        $scope.selectedTaxTimes[taxTime.Id] = false;
                    });
                } else if (response.data && response.data.msg) {
                    $scope.taxTimes = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.taxTimes = [];
                }
            })
            .catch(function(error) {
                console.error('載入稅金時間失敗:', error);
                $scope.taxTimes = [];
                swal({
                    title: "錯誤",
                    text: "載入稅金時間失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.loadingTaxTimes = false;
            });
    };

    // 載入客戶稅金時間設定列表
    $scope.loadCustomerTaxSettings = function() {
        $scope.loadingSettings = true;
        $http.get(Router.action('CustomerTaxSetting', 'GetCustomerTaxSettings'))
            .then(function(response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.customerSettings = response.data;
                    $scope.updateAvailableCustomers();
                } else if (response.data && response.data.msg) {
                    $scope.customerSettings = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.customerSettings = [];
                }
            })
            .catch(function(error) {
                console.error('載入客戶設定失敗:', error);
                $scope.customerSettings = [];
                swal({
                    title: "錯誤",
                    text: "載入客戶設定失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.loadingSettings = false;
            });
    };

    // 更新可選擇的客戶列表（排除已設定的客戶）
    $scope.updateAvailableCustomers = function() {
        if (!$scope.customers || !$scope.customerSettings) {
            $scope.availableCustomers = $scope.customers || [];
            return;
        }

        // 取得已設定的客戶代號列表
        var existingCustomerCodes = $scope.customerSettings.map(function(setting) {
            return setting.Cust_Code;
        });

        // 過濾出未設定的客戶
        $scope.availableCustomers = $scope.customers.filter(function(customer) {
            return existingCustomerCodes.indexOf(customer.Cust_Code) === -1;
        });
    };

    // 取得Modal中要顯示的客戶列表
    $scope.getModalCustomers = function() {
        if ($scope.modalMode === 'edit') {
            // 編輯模式：顯示所有客戶（但下拉選單會被禁用）
            return $scope.customers;
        } else {
            // 新增模式：只顯示未設定的客戶
            return $scope.availableCustomers;
        }
    };

    // 開啟新增Modal
    $scope.openAddModal = function() {
        // 檢查是否還有可新增的客戶
        if ($scope.availableCustomers.length === 0) {
            swal({
                title: "無法新增",
                text: "所有客戶都已設定稅金時間，無法新增更多設定",
                icon: "warning"
            });
            return;
        }

        $scope.modalMode = 'add';
        $scope.resetModalForm();
        $('#customerTaxSettingModal').modal('show');
    };

    // 開啟編輯Modal
    $scope.openEditModal = function(setting) {
        $scope.modalMode = 'edit';
        
        // 找到對應的客戶
        var customer = $scope.customers.find(function(c) {
            return c.Cust_Code === setting.Cust_Code;
        });
        
        if (customer) {
            $scope.selectedCustomer = customer;
            $scope.loadCustomerSetting();
            $('#customerTaxSettingModal').modal('show');
        } else {
            swal({
                title: "錯誤",
                text: "找不到對應的客戶資訊",
                icon: "error"
            });
        }
    };

    // 重置Modal表單
    $scope.resetModalForm = function() {
        $scope.selectedCustomer = null;
        $scope.resetTaxTimeSelection();
        $scope.hasExistingSetting = false;
    };

    // 載入特定客戶的設定
    $scope.loadCustomerSetting = function() {
        if (!$scope.selectedCustomer) {
            $scope.resetTaxTimeSelection();
            $scope.hasExistingSetting = false;
            return;
        }

        $http.get(Router.action('CustomerTaxSetting', 'GetCustomerTaxSetting') + '?custCode=' + $scope.selectedCustomer.Cust_Code)
            .then(function(response) {
                if (response.data && response.data.TaxTimeIds) {
                    // 有現有設定
                    $scope.hasExistingSetting = true;
                    $scope.resetTaxTimeSelection();
                    
                    // 設定勾選狀態
                    angular.forEach(response.data.TaxTimeIds, function(taxTimeId) {
                        $scope.selectedTaxTimes[taxTimeId] = true;
                    });
                } else {
                    // 沒有現有設定
                    $scope.hasExistingSetting = false;
                    $scope.resetTaxTimeSelection();
                }
            })
            .catch(function(error) {
                console.error('載入客戶設定失敗:', error);
                $scope.hasExistingSetting = false;
                $scope.resetTaxTimeSelection();
            });
    };

    // 重置稅金時間選擇
    $scope.resetTaxTimeSelection = function() {
        angular.forEach($scope.taxTimes, function(taxTime) {
            $scope.selectedTaxTimes[taxTime.Id] = false;
        });
    };

    // 檢查是否有勾選至少一個稅金時間
    $scope.hasSelectedTaxTime = function() {
        if (!$scope.selectedTaxTimes) return false;
        for (var key in $scope.selectedTaxTimes) {
            if ($scope.selectedTaxTimes[key]) return true;
        }
        return false;
    };

    // 儲存客戶稅金時間設定
    $scope.saveCustomerTaxSetting = function() {
        if (!$scope.selectedCustomer) {
            swal({
                title: "錯誤",
                text: "請先選擇客戶",
                icon: "error"
            });
            return;
        }

        // 取得選擇的稅金時間ID
        var selectedTaxTimeIds = [];
        angular.forEach($scope.selectedTaxTimes, function(selected, taxTimeId) {
            if (selected) {
                selectedTaxTimeIds.push(parseInt(taxTimeId));
            }
        });

        $scope.saving = true;
        $http.post(Router.action('CustomerTaxSetting', 'SaveCustomerTaxSetting'), {
                custCode: $scope.selectedCustomer.Cust_Code,
                taxTimeIds: selectedTaxTimeIds
            })
            .then(function(response) {
                if (response.data && response.data.status === 'success') {
                    swal({
                        title: "成功",
                        text: response.data.msg || "設定儲存成功",
                        icon: "success"
                    });
                    
                    // 關閉Modal
                    $('#customerTaxSettingModal').modal('hide');
                    
                    // 重新載入設定列表
                    $scope.loadCustomerTaxSettings();
                    $scope.hasExistingSetting = selectedTaxTimeIds.length > 0;
                } else {
                    swal({
                        title: "錯誤",
                        text: response.data.msg || "儲存失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function(error) {
                console.error('儲存設定失敗:', error);
                swal({
                    title: "錯誤",
                    text: "儲存設定失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.saving = false;
            });
    };

    // 刪除客戶稅金時間設定
    $scope.deleteCustomerTaxSetting = function() {
        if (!$scope.selectedCustomer) {
            return;
        }

        swal({
            title: "確認刪除",
            text: "確定要刪除客戶 " + $scope.selectedCustomer.Cust_Code + " 的稅金時間設定嗎？",
            icon: "warning",
            buttons: ["取消", "確定"],
            dangerMode: true
        }).then(function(willDelete) {
            if (willDelete) {
                $scope.performDelete($scope.selectedCustomer.Cust_Code);
            }
        });
    };

    // 執行刪除
    $scope.performDelete = function(custCode) {
        $scope.deleting = true;
        $http.post(Router.action('CustomerTaxSetting', 'DeleteCustomerTaxSetting'), {
                custCode: custCode
            })
            .then(function(response) {
                if (response.data && response.data.status === 'success') {
                    swal({
                        title: "成功",
                        text: response.data.msg || "設定刪除成功",
                        icon: "success"
                    });
                    
                    // 關閉Modal
                    $('#customerTaxSettingModal').modal('hide');
                    
                    // 重新載入設定列表
                    $scope.loadCustomerTaxSettings();
                } else {
                    swal({
                        title: "錯誤",
                        text: response.data.msg || "刪除失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function(error) {
                console.error('刪除設定失敗:', error);
                swal({
                    title: "錯誤",
                    text: "刪除設定失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.deleting = false;
            });
    };

    // 確認從表格刪除設定
    $scope.confirmDeleteSetting = function(custCode) {
        var customer = $scope.customers.find(function(c) {
            return c.Cust_Code === custCode;
        });
        
        var customerName = customer ? customer.Cust_Name : custCode;
        
        swal({
            title: "確認刪除",
            text: "確定要刪除客戶 " + custCode + " (" + customerName + ") 的稅金時間設定嗎？",
            icon: "warning",
            buttons: ["取消", "確定"],
            dangerMode: true
        }).then(function(willDelete) {
            if (willDelete) {
                $scope.performDelete(custCode);
            }
        });
    };

    // Modal關閉時重置表單
    $('#customerTaxSettingModal').on('hidden.bs.modal', function () {
        $scope.$apply(function() {
            $scope.resetModalForm();
        });
    });

    // 初始化執行
    $scope.init();
});