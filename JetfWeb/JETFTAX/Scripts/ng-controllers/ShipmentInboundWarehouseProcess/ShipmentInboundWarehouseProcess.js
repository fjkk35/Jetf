mainApp.controller('ShipmentInboundWarehouseProcessController', function ($scope, $http) {
    // 初始化資料
    $scope.data = [];
    $scope.loading = false;
    $scope.isSearched = false;
    $scope.saving = false;

    $scope.uploading = false;
    $scope.uploadResult = null;
    $scope.uploadErrors = [];

    // 查詢條件
    $scope.searchForm = {
        trackingNo: ''
    };

    // 處理狀態下拉選單
    $scope.warehouseProcessTypeList = [];

    // Modal 相關
    $scope.currentItem = null;
    $scope.modalTitle = '';
    $scope.processTypeForm = {
        warehouseProcessType: null
    };

    // 初始化
    $scope.init = function () {
        $scope.loadWarehouseProcessTypeList();
    };

    // 載入處理狀態清單
    $scope.loadWarehouseProcessTypeList = function () {
        $http.get(Router.action('ShipmentInboundWarehouseProcess', 'GetWarehouseProcessTypeList'))
            .then(function (response) {
                $scope.warehouseProcessTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入處理狀態清單失敗:', error);
            });
    };

    // 執行查詢
    $scope.search = function () {
        if (!$scope.searchForm.trackingNo) {
            swal({
                title: "提醒",
                text: "請輸入單號",
                icon: "warning"
            });
            return;
        }

        $scope.isSearched = true;
        $scope.loadData();
    };

    // 載入資料
    $scope.loadData = function () {
        $scope.loading = true;

        var searchRequest = {
            TrackingNo: $scope.searchForm.trackingNo
        };

        $http.post(Router.action('ShipmentInboundWarehouseProcess', 'SearchData'), searchRequest)
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

                $scope.data = response.data.Data || [];

                if ($scope.data.length === 0) {
                    swal({
                        title: "提醒",
                        text: "查無資料",
                        icon: "info"
                    });
                }
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
            trackingNo: ''
        };
        $scope.data = [];
        $scope.isSearched = false;
    };

    $scope.openBatchUploadModal = function () {
        $scope.uploadResult = null;
        $scope.uploading = false;
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
        var formData = new FormData();
        formData.append('file', file);

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadErrors = [];

        $http.post(Router.action('ShipmentInboundWarehouseProcess', 'BatchUpload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response) {
                if (response.data && response.data.Redirect) {
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
                // 無論成功或失敗都清除 file input，避免重複上傳相同檔案
                if (fileInput)
                    fileInput.value = '';

            });
    };

    // 開啟處理狀態 Modal
    $scope.openProcessTypeModal = function (item) {
        $scope.currentItem = item;
        $scope.modalTitle = '修改處理狀態';

        // 設定目前的處理狀態值
        $scope.processTypeForm = {
            warehouseProcessType: item.WarehouseProcessType || null
        };

        $('#processTypeModal').modal('show');
    };

    // 儲存處理狀態
    $scope.saveProcessType = function () {
        // 驗證處理狀態必選
        if (!$scope.processTypeForm.warehouseProcessType || $scope.processTypeForm.warehouseProcessType === '') {
            swal({
                title: "提醒",
                text: "請選擇處理狀態",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;

        var request = {
            Id: $scope.currentItem.Id,
            WarehouseProcessType: $scope.processTypeForm.warehouseProcessType
        };

        $http.post(Router.action('ShipmentInboundWarehouseProcess', 'UpdateProcessType'), request)
            .then(function (response) {
                if (response.data.status === 'success') {
                    swal({
                        title: "成功",
                        text: "更新成功",
                        icon: "success"
                    });
                    $('#processTypeModal').modal('hide');
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
                console.error('更新失敗:', error);
                swal({
                    title: "錯誤",
                    text: "更新失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    // 初始化
    $scope.init();
});
