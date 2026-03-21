mainApp.controller('DeliveryAssistantController', function ($scope, $http) {
    // 查詢條件
    $scope.searchForm = {
        dataType: '',
        transNo: '',
        startDate: new Date(),
        endDate: new Date()
    };

    // 下拉選單資料
    $scope.dataTypeList = [];
    $scope.transList = [];

    // 狀態
    $scope.exporting = false;
    $scope.uploading = false;
    $scope.uploadResult = null;
    $scope.uploadApiResults = {
        uploadOrderInfo: null,
        establishDcShip: null
    };

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

    // 開啟日期選擇器
    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    // 載入作業地區清單
    $scope.loadDataTypeList = function () {
        $http.get(Router.action('DeliveryAssistant', 'GetDataTypeList'))
            .then(function (response) {
                $scope.dataTypeList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入作業地區清單失敗:', error);
            });
    };

    // 載入派件公司清單
    $scope.loadTransList = function () {
        $http.get(Router.action('DeliveryAssistant', 'GetTransList'))
            .then(function (response) {
                $scope.transList = response.data || [];
            })
            .catch(function (error) {
                console.error('載入派件公司清單失敗:', error);
            });
    };

    // 格式化日期為 yyyy-MM-dd
    $scope.formatDate = function (date) {
        if (!date) return '';
        var d = new Date(date);
        return d.getFullYear() + '-' +
            String(d.getMonth() + 1).padStart(2, '0') + '-' +
            String(d.getDate()).padStart(2, '0');
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        if (!$scope.searchForm.dataType) {
            swal({ title: '提醒', text: '請選擇作業地區', icon: 'warning' });
            return;
        }
        if (!$scope.searchForm.transNo) {
            swal({ title: '提醒', text: '請選擇派件公司', icon: 'warning' });
            return;
        }
        if (!$scope.searchForm.startDate || !$scope.searchForm.endDate) {
            swal({ title: '提醒', text: '請選擇日期', icon: 'warning' });
            return;
        }

        $scope.exporting = true;

        var request = {
            DataType: $scope.searchForm.dataType,
            TransNo: $scope.searchForm.transNo,
            StartDate: $scope.formatDate($scope.searchForm.startDate),
            EndDate: $scope.formatDate($scope.searchForm.endDate)
        };

        var form = document.createElement('form');
        form.method = 'POST';
        form.action = Router.action('DeliveryAssistant', 'ExportExcel');
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

    // 開啟上傳 Modal
    $scope.openUploadModal = function () {
        $scope.uploadResult = null;
        $scope.uploadApiResults = {
            uploadOrderInfo: null,
            establishDcShip: null
        };
        $scope.uploading = false;

        var fileInput = document.getElementById('uploadOrderFile');
        if (fileInput) fileInput.value = '';

        $('#uploadOrderModal').modal('show');
    };

    // 執行上傳
    $scope.uploadOrderInfo = function () {
        var fileInput = document.getElementById('uploadOrderFile');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            swal({ title: '提醒', text: '請選擇 Excel 檔案', icon: 'warning' });
            return;
        }

        var file = fileInput.files[0];
        if (file.name.split('.').pop().toLowerCase() !== 'xlsx') {
            swal({ title: '錯誤', text: '副檔名需為 xlsx', icon: 'error' });
            return;
        }

        var formData = new FormData();
        formData.append('file', file);

        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadApiResults = {
            uploadOrderInfo: null,
            establishDcShip: null
        };

        $http.post(Router.action('DeliveryAssistant', 'UploadOrderInfo'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response) {
                if (response.data && response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                $scope.uploadResult = response.data || { status: 'error', msg: '上傳失敗' };
                var resultObject = $scope.uploadResult.ReturnObject || {};
                $scope.uploadApiResults.uploadOrderInfo = resultObject.UploadOrderInfo || null;
                $scope.uploadApiResults.establishDcShip = resultObject.EstablishDcShip || null;
            })
            .catch(function (error) {
                console.error('上傳失敗:', error);
                $scope.uploadResult = { status: 'error', msg: '上傳失敗，請稍後再試' };
            })
            .finally(function () {
                $scope.uploading = false;
                try { if (fileInput) fileInput.value = ''; } catch (e) { }
            });
    };

    // 初始化
    $scope.init = function () {
        $scope.loadDataTypeList();
        $scope.loadTransList();
    };

    $scope.init();
});
