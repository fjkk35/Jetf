
mainApp.controller('AuthorityGroupIndexController', function ($scope, $http) {
    // 初始化變數
    $scope.groups = [];
    $scope.authorities = [];
    $scope.groupedAuthorities = {};
    $scope.currentGroup = {};
    $scope.selectedAuthorities = {};
    $scope.modalTitle = '';
    $scope.isEdit = false;
    $scope.saving = false;
    $scope.expandedGroups = {}; // 記錄哪些群組的權限被展開

    // 載入群組列表
    $scope.loadGroups = function () {
        $http.get(Router.action('AuthorityGroup', 'GetGroups'))
            .then(function (response) {
                $scope.groups = response.data;
                console.log('群組列表載入成功:', $scope.groups);
                // 處理每個群組的權限分類
                $scope.processGroupAuthorities();
            })
            .catch(function (error) {
                console.error('載入群組列表失敗:', error);
                alert('載入群組列表失敗');
            });
    };

    // 載入權限列表
    $scope.loadAuthorities = function () {
       
        $http.get(Router.action('AuthorityGroup', 'GetAuthorities'))
            .then(function (response) {
                $scope.authorities = response.data;
                console.log('權限列表載入成功:', $scope.authorities);
                $scope.groupAuthoritiesByPartnerId();
            })
            .catch(function (error) {
                console.error('載入權限列表失敗:', error);
                alert('載入權限列表失敗');
            });
    };

    // 依 PartnerId 分組權限並按 PartnerSort 排序
    $scope.groupAuthoritiesByPartnerId = function () {
        // 先按照 PartnerSort 對所有權限進行排序
        var sortedAuthorities = $scope.authorities.sort(function (a, b) {
            // 首先按 PartnerSort 排序
            var partnerSortDiff = (a.PartnerSort || 0) - (b.PartnerSort || 0);
            if (partnerSortDiff !== 0) {
                return partnerSortDiff;
            }
            // 如果 PartnerSort 相同，則按 Sort 排序
            return (a.Sort || 0) - (b.Sort || 0);
        });

        // 建立有序的分組物件
        $scope.groupedAuthorities = {};
        var orderedKeys = []; // 記錄分類的順序

        angular.forEach(sortedAuthorities, function (auth) {
            var partnerName = auth.PartnerName || '其他';
            
            // 如果這個分類還沒有出現過，記錄其順序
            if (!$scope.groupedAuthorities[partnerName]) {
                $scope.groupedAuthorities[partnerName] = [];
                orderedKeys.push(partnerName);
            }
            $scope.groupedAuthorities[partnerName].push(auth);
        });

        // 確保每個分類內的權限按 Sort 排序
        angular.forEach($scope.groupedAuthorities, function (auths, partnerName) {
            auths.sort(function (a, b) {
                return (a.Sort || 0) - (b.Sort || 0);
            });
        });

        // 保存分類的排序順序供前端使用
        $scope.orderedPartnerNames = orderedKeys;

        console.log('權限分組完成:', $scope.groupedAuthorities);
        console.log('分類排序:', $scope.orderedPartnerNames);
    };

    // 處理每個群組的權限，將其按分類分組並按 PartnerSort 排序
    $scope.processGroupAuthorities = function () {
        angular.forEach($scope.groups, function (group) {
            if (group.Authorities && group.Authorities.length > 0) {
                // 先按照 PartnerSort 對群組權限進行排序
                var sortedGroupAuthorities = group.Authorities.sort(function (a, b) {
                    // 首先按 PartnerSort 排序
                    var partnerSortDiff = (a.PartnerSort || 0) - (b.PartnerSort || 0);
                    if (partnerSortDiff !== 0) {
                        return partnerSortDiff;
                    }
                    // 如果 PartnerSort 相同，則按 Sort 排序
                    return (a.Sort || 0) - (b.Sort || 0);
                });

                // 建立有序的群組分組物件
                group.groupedAuthorities = {};
                group.orderedPartnerNames = []; // 記錄該群組分類的順序

                angular.forEach(sortedGroupAuthorities, function (auth) {
                    var partnerName = auth.PartnerName || '其他';
                    
                    // 如果這個分類還沒有出現過，記錄其順序
                    if (!group.groupedAuthorities[partnerName]) {
                        group.groupedAuthorities[partnerName] = [];
                        group.orderedPartnerNames.push(partnerName);
                    }
                    group.groupedAuthorities[partnerName].push(auth);
                });

                // 確保每個分類內的權限按 Sort 排序
                angular.forEach(group.groupedAuthorities, function (auths, partnerName) {
                    auths.sort(function (a, b) {
                        return (a.Sort || 0) - (b.Sort || 0);
                    });
                });
            } else {
                group.groupedAuthorities = {};
                group.orderedPartnerNames = [];
            }
        });
        console.log('群組權限分類處理完成:', $scope.groups);
    };

    // 取得有序的分類名稱（用於 ng-repeat）
    $scope.getOrderedPartnerNames = function (groupedAuthorities, orderedPartnerNames) {
        if (!orderedPartnerNames || !groupedAuthorities) return [];
        
        return orderedPartnerNames.filter(function (partnerName) {
            return groupedAuthorities[partnerName] && groupedAuthorities[partnerName].length > 0;
        });
    };

    // 切換權限顯示
    $scope.toggleGroupAuthorities = function (groupId) {
        $scope.expandedGroups[groupId] = !$scope.expandedGroups[groupId];
    };

    // 檢查群組權限是否展開
    $scope.isGroupExpanded = function (groupId) {
        return $scope.expandedGroups[groupId] || false;
    };

    // 顯示新增 Modal
    $scope.showAddModal = function () {
        console.log('顯示新增 Modal');
        $scope.modalTitle = '新增權限群組';
        $scope.isEdit = false;
        $scope.currentGroup = {
            GroupName: '',
            Memo: ''
        };
        $scope.selectedAuthorities = {};
        $('#groupModal').modal('show');
    };

    // 顯示修改 Modal
    $scope.showEditModal = function (groupId) {
        console.log('顯示修改 Modal, ID:', groupId);
        $scope.modalTitle = '修改權限群組';
        $scope.isEdit = true;
        
        $http.get(Router.action('AuthorityGroup', 'GetGroup') +'?id=' + groupId)
            .then(function (response) {
                var group = response.data;
                console.log('載入群組資料:', group);
                if (group) {
                    $scope.currentGroup = {
                        Id: group.Id,
                        GroupName: group.GroupName,
                        Memo: group.Memo
                    };

                    // 設定已選權限
                    $scope.selectedAuthorities = {};
                    if (group.AuthorityIds) {
                        angular.forEach(group.AuthorityIds, function (authId) {
                            $scope.selectedAuthorities[authId] = true;
                        });
                    }

                    $('#groupModal').modal('show');
                } else {
                    alert('找不到該群組資料');
                }
            })
            .catch(function (error) {
                console.error('載入群組資料失敗:', error);
                alert('載入群組資料失敗');
            });
    };

    // 儲存群組
    $scope.saveGroup = function () {
        if (!$scope.currentGroup.GroupName) {
            alert('請輸入群組名稱');
            return;
        }

        $scope.saving = true;

        // 收集選中的權限 ID
        var authorityIds = [];
        angular.forEach($scope.selectedAuthorities, function (selected, authId) {
            if (selected) {
                authorityIds.push(authId);
            }
        });

        var data = {
            Id: $scope.isEdit ? $scope.currentGroup.Id : null,
            GroupName: $scope.currentGroup.GroupName,
            Memo: $scope.currentGroup.Memo || '',
            AuthorityIds: authorityIds
        };
        
        var url = $scope.isEdit ? Router.action('AuthorityGroup', 'Update') : Router.action('AuthorityGroup', 'Create');

        console.log('儲存群組資料:', data, 'URL:', url);

        $http.post(url, data)
            .then(function (response) {
                var result = response.data;
                console.log('儲存回應:', result);
                if (result.status === 'success') {
                    alert($scope.isEdit ? '修改成功' : '新增成功');
                    $('#groupModal').modal('hide');
                    $scope.loadGroups();
                } else {
                    alert(result.msg || '操作失敗');
                }
            })
            .catch(function (error) {
                console.error('儲存失敗:', error);
                alert('儲存失敗');
            })
            .finally(function () {
                $scope.saving = false;
            });
    };

    // 刪除群組
    $scope.deleteGroup = function (groupId, groupName) {
        if (!confirm('確定要刪除群組「' + groupName + '」嗎？\n此操作無法復原！')) {
            return;
        }

        $http.post(Router.action('AuthorityGroup', 'Delete'), { id: groupId })
            .then(function (response) {
                var result = response.data;
                console.log('刪除回應:', result);
                if (result.status === 'success') {
                    alert('刪除成功');
                    $scope.loadGroups();
                } else {
                    alert(result.msg || '刪除失敗');
                }
            })
            .catch(function (error) {
                console.error('刪除失敗:', error);
                alert('刪除失敗');
            });
    };

    // 初始化載入
    console.log('AuthorityGroupController 初始化');
    $scope.loadGroups();
    $scope.loadAuthorities();
});