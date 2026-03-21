// Controller
mainApp.controller('AbnormalStateManagementController', function ($scope, $http) {
    // 初始化資料
    $scope.abnormalStates = [];
    $scope.abnormalStateDetails = [];
    $scope.selectedAbnormalState = null;
    $scope.loadingAbnormalStates = true;
    $scope.loadingAbnormalStateDetails = false;
    $scope.saving = false;
    
    // Modal 模式和表單資料
    $scope.abnormalStateModalMode = 'add'; // 'add' 或 'edit'
    $scope.abnormalStateDetailModalMode = 'add'; // 'add' 或 'edit'
    $scope.abnormalStateForm = {};
    $scope.abnormalStateDetailForm = {};

    // 異常狀態拖曳排序設定 - 參考 ApprovalCategory
    $scope.abnormalStateSortableOptions = {
        axis: 'y', // 只允許垂直拖曳
        items: 'tr.sortable-row', // 指定可拖曳的元素
        placeholder: 'ui-sortable-placeholder',
        tolerance: 'pointer',
        distance: 5, // 拖曳5像素後才開始排序
        opacity: 0.8, // 拖曳時的透明度
        cursor: 'move',
        cancel: '.action-cell button', // 只有按鈕本身不能開始拖曳，其他地方都可以
        update: function (e, ui) {
            // 拖曳完成後更新排序
            setTimeout(function() {
                $scope.$apply(function() {
                    $scope.updateAbnormalStateSorts();
                });
            }, 100);
        },
        start: function(e, ui) {
            // 拖曳開始時的效果
            ui.item.addClass('dragging');
        },
        stop: function(e, ui) {
            // 拖曳結束時的效果
            ui.item.removeClass('dragging');
        }
    };

    // 異常狀態詳細拖曳排序設定 - 參考 ApprovalCategory
    $scope.abnormalStateDetailSortableOptions = {
        axis: 'y', // 只允許垂直拖曳
        items: 'tr.sortable-row', // 指定可拖曳的元素
        placeholder: 'ui-sortable-placeholder',
        tolerance: 'pointer',
        distance: 5, // 拖曳5像素後才開始排序
        opacity: 0.8, // 拖曳時的透明度
        cursor: 'move',
        cancel: '.action-cell button', // 只有按鈕本身不能開始拖曳，其他地方都可以
        update: function (e, ui) {
            // 拖曳完成後更新排序
            setTimeout(function() {
                $scope.$apply(function() {
                    $scope.updateAbnormalStateDetailSorts();
                });
            }, 100);
        },
        start: function(e, ui) {
            // 拖曳開始時的效果
            ui.item.addClass('dragging');
        },
        stop: function(e, ui) {
            // 拖曳結束時的效果
            ui.item.removeClass('dragging');
        }
    };

    // 載入所有異常狀態
    $scope.loadAbnormalStates = function () {
        $scope.loadingAbnormalStates = true;
        $http.get(Router.action('AbnormalState', 'GetAllAbnormalStatesWithDetails'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.abnormalStates = response.data;
                } else if (response.data && response.data.msg) {
                    $scope.abnormalStates = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.abnormalStates = [];
                }
            })
            .catch(function (error) {
                console.error('載入異常狀態失敗:', error);
                $scope.abnormalStates = [];
                swal({
                    title: "錯誤",
                    text: "載入異常狀態資料失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loadingAbnormalStates = false;
            });
    };

    // 更新異常狀態排序 - 參考 ApprovalCategory 的邏輯
    $scope.updateAbnormalStateSorts = function() {
        var sortUpdates = [];
        
        // 重新計算排序號碼
        for (var i = 0; i < $scope.abnormalStates.length; i++) {
            var abnormalState = $scope.abnormalStates[i];
            var newSort = i + 1;
            
            // 只有排序改變的才需要更新
            if (abnormalState.Sort !== newSort) {
                abnormalState.Sort = newSort;
                sortUpdates.push({
                    Id: abnormalState.Id,
                    Sort: newSort
                });
            }
        }

        // 如果有需要更新的排序
        if (sortUpdates.length > 0) {
            console.log('更新異常狀態排序:', sortUpdates);
            
            $http.post(Router.action('AbnormalState', 'UpdateAbnormalStateSorts'), sortUpdates)
                .then(function (response) {
                    if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                        console.log('異常狀態排序更新成功');
                        
                        // 簡短提示
                        if (typeof toastr !== 'undefined') {
                            toastr.success('排序更新成功', '', { timeOut: 2000 });
                        }
                    } else {
                        swal({
                            title: "排序更新失敗",
                            text: response.data.msg || "排序更新失敗",
                            icon: "error"
                        });
                        // 重新載入以恢復原始排序
                        $scope.loadAbnormalStates();
                    }
                })
                .catch(function (error) {
                    console.error('更新異常狀態排序失敗:', error);
                    swal({
                        title: "錯誤",
                        text: "排序更新失敗",
                        icon: "error"
                    });
                    // 重新載入以恢復原始排序
                    $scope.loadAbnormalStates();
                });
        }
    };

    // 更新異常狀態詳細排序 - 參考 ApprovalCategory 的邏輯
    $scope.updateAbnormalStateDetailSorts = function() {
        var sortUpdates = [];
        
        // 重新計算排序號碼
        for (var i = 0; i < $scope.abnormalStateDetails.length; i++) {
            var detail = $scope.abnormalStateDetails[i];
            var newSort = i + 1;
            
            // 只有排序改變的才需要更新
            if (detail.Sort !== newSort) {
                detail.Sort = newSort;
                sortUpdates.push({
                    Id: detail.Id,
                    Sort: newSort
                });
            }
        }

        // 如果有需要更新的排序
        if (sortUpdates.length > 0) {
            console.log('更新異常狀態詳細排序:', sortUpdates);
            
            $http.post(Router.action('AbnormalState', 'UpdateAbnormalStateDetailSorts'), sortUpdates)
                .then(function (response) {
                    if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                        console.log('異常狀態詳細排序更新成功');
                        
                        // 簡短提示
                        if (typeof toastr !== 'undefined') {
                            toastr.success('排序更新成功', '', { timeOut: 2000 });
                        }
                        
                        // 同步更新選中異常狀態的異常狀態詳細
                        if ($scope.selectedAbnormalState) {
                            $scope.selectedAbnormalState.AbnormalStateDetails = angular.copy($scope.abnormalStateDetails);
                            
                            // 同步更新主異常狀態陣列中對應異常狀態的詳細
                            var abnormalStateIndex = $scope.abnormalStates.findIndex(function(s) { 
                                return s.Id === $scope.selectedAbnormalState.Id; 
                            });
                            if (abnormalStateIndex >= 0) {
                                $scope.abnormalStates[abnormalStateIndex].AbnormalStateDetails = angular.copy($scope.abnormalStateDetails);
                            }
                        }
                    } else {
                        swal({
                            title: "排序更新失敗",
                            text: response.data.msg || "排序更新失敗",
                            icon: "error"
                        });
                        // 重新載入以恢復原始排序
                        $scope.selectAbnormalState($scope.selectedAbnormalState);
                    }
                })
                .catch(function (error) {
                    console.error('更新異常狀態詳細排序失敗:', error);
                    swal({
                        title: "錯誤",
                        text: "排序更新失敗",
                        icon: "error"
                    });
                    // 重新載入以恢復原始排序
                    $scope.selectAbnormalState($scope.selectedAbnormalState);
                });
        }
    };

    // 選擇異常狀態
    $scope.selectAbnormalState = function (abnormalState) {
        $scope.selectedAbnormalState = abnormalState;
        $scope.abnormalStateDetails = abnormalState.AbnormalStateDetails || [];
        console.log('選擇異常狀態:', abnormalState.AbnormalStateName, '詳細數量:', $scope.abnormalStateDetails.length);
    };

    // 顯示異常狀態 Modal
    $scope.showAbnormalStateModal = function (mode, abnormalState) {
        $scope.abnormalStateModalMode = mode;
        
        if (mode === 'add') {
            $scope.abnormalStateForm = {
                AbnormalStateName: ''
            };
        } else {
            $scope.abnormalStateForm = {
                Id: abnormalState.Id,
                AbnormalStateName: abnormalState.AbnormalStateName,
                Sort: abnormalState.Sort
            };
        }
        
        $('#abnormalStateModal').modal('show');
    };

    // 儲存異常狀態
    $scope.saveAbnormalState = function () {
        if (!$scope.abnormalStateForm.AbnormalStateName || $scope.abnormalStateForm.AbnormalStateName.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入異常狀態名稱",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;
        
        var action = $scope.abnormalStateModalMode === 'add' ? 'CreateAbnormalState' : 'UpdateAbnormalState';
        var successMessage = $scope.abnormalStateModalMode === 'add' ? '新增成功' : '更新成功';

        $http.post(Router.action('AbnormalState', action), $scope.abnormalStateForm)
            .then(function (response) {
                if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                    swal({
                        title: "成功",
                        text: successMessage,
                        icon: "success",
                        timer: 2000
                    });
                    
                    $('#abnormalStateModal').modal('hide');
                    $scope.loadAbnormalStates();
                    
                    // 如果是新增，選擇新建立的異常狀態
                    if ($scope.abnormalStateModalMode === 'add' && response.data.ReturnObject) {
                        setTimeout(function() {
                            var newAbnormalState = $scope.abnormalStates.find(function(s) { 
                                return s.Id === response.data.ReturnObject; 
                            });
                            if (newAbnormalState) {
                                $scope.selectAbnormalState(newAbnormalState);
                                $scope.$apply();
                            }
                        }, 500);
                    }
                } else {
                    swal({
                        title: "操作失敗",
                        text: response.data.msg || "操作失敗，請稍後再試",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('儲存異常狀態失敗:', error);
                swal({
                    title: "錯誤",
                    text: "儲存失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    // 刪除異常狀態
    $scope.deleteAbnormalState = function (abnormalState) {
        swal({
            title: "確認刪除",
            text: `確定要刪除異常狀態「${abnormalState.AbnormalStateName}」嗎？\n※ 如果有異常狀態詳細，需要先刪除異常狀態詳細`,
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    value: null,
                    visible: true,
                    className: "btn-secondary",
                    closeModal: true,
                },
                confirm: {
                    text: "確定刪除",
                    value: true,
                    visible: true,
                    className: "btn-danger",
                    closeModal: true
                }
            },
            dangerMode: true,
        }).then(function (willDelete) {
            if (willDelete) {
                $http.post(Router.action('AbnormalState', 'DeleteAbnormalState'), { id: abnormalState.Id })
                    .then(function (response) {
                        if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                            swal({
                                title: "刪除成功",
                                text: "異常狀態刪除完成",
                                icon: "success",
                                timer: 2000
                            });
                            
                            // 如果刪除的是當前選中的異常狀態，清除選擇
                            if ($scope.selectedAbnormalState && $scope.selectedAbnormalState.Id === abnormalState.Id) {
                                $scope.selectedAbnormalState = null;
                                $scope.abnormalStateDetails = [];
                            }
                            
                            $scope.loadAbnormalStates();
                        } else {
                            swal({
                                title: "刪除失敗",
                                text: response.data.msg || "刪除失敗，請稍後再試",
                                icon: "error"
                            });
                        }
                    })
                    .catch(function (error) {
                        console.error('刪除異常狀態失敗:', error);
                        swal({
                            title: "錯誤",
                            text: "刪除失敗，請稍後再試",
                            icon: "error"
                        });
                    });
            }
        });
    };

    // 顯示異常狀態詳細 Modal
    $scope.showAbnormalStateDetailModal = function (mode, abnormalStateDetail) {
        if (!$scope.selectedAbnormalState) {
            swal({
                title: "提示",
                text: "請先選擇異常狀態",
                icon: "warning"
            });
            return;
        }

        $scope.abnormalStateDetailModalMode = mode;
        
        if (mode === 'add') {
            $scope.abnormalStateDetailForm = {
                AbnormalStateId: $scope.selectedAbnormalState.Id,
                AbnormalStateDetailName: ''
            };
        } else {
            $scope.abnormalStateDetailForm = {
                Id: abnormalStateDetail.Id,
                AbnormalStateId: abnormalStateDetail.AbnormalStateId,
                AbnormalStateDetailName: abnormalStateDetail.AbnormalStateDetailName,
                Sort: abnormalStateDetail.Sort
            };
        }
        
        $('#abnormalStateDetailModal').modal('show');
    };

    // 儲存異常狀態詳細
    $scope.saveAbnormalStateDetail = function () {
        if (!$scope.abnormalStateDetailForm.AbnormalStateDetailName || $scope.abnormalStateDetailForm.AbnormalStateDetailName.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入異常狀態詳細名稱",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;
        
        var action = $scope.abnormalStateDetailModalMode === 'add' ? 'CreateAbnormalStateDetail' : 'UpdateAbnormalStateDetail';
        var successMessage = $scope.abnormalStateDetailModalMode === 'add' ? '新增成功' : '更新成功';

        $http.post(Router.action('AbnormalState', action), $scope.abnormalStateDetailForm)
            .then(function (response) {
                if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                    swal({
                        title: "成功",
                        text: successMessage,
                        icon: "success",
                        timer: 2000
                    });
                    
                    $('#abnormalStateDetailModal').modal('hide');
                    
                    // 重新載入異常狀態列表以更新計數
                    $scope.loadAbnormalStates();
                    
                    // 重新選擇當前異常狀態以更新異常狀態詳細列表
                    setTimeout(function() {
                        var currentAbnormalState = $scope.abnormalStates.find(function(s) { 
                            return s.Id === $scope.selectedAbnormalState.Id; 
                        });
                        if (currentAbnormalState) {
                            $scope.selectAbnormalState(currentAbnormalState);
                            $scope.$apply();
                        }
                    }, 500);
                } else {
                    swal({
                        title: "操作失敗",
                        text: response.data.msg || "操作失敗，請稍後再試",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('儲存異常狀態詳細失敗:', error);
                swal({
                    title: "錯誤",
                    text: "儲存失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    // 刪除異常狀態詳細
    $scope.deleteAbnormalStateDetail = function (abnormalStateDetail) {
        swal({
            title: "確認刪除",
            text: `確定要刪除異常狀態詳細「${abnormalStateDetail.AbnormalStateDetailName}」嗎？\n※ 此操作無法復原`,
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    value: null,
                    visible: true,
                    className: "btn-secondary",
                    closeModal: true,
                },
                confirm: {
                    text: "確定刪除",
                    value: true,
                    visible: true,
                    className: "btn-danger",
                    closeModal: true
                }
            },
            dangerMode: true,
        }).then(function (willDelete) {
            if (willDelete) {
                $http.post(Router.action('AbnormalState', 'DeleteAbnormalStateDetail'), { id: abnormalStateDetail.Id })
                    .then(function (response) {
                        if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                            swal({
                                title: "刪除成功",
                                text: "異常狀態詳細刪除完成",
                                icon: "success",
                                timer: 2000
                            });
                            
                            // 重新載入異常狀態列表以更新計數
                            $scope.loadAbnormalStates();
                            
                            // 重新選擇當前異常狀態以更新異常狀態詳細列表
                            setTimeout(function() {
                                var currentAbnormalState = $scope.abnormalStates.find(function(s) { 
                                    return s.Id === $scope.selectedAbnormalState.Id; 
                                });
                                if (currentAbnormalState) {
                                    $scope.selectAbnormalState(currentAbnormalState);
                                    $scope.$apply();
                                }
                            }, 500);
                        } else {
                            swal({
                                title: "刪除失敗",
                                text: response.data.msg || "刪除失敗，請稍後再試",
                                icon: "error"
                            });
                        }
                    })
                    .catch(function (error) {
                        console.error('刪除異常狀態詳細失敗:', error);
                        swal({
                            title: "錯誤",
                            text: "刪除失敗，請稍後再試",
                            icon: "error"
                        });
                    });
            }
        });
    };

    // Modal 事件處理
    $('#abnormalStateModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.abnormalStateForm = {};
            $scope.saving = false;
        });
    });

    $('#abnormalStateModal').on('shown.bs.modal', function () {
        // Modal 顯示時聚焦到名稱輸入框
        $('#abnormalStateName').focus();
    });

    $('#abnormalStateDetailModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.abnormalStateDetailForm = {};
            $scope.saving = false;
        });
    });

    $('#abnormalStateDetailModal').on('shown.bs.modal', function () {
        // Modal 顯示時聚焦到名稱輸入框
        $('#abnormalStateDetailName').focus();
    });

    // 初始化載入
    $scope.loadAbnormalStates();
});
