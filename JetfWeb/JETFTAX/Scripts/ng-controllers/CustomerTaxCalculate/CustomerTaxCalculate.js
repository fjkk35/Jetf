// 客戶稅金計算控制器
mainApp.controller('CustomerTaxCalculateController', function ($scope, $http) {
    // 初始化資料
    $scope.taxTimes = [];
    $scope.customerSettings = [];
    $scope.selectedTaxTime = null;
    $scope.selectedDate = new Date();
    $scope.isExporting = false;
    $scope.loadingSettings = false;
    
    // 日期選擇器設定
    $scope.dateOptions = {
        formatYear: 'yyyy',
        minDate: new Date(2020, 1, 1),
        maxDate: new Date(),
        startingDay: 0,
        showWeeks: false
    };

    // 日期彈出視窗狀態
    $scope.datePopup = {
        opened: false
    };

    // 開啟日期彈出視窗
    $scope.openDatePopup = function () {
        $scope.datePopup.opened = true;
    };

    // 初始化
    $scope.init = function() {
        $scope.loadTaxTimes();
        $scope.loadCustomerTaxSettings();
    };

    // 載入稅金時間列表
    $scope.loadTaxTimes = function() {
        $http.get(Router.action('CustomerTaxCalculate', 'GetTaxTimes'))
            .then(function(response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.taxTimes = response.data;
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
                console.error('載入稅金時間列表失敗:', error);
                $scope.taxTimes = [];
                swal({
                    title: "錯誤",
                    text: "載入稅金時間列表失敗，請稍後再試",
                    icon: "error"
                });
            });
    };

    // 載入客戶稅金時間設定列表
    $scope.loadCustomerTaxSettings = function() {
        $scope.loadingSettings = true;
        $http.get(Router.action('CustomerTaxSetting', 'GetCustomerTaxSettings'))
            .then(function(response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.customerSettings = response.data;
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
                console.error('載入客戶稅金設定失敗:', error);
                $scope.customerSettings = [];
                swal({
                    title: "錯誤",
                    text: "載入客戶稅金設定失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.loadingSettings = false;
            });
    };

    // 取得該時間區間的客戶名稱列表
    $scope.getSelectedCustomers = function() {
        if (!$scope.selectedTaxTime) {
            return [];
        }
        
        var selectedTaxTimeObj = $scope.taxTimes.find(function(item) {
            return item.Id === $scope.selectedTaxTime;
        });
        
        return selectedTaxTimeObj ? (selectedTaxTimeObj.CustomerNames || []) : [];
    };

    // 匯出Excel
    $scope.exportExcel = function() {
        if (!$scope.selectedTaxTime) {
            swal({
                title: "錯誤",
                text: "請選擇稅金時間",
                icon: "error"
            });
            return;
        }

        if (!$scope.selectedDate) {
            swal({
                title: "錯誤",
                text: "請選擇日期",
                icon: "error"
            });
            return;
        }

        $scope.performExport();
    };

    // 執行匯出
    $scope.performExport = function() {
        $scope.isExporting = true;

        var exportData = {
            taxTimeId: $scope.selectedTaxTime,
            selectedDate: moment($scope.selectedDate).format('YYYY-MM-DD')
        };

        $http.post(Router.action('CustomerTaxCalculate', 'ExportExcel'), exportData)
            .then(function(response) {
                if (response.data && response.data.success) {
                    // 使用與DownloadController相同的下載方式
                    window.location = Router.action('Download', 'DownloadFile') + 
                        '?fileGuid=' + response.data.fileGuid + 
                        '&fileName=' + encodeURIComponent(response.data.fileName);
                    
                    swal({
                        title: "匯出成功",
                        text: "共匯出 " + (response.data.recordCount || 0) + " 筆資料",
                        icon: "success"
                    });
                } else {
                    swal({
                        title: "匯出失敗",
                        text: response.data.message || "匯出過程中發生錯誤",
                        icon: "error"
                    });
                }
            })
            .catch(function(error) {
                console.error('匯出失敗:', error);
                swal({
                    title: "錯誤",
                    text: "匯出過程中發生錯誤，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function() {
                $scope.isExporting = false;
            });
    };

    // 初始化執行
    $scope.init();
});