mainApp.controller('SjlBatchImportSearchController', ['$scope', '$timeout', function ($scope, $timeout) {
        $scope.searchParams = {
            startDate: null,
            endDate: null,
            jetfSerial: ''
        };
        $scope.dataList = [];
        $scope.isSearched = false;
        $scope.loading = false;
        $scope.savingTransName = false;
        $scope.editingItem = null;
        $scope.editingItems = [];
        $scope.editModel = {
            transName: ''
        };
        $scope.selectAllCurrentPage = false;
        $scope.currentPage = 1;
        $scope.pageSize = 10;
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.pageSizeOptions = [10, 25, 50, 100];
        $scope.paginationRenderPromise = null;
        $scope.dateOptions = {
            formatYear: 'yyyy',
            minDate: new Date(1900, 0, 1),
            startingDay: 0,
            showWeeks: false
        };
        $scope.startDatePopup = { opened: false };
        $scope.endDatePopup = { opened: false };
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.performSearch = function () {
            if ($scope.searchParams.startDate && $scope.searchParams.endDate && $scope.searchParams.startDate > $scope.searchParams.endDate) {
                swal({
                    title: '錯誤',
                    text: '日期起不可大於日期迄',
                    icon: 'error'
                });
                return;
            }
            $scope.currentPage = 1;
            $scope.isSearched = true;
            $scope.loadData();
        };
        $scope.loadData = function () {
            $scope.pageSize = normalizePageSize($scope.pageSize);
            $scope.loading = true;
            $.ajax({
                url: Router.action('SjlBatchImport', 'SearchData'),
                type: 'POST',
                dataType: 'json',
                data: {
                    StartDate: formatDate($scope.searchParams.startDate),
                    EndDate: formatDate($scope.searchParams.endDate),
                    JetfSerial: ($scope.searchParams.jetfSerial || '').trim(),
                    Page: $scope.currentPage,
                    PageSize: $scope.pageSize
                },
                success: function (response) {
                    $scope.$apply(function () {
                        if (response.error) {
                            swal({
                                title: '查詢失敗',
                                text: response.error,
                                icon: 'error'
                            });
                            return;
                        }
                        $scope.dataList = response.Data || [];
                        for (var i = 0; i < $scope.dataList.length; i++) {
                            $scope.dataList[i].IsSelected = false;
                        }
                        $scope.selectAllCurrentPage = false;
                        $scope.totalCount = response.TotalCount || 0;
                        $scope.totalPages = Math.ceil($scope.totalCount / $scope.pageSize);
                        if ($scope.paginationRenderPromise) {
                            $timeout.cancel($scope.paginationRenderPromise);
                        }
                        $scope.paginationRenderPromise = $timeout(function () {
                            $scope.initPagination();
                            $scope.paginationRenderPromise = null;
                        }, 50);
                    });
                },
                error: function () {
                    swal({
                        title: '查詢失敗',
                        text: '請稍後再試',
                        icon: 'error'
                    });
                },
                complete: function () {
                    $scope.$apply(function () {
                        $scope.loading = false;
                    });
                }
            });
        };
        $scope.initPagination = function () {
            var $pagination = $('#pagination-twbs');
            destroyPagination($pagination);
            if ($scope.totalPages <= 1) {
                return;
            }
            try {
                $pagination.twbsPagination({
                    totalPages: $scope.totalPages,
                    visiblePages: Math.min(10, $scope.totalPages),
                    startPage: $scope.currentPage,
                    initiateStartPageClick: false,
                    hideOnlyOnePage: true,
                    first: '第一頁',
                    prev: '上一頁',
                    next: '下一頁',
                    last: '最後一頁',
                    onPageClick: function (_event, page) {
                        if (page !== $scope.currentPage) {
                            $scope.$apply(function () {
                                $scope.currentPage = page;
                                $scope.loadData();
                            });
                        }
                    }
                });
            }
            catch (error) {
                console.error('Error initializing twbsPagination:', error);
            }
        };
        $scope.getRecordsInfo = function () {
            if (!$scope.isSearched || $scope.totalCount === 0) {
                return '顯示第 0 到 0 筆資料，共 0 筆';
            }
            var start = ($scope.currentPage - 1) * $scope.pageSize + 1;
            var end = Math.min($scope.currentPage * $scope.pageSize, $scope.totalCount);
            return '顯示第 ' + start + ' 到 ' + end + ' 筆資料，共 ' + $scope.totalCount + ' 筆';
        };
        $scope.changePageSize = function () {
            $scope.pageSize = normalizePageSize($scope.pageSize);
            $scope.currentPage = 1;
            if ($scope.isSearched) {
                $scope.loadData();
            }
        };
        $scope.clearSearch = function () {
            $scope.searchParams = {
                startDate: null,
                endDate: null,
                jetfSerial: ''
            };
            $scope.dataList = [];
            $scope.isSearched = false;
            $scope.selectAllCurrentPage = false;
            $scope.editingItem = null;
            $scope.editingItems = [];
            $scope.editModel.transName = '';
            $scope.currentPage = 1;
            $scope.totalCount = 0;
            $scope.totalPages = 0;
            if ($scope.paginationRenderPromise) {
                $timeout.cancel($scope.paginationRenderPromise);
                $scope.paginationRenderPromise = null;
            }
            destroyPagination($('#pagination-twbs'));
        };
        $scope.getSelectedItems = function () {
            var selectedItems = [];
            for (var i = 0; i < $scope.dataList.length; i++) {
                if ($scope.dataList[i].IsSelected) {
                    selectedItems.push($scope.dataList[i]);
                }
            }
            return selectedItems;
        };
        $scope.getSelectedCount = function () {
            return $scope.getSelectedItems().length;
        };
        $scope.toggleSelectAllCurrentPage = function () {
            $scope.selectAllCurrentPage = !$scope.selectAllCurrentPage;
            for (var i = 0; i < $scope.dataList.length; i++) {
                $scope.dataList[i].IsSelected = $scope.selectAllCurrentPage;
            }
        };
        $scope.toggleItemSelection = function (item) {
            item.IsSelected = !item.IsSelected;
            $scope.syncSelectAllCurrentPage();
        };
        $scope.syncSelectAllCurrentPage = function () {
            if ($scope.dataList.length === 0) {
                $scope.selectAllCurrentPage = false;
                return;
            }
            for (var i = 0; i < $scope.dataList.length; i++) {
                if (!$scope.dataList[i].IsSelected) {
                    $scope.selectAllCurrentPage = false;
                    return;
                }
            }
            $scope.selectAllCurrentPage = true;
        };
        $scope.showEditTransNameModal = function (item) {
            $scope.editingItem = angular.copy(item);
            $scope.editingItems = [angular.copy(item)];
            $scope.editModel.transName = item.TransName || '';
            $('#transNameModal').modal('show');
        };
        $scope.showBatchEditTransNameModal = function () {
            var selectedItems = $scope.getSelectedItems();
            if (selectedItems.length === 0) {
                swal({
                    title: '提示',
                    text: '請至少勾選一筆資料',
                    icon: 'warning'
                });
                return;
            }
            $scope.editingItems = angular.copy(selectedItems);
            $scope.editingItem = selectedItems.length === 1 ? angular.copy(selectedItems[0]) : null;
            $scope.editModel.transName = '';
            $('#transNameModal').modal('show');
        };
        $scope.saveTransName = function () {
            if ($scope.editingItems.length === 0) {
                return;
            }
            var targetIds = [];
            for (var i = 0; i < $scope.editingItems.length; i++) {
                targetIds.push($scope.editingItems[i].Id);
            }
            if (!$scope.editModel.transName) {
                swal({
                    title: '錯誤',
                    text: '請選擇派件公司',
                    icon: 'error'
                });
                return;
            }
            var hasChanges = false;
            for (var j = 0; j < $scope.editingItems.length; j++) {
                if (($scope.editingItems[j].TransName || '') !== $scope.editModel.transName) {
                    hasChanges = true;
                    break;
                }
            }
            if (!hasChanges) {
                swal({
                    title: '提示',
                    text: '派件公司未異動，不需修改',
                    icon: 'warning'
                });
                return;
            }
            $scope.savingTransName = true;
            $.ajax({
                url: Router.action('SjlBatchImport', 'UpdateTransName'),
                type: 'POST',
                dataType: 'json',
                traditional: true,
                data: {
                    SjlShippingDataId: targetIds.length === 1 ? targetIds[0] : 0,
                    SjlShippingDataIds: targetIds,
                    TransName: $scope.editModel.transName
                },
                success: function (response) {
                    $scope.$apply(function () {
                        if (response.status === 'success') {
                            var updatedIds = (response.ReturnObject && response.ReturnObject.Ids) || [];
                            if (updatedIds.length === 0 && response.ReturnObject && response.ReturnObject.Id) {
                                updatedIds.push(response.ReturnObject.Id);
                            }
                            for (var i = 0; i < $scope.dataList.length; i++) {
                                if (updatedIds.indexOf($scope.dataList[i].Id) >= 0 && response.ReturnObject) {
                                    $scope.dataList[i].TransName = response.ReturnObject.TransName;
                                }
                                $scope.dataList[i].IsSelected = false;
                            }
                            $scope.selectAllCurrentPage = false;
                            $scope.editingItem = null;
                            $scope.editingItems = [];
                            $scope.editModel.transName = '';
                            $('#transNameModal').modal('hide');
                            swal({
                                title: '成功',
                                text: response.msg || '修改成功',
                                icon: 'success'
                            });
                        }
                        else {
                            swal({
                                title: '錯誤',
                                text: response.msg || '修改失敗',
                                icon: 'error'
                            });
                        }
                    });
                },
                error: function () {
                    swal({
                        title: '錯誤',
                        text: '修改失敗，請稍後再試',
                        icon: 'error'
                    });
                },
                complete: function () {
                    $scope.$apply(function () {
                        $scope.savingTransName = false;
                    });
                }
            });
        };
        function formatDate(value) {
            if (!value) {
                return '';
            }
            var dateValue = new Date(value);
            var month = ('0' + (dateValue.getMonth() + 1)).slice(-2);
            var day = ('0' + dateValue.getDate()).slice(-2);
            return dateValue.getFullYear() + '-' + month + '-' + day;
        }
        function normalizePageSize(value) {
            var pageSize = parseInt(String(value), 10);
            if (isNaN(pageSize) || pageSize <= 0) {
                return 10;
            }
            return pageSize;
        }
        function destroyPagination($pagination) {
            if ($pagination.data('twbs-pagination')) {
                try {
                    $pagination.twbsPagination('destroy');
                }
                catch (error) {
                    console.error('Error destroying twbsPagination:', error);
                }
            }
            $pagination.off();
            $pagination.removeData('twbs-pagination');
            $pagination.empty();
        }
    }]);
