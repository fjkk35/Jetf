// Controller
mainApp.controller('EtlErrorWorkController', function ($scope, $http) {
    // 初始化資料
    $scope.customerList = [];
    $scope.groupList = [];
    $scope.groupDetailsCache = {}; // 快取所有群組的客戶明細
    $scope.loadingCustomers = true;
    $scope.loadingGroups = true;
    $scope.downloading = false;

    // 日期選擇器狀態
    $scope.sDatePopup = { opened: false };
    $scope.eDatePopup = { opened: false };

    // 日期選擇器選項
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 12, 31),
        minDate: new Date(2000, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    // 表單資料
    $scope.searchData = {
        sDate: new Date(),
        eDate: new Date(),
        selectedGroup: ''
    };

    // 初始化
    $scope.init = function () {
        $scope.loadCustomerList();
        $scope.loadGroupList();
        $scope.loadAllGroupDetails(); // 一次載入所有群組客戶明細
    };

    // 開啟開始日期選擇器
    $scope.openSDatePopup = function () {
        $scope.sDatePopup.opened = true;
    };

    // 開啟結束日期選擇器
    $scope.openEDatePopup = function () {
        $scope.eDatePopup.opened = true;
    };

    // 載入群組列表
    $scope.loadGroupList = function () {
        $scope.loadingGroups = true;
        $http.get(Router.action('EtlErrorWork', 'GetCustomerGroupList'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.groupList = response.data;
                } else if (response.data && response.data.msg) {
                    $scope.groupList = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.groupList = [];
                }
            })
            .catch(function (error) {
                console.error('載入群組列表失敗:', error);
                $scope.groupList = [];
                swal({
                    title: "錯誤",
                    text: "載入群組列表失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loadingGroups = false;
            });
    };

    // 載入所有群組客戶明細 (一次撈完並快取)
    $scope.loadAllGroupDetails = function () {
        $http.get(Router.action('EtlErrorWork', 'GetAllCustomerGroupDetails'))
            .then(function (response) {
                if (response.data && typeof response.data === 'object' && !response.data.msg) {
                    $scope.groupDetailsCache = response.data;
                } else if (response.data && response.data.msg) {
                    $scope.groupDetailsCache = {};
                    console.error('載入群組客戶明細失敗:', response.data.msg);
                } else {
                    $scope.groupDetailsCache = {};
                }
            })
            .catch(function (error) {
                console.error('載入群組客戶明細失敗:', error);
                $scope.groupDetailsCache = {};
            });
    };

    // 載入客戶列表
    $scope.loadCustomerList = function () {
        $scope.loadingCustomers = true;
        $http.get(Router.action('EtlErrorWork', 'GetCustomerList'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.customerList = response.data.map(function (item) {
                        return {
                            Text: item.Text,
                            Value: item.Value,
                            Selected: false
                        };
                    });
                } else if (response.data && response.data.msg) {
                    $scope.customerList = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.customerList = [];
                }
            })
            .catch(function (error) {
                console.error('載入客戶列表失敗:', error);
                $scope.customerList = [];
                swal({
                    title: "錯誤",
                    text: "載入客戶列表失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loadingCustomers = false;
            });
    };

    // 群組變更事件
    $scope.onGroupChange = function () {
        // 先取消全部客戶選取
        angular.forEach($scope.customerList, function(customer) {
            customer.Selected = false;
        });

        if (!$scope.searchData.selectedGroup) {
            return;
        }

        // 從快取取得群組客戶明細
        var custCodes = $scope.groupDetailsCache[$scope.searchData.selectedGroup] || [];
        
        // 將群組內的客戶全部勾選
        angular.forEach($scope.customerList, function(customer) {
            if (custCodes.indexOf(customer.Value) !== -1) {
                customer.Selected = true;
            }
        });
    };

    // 取得目前選中的客戶
    $scope.getSelectedCustomers = function () {
        return $scope.customerList
            .filter(function(customer) { return customer.Selected; })
            .map(function(customer) { return customer.Text; });
    };

    // 全選客戶
    $scope.selectAllCustomers = function () {
        angular.forEach($scope.customerList, function(customer) {
            customer.Selected = true;
        });
    };

    // 全不選客戶
    $scope.deselectAllCustomers = function () {
        angular.forEach($scope.customerList, function(customer) {
            customer.Selected = false;
        });
    };

    // 格式化日期為 yyyy-MM-dd
    $scope.formatDate = function (date) {
        if (!date) return '';
        var d = new Date(date);
        var year = d.getFullYear();
        var month = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    };

    // 下載 Excel
    $scope.downloadExcel = function () {
        if (!$scope.searchData.sDate || !$scope.searchData.eDate) {
            swal({
                title: "提醒",
                text: "請選擇日期範圍",
                icon: "warning"
            });
            return;
        }

        var selectedCustomers = $scope.getSelectedCustomers();
        if (selectedCustomers.length === 0) {
            swal({
                title: "提醒",
                text: "請至少選擇一個客戶",
                icon: "warning"
            });
            return;
        }

        $scope.downloading = true;
        $('.loader').addClass("is-active");

        var postData = {
            sDate: $scope.formatDate($scope.searchData.sDate),
            eDate: $scope.formatDate($scope.searchData.eDate),
            custNames: selectedCustomers
        };

        $http({
            method: 'POST',
            url: Router.action('EtlErrorWork', 'DownloadExcel'),
            data: postData,
            headers: { 'Content-Type': 'application/json' }
        })
            .then(function (response) {
                if (response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                } else {
                    if (response.data.msg && response.data.msg !== "") {
                        swal({
                            title: response.data.msg,
                            icon: "error"
                        });
                    } else if (response.data.fileName && response.data.fileName !== "") {
                        var path = Router.action('Download', 'DownloadFile') +
                            '?fileGuid=' + response.data.fileGuid +
                            '&filename=' + response.data.fileName;

                        var link = document.createElement('a');
                        link.href = path;
                        link.download = response.data.fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                    }
                }
            })
            .catch(function (error) {
                console.error('下載失敗:', error);
                swal({
                    title: "錯誤",
                    text: "下載失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.downloading = false;
                $('.loader').removeClass("is-active");
            });
    };

    // 頁面載入時初始化
    $scope.init();
});
