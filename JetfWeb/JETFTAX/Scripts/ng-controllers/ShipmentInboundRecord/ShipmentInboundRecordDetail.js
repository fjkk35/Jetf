mainApp.controller('ShipmentInboundRecordDetailController', ['$scope', '$http', '$location', function ($scope, $http, $location) {
    // 初始化資料
    $scope.data = null;
    $scope.loading = true;
    $scope.errorMessage = '';
    $scope.editFieldNameText = '';
    $scope.newAmount = 0;
    $scope.currentFieldName = '';
    $scope.historyData = [];
    $scope.currentLightboxImage = '';

    // 從 URL 取得 id 參數
    function getQueryParam(name) {
        var url = window.location.href;
        name = name.replace(/[\[\]]/g, '\\$&');
        var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)');
        var results = regex.exec(url);
        if (!results) return null;
        if (!results[2]) return '';
        return decodeURIComponent(results[2].replace(/\+/g, ' '));
    }

    // 載入詳細資料
    $scope.loadDetail = function () {
        var id = getQueryParam('id');
        if (!id) {
            $scope.errorMessage = '缺少必要參數';
            $scope.loading = false;
            return;
        }

        $http.get(Router.action('ShipmentInboundRecord', 'GetDetailById'), {
            params: { id: id }
        })
            .then(function (response) {
                if (response.data.error) {
                    $scope.errorMessage = response.data.error;
                    return;
                }

                $scope.data = response.data.Data;
            })
            .catch(function (error) {
                console.error('載入詳細資料失敗:', error);
                $scope.errorMessage = '載入詳細資料失敗，請稍後再試';
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 編輯金額
    $scope.editAmount = function (fieldName, currentValue) {
        $scope.currentFieldName = fieldName;
        $scope.newAmount = currentValue;

        switch (fieldName) {
            case 'Cod':
                $scope.editFieldNameText = '到付款';
                break;
            case 'Tax':
                $scope.editFieldNameText = '稅金';
                break;
            case 'Ccfee':
                $scope.editFieldNameText = '報關費';
                break;
        }

        $('#editAmountModal').modal('show');
    };

    // 儲存金額
    $scope.saveAmount = function () {
        if (isNaN($scope.newAmount) || $scope.newAmount < 0) {
            swal({
                title: "錯誤",
                text: "請輸入有效的金額",
                icon: "error"
            });
            return;
        }

        var id = getQueryParam('id');
        $http.post(Router.action('ShipmentInboundRecord', 'UpdateAmount'), {
            Id: parseInt(id),
            FieldName: $scope.currentFieldName,
            NewValue: $scope.newAmount
        })
            .then(function (response) {
                if (response.data.error) {
                    swal({
                        title: "錯誤",
                        text: "更新失敗：" + response.data.error,
                        icon: "error"
                    });
                    return;
                }

                swal({
                    title: "成功",
                    text: "更新成功",
                    icon: "success"
                }).then(function () {
                    $('#editAmountModal').modal('hide');
                    $scope.loadDetail();
                });
            })
            .catch(function (error) {
                console.error('更新失敗:', error);
                swal({
                    title: "錯誤",
                    text: "更新失敗，請稍後再試",
                    icon: "error"
                });
            });
    };

    // 顯示編輯紀錄
    $scope.showEditHistory = function () {
        var id = getQueryParam('id');
        if (!id) {
            swal({
                title: "錯誤",
                text: "無法取得記錄",
                icon: "error"
            });
            return;
        }

        $http.get(Router.action('ShipmentInboundRecord', 'GetEditHistory'), {
            params: { shipmentInboundId: parseInt(id) }
        })
            .then(function (response) {
                if (response.data.error) {
                    swal({
                        title: "錯誤",
                        text: "查詢失敗：" + response.data.error,
                        icon: "error"
                    });
                    return;
                }

                $scope.historyData = response.data.Data || [];
                $('#editHistoryModal').modal('show');
            })
            .catch(function (error) {
                console.error('查詢編輯紀錄失敗:', error);
                swal({
                    title: "錯誤",
                    text: "查詢編輯紀錄失敗，請稍後再試",
                    icon: "error"
                });
            });
    };

    $scope.openLightbox = function (filePath, $event) {
        if ($event) {
            $event.preventDefault();
            $event.stopPropagation();
        }

        $scope.currentLightboxImage = filePath || '';
    };

    $scope.closeLightbox = function () {
        $scope.currentLightboxImage = '';
    };

    // 關閉視窗
    $scope.closeWindow = function () {
        window.close();
    };

    // 返回列表
    $scope.backToList = function () {
        window.location = Router.action('ShipmentInboundRecord', 'Index');
    };

    // 頁面載入時執行
    $scope.loadDetail();
}]);
