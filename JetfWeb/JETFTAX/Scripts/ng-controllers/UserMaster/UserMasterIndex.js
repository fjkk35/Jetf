mainApp.controller('UserMasterIndexController', function ($scope, $http) {
    // 初始化變數
    $scope.users = [];
    $scope.authorityGroupOptions = [];
    $scope.currentUser = {};
    $scope.modalTitle = '';
    $scope.isEdit = false;
    $scope.saving = false;
    $scope.query = {
        userId: '',
        userName: '',
        userStatus: 'all',
        authorityGroupId: 'all'
    };

    // 狀態選項
    $scope.statusOptions = [
        { Value: '1', Text: '啟用' },
        { Value: '0', Text: '停用' }
    ];

    // 載入會員清單
    $scope.loadUsers = function () {
        console.log('開始載入會員清單...');
        var params = {};

        if ($scope.query.userId) {
            params.userId = $scope.query.userId;
        }

        if ($scope.query.userName) {
            params.userName = $scope.query.userName;
        }

        if ($scope.query.userStatus !== 'all' && $scope.query.userStatus !== null && $scope.query.userStatus !== undefined) {
            params.userStatus = $scope.query.userStatus;
        }

        if ($scope.query.authorityGroupId !== 'all' && $scope.query.authorityGroupId !== null && $scope.query.authorityGroupId !== undefined) {
            params.authorityGroupId = $scope.query.authorityGroupId;
        }

        $http.get(Router.action('UserMaster', 'GetUsers'), {
            params: params
        })
            .then(function (response) {
                $scope.users = response.data;
                console.log('會員清單載入成功:', $scope.users);
            })
            .catch(function (error) {
                console.error('載入會員清單失敗:', error);
                alert('載入會員清單失敗');
            });
    };

    $scope.searchUsers = function () {
        $scope.loadUsers();
    };

    $scope.resetQuery = function () {
        $scope.query = {
            userId: '',
            userName: '',
            userStatus: 'all',
            authorityGroupId: 'all'
        };
        $scope.loadUsers();
    };

    // 載入權限群組選項
    $scope.loadAuthorityGroupOptions = function () {
        console.log('開始載入權限群組選項...');
        
        $http.get(Router.action('UserMaster', 'GetAuthorityGroupOptions'))
            .then(function (response) {
                $scope.authorityGroupOptions = response.data;
                console.log('權限群組選項載入成功:', $scope.authorityGroupOptions);
            })
            .catch(function (error) {
                console.error('載入權限群組選項失敗:', error);
                alert('載入權限群組選項失敗');
            });
    };

    // 顯示新增 Modal
    $scope.showAddModal = function () {
        console.log('顯示新增 Modal');
        $scope.modalTitle = '新增會員';
        $scope.isEdit = false;
        $scope.currentUser = {
            UserId: '',
            UserName: '',
            Password: '',
            UserStatus: '1', // 預設啟用
            SelectedGroups: {}
        };
        $('#userModal').modal('show');
    };

    // 顯示修改 Modal
    $scope.showEditModal = function (userId) {
        console.log('顯示修改 Modal, ID:', userId);
        $scope.modalTitle = '修改會員';
        $scope.isEdit = true;
        
        $http.get(Router.action('UserMaster', 'GetUser')+'?userId=' + userId)
            .then(function (response) {
                var user = response.data;
                console.log('載入會員資料:', user);
                if (user) {
                    $scope.currentUser = {
                        UserId: user.UserId,
                        UserName: user.UserName,
                        Password: '', // 修改時密碼留空
                        UserStatus: user.UserStatus,
                        SelectedGroups: {}
                    };

                    // 設定已選權限群組
                    if (user.AuthorityGroupIds && user.AuthorityGroupIds.length > 0) {
                        angular.forEach(user.AuthorityGroupIds, function (groupId) {
                            $scope.currentUser.SelectedGroups[groupId] = true;
                        });
                    }

                    console.log('設定後的已選群組:', $scope.currentUser.SelectedGroups);
                    $('#userModal').modal('show');
                } else {
                    alert('找不到該會員資料');
                }
            })
            .catch(function (error) {
                console.error('載入會員資料失敗:', error);
                alert('載入會員資料失敗');
            });
    };

    // 儲存會員
    $scope.saveUser = function () {
        if (!$scope.currentUser.UserId) {
            alert('請輸入會員ID');
            return;
        }

        if (!$scope.currentUser.UserName) {
            alert('請輸入會員名稱');
            return;
        }

        if (!$scope.isEdit && !$scope.currentUser.Password) {
            alert('請輸入密碼');
            return;
        }

        if (!$scope.currentUser.UserStatus) {
            alert('請選擇狀態');
            return;
        }

        $scope.saving = true;

        // 收集選中的權限群組 ID
        var authorityGroupIds = [];
        angular.forEach($scope.currentUser.SelectedGroups, function (selected, groupId) {
            if (selected) {
                authorityGroupIds.push(parseInt(groupId, 10));
            }
        });

        var data = {
            UserId: $scope.currentUser.UserId,
            UserName: $scope.currentUser.UserName,
            Password: $scope.currentUser.Password || null,
            UserStatus: $scope.currentUser.UserStatus,
            AuthorityGroupIds: authorityGroupIds,
            IsEdit: $scope.isEdit
        };

        var url = $scope.isEdit ? Router.action('UserMaster', 'Update') : Router.action('UserMaster', 'Create');

        console.log('儲存會員資料:', data, 'URL:', url);

        $http.post(url, data)
            .then(function (response) {
                var result = response.data;
                console.log('儲存回應:', result);
                if (result.status === 'success') {
                    alert($scope.isEdit ? '修改成功' : '新增成功');
                    $('#userModal').modal('hide');
                    $scope.loadUsers();
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

    // 取得權限群組名稱（向下相容舊版本，現已改用後端提供的完整群組資料）
    $scope.getAuthorityGroupName = function (authorityGroupId) {
        if (!authorityGroupId || !$scope.authorityGroupOptions) return '';
        
        var group = $scope.authorityGroupOptions.find(function (g) {
            return g.Id === authorityGroupId;
        });
        
        return group ? group.GroupName : '';
    };

    // 取得狀態文字
    $scope.getStatusText = function (userStatus) {
        var status = $scope.statusOptions.find(function (s) {
            return s.Value === userStatus;
        });
        return status ? status.Text : userStatus;
    };

    // 初始化載入
    console.log('UserMasterController 初始化');
    $scope.loadUsers();
    $scope.loadAuthorityGroupOptions();
});
