mainApp.controller('ShipmentInboundRecordDetailController', ['$scope', '$http', '$location', '$timeout', function ($scope, $http, $location, $timeout) {
    // 初始化資料
    $scope.data = null;
    $scope.loading = true;
    $scope.errorMessage = '';
    $scope.editFieldNameText = '';
    $scope.newAmount = 0;
    $scope.currentFieldName = '';
    $scope.historyData = [];
    $scope.editTrackingNo = '';
    $scope.savingTrackingNo = false;

    var exceptionViewer = null;

    function destroyExceptionViewer() {
        if (exceptionViewer) {
            try { exceptionViewer.viewer('destroy'); } catch (e) { }
            exceptionViewer = null;
        }
    }

    function initExceptionViewer() {
        $timeout(function () {
            var gallery = document.getElementById('exceptionImageGallery');
            if (!gallery || !gallery.querySelector('img')) {
                destroyExceptionViewer();
                return;
            }

            if (typeof $.fn.viewer === 'undefined') {
                $timeout(initExceptionViewer, 100);
                return;
            }

            destroyExceptionViewer();
            exceptionViewer = $(gallery);
            exceptionViewer.viewer({
                navbar: false,
                title: false,
                transition: true,
                rotatable: true,
                scalable: true,
                zoomable: true,
                toolbar: {
                    zoomIn: 1,
                    zoomOut: 1,
                    oneToOne: 1,
                    reset: 1,
                    prev: 0,
                    play: 0,
                    next: 0,
                    rotateLeft: 1,
                    rotateRight: 1,
                    flipHorizontal: 0,
                    flipVertical: 0
                }
            });
        }, 0);
    }

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
                    initExceptionViewer();
            })
            .catch(function (error) {
                console.error('載入詳細資料失敗:', error);
                $scope.errorMessage = '載入詳細資料失敗，請稍後再試';
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.openTrackingNoDialog = function () {
        $scope.editTrackingNo = $scope.data && $scope.data.TrackingNo ? $scope.data.TrackingNo : '';
        $('#editTrackingNoModal').modal('show');
    };

    $scope.saveTrackingNo = function () {
        var trackingNo = ($scope.editTrackingNo || '').trim();
        if (!trackingNo) {
            swal({
                title: '錯誤',
                text: '請輸入新單號',
                icon: 'error'
            });
            return;
        }

        var id = getQueryParam('id');
        $scope.savingTrackingNo = true;

        $http.post(Router.action('ShipmentInboundRecord', 'UpdateTrackingNo'), {
            Id: parseInt(id),
            NewTrackingNo: trackingNo
        })
            .then(function (response) {
                if (response.data.error) {
                    swal({
                        title: '錯誤',
                        text: response.data.error,
                        icon: 'error'
                    });
                    return;
                }

                swal({
                    title: '成功',
                    text: '單號更新成功',
                    icon: 'success'
                }).then(function () {
                    $('#editTrackingNoModal').modal('hide');
                    $scope.loadDetail();
                });
            })
            .catch(function (error) {
                console.error('更新單號失敗:', error);
                swal({
                    title: '錯誤',
                    text: '更新單號失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.savingTrackingNo = false;
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

    // 關閉視窗
    $scope.closeWindow = function () {
        window.close();
    };

    // 返回列表
    $scope.backToList = function () {
        window.location = Router.action('ShipmentInboundRecord', 'Index');
    };

    $scope.$on('$destroy', function () {
        destroyExceptionViewer();
    });

    // 頁面載入時執行
    $scope.loadDetail();
}]);
