// 客戶稅金結算控制器
mainApp.controller('CustomerTaxStatisticsController', function ($scope, $http) {
    // 初始化資料
    $scope.customers = [];
    $scope.selectedCustomer = null;
    $scope.startDate = new Date();
    $scope.endDate = new Date();
    $scope.isExporting = false;
    
    // 日期選擇器設定
    $scope.dateOptions = {
        formatYear: 'yyyy',
        minDate: new Date(2020, 1, 1),
        maxDate: new Date(),
        startingDay: 0,
        showWeeks: false
    };

    // 日期彈出視窗狀態
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };

    // 開啟開始日期彈出視窗
    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    // 開啟結束日期彈出視窗
    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    // 初始化
    $scope.init = function() {
        $scope.loadCustomers();
        // 設定預設日期為本月第一天到今天
        var now = new Date();
        $scope.startDate = new Date(now.getFullYear(), now.getMonth(), 1);
        $scope.endDate = now;
    };

    // 載入客戶列表
    $scope.loadCustomers = function() {
        $http.get(Router.action('CustomerTaxStatistics', 'GetCustomers'))
            .then(function(response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.customers = response.data;
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
            });
    };

    // 匯出Excel
    $scope.exportExcel = function() {
        if (!$scope.selectedCustomer) {
            swal({
                title: "錯誤",
                text: "請選擇客戶",
                icon: "error"
            });
            return;
        }

        if (!$scope.startDate) {
            swal({
                title: "錯誤",
                text: "請選擇開始日期",
                icon: "error"
            });
            return;
        }

        if (!$scope.endDate) {
            swal({
                title: "錯誤",
                text: "請選擇結束日期",
                icon: "error"
            });
            return;
        }

        if ($scope.startDate > $scope.endDate) {
            swal({
                title: "錯誤",
                text: "開始日期不能大於結束日期",
                icon: "error"
            });
            return;
        }

        //匯出
        $scope.performExport();
    };

    // 執行匯出
    $scope.performExport = function() {
        $scope.isExporting = true;

        var exportData = {
            customerCode: $scope.selectedCustomer,
            startDate: moment($scope.startDate).format('YYYYMMDD'),
            endDate: moment($scope.endDate).format('YYYYMMDD')
        };

        $http.post(Router.action('CustomerTaxStatistics', 'ExportExcel'), exportData)
            .then(function(response) {
                if (response.data && response.data.success) {
                    // 使用與DownloadController相同的下載方式
                    window.location = Router.action('Download', 'DownloadFile') + 
                        '?fileGuid=' + response.data.fileGuid + 
                        '&fileName=' + encodeURIComponent(response.data.fileName);
                    
                    swal({
                        title: "匯出成功",
                        text: "共處理 " + (response.data.recordCount || 0) + " 筆資料",
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