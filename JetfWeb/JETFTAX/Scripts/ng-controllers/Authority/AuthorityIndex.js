mainApp.controller('AuthorityIndexController', function ($scope, $http) {
    // 初始化變數
    $scope.authorities = [];
    $scope.groupedAuthorities = {};
    $scope.partnerOptions = [];
    $scope.currentAuthority = {};
    $scope.modalTitle = '';
    $scope.isEdit = false;
    $scope.saving = false;

    // 載入權限清單
    $scope.loadAuthorities = function () {
        console.log('開始載入權限清單...');
        $http.get(Router.action('Authority', 'GetAuthorities'))
            .then(function (response) {
                $scope.authorities = response.data;
                console.log('權限清單載入成功:', $scope.authorities);
                $scope.groupAuthoritiesByPartner();
            })
            .catch(function (error) {
                console.error('載入權限清單失敗:', error);
                alert('載入權限清單失敗');
            });
    };

    // 載入權限分類選項
    $scope.loadPartnerOptions = function () {
        console.log('開始載入權限分類選項...');
        
        $http.get(Router.action('Authority', 'GetPartnerOptions'))
            .then(function (response) {
                $scope.partnerOptions = response.data;
                console.log('權限分類選項載入成功:', $scope.partnerOptions);
            })
            .catch(function (error) {
                console.error('載入權限分類選項失敗:', error);
                alert('載入權限分類選項失敗');
            });
    };

    // 依 PartnerName 分組權限
    $scope.groupAuthoritiesByPartner = function () {
        $scope.groupedAuthorities = {};
        angular.forEach($scope.authorities, function (auth) {
            var partnerName = auth.PartnerName || '其他';
            if (!$scope.groupedAuthorities[partnerName]) {
                $scope.groupedAuthorities[partnerName] = [];
            }
            $scope.groupedAuthorities[partnerName].push(auth);
        });

        // 依 Sort 排序
        angular.forEach($scope.groupedAuthorities, function (auths, partnerName) {
            auths.sort(function (a, b) {
                return (a.Sort || 0) - (b.Sort || 0);
            });
        });

        console.log('權限分組完成:', $scope.groupedAuthorities);
    };

    // 顯示新增 Modal
    $scope.showAddModal = function () {
        console.log('顯示新增 Modal');
        $scope.modalTitle = '新增權限';
        $scope.isEdit = false;
        $scope.currentAuthority = {
            Id: '',
            Text: '',
            PartnerId: '',
            Sort: 0
        };
        $('#authorityModal').modal('show');
    };

    // 顯示修改 Modal
    $scope.showEditModal = function (authorityId) {
        console.log('顯示修改 Modal, ID:', authorityId);
        $scope.modalTitle = '修改權限';
        $scope.isEdit = true;
        
        $http.get(Router.action('Authority', 'GetAuthority')+'?id=' + authorityId)
            .then(function (response) {
                var authority = response.data;
                console.log('載入權限資料:', authority);
                if (authority) {
                    $scope.currentAuthority = {
                        Id: authority.Id,
                        Text: authority.Text,
                        PartnerId: authority.PartnerId,
                        Sort: authority.Sort
                    };

                    $('#authorityModal').modal('show');
                } else {
                    alert('找不到該權限資料');
                }
            })
            .catch(function (error) {
                console.error('載入權限資料失敗:', error);
                alert('載入權限資料失敗');
            });
    };

    // 儲存權限
    $scope.saveAuthority = function () {
        if (!$scope.currentAuthority.Id) {
            alert('請輸入權限ID');
            return;
        }

        if (!$scope.currentAuthority.Text) {
            alert('請輸入權限說明');
            return;
        }

        if (!$scope.currentAuthority.PartnerId) {
            alert('請選擇權限分類');
            return;
        }

        $scope.saving = true;

        var data = {
            Id: $scope.currentAuthority.Id,
            Text: $scope.currentAuthority.Text,
            PartnerId: $scope.currentAuthority.PartnerId,
            Sort: $scope.currentAuthority.Sort || 0
        };

        var url = $scope.isEdit ? Router.action('Authority', 'Update') : Router.action('Authority', 'Create');

        console.log('儲存權限資料:', data, 'URL:', url);

        $http.post(url, data)
            .then(function (response) {
                var result = response.data;
                console.log('儲存回應:', result);
                if (result.status === 'success') {
                    alert($scope.isEdit ? '修改成功' : '新增成功');
                    $('#authorityModal').modal('hide');
                    $scope.loadAuthorities();
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

    // 取得權限分類名稱
    $scope.getPartnerName = function (partnerId) {
        if (!partnerId || !$scope.partnerOptions) return '';

        var partner = $scope.partnerOptions.find(function (p) {
            return p.Value === partnerId;
        });

        return partner ? partner.Text : partnerId;
    };

    // 初始化載入
    console.log('AuthorityController 初始化');
    $scope.loadAuthorities();
    $scope.loadPartnerOptions();
});