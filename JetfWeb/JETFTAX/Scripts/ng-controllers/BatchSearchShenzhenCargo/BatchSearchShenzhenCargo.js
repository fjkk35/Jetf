mainApp.controller('BatchSearchShenzhenCargoController', ['$scope', '$http', '$window', function ($scope, $http, $window) {

    $scope.init = function () {
        $scope.queryData = {
            trackingNoList: ''
        };
        $scope.searchResults = [];
        $scope.isLoading = false;
    };

    $scope.search = function () {
        if (!$scope.queryData.trackingNoList || $scope.queryData.trackingNoList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入分提單號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;
        $scope.searchResults = [];

        $http.post(Router.action('BatchSearchShenzhenCargo', 'Query'), $scope.queryData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status === 'success') {
                    $scope.searchResults = response.data.ReturnObject || [];
                    if ($scope.searchResults.length === 0) {
                        swal({
                            title: "提示",
                            text: "查無資料",
                            icon: "info"
                        });
                    }
                } else {
                    swal({
                        title: "查詢失敗",
                        text: response.data.msg || "查詢失敗",
                        icon: "error"
                    });
                }
            }, function (error) {
                $scope.isLoading = false;
                swal({
                    title: "錯誤",
                    text: "查詢發生錯誤",
                    icon: "error"
                });
            });
    };

    $scope.exportExcel = function () {
        if (!$scope.queryData.trackingNoList || $scope.queryData.trackingNoList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入分提單號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        $http.post(Router.action('BatchSearchShenzhenCargo', 'ExportExcel'), $scope.queryData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.fileGuid) {
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

    $scope.clear = function () {
        $scope.queryData.trackingNoList = '';
        $scope.searchResults = [];
    };

    $scope.init();
}]);
