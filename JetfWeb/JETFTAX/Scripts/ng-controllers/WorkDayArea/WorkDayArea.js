mainApp.controller('WorkDayAreaController', ['$scope', '$http', function ($scope, $http) {

    // 初始化
    $scope.init = function () {
        $scope.workAreaList = [];

        //修改權限  
        $scope.workDayAreaAuthoritiy = window.UserCtx.authorities.indexOf('WorkDayArea') > -1;

        // 取得當月第一天和最後一天
        var today = new Date();
        var firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
        var lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);
        
        $scope.queryData = {
            workAreaId: 0,
            startDate: firstDay,
            endDate: lastDay
        };
        $scope.isLoading = false;
        $scope.results = [];

        // 日期選擇器設定
        $scope.dateOptions = {
            formatYear: 'yyyy',
            minDate: new Date(2020, 1, 1),
            maxDate: new Date(2030, 12, 31),
            startingDay: 0,
            showWeeks: false
        };

        // 日期彈出視窗狀態
        $scope.startDatePopup = {
            opened: false
        };
        $scope.endDatePopup = {
            opened: false
        };

        // 載入作業地區列表
        $scope.loadWorkAreaList();
    };

    // 監聽作業地區變更
    $scope.$watch('queryData.workAreaId', function (newValue, oldValue) {
        // 如果作業地區變更且不是初始化時，清空查詢結果
        if (newValue !== oldValue && oldValue !== undefined) {
            $scope.results = [];
        }
    });

    // 開啟開始日期彈出視窗
    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    // 開啟結束日期彈出視窗
    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    // 取得當前作業地區名稱
    $scope.getCurrentWorkAreaName = function () {
        if (!$scope.queryData.workAreaId || !$scope.workAreaList || $scope.workAreaList.length === 0) {
            return '';
        }
        var selectedArea = $scope.workAreaList.find(function (area) {
            return area.Id == $scope.queryData.workAreaId;
        });
        return selectedArea ? selectedArea.AreaName : '';
    };

    // 載入作業地區列表
    $scope.loadWorkAreaList = function () {
        $http.post(Router.action('WorkDayArea', 'GetWorkAreaList'), {})
            .then(function (response) {
                if (response.data.status == 'success') {
                    $scope.workAreaList = response.data.ReturnObject || [];
                    // 自動選擇第一個作業地區
                    if ($scope.workAreaList.length > 0) {
                        $scope.queryData.workAreaId = $scope.workAreaList[0].Id;
                    }
                } else {
                    swal({
                        title: "錯誤",
                        text: response.data.msg || "載入作業地區失敗",
                        icon: "error"
                    });
                }
            }, function (error) {
                swal({
                    title: "錯誤",
                    text: "載入作業地區發生錯誤",
                    icon: "error"
                });
                console.error(error);
            });
    };

    // 查詢
    $scope.query = function () {
        if (!$scope.queryData.workAreaId || $scope.queryData.workAreaId == 0) {
            swal({
                title: "錯誤",
                text: "請選擇作業地區",
                icon: "error"
            });
            return;
        }

        if (!$scope.queryData.startDate || !$scope.queryData.endDate) {
            swal({
                title: "錯誤",
                text: "請選擇日期區間",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var requestData = {
            workAreaId: parseInt($scope.queryData.workAreaId),
            startDate: $scope.queryData.startDate,
            endDate: $scope.queryData.endDate
        };

        $http.post(Router.action('WorkDayArea', 'Query'), requestData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.results = response.data.ReturnObject || [];
                    if ($scope.results.length === 0) {
                        swal({
                            title: "查詢結果",
                            text: "查無資料",
                            icon: "info"
                        });
                    }
                } else {
                    swal({
                        title: "查詢失敗",
                        text: response.data.msg || "查詢失敗",
                        icon: "error"
                    });
                }
            }, function (error) {
                $scope.isLoading = false;
                swal({
                    title: "錯誤",
                    text: "查詢發生錯誤",
                    icon: "error"
                });
                console.error(error);
            });
    };

    // 切換工作天類型
    $scope.toggleDateType = function (item) {
        if (!item) return;

        // 切換類型：工作天(1) <-> 假日(2)
        var newDateType = item.DateType == 1 ? 2 : 1;
        var newDateTypeName = newDateType == 1 ? "工作天" : "假日";

        swal({
            title: "確認修改",
            text: "確定要將 " + item.Date + " 的類別改為「" + newDateTypeName + "」嗎？",
            icon: "warning",
            buttons: ["取消", "確定"],
            dangerMode: false
        }).then(function (willUpdate) {
            if (willUpdate) {
                $scope.updateWorkDayType(item, newDateType);
            }
        });
    };

    // 更新工作天類型
    $scope.updateWorkDayType = function (item, newDateType) {
        $scope.isLoading = true;

        var requestData = {
            workAreaId: parseInt($scope.queryData.workAreaId),
            date: item.Date,
            dateType: newDateType
        };

        $http.post(Router.action('WorkDayArea', 'UpdateWorkDayType'), requestData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    // 更新顯示
                    item.DateType = newDateType;
                    item.DateTypeName = newDateType == 1 ? "工作天" : "假日";
                    
                    swal({
                        title: "成功",
                        text: "修改成功",
                        icon: "success",
                        timer: 1500,
                        buttons: false
                    });
                } else {
                    swal({
                        title: "修改失敗",
                        text: response.data.msg || "修改失敗",
                        icon: "error"
                    });
                }
            }, function (error) {
                $scope.isLoading = false;
                swal({
                    title: "錯誤",
                    text: "修改發生錯誤",
                    icon: "error"
                });
                console.error(error);
            });
    };

    // 取得切換按鈕文字
    $scope.getToggleButtonText = function (dateType) {
        return dateType == 1 ? "假　日" : "工作日";
    };

    // 取得日期類型的CSS class
    $scope.getDateTypeClass = function (dateType) {
        return dateType == 1 ? "label-success" : "label-danger";
    };

    // 取得切換按鈕的CSS class
    $scope.getToggleButtonClass = function (dateType) {
        // dateType == 1 是工作天，按鈕顯示「假日」，用橘色
        // dateType == 2 是假日，按鈕顯示「工作日」，用綠色
        return dateType == 1 ? "btn-warning" : "btn-success";
    };

    // 初始化
    $scope.init();

}]);
