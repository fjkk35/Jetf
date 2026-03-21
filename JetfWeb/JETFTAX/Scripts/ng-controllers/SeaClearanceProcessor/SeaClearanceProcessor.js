// Controller
mainApp.controller('SeaClearanceProcessorController', function ($scope, $http) {
    // 初始化資料
    $scope.processorList = [];
    $scope.steps = [];
    $scope.customers = [];
    $scope.loading = false;
    $scope.saving = false;
    $scope.modalMode = 'add'; // 'add' or 'edit'

    // 查詢條件
    $scope.query = {
        StepId: null,
        Cust_Code: ''
    };

    // 表單資料
    $scope.processorForm = {};

    // 初始化
    $scope.init = function () {
        $scope.loadSteps();
        $scope.loadCustomers();
        $scope.loadProcessorList();
    };

    // 載入步驟列表
    $scope.loadSteps = function () {
        $http.get(Router.action('Step', 'GetAllSteps'))
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.steps = response.data.ReturnObject;
                } else if (response.data && Array.isArray(response.data)) {
                    $scope.steps = response.data;
                } else {
                    $scope.steps = [];
                }
            })
            .catch(function (error) {
                console.error('載入步驟失敗:', error);
                swal({
                    title: "錯誤",
                    text: "載入步驟失敗，請稍後再試",
                    icon: "error"
                });
            });
    };

    // 載入客戶列表
    $scope.loadCustomers = function () {
        $http.get(Router.action('SeaClearanceCustomer', 'GetSelectedCustomers'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.customers = response.data;
                } else if (response.data && response.data.ReturnObject && Array.isArray(response.data.ReturnObject)) {
                    $scope.customers = response.data.ReturnObject;
                } else {
                    $scope.customers = [];
                }
            })
            .catch(function (error) {
                console.error('載入客戶失敗:', error);
                swal({
                    title: "錯誤",
                    text: "載入客戶失敗，請稍後再試",
                    icon: "error"
                });
            });
    };

    // 載入負責人列表
    $scope.loadProcessorList = function () {
        $scope.loading = true;

        var queryData = {
            StepId: $scope.query.StepId ? parseInt($scope.query.StepId) : null,
            Cust_Code: $scope.query.Cust_Code || null
        };

        $http.post(Router.action('SeaClearanceProcessor', 'GetProcessorList'), queryData)
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.processorList = response.data.ReturnObject;
                } else if (response.data && Array.isArray(response.data)) {
                    $scope.processorList = response.data;
                } else {
                    $scope.processorList = [];
                }
            })
            .catch(function (error) {
                console.error('載入負責人列表失敗:', error);
                swal({
                    title: "錯誤",
                    text: "載入負責人列表失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 重置查詢條件
    $scope.resetQuery = function () {
        $scope.query = {
            StepId: null,
            Cust_Code: ''
        };
        $scope.loadProcessorList();
    };

    // 開啟 Modal
    $scope.openModal = function (mode, item) {
        $scope.modalMode = mode;

        if (mode === 'add') {
            $scope.processorForm = {
                StepId: null,
                Cust_Code: '',
                X2: '',
                X3: '',
                G1: '',
                MoveWarehouse: '',
                TransferG1: '',
                TransferWarehouse: ''
            };
        } else if (mode === 'edit' && item) {
            $scope.processorForm = {
                Id: item.Id,
                StepId: item.StepId,
                Cust_Code: item.Cust_Code,
                X2: item.X2 || '',
                X3: item.X3 || '',
                G1: item.G1 || '',
                MoveWarehouse: item.MoveWarehouse || '',
                TransferG1: item.TransferG1 || '',
                TransferWarehouse: item.TransferWarehouse || ''
            };
        }

        $('#processorModal').modal('show');
    };

    // 檢查是否可以儲存
    $scope.canSave = function () {
        if (!$scope.processorForm.StepId || !$scope.processorForm.Cust_Code) {
            return false;
        }

        // 至少要有一個負責人
        var hasProcessor = $scope.processorForm.X2 ||
            $scope.processorForm.X3 ||
            $scope.processorForm.G1 ||
            $scope.processorForm.MoveWarehouse ||
            $scope.processorForm.TransferG1 ||
            $scope.processorForm.TransferWarehouse;

        return hasProcessor;
    };

    // 儲存負責人
    $scope.saveProcessor = function () {
        if (!$scope.canSave()) {
            swal({
                title: "警告",
                text: "請填寫必填欄位，並至少填寫一個負責人",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;

        var url = $scope.modalMode === 'add' ?
            Router.action('SeaClearanceProcessor', 'CreateProcessor') :
            Router.action('SeaClearanceProcessor', 'UpdateProcessor');

        // 確保數值欄位正確轉換
        var data = angular.copy($scope.processorForm);
        data.StepId = parseInt(data.StepId);

        $http.post(url, data)
            .then(function (response) {
                if (response.data && response.data.msg) {
                    swal({
                        title: "成功",
                        text: response.data.msg,
                        icon: "success"
                    }).then(function () {
                        $('#processorModal').modal('hide');
                        $scope.loadProcessorList();
                    });
                } else {
                    swal({
                        title: "錯誤",
                        text: response.data.msg || "操作失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('儲存失敗:', error);
                var errorMsg = "儲存失敗，請稍後再試";
                if (error.data && error.data.msg) {
                    errorMsg = error.data.msg;
                }
                swal({
                    title: "錯誤",
                    text: errorMsg,
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    // 刪除負責人
    $scope.deleteProcessor = function (item) {
        swal({
            title: "確認刪除",
            text: "確定要刪除此負責人設定嗎？",
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    visible: true,
                    className: "btn-secondary"
                },
                confirm: {
                    text: "確定刪除",
                    className: "btn-danger"
                }
            }
        }).then(function (willDelete) {
            if (willDelete) {
                $http.post(Router.action('SeaClearanceProcessor', 'DeleteProcessor'), { id: item.Id })
                    .then(function (response) {
                        if (response.data && response.data.msg) {
                            swal({
                                title: "成功",
                                text: response.data.msg,
                                icon: "success"
                            }).then(function () {
                                $scope.loadProcessorList();
                            });
                        } else {
                            swal({
                                title: "錯誤",
                                text: response.data.msg || "刪除失敗",
                                icon: "error"
                            });
                        }
                    })
                    .catch(function (error) {
                        console.error('刪除失敗:', error);
                        swal({
                            title: "錯誤",
                            text: "刪除失敗，請稍後再試",
                            icon: "error"
                        });
                    });
            }
        });
    };

    // 初始化執行
    $scope.init();
});
