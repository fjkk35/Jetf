mainApp.controller('ShipmentInboundRecordDetailController', ['$scope', '$http', '$location', '$timeout', '$q', function ($scope, $http, $location, $timeout, $q) {
    // 初始化資料
    $scope.data = null;
    $scope.loading = true;
    $scope.errorMessage = '';
    $scope.historyData = [];
    $scope.editTrackingNo = '';
    $scope.savingTrackingNo = false;
    $scope.editSourceType = '';
    $scope.savingSourceType = false;
    $scope.savingBasicInfo = false;
    $scope.customerOptions = [];
    $scope.transOptions = [];
    $scope.sourceTypeList = [];

    $scope.dataTypes = [
        { Value: '海運', Text: '海運' },
        { Value: '空運', Text: '空運' }
    ];

    $scope.basicInfoForm = {
        dataType: '',
        custCode: '',
        sourceType: '',
        selectedTrans: null
    };

    var exceptionViewer = null;

    function normalizeText(value) {
        return value ? value.toString().trim() : '';
    }

    function resetBasicInfoForm() {
        $scope.basicInfoForm = {
            dataType: '',
            custCode: '',
            sourceType: '',
            selectedTrans: null
        };
    }

    function findMatchedTransOption(transNo, transName) {
        var normalizedTransNo = normalizeText(transNo);
        var normalizedTransName = normalizeText(transName);

        for (var index = 0; index < $scope.transOptions.length; index++) {
            var item = $scope.transOptions[index];
            if (normalizeText(item.TransNo) === normalizedTransNo
                && normalizeText(item.TransName) === normalizedTransName) {
                return item;
            }
        }

        return null;
    }

    function loadSourceTypeList() {
        if ($scope.sourceTypeList.length > 0) {
            return $q.when($scope.sourceTypeList);
        }

        return $http.get(Router.action('ShipmentInboundRecord', 'GetSourceTypeList'))
            .then(function (response) {
                $scope.sourceTypeList = response.data || [];
                return $scope.sourceTypeList;
            });
    }

    function loadUnknownShipmentCustList(dataType) {
        if (!dataType) {
            $scope.customerOptions = [];
            return $q.when([]);
        }

        return $http.get(Router.action('ShipmentInboundRecord', 'GetUnknownShipmentCustList'), {
            params: { dataType: dataType }
        }).then(function (response) {
            $scope.customerOptions = response.data || [];
            return $scope.customerOptions;
        });
    }

    function loadUnknownShipmentTransList(dataType) {
        if (!dataType) {
            $scope.transOptions = [];
            return $q.when([]);
        }

        return $http.get(Router.action('ShipmentInboundRecord', 'GetUnknownShipmentTransList'), {
            params: { dataType: dataType }
        }).then(function (response) {
            $scope.transOptions = response.data || [];
            return $scope.transOptions;
        });
    }

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

    $scope.getCargoReceiptUrl = function (cargoNumber) {
        return Router.action('Cargo', 'CargoSignReceipt') +
            '?cargoNumber=' + encodeURIComponent((cargoNumber || '').toString().trim());
    };

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

    $scope.openSourceTypeDialog = function () {
        if (!$scope.data) {
            return;
        }

        $scope.editSourceType = $scope.data.SourceType != null ? $scope.data.SourceType.toString() : '';

        loadSourceTypeList()
            .then(function () {
                $('#editSourceTypeModal').modal('show');
            })
            .catch(function (error) {
                console.error('載入貨件來源清單失敗:', error);
                swal({
                    title: '載入失敗',
                    text: (error && error.data && error.data.error) ? error.data.error : '無法載入貨件來源清單，請稍後再試',
                    icon: 'error'
                });
            });
    };

    $scope.onBasicInfoDataTypeChange = function () {
        $scope.basicInfoForm.custCode = '';
        $scope.basicInfoForm.selectedTrans = null;

        return $q.all([
            loadUnknownShipmentCustList($scope.basicInfoForm.dataType),
            loadUnknownShipmentTransList($scope.basicInfoForm.dataType)
        ]);
    };

    $scope.openBasicInfoDialog = function () {
        if (!$scope.data || $scope.data.IsOrderOriginal !== false) {
            return;
        }

        resetBasicInfoForm();
        $scope.basicInfoForm.dataType = normalizeText($scope.data.DataType);
        $scope.basicInfoForm.sourceType = $scope.data.SourceType != null ? $scope.data.SourceType.toString() : '';

        loadSourceTypeList()
            .then(function () {
                return $scope.onBasicInfoDataTypeChange();
            })
            .then(function () {
                $scope.basicInfoForm.custCode = normalizeText($scope.data.CustCode);
                $scope.basicInfoForm.selectedTrans = findMatchedTransOption($scope.data.TransNo, $scope.data.TransName);
                $('#editBasicInfoModal').modal('show');
            })
            .catch(function (error) {
                console.error('載入不明貨件基本資料編輯選項失敗:', error);
                swal({
                    title: '載入失敗',
                    text: (error && error.data && error.data.error) ? error.data.error : '無法載入編輯選項，請稍後再試',
                    icon: 'error'
                });
            });
    };

    $scope.saveBasicInfo = function () {
        var id = getQueryParam('id');
        if (!id) {
            swal({
                title: '錯誤',
                text: '缺少必要參數',
                icon: 'error'
            });
            return;
        }

        $scope.savingBasicInfo = true;

        $http.post(Router.action('ShipmentInboundRecord', 'UpdateUnknownShipmentBasicInfo'), {
            Id: parseInt(id, 10),
            DataType: $scope.basicInfoForm.dataType,
            CustCode: $scope.basicInfoForm.custCode,
            TransNo: $scope.basicInfoForm.selectedTrans ? $scope.basicInfoForm.selectedTrans.TransNo : null,
            TransName: $scope.basicInfoForm.selectedTrans ? $scope.basicInfoForm.selectedTrans.TransName : null,
            SourceType: ($scope.basicInfoForm.sourceType === '' || $scope.basicInfoForm.sourceType === null || typeof $scope.basicInfoForm.sourceType === 'undefined')
                ? null
                : parseInt($scope.basicInfoForm.sourceType, 10)
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
                    text: '基本資料更新成功',
                    icon: 'success'
                }).then(function () {
                    $('#editBasicInfoModal').modal('hide');
                    $scope.loadDetail();
                });
            })
            .catch(function (error) {
                console.error('更新不明貨件基本資料失敗:', error);
                swal({
                    title: '錯誤',
                    text: '更新失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.savingBasicInfo = false;
            });
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

    $scope.saveSourceType = function () {
        if ($scope.editSourceType === '' || $scope.editSourceType === null || typeof $scope.editSourceType === 'undefined') {
            swal({
                title: '錯誤',
                text: '請選擇貨件來源',
                icon: 'error'
            });
            return;
        }

        var id = getQueryParam('id');
        if (!id) {
            swal({
                title: '錯誤',
                text: '缺少必要參數',
                icon: 'error'
            });
            return;
        }

        $scope.savingSourceType = true;

        $http.post(Router.action('ShipmentInboundRecord', 'UpdateSourceType'), {
            Id: parseInt(id, 10),
            SourceType: parseInt($scope.editSourceType, 10)
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
                    text: '貨件來源更新成功',
                    icon: 'success'
                }).then(function () {
                    $('#editSourceTypeModal').modal('hide');
                    $scope.loadDetail();
                });
            })
            .catch(function (error) {
                console.error('更新貨件來源失敗:', error);
                swal({
                    title: '錯誤',
                    text: '更新貨件來源失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.savingSourceType = false;
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
