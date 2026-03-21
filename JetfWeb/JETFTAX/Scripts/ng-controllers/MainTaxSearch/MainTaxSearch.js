mainApp.controller('MainTaxSearchController', ['$scope', '$http', '$window', function ($scope, $http, $window) {

    // 初始化
    $scope.init = function () {
        $scope.queryData = {
            mainNumberList: ''
        };
        $scope.isLoading = false;
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        if (!$scope.queryData.mainNumberList || $scope.queryData.mainNumberList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入主號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        $http.post(Router.action('MainTaxSearch', 'ExportExcel'), $scope.queryData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.fileGuid) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') +
                        '?fileGuid=' + response.data.fileGuid +
                        '&fileName=' + encodeURIComponent(response.data.fileName);
                    $window.location.href = downloadUrl;
                } else {
                    swal({
                        title: "匯出失敗",
                        text: response.data.msg || "匯出失敗",
                        icon: "error"
                    });
                }
            }, function (error) {
                $scope.isLoading = false;
                swal({
                    title: "錯誤",
                    text: "匯出發生錯誤",
                    icon: "error"
                });
                console.error(error);
            });
    };

    // 清除
    $scope.clear = function () {
        $scope.queryData.mainNumberList = '';
    };

    // 初始化
    $scope.init();
}]);
