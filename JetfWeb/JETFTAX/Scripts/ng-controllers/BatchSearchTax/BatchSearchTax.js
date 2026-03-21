mainApp.controller('BatchSearchTaxController', ['$scope', '$http', '$window', function ($scope, $http, $window) {

    // 初始化
    $scope.init = function () {
        $scope.queryData = {
            dlvInvList: ''
        };
        $scope.isLoading = false;
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        if (!$scope.queryData.dlvInvList || $scope.queryData.dlvInvList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入物流貨號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        $http.post(Router.action('BatchSearchTax', 'ExportExcel'), $scope.queryData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.fileGuid) {
                    // 下載檔案
                    var downloadUrl = Router.action('Download', 'DownloadFile') + 
                        '?fileGuid=' + response.data.fileGuid + 
                        '&fileName=' + encodeURIComponent(response.data.fileName);
                    $window.location.href = downloadUrl;

                    swal({
                        title: "成功",
                        text: "匯出成功",
                        icon: "success"
                    });
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
        $scope.queryData.dlvInvList = '';
    };

    // 初始化
    $scope.init();
}]);
