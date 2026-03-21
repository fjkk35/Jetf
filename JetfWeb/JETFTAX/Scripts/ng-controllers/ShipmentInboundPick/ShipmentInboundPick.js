mainApp.controller('ShipmentInboundPickController', ['$scope', '$http', '$window', function ($scope, $http, $window) {
    // 初始化資料
    $scope.data = [];
    $scope.loading = false;
    $scope.isSearched = false;

    // 日期選擇器狀態
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };

    // 日期選擇器選項
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 12, 31),
        minDate: new Date(2000, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    // 設定預設日期為今天
    var today = new Date();
    today.setHours(0, 0, 0, 0);

    // 查詢條件
    $scope.searchForm = {
        processTimeStart: new Date(today),
        processTimeEnd: new Date(today)
    };

    // customer-multi-select 需要的變數
    $scope.customerSelectAll = true;
    $scope.selectedCustCodes = [];
    $scope.customerDisplayText = '';
    $scope.customerDisplayFullText = '';

    // 開啟開始日期選擇器
    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    // 開啟結束日期選擇器
    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    // 查詢
    $scope.search = function () {
        $scope.loadData();
    };

    // 清除查詢條件
    $scope.clearSearch = function () {
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        $scope.searchForm = {
            processTimeStart: new Date(today),
            processTimeEnd: new Date(today)
        };
        
        $scope.customerSelectAll = true;
        $scope.selectedCustCodes = [];
        $scope.customerDisplayText = '';
        $scope.customerDisplayFullText = '';
        
        $scope.data = [];
        $scope.isSearched = false;
    };

    // 載入資料
    $scope.loadData = function () {
        if (!$scope.searchForm.processTimeStart || !$scope.searchForm.processTimeEnd) {
            alert('請選擇日期區間');
            return;
        }

        if ($scope.searchForm.processTimeStart > $scope.searchForm.processTimeEnd) {
            alert('開始日期不可大於結束日期');
            return;
        }

        $scope.loading = true;

        var request = {
            ProcessTimeStart: formatDate($scope.searchForm.processTimeStart),
            ProcessTimeEnd: formatDate($scope.searchForm.processTimeEnd),
            CustCodes: ($scope.selectedCustCodes && $scope.selectedCustCodes.length > 0) ? $scope.selectedCustCodes : null
        };

        $http.post(Router.action('ShipmentInboundPick', 'SearchData'), request)
            .then(function (response) {
                if (response.data.error) {
                    alert('查詢失敗: ' + response.data.error);
                    return;
                }

                $scope.data = response.data.Data || [];
                $scope.isSearched = true;
            })
            .catch(function (error) {
                console.error('查詢失敗:', error);
                alert('查詢失敗，請稍後再試');
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        var request = {
            ProcessTimeStart: formatDate($scope.searchForm.processTimeStart),
            ProcessTimeEnd: formatDate($scope.searchForm.processTimeEnd),
            CustCodes: ($scope.selectedCustCodes && $scope.selectedCustCodes.length > 0) ? $scope.selectedCustCodes : null
        };

        var form = document.createElement('form');
        form.method = 'POST';
        form.action = Router.action('ShipmentInboundPick', 'ExportExcel');

        for (var key in request) {
            if (request[key] !== null && request[key] !== undefined) {
                if (Array.isArray(request[key])) {
                    for (var i = 0; i < request[key].length; i++) {
                        var input = document.createElement('input');
                        input.type = 'hidden';
                        input.name = key + '[' + i + ']';
                        input.value = request[key][i];
                        form.appendChild(input);
                    }
                } else {
                    var input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = key;
                    input.value = request[key];
                    form.appendChild(input);
                }
            }
        }

        document.body.appendChild(form);
        form.submit();
        document.body.removeChild(form);
    };

    // 格式化日期
    function formatDate(date) {
        if (!date) return '';
        var d = new Date(date);
        var month = '' + (d.getMonth() + 1);
        var day = '' + d.getDate();
        var year = d.getFullYear();

        if (month.length < 2) month = '0' + month;
        if (day.length < 2) day = '0' + day;

        return [year, month, day].join('-');
    }
}]);
