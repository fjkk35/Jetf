mainApp.controller('ShipmentInboundLocationTransferController', function ($scope, $http) {
    $scope.data = [];
    $scope.loading = false;
    $scope.isSearched = false;
    $scope.saving = false;
    $scope.totalCount = 0;

    $scope.searchForm = {
        locationCode: '',
        trackingNo: '',
        seqNo: ''
    };

    $scope.selectedItems = [];
    $scope.selectAll = false;

    // Modal 相關
    $scope.currentItem = null;
    $scope.modalTitle = '';
    $scope.transferMode = ''; // 'single' or 'batch'
    $scope.transferForm = {
        newLocationCode: ''
    };

    $scope.init = function () {
    };

    $scope.search = function () {
        if (!$scope.searchForm.locationCode || $scope.searchForm.locationCode.trim() === '') {
            if ((!$scope.searchForm.trackingNo || $scope.searchForm.trackingNo.trim() === '')
                && (!$scope.searchForm.seqNo || $scope.searchForm.seqNo.trim() === '')) {
                swal({
                    title: "提醒",
                    text: "請輸入儲位、單號或流水號",
                    icon: "warning"
                });
                return;
            }
        }

        $scope.isSearched = true;
        $scope.loadData();
    };

    $scope.loadData = function () {
        $scope.loading = true;

        var searchRequest = {
            LocationCode: $scope.searchForm.locationCode,
            TrackingNo: $scope.searchForm.trackingNo,
            SeqNo: $scope.searchForm.seqNo
        };

        $http.post(Router.action('ShipmentInboundLocationTransfer', 'SearchData'), searchRequest)
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
                $scope.totalCount = response.data.TotalCount || 0;

                $scope.selectedItems = [];
                $scope.selectAll = false;
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

    $scope.clearSearch = function () {
        $scope.searchForm = {
            locationCode: '',
            trackingNo: '',
            seqNo: ''
        };
        $scope.data = [];
        $scope.isSearched = false;
        $scope.totalCount = 0;
        $scope.selectedItems = [];
        $scope.selectAll = false;
    };

    $scope.toggleSelectAll = function () {
        $scope.selectedItems = [];
        if ($scope.selectAll) {
            angular.forEach($scope.data, function (item) {
                $scope.selectedItems.push(item.Id);
            });
        }
    };

    $scope.toggleSelection = function (id) {
        var idx = $scope.selectedItems.indexOf(id);
        if (idx > -1) {
            $scope.selectedItems.splice(idx, 1);
        } else {
            $scope.selectedItems.push(id);
        }

        $scope.selectAll = ($scope.selectedItems.length === $scope.data.length);
    };

    $scope.isSelected = function (id) {
        return $scope.selectedItems.indexOf(id) > -1;
    };

    // 開啟單件移儲 Modal
    $scope.openSingleTransferModal = function (item) {
        $scope.currentItem = item;
        $scope.transferMode = 'single';
        $scope.modalTitle = '單件移儲';
        $scope.transferForm = {
            newLocationCode: ''
        };

        $('#transferModal').modal('show');
    };

    // 開啟批次移儲 Modal
    $scope.openBatchTransferModal = function () {
        if ($scope.selectedItems.length === 0) {
            swal({
                title: "提醒",
                text: "請至少選擇一筆資料",
                icon: "warning"
            });
            return;
        }

        $scope.transferMode = 'batch';
        $scope.modalTitle = '批次移儲';
        $scope.transferForm = {
            newLocationCode: ''
        };

        $('#transferModal').modal('show');
    };

    // 檢查新儲位是否與現有儲位相同
    $scope.isSameLocation = function () {
        if (!$scope.transferForm.newLocationCode || $scope.transferForm.newLocationCode.trim() === '') {
            return true;
        }

        var newLocation = $scope.transferForm.newLocationCode.trim();

        if ($scope.transferMode === 'single') {
            // 單件移儲：檢查是否與該項目的儲位相同
            return $scope.currentItem && $scope.currentItem.LocationCode === newLocation;
        } else {
            // 批次移儲：檢查是否所有選中的項目都有相同的儲位
            var allSameLocation = true;
            for (var i = 0; i < $scope.selectedItems.length; i++) {
                var item = $scope.data.find(function (d) {
                    return d.Id === $scope.selectedItems[i];
                });
                if (item && item.LocationCode !== newLocation) {
                    allSameLocation = false;
                    break;
                }
            }
            return allSameLocation;
        }
    };

    // 儲存移儲
    $scope.saveTransfer = function () {
        // 驗證新儲位必填
        if (!$scope.transferForm.newLocationCode || $scope.transferForm.newLocationCode.trim() === '') {
            swal({
                title: "提醒",
                text: "請輸入新儲位",
                icon: "warning"
            });
            return;
        }

        var request;
        if ($scope.transferMode === 'single') {
            request = {
                Ids: [$scope.currentItem.Id],
                NewLocationCode: $scope.transferForm.newLocationCode
            };
        } else {
            request = {
                Ids: $scope.selectedItems,
                NewLocationCode: $scope.transferForm.newLocationCode
            };
        }

        $scope.performUpdate(request);
    };

    $scope.performUpdate = function (request) {
        $scope.saving = true;

        $http.post(Router.action('ShipmentInboundLocationTransfer', 'UpdateLocation'), request)
            .then(function (response) {
                if (response.data.status === 'success') {
                    swal({
                        title: "成功",
                        text: response.data.msg || "儲位更新成功",
                        icon: "success"
                    });
                    $('#transferModal').modal('hide');
                    $scope.loadData();
                } else {
                    swal({
                        title: "失敗",
                        text: response.data.msg || "儲位更新失敗",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('更新失敗:', error);
                swal({
                    title: "錯誤",
                    text: "儲位更新失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    $scope.init();
});
