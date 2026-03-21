// Controller
mainApp.controller('StepManagementController', function ($scope, $http) {
    // 初始化資料
    $scope.steps = [];
    $scope.stepDetails = [];
    $scope.selectedStep = null;
    $scope.loadingSteps = true;
    $scope.loadingStepDetails = false;
    $scope.saving = false;
    
    // Modal 模式和表單資料
    $scope.stepModalMode = 'add'; // 'add' 或 'edit'
    $scope.stepDetailModalMode = 'add'; // 'add' 或 'edit'
    $scope.stepForm = {};
    $scope.stepDetailForm = {};

    // 步驟拖曳排序設定 - 參考 ApprovalCategory
    $scope.stepSortableOptions = {
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
                    $scope.updateStepSorts();
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

    // 步驟詳細拖曳排序設定 - 參考 ApprovalCategory
    $scope.stepDetailSortableOptions = {
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
                    $scope.updateStepDetailSorts();
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

    // 載入所有步驟
    $scope.loadSteps = function () {
        $scope.loadingSteps = true;
        $http.get(Router.action('Step', 'GetAllStepsWithDetails'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.steps = response.data;
                } else if (response.data && response.data.msg) {
                    $scope.steps = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.steps = [];
                }
            })
            .catch(function (error) {
                console.error('載入步驟失敗:', error);
                $scope.steps = [];
                swal({
                    title: "錯誤",
                    text: "載入步驟資料失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loadingSteps = false;
            });
    };

    // 更新步驟排序 - 參考 ApprovalCategory 的邏輯
    $scope.updateStepSorts = function() {
        var sortUpdates = [];
        
        // 重新計算排序號碼
        for (var i = 0; i < $scope.steps.length; i++) {
            var step = $scope.steps[i];
            var newSort = i + 1;
            
            // 只有排序改變的才需要更新
            if (step.Sort !== newSort) {
                step.Sort = newSort;
                sortUpdates.push({
                    Id: step.Id,
                    Sort: newSort
                });
            }
        }

        // 如果有需要更新的排序
        if (sortUpdates.length > 0) {
            console.log('更新步驟排序:', sortUpdates);
            
            $http.post(Router.action('Step', 'UpdateStepSorts'), sortUpdates)
                .then(function (response) {
                    if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                        console.log('步驟排序更新成功');
                        
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
                        $scope.loadSteps();
                    }
                })
                .catch(function (error) {
                    console.error('更新步驟排序失敗:', error);
                    swal({
                        title: "錯誤",
                        text: "排序更新失敗",
                        icon: "error"
                    });
                    // 重新載入以恢復原始排序
                    $scope.loadSteps();
                });
        }
    };

    // 更新步驟詳細排序 - 參考 ApprovalCategory 的邏輯
    $scope.updateStepDetailSorts = function() {
        var sortUpdates = [];
        
        // 重新計算排序號碼
        for (var i = 0; i < $scope.stepDetails.length; i++) {
            var detail = $scope.stepDetails[i];
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
            console.log('更新步驟詳細排序:', sortUpdates);
            
            $http.post(Router.action('Step', 'UpdateStepDetailSorts'), sortUpdates)
                .then(function (response) {
                    if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                        console.log('步驟詳細排序更新成功');
                        
                        // 簡短提示
                        if (typeof toastr !== 'undefined') {
                            toastr.success('排序更新成功', '', { timeOut: 2000 });
                        }
                        
                        // 同步更新選中步驟的步驟詳細
                        if ($scope.selectedStep) {
                            $scope.selectedStep.StepDetails = angular.copy($scope.stepDetails);
                            
                            // 同步更新主步驟陣列中對應步驟的詳細
                            var stepIndex = $scope.steps.findIndex(function(s) { 
                                return s.Id === $scope.selectedStep.Id; 
                            });
                            if (stepIndex >= 0) {
                                $scope.steps[stepIndex].StepDetails = angular.copy($scope.stepDetails);
                            }
                        }
                    } else {
                        swal({
                            title: "排序更新失敗",
                            text: response.data.msg || "排序更新失敗",
                            icon: "error"
                        });
                        // 重新載入以恢復原始排序
                        $scope.selectStep($scope.selectedStep);
                    }
                })
                .catch(function (error) {
                    console.error('更新步驟詳細排序失敗:', error);
                    swal({
                        title: "錯誤",
                        text: "排序更新失敗",
                        icon: "error"
                    });
                    // 重新載入以恢復原始排序
                    $scope.selectStep($scope.selectedStep);
                });
        }
    };

    // 選擇步驟
    $scope.selectStep = function (step) {
        $scope.selectedStep = step;
        $scope.stepDetails = step.StepDetails || [];
        console.log('選擇步驟:', step.StepName, '詳細數量:', $scope.stepDetails.length);
    };

    // 顯示步驟 Modal
    $scope.showStepModal = function (mode, step) {
        $scope.stepModalMode = mode;
        
        if (mode === 'add') {
            $scope.stepForm = {
                StepName: '',
                IsMultiple: false
            };
        } else {
            $scope.stepForm = {
                Id: step.Id,
                StepName: step.StepName,
                IsMultiple: step.IsMultiple || false,
                Sort: step.Sort
            };
        }
        
        $('#stepModal').modal('show');
    };

    // 儲存步驟
    $scope.saveStep = function () {
        if (!$scope.stepForm.StepName || $scope.stepForm.StepName.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入步驟名稱",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;
        
        var action = $scope.stepModalMode === 'add' ? 'CreateStep' : 'UpdateStep';
        var successMessage = $scope.stepModalMode === 'add' ? '新增成功' : '更新成功';

        $http.post(Router.action('Step', action), $scope.stepForm)
            .then(function (response) {
                if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                    swal({
                        title: "成功",
                        text: successMessage,
                        icon: "success",
                        timer: 2000
                    });
                    
                    $('#stepModal').modal('hide');
                    $scope.loadSteps();
                    
                    // 如果是新增，選擇新建立的步驟
                    if ($scope.stepModalMode === 'add' && response.data.ReturnObject) {
                        setTimeout(function() {
                            var newStep = $scope.steps.find(function(s) { 
                                return s.Id === response.data.ReturnObject; 
                            });
                            if (newStep) {
                                $scope.selectStep(newStep);
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
                console.error('儲存步驟失敗:', error);
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

    // 刪除步驟
    $scope.deleteStep = function (step) {
        swal({
            title: "確認刪除",
            text: `確定要刪除步驟「${step.StepName}」嗎？\n※ 如果有步驟詳細，需要先刪除步驟詳細`,
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
                $http.post(Router.action('Step', 'DeleteStep'), { id: step.Id })
                    .then(function (response) {
                        if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                            swal({
                                title: "刪除成功",
                                text: "步驟刪除完成",
                                icon: "success",
                                timer: 2000
                            });
                            
                            // 如果刪除的是當前選中的步驟，清除選擇
                            if ($scope.selectedStep && $scope.selectedStep.Id === step.Id) {
                                $scope.selectedStep = null;
                                $scope.stepDetails = [];
                            }
                            
                            $scope.loadSteps();
                        } else {
                            swal({
                                title: "刪除失敗",
                                text: response.data.msg || "刪除失敗，請稍後再試",
                                icon: "error"
                            });
                        }
                    })
                    .catch(function (error) {
                        console.error('刪除步驟失敗:', error);
                        swal({
                            title: "錯誤",
                            text: "刪除失敗，請稍後再試",
                            icon: "error"
                        });
                    });
            }
        });
    };

    // 顯示步驟詳細 Modal
    $scope.showStepDetailModal = function (mode, stepDetail) {
        if (!$scope.selectedStep) {
            swal({
                title: "提示",
                text: "請先選擇步驟",
                icon: "warning"
            });
            return;
        }

        $scope.stepDetailModalMode = mode;
        
        if (mode === 'add') {
            $scope.stepDetailForm = {
                StepId: $scope.selectedStep.Id,
                StepDetailName: ''
            };
        } else {
            $scope.stepDetailForm = {
                Id: stepDetail.Id,
                StepId: stepDetail.StepId,
                StepDetailName: stepDetail.StepDetailName,
                Sort: stepDetail.Sort
            };
        }
        
        $('#stepDetailModal').modal('show');
    };

    // 儲存步驟詳細
    $scope.saveStepDetail = function () {
        if (!$scope.stepDetailForm.StepDetailName || $scope.stepDetailForm.StepDetailName.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入步驟詳細名稱",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;
        
        var action = $scope.stepDetailModalMode === 'add' ? 'CreateStepDetail' : 'UpdateStepDetail';
        var successMessage = $scope.stepDetailModalMode === 'add' ? '新增成功' : '更新成功';

        $http.post(Router.action('Step', action), $scope.stepDetailForm)
            .then(function (response) {
                if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                    swal({
                        title: "成功",
                        text: successMessage,
                        icon: "success",
                        timer: 2000
                    });
                    
                    $('#stepDetailModal').modal('hide');
                    
                    // 重新載入步驟列表以更新計數
                    $scope.loadSteps();
                    
                    // 重新選擇當前步驟以更新步驟詳細列表
                    setTimeout(function() {
                        var currentStep = $scope.steps.find(function(s) { 
                            return s.Id === $scope.selectedStep.Id; 
                        });
                        if (currentStep) {
                            $scope.selectStep(currentStep);
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
                console.error('儲存步驟詳細失敗:', error);
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

    // 刪除步驟詳細
    $scope.deleteStepDetail = function (stepDetail) {
        swal({
            title: "確認刪除",
            text: `確定要刪除步驟詳細「${stepDetail.StepDetailName}」嗎？\n※ 此操作無法復原`,
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
                $http.post(Router.action('Step', 'DeleteStepDetail'), { id: stepDetail.Id })
                    .then(function (response) {
                        if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                            swal({
                                title: "刪除成功",
                                text: "步驟詳細刪除完成",
                                icon: "success",
                                timer: 2000
                            });
                            
                            // 重新載入步驟列表以更新計數
                            $scope.loadSteps();
                            
                            // 重新選擇當前步驟以更新步驟詳細列表
                            setTimeout(function() {
                                var currentStep = $scope.steps.find(function(s) { 
                                    return s.Id === $scope.selectedStep.Id; 
                                });
                                if (currentStep) {
                                    $scope.selectStep(currentStep);
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
                        console.error('刪除步驟詳細失敗:', error);
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
    $('#stepModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.stepForm = {};
            $scope.saving = false;
        });
    });

    $('#stepModal').on('shown.bs.modal', function () {
        // Modal 顯示時聚焦到名稱輸入框
        $('#stepName').focus();
    });

    $('#stepDetailModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.stepDetailForm = {};
            $scope.saving = false;
        });
    });

    $('#stepDetailModal').on('shown.bs.modal', function () {
        // Modal 顯示時聚焦到名稱輸入框
        $('#stepDetailName').focus();
    });

    // 初始化載入
    $scope.loadSteps();
});