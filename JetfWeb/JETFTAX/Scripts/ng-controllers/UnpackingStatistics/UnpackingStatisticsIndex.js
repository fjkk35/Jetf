// 註冊到全域 mainApp 模組，避免衝突
mainApp.controller('UnpackingStatisticsController', function ($scope, $http) {
    $scope.query = {
        startDate: new Date(moment().startOf('month').toDate()),
        endDate: new Date(moment().toDate())
    };
    $scope.sheets = [];
    $scope.isExporting = false;

    // 日期選擇器設定
    $scope.dateOptions = {
        formatYear: 'yyyy',
        minDate: new Date(1900, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    // 日期彈出視窗狀態
    $scope.datePopup = {};

    // 開啟日期彈出視窗
    $scope.openDatePopup = function (fieldName) {
        $scope.datePopup[fieldName] = true;
    };

    // 快速日期選擇
    $scope.setQuickDate = function (type) {
        var today = moment();
        
        switch (type) {
            case 'today':
                // 當日：開始和結束都是今天
                $scope.query.startDate = new Date(today.toDate());
                $scope.query.endDate = new Date(today.toDate());
                break;
                
            case 'thisMonth':
                // 當月：當月第一天到今天
                $scope.query.startDate = new Date(today.startOf('month').toDate());
                $scope.query.endDate = new Date(moment().toDate()); // 重新建立moment物件避免修改到startOf
                break;
                
            case 'lastMonth':
                // 上月：上月第一天到上月最後一天
                var lastMonth = moment().subtract(1, 'month');
                $scope.query.startDate = new Date(lastMonth.startOf('month').toDate());
                $scope.query.endDate = new Date(lastMonth.endOf('month').toDate());
                break;
        }
    };

    // 根據客戶數量決定表格容器樣式
    $scope.getTableStyle = function (sheet) {
        if (!sheet || !sheet.Customers) return {};
        
        var customerCount = sheet.Customers.length;
        
        if (customerCount <= 3) {
            // 客戶數量少時，不需要水平滾動
            return {
                'overflow-x': 'visible'
            };
        } else {
            // 客戶數量多時，啟用水平滾動
            return {
                'overflow-x': 'auto',
                'max-width': '100%'
            };
        }
    };

    // 根據客戶數量決定表格寬度樣式
    $scope.getTableWidthStyle = function (sheet) {
        if (!sheet || !sheet.Customers) return {};
        
        var customerCount = sheet.Customers.length;
        
        if (customerCount <= 3) {
            // 客戶數量少時，使用固定寬度
            var baseWidth = 200; // 日期 + 當日合計欄位
            var customerWidth = 120; // 每個客戶欄位寬度
            var totalWidth = baseWidth + (customerCount * customerWidth);
            
            return {
                'width': totalWidth + 'px',
            };
        } else {
            // 客戶數量多時，使用全寬度
            return {
                'width': '100%',
                'min-width': (200 + customerCount * 120) + 'px'
            };
        }
    };

    // 根據客戶數量決定客戶欄位樣式
    $scope.getColumnStyle = function (sheet) {
        if (!sheet || !sheet.Customers) return {};
        
        var customerCount = sheet.Customers.length;
        
        return {
            'min-width': customerCount <= 3 ? '120px' : '100px',
            'white-space': 'nowrap'
        };
    };

    $scope.load = function () {
        if (!$scope.query.startDate || !$scope.query.endDate) {
            alert('請選擇日期區間');
            return;
        }
        
        // 轉換日期為字串格式
        var startDateStr = moment($scope.query.startDate).format('YYYY-MM-DD');
        var endDateStr = moment($scope.query.endDate).format('YYYY-MM-DD');
        
        var url = Router.action('UnpackingStatistics', 'GetData') + '?startDate=' + startDateStr + '&endDate=' + endDateStr;
        $http.get(url).then(function (res) {
            if (res.data.status === 'error') {
                alert(res.data.msg || '取得資料失敗');
                return;
            }
            $scope.sheets = res.data;
        }, function () { alert('取得資料失敗'); });
    };

    $scope.getColTotal = function (sheet, customer) {
        var total = 0;
        angular.forEach(sheet.Rows, function (r) {
            total += (r.CustomerValues[customer] || 0);
        });
        return total;
    };

    $scope.export = function () {
        if ($scope.sheets.length === 0) { return; }
        
        $scope.isExporting = true;
        
        // 轉換日期為字串格式
        var startDateStr = moment($scope.query.startDate).format('YYYY-MM-DD');
        var endDateStr = moment($scope.query.endDate).format('YYYY-MM-DD');
        
        $http({
            method: 'POST',
            url: Router.action('UnpackingStatistics', 'Export'),
            data: {
                startDate: startDateStr || '',
                endDate: endDateStr || ''
            },
            responseType: 'arraybuffer' // 重點：告訴 $http 回傳二進位資料
        }).then(function (response) {
            $scope.isExporting = false;
            // 檢查是否回傳錯誤（檢查 Content-Type）
            var contentType = response.headers('content-type');
            if (contentType && contentType.indexOf('application/json') !== -1) {
                // 如果是 JSON，表示有錯誤
                var errorText = new TextDecoder().decode(response.data);
                var errorData = JSON.parse(errorText);
                swal({
                    title: "錯誤",
                    text: errorData.msg || "匯出失敗",
                    icon: "error"
                });
                return;
            }
            
            // 成功時建立下載
            var fileName = "拆袋統計表_" + moment().format('YYYYMMDDHHMMSS') + ".xlsx";
            var blob = new Blob([response.data], { 
                type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
            });
            var downloadUrl = URL.createObjectURL(blob);

            var a = document.createElement("a");
            a.href = downloadUrl;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(downloadUrl);
            
        }, function(error) {
            $scope.isExporting = false;
            swal({
                title: "錯誤",
                text: "匯出失敗",
                icon: "error"
            });
        });
    };

    // 初始載入
    $scope.load();
});
