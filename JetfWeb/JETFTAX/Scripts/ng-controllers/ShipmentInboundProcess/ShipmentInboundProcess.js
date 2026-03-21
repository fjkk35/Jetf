mainApp.controller('ShipmentInboundProcessController', function ($scope, $http) {
    // 初始化資料
    $scope.data = [];
    $scope.loading = false;
    $scope.isSearched = false;
    $scope.saving = false;
    $scope.exporting = false;

    $scope.uploading = false;
    $scope.uploadResult = null;
    $scope.uploadErrors = [];

    $scope.uploadingReturnReason = false;
    $scope.uploadReturnReasonResult = null;
    $scope.uploadReturnReasonErrors = [];

    // 分頁相關
    $scope.currentPage = 1;
    $scope.pageSize = "10";
    $scope.totalCount = 0;
    $scope.totalPages = 0;

    // 日期選擇器狀態
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.pickupTimePopup = { opened: false };

    // 日期選擇器選項
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 12, 31),
        minDate: new Date(2000, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    // 查詢條件
    $scope.searchForm = {
        dataType: '',
        sourceType: '',
        trackingNo: '',
        isClosed: '',
        inboundDateStart: new Date(),
        inboundDateEnd: new Date()
    };

    // 進口方式下拉選單
    $scope.dataTypes = [
        { Value: '海運', Text: '海運' },
        { Value: '空運', Text: '空運' }
    ];

    // 貨物來源下拉選單
    $scope.sourceTypeList = [];

    // 處理方式相關
    $scope.processTypeList = [];
    $scope.processTransNoList = [];
    $scope.freightPayerNoList = [];
    $scope.currentItem = null;
    $scope.modalTitle = '';
    $scope.isViewMode = false;

    $scope.processForm = {
        processType: null,
        processTransNo: null,
        processImporter: '',
        processImporterPhone: '',
        processImporterAddr: '',
        storeCode: '',
        storeName: '',
        tax: null,
        ccfee: null,
        cod: null,
        freightPayerNo: null,
        freightFee: 0,
        fee: 0,
        carNo: '',
        pickupTime: '',
        remark: ''
    };

    // 初始化
    $scope.init = function () {
        $scope.loadProcessTypeList();
        $scope.loadProcessTransNoList();
        $scope.loadFreightPayerNoList();
        $scope.loadSourceTypeList();

        // 監聽 Modal 關閉事件
        $('#processModal').on('hidden.bs.modal', function () {
            // 重置未處理項目的選擇
            if ($scope.currentItem && !$scope.currentItem.ProcessType) {
                $scope.currentItem.selectedProcessType = '';
                $scope.$apply(); // 通知 Angular 更新視圖
            }
        });
    };

    // 載入處理方式清單
    $scope.loadProcessTypeList = function () {
        $http.get(Router.action('ShipmentInboundProcess', 'GetProcessTypeList'))
            .then(function (response) {
                $scope.processTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入處理方式清單失敗:', error);
            });
    };

    // 載入派件公司清單
    $scope.loadProcessTransNoList = function () {
        $http.get(Router.action('ShipmentInboundProcess', 'GetProcessTransNoList'))
            .then(function (response) {
                $scope.processTransNoList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入派件公司清單失敗:', error);
            });
    };

    // 載入運費支付方清單
    $scope.loadFreightPayerNoList = function () {
        $http.get(Router.action('ShipmentInboundProcess', 'GetFreightPayerNoList'))
            .then(function (response) {
                $scope.freightPayerNoList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入運費支付方清單失敗:', error);
            });
    };

    // customer-multi-select 需要的變數
    $scope.customerSelectAll = true;
    $scope.selectedCustCodes = [];
    $scope.customerDisplayText = '';
    $scope.customerDisplayFullText = '';

    // 載入貨物來源清單
    $scope.loadSourceTypeList = function () {
        $http.get(Router.action('ShipmentInboundProcess', 'GetSourceTypeList'))
            .then(function (response) {
                $scope.sourceTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入貨物來源清單失敗:', error);
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

    // 開啟預計自取時間選擇器
    $scope.openPickupTimePopup = function () {
        $scope.pickupTimePopup.opened = true;
    };

    // 自動計算手續費
    $scope.calcFee = function () {
        var freightFee = parseFloat($scope.processForm.freightFee) || 0;
        var tax = parseFloat($scope.processForm.tax) || 0;
        var ccfee = parseFloat($scope.processForm.ccfee) || 0;
        var cod = parseFloat($scope.processForm.cod) || 0;
        $scope.processForm.fee = (freightFee > 0 || tax > 0 || ccfee > 0 || cod > 0) ? 30 : 0;
    };

    // 重出派件公司變更時的處理
    $scope.onProcessTransNoChange = function () {
        // 當選擇 processTransNo = 3 時，清空宅配地址
        if ($scope.processForm.processTransNo == 3) {
            $scope.processForm.processImporterAddr = '';
        } else {
            // 當選擇 processTransNo != 3 時，清空門市店號和門市名稱
            $scope.processForm.storeCode = '';
            $scope.processForm.storeName = '';
        }
    };

    // 重出運費支付方變更時的處理
    $scope.onFreightPayerNoChange = function () {
        // 當選擇 freightPayerNo = 1 時，重出運費 = 120，其他選項 = 0
        if ($scope.processForm.freightPayerNo == 1) {
            $scope.processForm.freightFee = 120;
        } else {
            $scope.processForm.freightFee = 0;
        }

        $scope.calcFee();
    };

    // 處理方式變更時的處理
    $scope.onProcessTypeChange = function () {
        // 當處理方式改變時，清空不相關的欄位
        if ($scope.processForm.processType != 1 && $scope.processForm.processType != 9) {
            $scope.processForm.processTransNo = null;
            $scope.processForm.freightPayerNo = null;
            $scope.processForm.storeCode = '';
            $scope.processForm.storeName = '';
            $scope.processForm.freightFee = 0;
        }

        if ($scope.processForm.processType != 4) {
            $scope.processForm.carNo = '';
            $scope.processForm.pickupTime = null;
        }

        // 處理方式為 2,3,5,6,7,8 時，清空收件人相關欄位
        if ([2, 3, 5, 6, 7, 8].indexOf($scope.processForm.processType) > -1) {
            $scope.processForm.processImporter = '';
            $scope.processForm.processImporterPhone = '';
            $scope.processForm.processImporterAddr = '';
        }

        // 更新 Modal 標題
        var processTypeName = $scope.processTypeList.find(function (t) {
            return t.value == $scope.processForm.processType;
        });

        if (processTypeName) {
            if ($scope.currentItem && $scope.currentItem.ProcessType) {
                $scope.modalTitle = processTypeName.Text + ' (修改)';
            } else {
                $scope.modalTitle = processTypeName.Text;
            }
        }
    };

    // 開啟處理方式 Modal
    $scope.openProcessModal = function (item) {
        if (!item.selectedProcessType && !item.ProcessType) {
            return;
        }

        $scope.currentItem = item;
        $scope.isViewMode = false;

        var processTypeValue = item.selectedProcessType || item.ProcessType;
        $scope.processForm = {
            trackingNo: item.TrackingNo || '',
            processType: parseInt(processTypeValue),
            processTransNo: null,
            processImporter: '',
            processImporterPhone: '',
            processImporterAddr: '',
            storeCode: '',
            storeName: '',
            tax: null,
            ccfee: null,
            cod: null,
            freightPayerNo: null,
            freightFee: 0,
            fee: 0,
            carNo: '',
            pickupTime: null,
            remark: ''
        };

        // 從 currentItem 載入稅金、報關費、到付款資料
        if (item.Tax !== undefined && item.Tax !== null) {
            $scope.processForm.tax = item.Tax;
        }
        if (item.Ccfee !== undefined && item.Ccfee !== null) {
            $scope.processForm.ccfee = item.Ccfee;
        }
        if (item.Cod !== undefined && item.Cod !== null) {
            $scope.processForm.cod = item.Cod;
        }

        $scope.calcFee();

        var processTypeName = $scope.processTypeList.find(function (t) {
            return t.Value == processTypeValue;
        });
        $scope.modalTitle = processTypeName ? processTypeName.Text : '處理方式';

        $('#processModal').modal('show');
    };
    // 查看已處理項目的詳細資料
    $scope.viewProcessDetail = function (item) {
        $scope.currentItem = item;
        $scope.isViewMode = false;
        $scope.loading = true;

        $http.get(Router.action('ShipmentInboundProcess', 'GetDetailById') + '?id=' + item.Id)
            .then(function (response) {
                if (response.data.error) {
                    swal({
                        title: "載入失敗",
                        text: response.data.error,
                        icon: "error"
                    });
                    return;
                }

                var detail = response.data;

                // 處理 PickupTime 日期格式
                var pickupTimeDate = null;
                if (detail.PickupTime) {
                    // 處理 JSON 日期字串 (例如: "2024-01-15T10:30:00" 或 "/Date(1234567890000)/")
                    if (typeof detail.PickupTime === 'string') {
                        // 檢查是否為 .NET JSON 日期格式 /Date(...)/
                        var dateMatch = detail.PickupTime.match(/\/Date\((\d+)\)\//);
                        if (dateMatch) {
                            pickupTimeDate = new Date(parseInt(dateMatch[1]));
                        } else {
                            // ISO 8601 格式或其他標準日期字串
                            pickupTimeDate = new Date(detail.PickupTime);
                        }
                    } else {
                        pickupTimeDate = new Date(detail.PickupTime);
                    }

                    // 確保日期有效
                    if (isNaN(pickupTimeDate.getTime())) {
                        pickupTimeDate = null;
                    }
                }

                $scope.processForm = {
                    trackingNo: item.TrackingNo || '',
                    processType: parseInt(detail.ProcessType),
                    processTransNo: detail.ProcessTransNo,
                    processImporter: detail.ProcessImporter || '',
                    processImporterPhone: detail.ProcessImporterPhone || '',
                    processImporterAddr: detail.ProcessImporterAddr || '',
                    storeCode: detail.StoreCode || '',
                    storeName: detail.StoreName || '',
                    tax: detail.Tax,
                    ccfee: detail.Ccfee,
                    cod: detail.Cod,
                    freightPayerNo: detail.FreightPayerNo,
                    freightFee: detail.FreightFee || 0,
                    fee: detail.Fee || 0,
                    carNo: detail.CarNo || '',
                    pickupTime: pickupTimeDate,
                    remark: detail.Remark || ''
                };

                $scope.calcFee();

                var processTypeName = $scope.processTypeList.find(function (t) {
                    return t.Value == detail.ProcessType;
                });
                $scope.modalTitle = processTypeName ? processTypeName.Text + ' (修改)' : '處理方式 (修改)';

                $('#processModal').modal('show');
            })
            .catch(function (error) {
                console.error('載入詳細資料失敗:', error);
                swal({
                    title: "載入失敗",
                    text: "無法載入詳細資料，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 儲存處理方式
    $scope.saveProcess = function () {
        // 驗證處理方式必選
        if (!$scope.processForm.processType) {
            swal({
                title: "提醒",
                text: "請選擇處理方式",
                icon: "warning"
            });
            return;
        }

        // 驗證必填欄位
        if ($scope.processForm.processType == 1) {
            if (!$scope.processForm.processTransNo) {
                swal({
                    title: "提醒",
                    text: "請選擇重出派件公司",
                    icon: "warning"
                });
                return;
            }
        }

        $scope.saving = true;

        // 格式化預計自取時間
        var pickupTimeStr = null;
        if ($scope.processForm.pickupTime) {
            var dt = new Date($scope.processForm.pickupTime);
            pickupTimeStr = dt.getFullYear() + '-' +
                String(dt.getMonth() + 1).padStart(2, '0') + '-' +
                String(dt.getDate()).padStart(2, '0') + ' ' +
                String(dt.getHours()).padStart(2, '0') + ':' +
                String(dt.getMinutes()).padStart(2, '0') + ':00';
        }

        var request = {
            Id: $scope.currentItem.Id,
            ProcessType: $scope.processForm.processType,
            ProcessTransNo: $scope.processForm.processTransNo,
            ProcessImporter: $scope.processForm.processImporter,
            ProcessImporterPhone: $scope.processForm.processImporterPhone,
            ProcessImporterAddr: $scope.processForm.processImporterAddr,
            StoreCode: $scope.processForm.storeCode,
            StoreName: $scope.processForm.storeName,
            Tax: $scope.processForm.tax,
            Ccfee: $scope.processForm.ccfee,
            Cod: $scope.processForm.cod,
            FreightPayerNo: $scope.processForm.freightPayerNo,
            FreightFee: $scope.processForm.freightFee,
            Fee: $scope.processForm.fee,
            CarNo: $scope.processForm.carNo,
            PickupTime: pickupTimeStr,
            Remark: $scope.processForm.remark
        };

        $http.post(Router.action('ShipmentInboundProcess', 'UpdateProcessType'), request)
            .then(function (response) {
                if (response.data.status === 'success') {
                    swal({
                        title: "成功",
                        text: "更新成功",
                        icon: "success"
                    });
                    $('#processModal').modal('hide');
                    $scope.loadData();
                } else {
                    swal({
                        title: "失敗",
                        text: response.data.msg || "更新失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('儲存失敗:', error);
                swal({
                    title: "錯誤",
                    text: "儲存失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    // 建立查詢參數的共用方法
    $scope.buildSearchRequest = function (includePaging) {
        var startDate = new Date($scope.searchForm.inboundDateStart);
        var endDate = new Date($scope.searchForm.inboundDateEnd);

        var dateStart = startDate.getFullYear() + '-' +
            String(startDate.getMonth() + 1).padStart(2, '0') + '-' +
            String(startDate.getDate()).padStart(2, '0');

        var dateEnd = endDate.getFullYear() + '-' +
            String(endDate.getMonth() + 1).padStart(2, '0') + '-' +
            String(endDate.getDate()).padStart(2, '0');

        var isClosed = null;
        if ($scope.searchForm.isClosed === 'true') {
            isClosed = true;
        } else if ($scope.searchForm.isClosed === 'false') {
            isClosed = false;
        }

        var request = {
            DataType: $scope.searchForm.dataType,
            CustCodes: ($scope.selectedCustCodes && $scope.selectedCustCodes.length > 0) ? $scope.selectedCustCodes : null,
            SourceType: $scope.searchForm.sourceType ? parseInt($scope.searchForm.sourceType) : null,
            TrackingNo: $scope.searchForm.trackingNo,
            IsClosed: isClosed,
            InboundDateStart: dateStart,
            InboundDateEnd: dateEnd
        };

        if (includePaging) {
            request.Page = $scope.currentPage;
            request.PageSize = parseInt($scope.pageSize);
        }

        return request;
    };

    // 執行查詢
    $scope.search = function () {
        if (!$scope.searchForm.inboundDateStart || !$scope.searchForm.inboundDateEnd) {
            swal({
                title: "提醒",
                text: "請選擇入庫日期",
                icon: "warning"
            });
            return;
        }

        $scope.currentPage = 1;
        $scope.isSearched = true;
        $scope.loadData();
    };

    // 載入資料
    $scope.loadData = function () {
        $scope.loading = true;

        var searchRequest = $scope.buildSearchRequest(true);

        $http.post(Router.action('ShipmentInboundProcess', 'SearchData'), searchRequest)
            .then(function (response) {
                if (response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (response.data.error) {
                    swal({
                        title: "查詢失敗",
                        text: response.data.error,
                        icon: "error"
                    });
                    return;
                }

                $scope.data = response.data.Data || response.data.data || [];
                $scope.totalCount = response.data.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize));

                $scope.updateRecordsInfo();
            })
            .catch(function (error) {
                console.error('查詢失敗:', error);
                swal({
                    title: "查詢失敗",
                    text: "請稍後再試或聯繫系統管理員",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 清除查詢表單
    $scope.clearSearch = function () {
        $scope.searchForm = {
            dataType: '',
            sourceType: '',
            trackingNo: '',
            isClosed: '',
            inboundDateStart: new Date(),
            inboundDateEnd: new Date()
        };

        $scope.customerSelectAll = true;
        $scope.selectedCustCodes = [];
        $scope.customerDisplayText = '';
        $scope.customerDisplayFullText = '';

        $scope.data = [];
        $scope.isSearched = false;
        $scope.currentPage = 1;
        $scope.totalCount = 0;
        $scope.totalPages = 0;
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        if (!$scope.searchForm.inboundDateStart || !$scope.searchForm.inboundDateEnd) {
            swal({
                title: "提醒",
                text: "請選擇入庫日期",
                icon: "warning"
            });
            return;
        }

        $scope.exporting = true;

        var request = $scope.buildSearchRequest(false);

        var form = document.createElement('form');
        form.method = 'POST';
        form.action = Router.action('ShipmentInboundProcess', 'ExportExcel');
        form.target = '_blank';

        for (var key in request) {
            if (request[key] !== null && request[key] !== undefined && request[key] !== '') {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = key;
                input.value = request[key];
                form.appendChild(input);
            }
        }

        document.body.appendChild(form);
        form.submit();
        document.body.removeChild(form);

        $scope.exporting = false;
    };

    // 更新記錄資訊
    $scope.updateRecordsInfo = function () {

        if ($scope.totalCount === 0) {
            return;
        }

        var start = ($scope.currentPage - 1) * parseInt($scope.pageSize) + 1;
        var end = Math.min($scope.currentPage * parseInt($scope.pageSize), $scope.totalCount);
        $scope.recordsInfo = '顯示 ' + start + ' 到 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
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

    // 改變每頁顯示筆數
    $scope.changePageSize = function () {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    // 產生頁碼陣列
    $scope.getPages = function () {
        var pages = [];
        var startPage = Math.max(1, $scope.currentPage - 2);
        var endPage = Math.min($scope.totalPages, $scope.currentPage + 2);

        for (var i = startPage; i <= endPage; i++) {
            pages.push(i);
        }
        return pages;
    };

    // 批量上傳相關
    $scope.openBatchUploadModal = function () {
        $scope.uploading = false;
        $scope.uploadResult = null;
        $scope.uploadErrors = [];

        var fileInput = document.getElementById('batchUploadFile');
        if (fileInput) {
            fileInput.value = '';
        }

        $('#batchUploadModal').modal('show');
    };

    $scope.uploadBatchExcel = function () {
        var fileInput = document.getElementById('batchUploadFile');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            swal({
                title: "提醒",
                text: "請選擇 Excel 檔案",
                icon: "warning"
            });
            return;
        }

        var file = fileInput.files[0];
        var fileExtension = file.name.split('.').pop().toLowerCase();
        if (fileExtension !== 'xlsx') {
            swal({
                title: "錯誤",
                text: "副檔名需為 xlsx",
                icon: "error"
            });
            return;
        }

        var formData = new FormData();
        formData.append('file', file);

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadErrors = [];

        $http.post(Router.action('ShipmentInboundProcess', 'BatchUpload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response) {
                if (response.data && response.data.Redirect) {
                    try { if (fileInput) fileInput.value = ''; } catch (e) { }
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                $scope.uploadResult = response.data || { status: 'error', msg: '上傳失敗' };
                $scope.uploadErrors = ($scope.uploadResult && $scope.uploadResult.ReturnObject) ? $scope.uploadResult.ReturnObject : [];

                if ($scope.uploadResult.status === 'success') {
                    swal({
                        title: "成功",
                        text: $scope.uploadResult.msg || "上傳成功",
                        icon: "success"
                    });

                    if ($scope.isSearched) {
                        $scope.loadData();
                    }
                } else {
                    swal({
                        title: "失敗",
                        text: $scope.uploadResult.msg || "上傳失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('批量上傳失敗:', error);
                swal({
                    title: "錯誤",
                    text: "上傳失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.uploading = false;
                // 無論成功或失敗都清除 file input，避免重複點上傳
                try {
                    if (fileInput) fileInput.value = '';
                } catch (e) { }
            });
    };

    // 退件原因編輯相關
    $scope.startEditReturnReason = function (item) {
        item.tempReturnReason = item.ReturnReason || '';
        item.editingReturnReason = true;
    };

    $scope.cancelEditReturnReason = function (item) {
        item.editingReturnReason = false;
        item.tempReturnReason = '';
    };

    $scope.saveReturnReason = function (item) {
        if (item.savingReturnReason) {
            return;
        }

        item.savingReturnReason = true;

        $http.post(Router.action('ShipmentInboundProcess', 'UpdateReturnReason'), {
            id: item.Id,
            returnReason: item.tempReturnReason
        })
            .then(function (response) {
                if (response.data.status === 'success' || !response.data.status) {
                    item.ReturnReason = item.tempReturnReason;
                    item.editingReturnReason = false;
                    swal({
                        title: "成功",
                        text: "退件原因已更新",
                        icon: "success",
                        timer: 1500,
                        buttons: false
                    });
                } else {
                    swal({
                        title: "失敗",
                        text: response.data.msg || "更新失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('更新退件原因失敗:', error);
                swal({
                    title: "錯誤",
                    text: "更新失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                item.savingReturnReason = false;
            });
    };

    // 批量上傳退件原因相關
    $scope.openBatchUploadReturnReasonModal = function () {
        $scope.uploadingReturnReason = false;
        $scope.uploadReturnReasonResult = null;
        $scope.uploadReturnReasonErrors = [];

        var fileInput = document.getElementById('batchUploadReturnReasonFile');
        if (fileInput) {
            fileInput.value = '';
        }

        $('#batchUploadReturnReasonModal').modal('show');
    };

    $scope.uploadBatchReturnReasonExcel = function () {
        var fileInput = document.getElementById('batchUploadReturnReasonFile');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            swal({
                title: "提醒",
                text: "請選擇 Excel 檔案",
                icon: "warning"
            });
            return;
        }

        var file = fileInput.files[0];
        var fileExtension = file.name.split('.').pop().toLowerCase();
        if (fileExtension !== 'xlsx') {
            swal({
                title: "錯誤",
                text: "副檔名需為 xlsx",
                icon: "error"
            });
            return;
        }

        var formData = new FormData();
        formData.append('file', file);

        $scope.uploadingReturnReason = true;
        $scope.uploadReturnReasonResult = null;
        $scope.uploadReturnReasonErrors = [];

        $http.post(Router.action('ShipmentInboundProcess', 'BatchUploadReturnReason'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response) {
                if (response.data && response.data.Redirect) {
                    try { if (fileInput) fileInput.value = ''; } catch (e) { }
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                $scope.uploadReturnReasonResult = response.data || { status: 'error', msg: '上傳失敗' };
                $scope.uploadReturnReasonErrors = ($scope.uploadReturnReasonResult && $scope.uploadReturnReasonResult.ReturnObject) ? $scope.uploadReturnReasonResult.ReturnObject : [];

                if ($scope.uploadReturnReasonResult.status === 'success') {
                    swal({
                        title: "成功",
                        text: $scope.uploadReturnReasonResult.msg || "上傳成功",
                        icon: "success"
                    });

                    if ($scope.isSearched) {
                        $scope.loadData();
                    }
                } else {
                    swal({
                        title: "失敗",
                        text: $scope.uploadReturnReasonResult.msg || "上傳失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('批量上傳退件原因失敗:', error);
                swal({
                    title: "錯誤",
                    text: "上傳失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.uploadingReturnReason = false;
                try {
                    if (fileInput) fileInput.value = '';
                } catch (e) { }
            });
    };

    // 初始化
    $scope.init();
});
