mainApp.controller('ShipmentInboundProcessStageController', function ($scope, $http) {
    var remarkOnlyProcessTypes = [2, 3, 5, 6, 7, 8, 10];

    $scope.data = [];
    $scope.loading = false;
    $scope.dialogLoading = false;
    $scope.saving = false;
    $scope.enrichingAmount = false;
    $scope.isSearched = false;
    $scope.uploadingReturnReason = false;
    $scope.uploadReturnReasonResult = null;
    $scope.uploadReturnReasonSummary = null;
    $scope.uploadReturnReasonErrors = [];
    $scope.currentPage = 1;
    $scope.pageSize = "10";
    $scope.totalCount = 0;
    $scope.totalPages = 0;
    $scope.recordsInfo = '';

    $scope.searchForm = {
        trackingNo: '',
        createdTimeStart: null,
        createdTimeEnd: null,
        createdOpe: '',
        matchTimieStart: null,
        matchTimieEnd: null,
        isMatched: ''
    };

    $scope.processTypeList = [];
    $scope.processTransNoList = [];
    $scope.freightPayerNoList = [];
    $scope.currentItem = null;
    $scope.isReadOnlyMode = false;
    $scope.modalTitle = '新增預先登記處理';
    $scope.createdTimeStartPopup = { opened: false };
    $scope.createdTimeEndPopup = { opened: false };
    $scope.matchTimieStartPopup = { opened: false };
    $scope.matchTimieEndPopup = { opened: false };
    $scope.pickupTimePopup = { opened: false };
    var enrichAmountRequestId = 0;
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 12, 31),
        minDate: new Date(2000, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    function normalizeText(value) {
        return value ? value.trim() : '';
    }

    function normalizeMatchedValue(value) {
        if (value === true || value === 'true') {
            return true;
        }

        if (value === false || value === 'false') {
            return false;
        }

        return null;
    }

    function buildEmptyForm() {
        return {
            trackingNo: '',
            returnReason: '',
            processType: null,
            processTransNo: null,
            processImporter: '',
            processImporterPhone: '',
            processImporterAddr: '',
            storeCode: '',
            storeName: '',
            tax: null,
            ccFee: null,
            cod: null,
            freightPayerNo: null,
            freightFee: 0,
            fee: 0,
            carNo: '',
            pickupTime: null,
            remark: ''
        };
    }

    function formatDate(dateValue) {
        if (!dateValue) {
            return null;
        }

        var date = new Date(dateValue);
        if (isNaN(date.getTime())) {
            return null;
        }

        return date.getFullYear() + '-' +
            String(date.getMonth() + 1).padStart(2, '0') + '-' +
            String(date.getDate()).padStart(2, '0');
    }

    function parseDate(value) {
        if (!value) {
            return null;
        }

        if (typeof value === 'string') {
            var dateMatch = value.match(/\/Date\((\d+)\)\//);
            if (dateMatch) {
                return new Date(parseInt(dateMatch[1]));
            }
        }

        var date = new Date(value);
        return isNaN(date.getTime()) ? null : date;
    }

    function shouldEnrichAmount(processType) {
        return processType == 1 || processType == 4 || processType == 9;
    }

    function resetAmountFields() {
        $scope.processForm.tax = null;
        $scope.processForm.ccFee = null;
        $scope.processForm.cod = null;
    }

    function refreshAmountFields() {
        var trackingNo = normalizeText($scope.processForm.trackingNo);
        var requestId = ++enrichAmountRequestId;

        if (!shouldEnrichAmount($scope.processForm.processType) || !trackingNo) {
            $scope.enrichingAmount = false;
            resetAmountFields();
            $scope.calcFee();
            return;
        }

        $scope.enrichingAmount = true;

        $http.get(Router.action('ShipmentInboundProcessStage', 'GetAmountDetailByTrackingNo'), {
            params: { trackingNo: trackingNo }
        }).then(function (response) {
            if (requestId !== enrichAmountRequestId) {
                return;
            }

            if (response.data.error) {
                throw new Error(response.data.error);
            }

            $scope.processForm.tax = response.data.Tax;
            $scope.processForm.ccFee = response.data.CcFee;
            $scope.processForm.cod = response.data.Cod;
            $scope.calcFee();
        }).catch(function (error) {
            if (requestId !== enrichAmountRequestId) {
                return;
            }

            resetAmountFields();
            $scope.calcFee();

            swal({
                title: '查詢失敗',
                text: (error && error.message) ? error.message : '無法取得金額資訊，請稍後再試',
                icon: 'error'
            });
        }).finally(function () {
            if (requestId === enrichAmountRequestId) {
                $scope.enrichingAmount = false;
            }
        });
    }

    $scope.processForm = buildEmptyForm();

    $scope.init = function () {
        $scope.loadProcessTypeList();
        $scope.loadProcessTransNoList();
        $scope.loadFreightPayerNoList();

        $('#processStageModal').on('hidden.bs.modal', function () {
            enrichAmountRequestId++;
            $scope.currentItem = null;
            $scope.isReadOnlyMode = false;
            $scope.enrichingAmount = false;
            $scope.processForm = buildEmptyForm();

            if (!$scope.$$phase) {
                $scope.$apply();
            }
        });
    };

    $scope.loadProcessTypeList = function () {
        $http.get(Router.action('ShipmentInboundProcessStage', 'GetProcessTypeList'))
            .then(function (response) {
                $scope.processTypeList = response.data || [];
            });
    };

    $scope.loadProcessTransNoList = function () {
        $http.get(Router.action('ShipmentInboundProcessStage', 'GetProcessTransNoList'))
            .then(function (response) {
                $scope.processTransNoList = response.data || [];
            });
    };

    $scope.loadFreightPayerNoList = function () {
        $http.get(Router.action('ShipmentInboundProcessStage', 'GetFreightPayerNoList'))
            .then(function (response) {
                $scope.freightPayerNoList = response.data || [];
            });
    };

    $scope.openPickupTimePopup = function () {
        $scope.pickupTimePopup.opened = true;
    };

    $scope.openCreatedTimeStartPopup = function () {
        $scope.createdTimeStartPopup.opened = true;
    };

    $scope.openCreatedTimeEndPopup = function () {
        $scope.createdTimeEndPopup.opened = true;
    };

    $scope.openMatchTimieStartPopup = function () {
        $scope.matchTimieStartPopup.opened = true;
    };

    $scope.openMatchTimieEndPopup = function () {
        $scope.matchTimieEndPopup.opened = true;
    };

    $scope.calcFee = function () {
        if ($scope.processForm.processType == 4) {
            $scope.processForm.fee = 0;
            return;
        }

        var freightFee = parseFloat($scope.processForm.freightFee) || 0;
        var tax = parseFloat($scope.processForm.tax) || 0;
        var ccFee = parseFloat($scope.processForm.ccFee) || 0;

        // 只有「開新單號重出 + 7-11」時，才額外以運費支付方判斷是否收 30；其餘情況維持原本有運費/稅金/報關費就收 30 的邏輯。
        if ($scope.processForm.processType == 1 && $scope.processForm.processTransNo == 3) {
            $scope.processForm.fee = $scope.processForm.freightPayerNo == 1 ? 30 : 0;
            return;
        }

        $scope.processForm.fee = (freightFee > 0 || tax > 0 || ccFee > 0) ? 30 : 0;
    };

    $scope.onProcessTransNoChange = function () {
        if ($scope.processForm.processTransNo == 3) {
            $scope.processForm.processImporterAddr = '';
        } else {
            $scope.processForm.storeCode = '';
            $scope.processForm.storeName = '';
        }

        $scope.calcFee();
    };

    $scope.onFreightPayerNoChange = function () {
        $scope.processForm.freightFee = $scope.processForm.freightPayerNo == 1 ? 120 : 0;
        $scope.calcFee();
    };

    $scope.onProcessTypeChange = function () {
        if ($scope.processForm.processType != 1 && $scope.processForm.processType != 9) {
            $scope.processForm.processTransNo = null;
            $scope.processForm.freightPayerNo = null;
            $scope.processForm.freightFee = 0;
            $scope.processForm.fee = 0;
            $scope.processForm.storeCode = '';
            $scope.processForm.storeName = '';
            $scope.processForm.processImporterAddr = '';
        }

        if ($scope.processForm.processType != 1 && $scope.processForm.processType != 4 && $scope.processForm.processType != 9) {
            $scope.processForm.tax = null;
            $scope.processForm.ccFee = null;
            $scope.processForm.cod = null;
        }

        if ($scope.processForm.processType != 4) {
            $scope.processForm.carNo = '';
            $scope.processForm.pickupTime = null;
        }

        if (remarkOnlyProcessTypes.indexOf($scope.processForm.processType) > -1) {
            $scope.processForm.processImporter = '';
            $scope.processForm.processImporterPhone = '';
            $scope.processForm.processImporterAddr = '';
        }

        $scope.calcFee();
        refreshAmountFields();
    };

    $scope.onTrackingNoChanged = function () {
        refreshAmountFields();
    };

    $scope.buildSearchRequest = function () {
        return {
            TrackingNo: normalizeText($scope.searchForm.trackingNo),
            CreatedTimeStart: formatDate($scope.searchForm.createdTimeStart),
            CreatedTimeEnd: formatDate($scope.searchForm.createdTimeEnd),
            CreatedOpe: normalizeText($scope.searchForm.createdOpe),
            MatchTimieStart: formatDate($scope.searchForm.matchTimieStart),
            MatchTimieEnd: formatDate($scope.searchForm.matchTimieEnd),
            IsMatched: normalizeMatchedValue($scope.searchForm.isMatched),
            Page: $scope.currentPage,
            PageSize: parseInt($scope.pageSize)
        };
    };

    $scope.search = function () {
        $scope.currentPage = 1;
        $scope.isSearched = true;
        $scope.loadData();
    };

    $scope.loadData = function () {
        $scope.loading = true;

        $http.post(Router.action('ShipmentInboundProcessStage', 'SearchData'), $scope.buildSearchRequest())
            .then(function (response) {
                if (response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (response.data.error) {
                    swal({
                        title: '查詢失敗',
                        text: response.data.error,
                        icon: 'error'
                    });
                    return;
                }

                $scope.data = response.data.Data || [];
                $scope.totalCount = response.data.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize));
                $scope.updateRecordsInfo();
            })
            .catch(function () {
                swal({
                    title: '查詢失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.clearSearch = function () {
        $scope.searchForm = {
            trackingNo: '',
            createdTimeStart: null,
            createdTimeEnd: null,
            createdOpe: '',
            matchTimieStart: null,
            matchTimieEnd: null,
            isMatched: ''
        };
        $scope.data = [];
        $scope.isSearched = false;
        $scope.currentPage = 1;
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.recordsInfo = '';
    };

    $scope.updateRecordsInfo = function () {
        if ($scope.totalCount === 0) {
            $scope.recordsInfo = '';
            return;
        }

        var pageSize = parseInt($scope.pageSize);
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo = '顯示 ' + start + ' 到 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    };

    $scope.changePage = function (page) {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }

        $scope.currentPage = page;
        $scope.loadData();
    };

    $scope.previousPage = function () {
        if ($scope.currentPage > 1) {
            $scope.currentPage--;
            $scope.loadData();
        }
    };

    $scope.nextPage = function () {
        if ($scope.currentPage < $scope.totalPages) {
            $scope.currentPage++;
            $scope.loadData();
        }
    };

    $scope.changePageSize = function () {
        $scope.currentPage = 1;
        if ($scope.isSearched) {
            $scope.loadData();
        }
    };

    $scope.getPageNumbers = function () {
        var pages = [];
        var maxVisible = 10;
        var startPage = Math.max(1, $scope.currentPage - Math.floor(maxVisible / 2));
        var endPage = Math.min($scope.totalPages, startPage + maxVisible - 1);

        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        for (var index = startPage; index <= endPage; index++) {
            pages.push(index);
        }

        return pages;
    };

    $scope.goToPage = $scope.changePage;

    $scope.openCreateModal = function () {
        $scope.currentItem = null;
        $scope.isReadOnlyMode = false;
        $scope.enrichingAmount = false;
        $scope.modalTitle = '新增預先登記處理';
        $scope.processForm = buildEmptyForm();
        $('#processStageModal').modal('show');
    };

    $scope.openBatchUploadReturnReasonModal = function () {
        $scope.uploadingReturnReason = false;
        $scope.uploadReturnReasonResult = null;
        $scope.uploadReturnReasonSummary = null;
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
                title: '提醒',
                text: '請選擇 Excel 檔案',
                icon: 'warning'
            });
            return;
        }

        var file = fileInput.files[0];
        var fileExtension = file.name.split('.').pop().toLowerCase();
        if (fileExtension !== 'xlsx') {
            swal({
                title: '錯誤',
                text: '副檔名需為 xlsx',
                icon: 'error'
            });
            return;
        }

        var formData = new FormData();
        formData.append('file', file);

        $scope.uploadingReturnReason = true;
        $scope.uploadReturnReasonResult = null;
        $scope.uploadReturnReasonSummary = null;
        $scope.uploadReturnReasonErrors = [];

        $http.post(Router.action('ShipmentInboundProcessStage', 'BatchUploadReturnReason'), formData, {
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

                var resultObject = $scope.uploadReturnReasonResult.ReturnObject || {};
                var errors = angular.isArray(resultObject) ? resultObject : (resultObject.Errors || []);
                var failureCount = angular.isNumber(resultObject.FailureCount) ? resultObject.FailureCount : 0;
                var successCount = angular.isNumber(resultObject.SuccessCount) ? resultObject.SuccessCount : 0;

                $scope.uploadReturnReasonErrors = errors;
                $scope.uploadReturnReasonSummary = {
                    SuccessCount: successCount,
                    FailureCount: failureCount
                };

                if ($scope.uploadReturnReasonResult.status === 'success') {
                    swal({
                        title: '成功',
                        text: $scope.uploadReturnReasonResult.msg || '上傳成功',
                        icon: 'success'
                    });

                    if ($scope.isSearched) {
                        $scope.loadData();
                    }
                } else {
                    swal({
                        title: '失敗',
                        text: $scope.uploadReturnReasonResult.msg || '上傳失敗',
                        icon: 'error'
                    });
                }
            })
            .catch(function (error) {
                console.error('批量上傳退件原因失敗:', error);
                swal({
                    title: '錯誤',
                    text: '上傳失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.uploadingReturnReason = false;
                try { if (fileInput) fileInput.value = ''; } catch (e) { }
            });
    };

    $scope.beginProcessEdit = function (item) {
        if ($scope.dialogLoading) {
            return;
        }

        $scope.dialogLoading = true;
        $scope.currentItem = item;
        $scope.isReadOnlyMode = !!(item && item.MatchTimie);
        $scope.viewProcessDetail(item)
            .finally(function () {
                $scope.dialogLoading = false;
            });
    };

    $scope.viewProcessDetail = function (item) {
        return $http.get(Router.action('ShipmentInboundProcessStage', 'GetDetailById') + '?id=' + item.Id)
            .then(function (response) {
                if (response.data.error) {
                    swal({
                        title: '載入失敗',
                        text: response.data.error,
                        icon: 'error'
                    });
                    return;
                }

                var detail = response.data;
                $scope.modalTitle = (item.ProcessTypeName || '處理方式') + ($scope.isReadOnlyMode ? ' (查看)' : ' (修改)');
                $scope.processForm = {
                    trackingNo: detail.TrackingNo || '',
                    returnReason: detail.ReturnReason || '',
                    processType: detail.ProcessType ? parseInt(detail.ProcessType) : null,
                    processTransNo: detail.ProcessTransNo,
                    processImporter: detail.ProcessImporter || '',
                    processImporterPhone: detail.ProcessImporterPhone || '',
                    processImporterAddr: detail.ProcessImporterAddr || '',
                    storeCode: detail.StoreCode || '',
                    storeName: detail.StoreName || '',
                    tax: detail.Tax,
                    ccFee: detail.CcFee,
                    cod: detail.Cod,
                    freightPayerNo: detail.FreightPayerNo,
                    freightFee: detail.FreightFee || 0,
                    fee: detail.Fee || 0,
                    carNo: detail.CarNo || '',
                    pickupTime: parseDate(detail.PickupTime),
                    remark: detail.Remark || ''
                };

                $scope.calcFee();
                $('#processStageModal').modal('show');
            })
            .catch(function () {
                swal({
                    title: '載入失敗',
                    text: '無法載入詳細資料，請稍後再試',
                    icon: 'error'
                });
            });
    };

    $scope.getProcessButtonClass = function (item) {
        return item && item.ProcessType ? 'btn-info' : 'btn-warning';
    };

    $scope.saveProcess = function () {
        if ($scope.isReadOnlyMode) {
            return;
        }

        if (!normalizeText($scope.processForm.trackingNo)) {
            swal({
                title: '提醒',
                text: '請輸入單號',
                icon: 'warning'
            });
            return;
        }

        if ($scope.processForm.processType == 1) {
            if (!$scope.processForm.processTransNo) {
                swal({ title: '提醒', text: '請選擇重出派件公司', icon: 'warning' });
                return;
            }

            if (!normalizeText($scope.processForm.processImporter)) {
                swal({ title: '提醒', text: '請輸入收件人', icon: 'warning' });
                return;
            }

            if (!normalizeText($scope.processForm.processImporterPhone)) {
                swal({ title: '提醒', text: '請輸入電話', icon: 'warning' });
                return;
            }

            if (!$scope.processForm.freightPayerNo) {
                swal({ title: '提醒', text: '請選擇重出運費支付方', icon: 'warning' });
                return;
            }

            if ($scope.processForm.processTransNo == 3) {
                if (!normalizeText($scope.processForm.storeCode)) {
                    swal({ title: '提醒', text: '請輸入門市店號', icon: 'warning' });
                    return;
                }

                if (!normalizeText($scope.processForm.storeName)) {
                    swal({ title: '提醒', text: '請輸入門市名稱', icon: 'warning' });
                    return;
                }
            } else if (!normalizeText($scope.processForm.processImporterAddr)) {
                swal({ title: '提醒', text: '請輸入宅配地址', icon: 'warning' });
                return;
            }
        }

        $scope.saving = true;

        var request = {
            Id: $scope.currentItem ? $scope.currentItem.Id : null,
            TrackingNo: normalizeText($scope.processForm.trackingNo),
            ReturnReason: normalizeText($scope.processForm.returnReason),
            ProcessType: $scope.processForm.processType,
            ProcessTransNo: $scope.processForm.processTransNo,
            ProcessImporter: normalizeText($scope.processForm.processImporter),
            ProcessImporterPhone: normalizeText($scope.processForm.processImporterPhone),
            ProcessImporterAddr: normalizeText($scope.processForm.processImporterAddr),
            StoreCode: normalizeText($scope.processForm.storeCode),
            StoreName: normalizeText($scope.processForm.storeName),
            Tax: $scope.processForm.tax,
            CcFee: $scope.processForm.ccFee,
            Cod: $scope.processForm.cod,
            FreightPayerNo: $scope.processForm.freightPayerNo,
            FreightFee: $scope.processForm.freightFee,
            Fee: $scope.processForm.fee,
            CarNo: normalizeText($scope.processForm.carNo),
            PickupTime: formatDate($scope.processForm.pickupTime),
            Remark: normalizeText($scope.processForm.remark)
        };

        $http.post(Router.action('ShipmentInboundProcessStage', 'SaveProcess'), request)
            .then(function (response) {
                if (response.data.status === 'success') {

                    swal({
                        title: '成功',
                        text: '儲存成功',
                        icon: 'success'
                    });

                    $('#processStageModal').modal('hide');

                    if ($scope.isSearched) {
                        $scope.loadData();
                    } else {
                        $scope.searchForm.trackingNo = request.TrackingNo;
                        $scope.search();
                    }
                } else {
                    swal({
                        title: '失敗',
                        text: response.data.msg || '儲存失敗',
                        icon: 'error'
                    });
                }
            })
            .catch(function () {
                swal({
                    title: '錯誤',
                    text: '儲存失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };
});
