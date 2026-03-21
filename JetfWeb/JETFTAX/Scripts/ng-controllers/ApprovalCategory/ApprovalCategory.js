// Controller
mainApp.controller('ApprovalCategoryController', function ($scope, $http) {
    // 初始化資料
    $scope.categories = [];
    $scope.currentCategory = {};
    $scope.modalTitle = '';
    $scope.isEditMode = false;
    $scope.loading = true;
    $scope.saving = false;

    // 拖曳排序設定 - 適應 table 結構
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
        },
        stop: function(e, ui) {
            // 拖曳結束時的效果
            ui.item.removeClass('dragging');
        }
    };

    // 載入所有簽審類別
    $scope.loadCategories = function () {
        $scope.loading = true;
        $http.get(Router.action('ApprovalCategory', 'GetAll'))
            .then(function (response) {
                if (response.data && Array.isArray(response.data)) {
                    $scope.categories = response.data;
                } else if (response.data && response.data.msg) {
                    $scope.categories = [];
                    swal({
                        title: "載入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                } else {
                    $scope.categories = [];
                }
            })
            .catch(function (error) {
                console.error('載入簽審類別失敗:', error);
                $scope.categories = [];
                swal({
                    title: "錯誤",
                    text: "載入簽審類別失敗，請稍後再試",
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
        $scope.modalTitle = '新增簽審類別';
        $scope.currentCategory = {
            CategoryName: '',
            Sort: $scope.getNextSortNumber()
        };
        
        // 重置表單驗證狀態
        if ($scope.categoryForm) {
            $scope.categoryForm.$setPristine();
            $scope.categoryForm.$setUntouched();
        }
        
        $('#categoryModal').modal('show');
    };

    // 顯示編輯 Modal
    $scope.showEditModal = function (category) {
        $scope.isEditMode = true;
        $scope.modalTitle = '編輯簽審類別';
        $scope.currentCategory = angular.copy(category);
        
        // 重置表單驗證狀態
        if ($scope.categoryForm) {
            $scope.categoryForm.$setPristine();
            $scope.categoryForm.$setUntouched();
        }
        
        $('#categoryModal').modal('show');
    };

    // 取得下一個排序號碼
    $scope.getNextSortNumber = function () {
        if ($scope.categories.length === 0) {
            return 1;
        }
        var maxSort = Math.max.apply(Math, $scope.categories.map(function (c) { return c.Sort || 0; }));
        return maxSort + 1;
    };

    // 儲存簽審類別
    $scope.saveCategory = function () {
        if ($scope.saving) return;

        // 客戶端驗證
        if (!$scope.currentCategory.CategoryName || !$scope.currentCategory.CategoryName.trim()) {
            swal({
                title: "驗證失敗",
                text: "請輸入類別名稱",
                icon: "warning"
            });
            return;
        }

        if (!$scope.currentCategory.Sort || $scope.currentCategory.Sort < 1) {
            swal({
                title: "驗證失敗",
                text: "請輸入有效的排序數字",
                icon: "warning"
            });
            return;
        }

        $scope.saving = true;

        var action = $scope.isEditMode ? 'Update' : 'Create';
        var url = Router.action('ApprovalCategory', action);

        $http.post(url, $scope.currentCategory)
            .then(function (response) {
                if (response.data && (response.data.status === 'success' || !response.data.msg)) {
                    $('#categoryModal').modal('hide');
                    
                    var message = $scope.isEditMode ? "簽審類別更新成功" : "簽審類別新增成功";
                    if (!$scope.isEditMode) {
                        message += "，系統已自動調整其他項目的排序";
                    }
                    
                    swal({
                        title: "成功",
                        text: message,
                        icon: "success"
                    });
                    $scope.loadCategories(); // 重新載入資料
                } else {
                    swal({
                        title: "操作失敗",
                        text: response.data.msg || "操作失敗，請稍後再試",
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('儲存簽審類別失敗:', error);
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
        for (var i = 0; i < $scope.categories.length; i++) {
            var category = $scope.categories[i];
            var newSort = i + 1;
            
            // 只有排序改變的才需要更新
            if (category.Sort !== newSort) {
                category.Sort = newSort;
                sortUpdates.push({
                    Id: category.Id,
                    CategoryName: category.CategoryName,
                    Sort: newSort
                });
            }
        }

        // 如果有需要更新的排序
        if (sortUpdates.length > 0) {
            $http.post(Router.action('ApprovalCategory', 'UpdateSorts'), sortUpdates)
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
                        $scope.loadCategories();
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
                    $scope.loadCategories();
                });
        }
    };

    // 初始化載入
    $scope.loadCategories();

    // Modal 事件處理
    $('#categoryModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.currentCategory = {};
            $scope.saving = false;
            $scope.isEditMode = false;
        });
    });

    $('#categoryModal').on('shown.bs.modal', function () {
        // Modal 顯示時聚焦到名稱輸入框
        $('#categoryName').focus();
    });
});