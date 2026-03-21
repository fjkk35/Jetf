// Controller
mainApp.controller('SearchCargoDetailController', function ($scope, $http, $window, $location) {
    // 初始化資料
    $scope.detailData = null;
    $scope.loading = true;
    $scope.processList = [];
    $scope.saving = false;
    $scope.modalTitle = '';
    $scope.processData = {
        type: '1',
        remark: '',
        file: null
    };
    $scope.logList = [];
    $scope.bagNumberList = [];
    $scope.trackingNoList = [];
    $scope.shenzhenCargoList = [];
    $scope.showAllItems = false; // 控制申報品名是否顯示全部

    // 從URL獲取ID
    var urlParams = new URLSearchParams($window.location.search);
    var id = urlParams.get('id');

    if (!id) {
        swal({
            title: "錯誤",
            text: "缺少必要參數",
            icon: "error"
        }).then(function () {
            $window.close();
        });
        return;
    }

    // 載入明細資料
    $scope.loadDetail = function () {
        $scope.loading = true;

        $http.post(Router.action('SearchCargo', 'GetDetail'), { id: id })
            .then(function (response) {
                if (response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (!response.data.success) {
                    swal({
                        title: "查詢失敗",
                        text: response.data.error || "查詢時發生錯誤",
                        icon: "error"
                    });
                    return;
                }

                $scope.detailData = response.data.data;

                // 載入處置說明
                if ($scope.detailData && $scope.detailData.Dlv_Inv) {
                    $scope.loadProcessList($scope.detailData.Dlv_Inv);
                }
            })
            .catch(function (error) {
                console.error('載入明細失敗:', error);
                swal({
                    title: "載入失敗",
                    text: "請稍後再試或聯繫系統管理員",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 載入處置說明列表
    $scope.loadProcessList = function (dlv_inv) {
        $http.post(Router.action('SearchCargo', 'GetProcessList'), { dlv_inv: dlv_inv })
            .then(function (response) {
                if (response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (response.data.success) {
                    $scope.processList = response.data.data;
                }
            })
            .catch(function (error) {
                console.error('載入處置說明失敗:', error);
            });
    };

    // 載入通關袋號明細
    $scope.loadBagNumberList = function (bagNumber) {
        $http.post(Router.action('SearchCargo', 'GetCargoTargetBagNumber'), { bagNumber: bagNumber })
            .then(function (response) {
                if (response.data.Redirect) {
                    window.location = Router.action('Account', 'Login');
                    return;
                }

                if (response.data.success) {
                    $scope.bagNumberList = response.data.data;
                }
            })
            .catch(function (error) {
                console.error('載入通關袋號失敗:', error);
            });
    };

    // 顯示通關袋號明細
    $scope.showBagNumberDetail = function () {
        if (!$scope.detailData.Bag_Number) {
            swal("提示", "無清關袋號資料", "info");
            return;
        }

        $http.post(Router.action('SearchCargo', 'GetCargoTargetBagNumber'), {
            bagNumber: $scope.detailData.Bag_Number
        }).then(function (response) {
            if (response.data.Redirect) {
                window.location = Router.action('Account', 'Login');
                return;
            }

            if (response.data.success) {
                $scope.bagNumberList = response.data.data;
                $('#bagNumberModal').modal('show');
            } else {
                swal("錯誤", response.data.error || "載入通關袋號明細失敗", "error");
            }
        }).catch(function (error) {
            console.error('載入通關袋號明細失敗:', error);
            swal("錯誤", "載入通關袋號明細失敗", "error");
        });
    };

    // 顯示通關分提單號明細
    $scope.showTrackingNoDetail = function () {
        if (!$scope.detailData.Dlv_Inv) {
            swal("提示", "無分提單號資料", "info");
            return;
        }

        $http.post(Router.action('SearchCargo', 'GetCargoTargetTrackingNo'), {
            bagNumber: $scope.detailData.Dlv_Inv
        }).then(function (response) {
            if (response.data.Redirect) {
                window.location = Router.action('Account', 'Login');
                return;
            }

            if (response.data.success) {
                $scope.trackingNoList = response.data.data;
                $('#trackingNoModal').modal('show');
            } else {
                swal("錯誤", response.data.error || "載入通關分提單號明細失敗", "error");
            }
        }).catch(function (error) {
            console.error('載入通關分提單號明細失敗:', error);
            swal("錯誤", "載入通關分提單號明細失敗", "error");
        });
    };

    // 顯示新增處置說明對話框
    $scope.showAddProcessDialog = function () {
        $scope.modalTitle = '新增處置說明';
        $scope.processData = {
            type: '1',
            remark: '',
            file: null
        };
        $scope.processForm.$setPristine();
        $scope.processForm.$setUntouched();
        $('#processModal').modal('show');
    };

    // 檔案選擇處理
    $scope.onFileSelect = function (element) {
        $scope.$apply(function () {
            $scope.processData.file = element.files[0];
        });
    };

    // 儲存處置說明
    $scope.saveProcess = function () {
        if ($scope.processForm.$invalid) {
            return;
        }

        if (!$scope.processData.remark || $scope.processData.remark.trim() === '') {
            swal("錯誤", "請輸入處置說明", "error");
            return;
        }

        $scope.saving = true;

        var formData = new FormData();
        formData.append('dlv_inv', $scope.detailData.Dlv_Inv);
        formData.append('process_type', $scope.processData.type);
        formData.append('remark', $scope.processData.remark);

        if ($scope.processData.file) {
            formData.append('file', $scope.processData.file);
        }

        $http.post(Router.action('SearchCargo', 'AddProcess'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (response) {
            if (response.data.Redirect) {
                window.location = Router.action('Account', 'Login');
                return;
            }

            if (response.data.status === 'success') {
                $('#processModal').modal('hide');
                swal("成功", response.data.msg, "success");
                $scope.loadProcessList($scope.detailData.Dlv_Inv);
            } else {
                swal("錯誤", response.data.msg, "error");
            }
        }).catch(function (error) {
            console.error('新增處置說明失敗:', error);
            swal("錯誤", "新增處置說明失敗", "error");
        }).finally(function () {
            $scope.saving = false;
        });
    };

    // 處置說明結案
    $scope.finishProcess = function () {
        swal({
            title: "處置說明結案",
            text: "確定要將此貨況的處置說明結案嗎?",
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    visible: true
                },
                confirm: {
                    text: "確定"
                }
            }
        }).then(function (value) {
            if (value) {
                $http.post(Router.action('SearchCargo', 'FinishProcess'), {
                    dlv_inv: $scope.detailData.Dlv_Inv
                }).then(function (response) {
                    if (response.data.Redirect) {
                        window.location = Router.action('Account', 'Login');
                        return;
                    }

                    if (response.data.status === 'success') {
                        swal("成功", response.data.msg, "success");
                        $scope.loadProcessList($scope.detailData.Dlv_Inv);
                    } else {
                        swal("錯誤", response.data.msg, "error");
                    }
                }).catch(function (error) {
                    console.error('處置說明結案失敗:', error);
                    swal("錯誤", "處置說明結案失敗", "error");
                });
            }
        });
    };

    // 刪除處置說明
    $scope.deleteProcess = function (id) {
        swal({
            title: "刪除處置說明",
            text: "確定要刪除此處置說明嗎？",
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    visible: true
                },
                confirm: {
                    text: "確定",
                    closeModal: false
                }
            },
            dangerMode: true
        }).then(function (value) {
            if (value) {
                $http.post(Router.action('SearchCargo', 'DeleteProcess'), {
                    id: id
                }).then(function (response) {
                    if (response.data.Redirect) {
                        window.location = Router.action('Account', 'Login');
                        return;
                    }

                    if (response.data.status === 'success') {
                        swal("成功", response.data.msg, "success");
                        $scope.loadProcessList($scope.detailData.Dlv_Inv);
                    } else {
                        swal("錯誤", response.data.msg, "error");
                    }
                }).catch(function (error) {
                    console.error('刪除處置說明失敗:', error);
                    swal("錯誤", "刪除處置說明失敗", "error");
                });
            }
        });
    };

    // 顯示查詢紀錄
    $scope.showLogCargoStatus = function () {
        $http.post(Router.action('SearchCargo', 'GetLogCargoStatus'), {
            dlv_inv: $scope.detailData.Dlv_Inv
        }).then(function (response) {
            if (response.data.Redirect) {
                window.location = Router.action('Account', 'Login');
                return;
            }

            if (response.data.success) {
                $scope.logList = response.data.data;
                $('#logModal').modal('show');
            } else {
                swal("錯誤", response.data.error || "載入查詢紀錄失敗", "error");
            }
        }).catch(function (error) {
            console.error('載入查詢紀錄失敗:', error);
            swal("錯誤", "載入查詢紀錄失敗", "error");
        });
    };

    // 顯示速派新遞貨號
    $scope.showShenzhenCargoDetail = function () {
        $http.post(Router.action('SearchCargo', 'GetShenzhenCargoByTrackingNo'), {
            trackingNo: $scope.detailData.Bag_Number
        }).then(function (response) {
            if (response.data.Redirect) {
                window.location = Router.action('Account', 'Login');
                return;
            }

            if (response.data.success) {
                $scope.shenzhenCargoList = response.data.data;
                $('#shenzhenCargoModal').modal('show');
            } else {
                swal("錯誤", response.data.error || "載入速派新遞貨號失敗", "error");
            }
        }).catch(function (error) {
            console.error('載入速派新遞貨號失敗:', error);
            swal("錯誤", "載入速派新遞貨號失敗", "error");
        });
    };

    // 關閉視窗
    $scope.closeWindow = function () {
        $window.close();
    };

    // 返回列表
    $scope.backToList = function () {
        $window.close();
    };

    // 切換申報品名顯示
    $scope.toggleShowAllItems = function () {
        $scope.showAllItems = !$scope.showAllItems;
    };

    // 初始化載入
    $scope.loadDetail();
});
