// Controller
mainApp.controller('AuthorizationFormController', function ($scope, $http) {
    // 初始化資料
    $scope.forms = [];
    $scope.currentForm = {};
    $scope.modalTitle = '';
    $scope.isEditMode = false;
    $scope.loading = true;
    $scope.saving = false;

    // 拖曳排序設定
    $scope.sortableOptions = {
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
                    $scope.updateSortOrder();
                });
            }, 100);
        },
        start: function(e, ui) {
            // 拖曳開始時的效果
            ui.item.addClass('dragging');
            ui.placeholder.html('<td colspan="3" style="height: 45px; border: none; background: transparent;"></td>');
        },
        stop: function(e, ui) {
            // 拖曳結束時的效果
            ui.item.removeClass('dragging');
        }
    };

    // 載入所有文件名稱
    $scope.loadForms = function () {
        $scope.loading = true;
        $http.get(Router.action('AuthorizationForm', 'GetAll'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.forms = response.data;
                } else if (response.data && response.data.msg) {
                    $scope.forms = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.forms = [];
                }
            })
            .catch(function (error) {
                console.error('載入文件名稱失敗:', error);
                $scope.forms = [];
                swal({
                    title: "錯誤",
                    text: "載入文件名稱失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 顯示新增 Modal
    $scope.showAddModal = function () {
        $scope.isEditMode = false;
        $scope.modalTitle = '新增文件名稱';
        $scope.currentForm = {
            FormName: '',
            Sort: $scope.getNextSortNumber()
        };
        
        // 重置表單驗證狀態
        if ($scope.formForm) {
            $scope.formForm.$setPristine();
            $scope.formForm.$setUntouched();
        }
        
        $('#formModal').modal('show');
    };

    // 顯示編輯 Modal
    $scope.showEditModal = function (form) {
        $scope.isEditMode = true;
        $scope.modalTitle = '編輯文件名稱';
        $scope.currentForm = angular.copy(form);
        
        // 重置表單驗證狀態
        if ($scope.formForm) {
            $scope.formForm.$setPristine();
            $scope.formForm.$setUntouched();
        }
        
        $('#formModal').modal('show');
    };

    // 取得下一個排序號碼
    $scope.getNextSortNumber = function () {
        if ($scope.forms.length === 0) {
            return 1;
        }
        var maxSort = Math.max.apply(Math, $scope.forms.map(function (c) { return c.Sort || 0; }));
        return maxSort + 1;
    };

    // 儲存文件名稱
    $scope.saveForm = function () {
        if ($scope.saving) return;

        // 客戶端驗證
        if (!$scope.currentForm.FormName || !$scope.currentForm.FormName.trim()) {
            swal({
                title: "驗證失敗",
                text: "請輸入文件名稱",
                icon: "warning"
            });
            return;
        }

        if (!$scope.currentForm.Sort || $scope.currentForm.Sort < 1) {
            swal({
                title: "驗證失敗",
                text: "請輸入有效的排序數字",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;

        var action = $scope.isEditMode ? 'Update' : 'Create';
        var url = Router.action('AuthorizationForm', action);

        $http.post(url, $scope.currentForm)
            .then(function (response) {
                if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                    $('#formModal').modal('hide');
                    
                    var message = $scope.isEditMode ? "文件名稱更新成功" : "文件名稱新增成功";
                    if (!$scope.isEditMode) {
                        message += "，系統已自動調整其他項目的排序";
                    }
                    
                    swal({
                        title: "成功",
                        text: message,
                        icon: "success"
                    });
                    $scope.loadForms(); // 重新載入資料
                } else {
                    swal({
                        title: "操作失敗",
                        text: response.data.msg || "操作失敗，請稍後再試",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('儲存文件名稱失敗:', error);
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

    // 更新排序
    $scope.updateSortOrder = function () {
        var sortUpdates = [];
        
        // 重新計算排序號碼
        for (var i = 0; i < $scope.forms.length; i++) {
            var form = $scope.forms[i];
            var newSort = i + 1;
            
            // 只有排序改變的才需要更新
            if (form.Sort !== newSort) {
                form.Sort = newSort;
                sortUpdates.push({
                    Id: form.Id,
                    FormName: form.FormName,
                    Sort: newSort
                });
            }
        }

        // 如果有需要更新的排序
        if (sortUpdates.length > 0) {
            $http.post(Router.action('AuthorizationForm', 'UpdateSorts'), sortUpdates)
                .then(function (response) {
                    if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                        // 顯示簡短的成功提示
                        console.log('排序更新成功');
                        
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
                        $scope.loadForms();
                    }
                })
                .catch(function (error) {
                    console.error('更新排序失敗:', error);
                    swal({
                        title: "錯誤",
                        text: "排序更新失敗",
                        icon: "error"
                    });
                    // 重新載入以恢復原始排序
                    $scope.loadForms();
                });
        }
    };

    // 初始化載入
    $scope.loadForms();

    // Modal 事件處理
    $('#formModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.currentForm = {};
            $scope.saving = false;
            $scope.isEditMode = false;
        });
    });

    $('#formModal').on('shown.bs.modal', function () {
        // Modal 顯示時聚焦到名稱輸入框
        $('#formName').focus();
    });
});