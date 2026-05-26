mainApp.controller('BatchSearchCargo2Controller', ['$scope', '$http', '$window', function ($scope, $http, $window) {
    function setActiveMenu() {
        angular.element('#collapseSearch').addClass('show');
        angular.element('#BatchSearchCargo2').addClass('active');
    }
    function openLoginPage() {
        $window.location.href = Router.action('Account', 'Login');
    }
    function hasTrackingNoList() {
        return !!($scope.request.trackingNoList && $scope.request.trackingNoList.trim());
    }
    function buildDownloadUrl(fileGuid, fileName) {
        return Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(fileGuid)
            + '&fileName=' + encodeURIComponent(fileName);
    }
    $scope.request = {
        trackingNoList: ''
    };
    $scope.isLoading = false;
    setActiveMenu();
    $scope.clear = function () {
        $scope.request.trackingNoList = '';
    };
    $scope.exportExcel = function () {
        if (!hasTrackingNoList()) {
            swal({
                title: '錯誤',
                text: '請輸入分提單號',
                icon: 'error'
            });
            return;
        }
        $scope.isLoading = true;
        $http.post(Router.action('BatchSearchCargo2', 'ExportExcel'), $scope.request)
            .then(function (response) {
            var result = response.data || {};
            if (result.Redirect) {
                openLoginPage();
                return;
            }
            if (result.fileGuid && result.fileName) {
                $window.location.href = buildDownloadUrl(result.fileGuid, result.fileName);
                swal({
                    title: '成功',
                    text: '匯出成功',
                    icon: 'success'
                });
                return;
            }
            swal({
                title: '匯出失敗',
                text: result.msg || '匯出失敗',
                icon: 'error'
            });
        })
            .catch(function () {
            swal({
                title: '錯誤',
                text: '匯出發生錯誤',
                icon: 'error'
            });
        })
            .finally(function () {
            $scope.isLoading = false;
        });
    };
}]);