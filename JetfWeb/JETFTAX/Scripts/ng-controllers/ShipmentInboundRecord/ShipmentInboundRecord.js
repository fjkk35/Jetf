mainApp.controller('ShipmentInboundRecordController', ['$scope', '$http', function ($scope, $http) {
    // 初始化資料
    $scope.data = [];
    $scope.loading = false;
    $scope.exporting = false;
    $scope.exportingCustomer = false;
    $scope.isSearched = false;

    // 分頁相關
    $scope.currentPage = 1;
    $scope.pageSize = "10";
    $scope.totalCount = 0;
    $scope.totalPages = 0;

    // 欄位顯示/隱藏設定 (Cookie)
    var columnSettingCookieKey = 'ShipmentInboundRecord_ColumnVisible_v3';

    $scope.columnOptions = [
        { key: 'seq', title: '序號', locked: false },
        { key: 'inboundDate', title: '入庫日期', locked: false },
        { key: 'dataType', title: '進口方式', locked: false },
        { key: 'customer', title: '客戶', locked: false },
        { key: 'sourceType', title: '貨件來源', locked: false },
        { key: 'processType', title: '處理方式', locked: false },
        { key: 'processTime', title: '客服處理日期', locked: false },
        { key: 'processOpe', title: '客服處理人', locked: false },
        { key: 'outboundDate', title: '出庫日期', locked: false },
        { key: 'outboundTime', title: '出庫操作日', locked: false },
        { key: 'outboundOpe', title: '出庫操作人', locked: false },
        { key: 'warehouseProcessName', title: '倉庫狀態', locked: false },
        { key: 'warehouseProcessTime', title: '倉庫狀態操作日', locked: false },
        { key: 'warehouseProcessOpe', title: '倉庫狀態操作人', locked: false },
        { key: 'processTransName', title: '重出派件公司', locked: false },
        { key: 'processImporter', title: '收件人', locked: false },
        { key: 'processImporterPhone', title: '電話', locked: false },
        { key: 'processImporterAddr', title: '宅配地址', locked: false },
        { key: 'storeCode', title: '門市店號', locked: false },
        { key: 'storeName', title: '門市名稱', locked: false },
        { key: 'freightPayerName', title: '運費支付方', locked: false },
        { key: 'totalAmount', title: '代收款總金額', locked: false },
        { key: 'returnReason', title: '退件原因', locked: false },
        { key: 'exceptionReason', title: '異常原因', locked: false },
        { key: 'remark', title: '備註', locked: false },
        { key: 'seqNo', title: '流水號', locked: false },
        { key: 'locationCode', title: '儲位', locked: false },
        { key: 'size', title: '尺寸', locked: false },
        { key: 'outboundTrackingNo', title: '重出單號', locked: false },
        { key: 'cod', title: '到付款', locked: false },
        { key: 'freightFee', title: '運費', locked: false },
        { key: 'tax', title: '稅金', locked: false },
        { key: 'ccfee', title: '報關費', locked: false },
        { key: 'fee', title: '代收手續費', locked: false }
    ];

    function getDefaultVisibleColumns() {
        var visible = {};
        $scope.columnOptions.forEach(function (c) {
            visible[c.key] = true;
        });
        // 預設至少要顯示單號（fixed column 也比較合理保留）
        visible.trackingNo = true;
        return visible;
    }

    function setCookie(name, value, days) {
        try {
            var expires = '';
            if (days && days > 0) {
                var date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = '; expires=' + date.toUTCString();
            }
            document.cookie = name + '=' + encodeURIComponent(value) + expires + '; path=/';
        } catch (e) {
            // ignore
        }
    }

    function getCookie(name) {
        try {
            var nameEQ = name + '=';
            var ca = (document.cookie || '').split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) === ' ') c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) === 0) return decodeURIComponent(c.substring(nameEQ.length, c.length));
            }
            return null;
        } catch (e) {
            return null;
        }
    }

    function loadColumnSettingFromCookie() {
        try {
            var raw = getCookie(columnSettingCookieKey);
            if (!raw) return null;
            var obj = JSON.parse(raw);
            if (!obj || typeof obj !== 'object') return null;
            return obj;
        } catch (e) {
            return null;
        }
    }

    function saveColumnSettingToCookie() {
        try {
            setCookie(columnSettingCookieKey, JSON.stringify($scope.visibleColumns || {}), 365);
        } catch (e) {
            // ignore
        }
    }

    $scope.getCargoReceiptUrl = function (cargoNumber) {
        return Router.action('Cargo', 'CargoSignReceipt') +
            '?cargoNumber=' + encodeURIComponent((cargoNumber || '').toString().trim());
    };

    $scope.visibleColumns = loadColumnSettingFromCookie() || getDefaultVisibleColumns();

    // 每次勾選立即保存
    $scope.$watch('visibleColumns', function () {
        // locked 欄位強制維持 true
        $scope.columnOptions.forEach(function (c) {
            if (c.locked) {
                $scope.visibleColumns[c.key] = true;
            }
        });
        saveColumnSettingToCookie();
    }, true);

    $scope.selectAllColumns = function () {
        $scope.columnOptions.forEach(function (c) {
            $scope.visibleColumns[c.key] = true;
        });
    };

    $scope.deselectAllColumns = function () {
        $scope.columnOptions.forEach(function (c) {
            $scope.visibleColumns[c.key] = c.locked ? true : false;
        });
    };

    $scope.openColumnSetting = function () {
        // 使用 Bootstrap (Modal) 方式彈出視窗
        $('#columnSettingModal').modal('show');
    };

    // 日期選擇器狀態
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.outboundStartDatePopup = { opened: false };
    $scope.outboundEndDatePopup = { opened: false };

    // 日期選擇器選項
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 12, 31),
        minDate: new Date(2000, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    // 進口方式下拉選單
    $scope.dataTypes = [
        { Value: '海運', Text: '海運' },
        { Value: '空運', Text: '空運' }
    ];

    // 查詢條件
    $scope.searchForm = {
        inboundDateStart: null,
        inboundDateEnd: null,
        outboundDateStart: null,
        outboundDateEnd: null,
        isOutbound: '',
        custCode: '',
        sourceType: '',
        trackingNo: '',
        outboundTrackingNo: '',
        processType: '',
        locationCode: '',
        dataType: '',
        warehouseProcessType: '',
        warehouseProcessTypeIsEmpty: false,
        unknownShipmentOnly: false
    };

    // 客戶多選輸出（由 customerMultiSelect directive 寫入）
    $scope.customerSelectAll = true;
    $scope.selectedCustCodes = [];
    $scope.customerDisplayText = '全選';
    $scope.customerDisplayFullText = '全選';

    // 下拉選單
    // 移除:$scope.custList / customerSelection 與其相關函式,改由 directive 自行載入與管理
    $scope.sourceTypeList = [];
    $scope.processTypeList = [];
    $scope.warehouseProcessTypeList = [];

    // 初始化
    $scope.init = function () {
        // 移除:$scope.loadCustList();
        $scope.loadSourceTypeList();
        $scope.loadProcessTypeList();
        $scope.loadWarehouseProcessTypeList();
    };

    // 載入貨物來源清單
    $scope.loadSourceTypeList = function () {
        $http.get(Router.action('ShipmentInboundRecord', 'GetSourceTypeList'))
            .then(function (response) {
                $scope.sourceTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入貨物來源清單失敗:', error);
                alert('載入貨物來源清單失敗');
            });
    };

    // 載入處理方式清單
    $scope.loadProcessTypeList = function () {
        $http.get(Router.action('ShipmentInboundRecord', 'GetProcessTypeList'))
            .then(function (response) {
                $scope.processTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入處理方式清單失敗:', error);
                alert('載入處理方式清單失敗');
            });
    };

    // 載入倉庫狀態清單
    $scope.loadWarehouseProcessTypeList = function () {
        $http.get(Router.action('ShipmentInboundRecord', 'GetWarehouseProcessTypeList'))
            .then(function (response) {
                $scope.warehouseProcessTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入倉庫狀態清單失敗:', error);
                alert('載入倉庫狀態清單失敗');
            });
    };

    // 開啟開始日期選擇器
    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    // 開啟結束日期選擇器
    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    // 開啟出庫開始日期選擇器
    $scope.openOutboundStartDatePopup = function () {
        $scope.outboundStartDatePopup.opened = true;
    };

    // 開啟出庫結束日期選擇器
    $scope.openOutboundEndDatePopup = function () {
        $scope.outboundEndDatePopup.opened = true;
    };

    // 查詢
    $scope.search = function () {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    $scope.onWarehouseProcessTypeIsEmptyChanged = function () {
        if ($scope.searchForm.warehouseProcessTypeIsEmpty) {
            $scope.searchForm.warehouseProcessType = '';
        }
    };

    function getIsOutbound() {
        if ($scope.searchForm.isOutbound === 'true') {
            return true;
        }

        if ($scope.searchForm.isOutbound === 'false') {
            return false;
        }

        return null;
    }

    // 清除查詢條件
    $scope.clearSearch = function () {
        $scope.searchForm = {
            inboundDateStart: null,
            inboundDateEnd: null,
            outboundDateStart: null,
            outboundDateEnd: null,
            isOutbound: '',
            custCode: '',
            sourceType: '',
            trackingNo: '',
            outboundTrackingNo: '',
            processType: '',
            locationCode: '',
            dataType: '',
            warehouseProcessType: '',
            warehouseProcessTypeIsEmpty: false,
            unknownShipmentOnly: false
        };

        // directive 預設即全選；這裡只重置輸出綁定即可
        $scope.customerSelectAll = true;
        $scope.selectedCustCodes = [];
        $scope.customerDisplayText = '全選';
        $scope.customerDisplayFullText = '全選';

        $scope.data = [];
        $scope.isSearched = false;
        $scope.totalCount = 0;
    };

    // 載入資料
    $scope.loadData = function () {
        if (!validateDateRanges()) {
            return;
        }

        $scope.loading = true;

        var request = {
            InboundDateStart: formatDate($scope.searchForm.inboundDateStart),
            InboundDateEnd: formatDate($scope.searchForm.inboundDateEnd),
            OutboundDateStart: formatDate($scope.searchForm.outboundDateStart),
            OutboundDateEnd: formatDate($scope.searchForm.outboundDateEnd),
            IsOutbound: getIsOutbound(),
            CustCode: $scope.searchForm.custCode,
            CustCodes: $scope.customerSelectAll ? [] : ($scope.selectedCustCodes || []),
            SourceType: $scope.searchForm.sourceType,
            TrackingNo: $scope.searchForm.trackingNo,
            OutboundTrackingNo: $scope.searchForm.outboundTrackingNo,
            ProcessType: $scope.searchForm.processType,
            LocationCode: $scope.searchForm.locationCode,
            DataType: $scope.searchForm.dataType,
            WarehouseProcessType: $scope.searchForm.warehouseProcessType,
            WarehouseProcessTypeIsEmpty: $scope.searchForm.warehouseProcessTypeIsEmpty,
            IsOrderOriginal: $scope.searchForm.unknownShipmentOnly ? false : null,
            Page: $scope.currentPage,
            PageSize: parseInt($scope.pageSize)
        };

        $http.post(Router.action('ShipmentInboundRecord', 'SearchData'), request)
            .then(function (response) {
                if (response.data.error) {
                    alert('查詢失敗: ' + response.data.error);
                    return;
                }

                $scope.data = response.data.Data || [];
                $scope.totalCount = response.data.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize));
                $scope.isSearched = true;

                updateRecordsInfo();
            })
            .catch(function (error) {
                console.error('查詢失敗:', error);
                alert('查詢失敗，請稍後再試');
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 下載Excel
    $scope.downloadExcel = function () {
        if (!validateDateRanges()) {
            return;
        }

        $scope.exporting = true;

        var request = {
            InboundDateStart: formatDate($scope.searchForm.inboundDateStart),
            InboundDateEnd: formatDate($scope.searchForm.inboundDateEnd),
            OutboundDateStart: formatDate($scope.searchForm.outboundDateStart),
            OutboundDateEnd: formatDate($scope.searchForm.outboundDateEnd),
            IsOutbound: getIsOutbound(),
            CustCode: $scope.searchForm.custCode,
            CustCodes: $scope.customerSelectAll ? [] : ($scope.selectedCustCodes || []),
            SourceType: $scope.searchForm.sourceType,
            TrackingNo: $scope.searchForm.trackingNo,
            OutboundTrackingNo: $scope.searchForm.outboundTrackingNo,
            ProcessType: $scope.searchForm.processType,
            LocationCode: $scope.searchForm.locationCode,
            DataType: $scope.searchForm.dataType,
            WarehouseProcessType: $scope.searchForm.warehouseProcessType,
            WarehouseProcessTypeIsEmpty: $scope.searchForm.warehouseProcessTypeIsEmpty,
            IsOrderOriginal: $scope.searchForm.unknownShipmentOnly ? false : null,
            Page: 1,
            PageSize: 10
        };

        $http.post(Router.action('ShipmentInboundRecord', 'ExportExcel'), request)
            .then(function (response) {
                var data = response.data || {};

                if (data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (data.msg) {
                    alert(data.msg);
                    return;
                }

                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') + '?fileGuid=' + data.fileGuid + '&fileName=' + encodeURIComponent(data.fileName);
                    // 觸發下載
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            })
            .catch(function (error) {
                console.error('下載失敗:', error);
                alert('下載失敗，請稍後再試');
            })
            .finally(function () {
                $scope.exporting = false;
            });
    };

    // 下載客戶版 Excel
    $scope.downloadCustomerExcel = function () {
        if (!validateDateRanges()) {
            return;
        }

        $scope.exportingCustomer = true;

        var request = {
            InboundDateStart: formatDate($scope.searchForm.inboundDateStart),
            InboundDateEnd: formatDate($scope.searchForm.inboundDateEnd),
            OutboundDateStart: formatDate($scope.searchForm.outboundDateStart),
            OutboundDateEnd: formatDate($scope.searchForm.outboundDateEnd),
            IsOutbound: getIsOutbound(),
            CustCode: $scope.searchForm.custCode,
            CustCodes: $scope.customerSelectAll ? [] : ($scope.selectedCustCodes || []),
            SourceType: $scope.searchForm.sourceType,
            TrackingNo: $scope.searchForm.trackingNo,
            OutboundTrackingNo: $scope.searchForm.outboundTrackingNo,
            ProcessType: $scope.searchForm.processType,
            LocationCode: $scope.searchForm.locationCode,
            DataType: $scope.searchForm.dataType,
            WarehouseProcessType: $scope.searchForm.warehouseProcessType,
            WarehouseProcessTypeIsEmpty: $scope.searchForm.warehouseProcessTypeIsEmpty,
            IsOrderOriginal: $scope.searchForm.unknownShipmentOnly ? false : null,
            Page: 1,
            PageSize: 10
        };

        $http.post(Router.action('ShipmentInboundRecord', 'ExportCustomerExcel'), request)
            .then(function (response) {
                var data = response.data || {};

                if (data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (data.msg) {
                    alert(data.msg);
                    return;
                }

                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') + '?fileGuid=' + data.fileGuid + '&fileName=' + encodeURIComponent(data.fileName);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            })
            .catch(function (error) {
                console.error('下載客戶 Excel 失敗:', error);
                alert('下載失敗，請稍後再試');
            })
            .finally(function () {
                $scope.exportingCustomer = false;
            });
    };

    // 切換每頁顯示筆數
    $scope.changePageSize = function () {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    // 換頁
    $scope.changePage = function (page) {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }
        $scope.currentPage = page;
        $scope.loadData();
    };

    // 上一頁
    $scope.previousPage = function () {
        if ($scope.currentPage > 1) {
            $scope.currentPage--;
            $scope.loadData();
        }
    };

    // 下一頁
    $scope.nextPage = function () {
        if ($scope.currentPage < $scope.totalPages) {
            $scope.currentPage++;
            $scope.loadData();
        }
    };

    // 產生頁碼陣列
    $scope.getPages = function () {
        var pages = [];
        var maxVisible = 10;
        var startPage = Math.max(1, $scope.currentPage - Math.floor(maxVisible / 2));
        var endPage = Math.min($scope.totalPages, startPage + maxVisible - 1);

        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        for (var i = startPage; i <= endPage; i++) {
            pages.push(i);
        }
        return pages;
    };

    // 更新分頁資訊
    function updateRecordsInfo() {
        if ($scope.totalCount === 0) {
            return;
        }

        var start = ($scope.currentPage - 1) * parseInt($scope.pageSize) + 1;
        var end = Math.min($scope.currentPage * parseInt($scope.pageSize), $scope.totalCount);
        $scope.recordsInfo = '顯示第 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    }

    function validateDateRanges() {
        if ($scope.searchForm.inboundDateStart && $scope.searchForm.inboundDateEnd &&
            $scope.searchForm.inboundDateStart > $scope.searchForm.inboundDateEnd) {
            alert('入庫日期(起)不可大於入庫日期(迄)');
            return false;
        }

        if ($scope.searchForm.outboundDateStart && $scope.searchForm.outboundDateEnd &&
            $scope.searchForm.outboundDateStart > $scope.searchForm.outboundDateEnd) {
            alert('出庫日期(起)不可大於出庫日期(迄)');
            return false;
        }

        return true;
    }

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

    // 頁面載入時執行初始化
    $scope.init();
}]);
