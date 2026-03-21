mainApp.controller('TactController', ['$scope', '$http', '$window', function ($scope, $http, $window) {

    // 初始化
    $scope.init = function () {
        $scope.loginData = {
            acctId: '3027',
            acctPw: '24951752'
        };
        $scope.queryData = {
            inputList: ''
        };
        // 查詢類型選項
        $scope.queryTypes = [
            { value: 'hwb', label: '分提單號' },
            { value: 'mwb', label: '主號' },
            { value: 'bag', label: '併袋號' }
        ];
        $scope.selectedQueryType = 'hwb'; // 預設為分提單號
        $scope.isLoggedIn = false;
        $scope.isLoading = false;
        $scope.results = [];
        $scope.mainResults = [];
        $scope.bagResults = []; // 併袋號查詢結果
    };

    // 登入
    $scope.login = function () {
        if (!$scope.loginData.acctId || !$scope.loginData.acctPw) {
            swal({
                title: "錯誤",
                text: "請輸入帳號和密碼",
                icon: "error"
            });
            return;
        }
        $scope.isLoading = true;
        $http.post(Router.action('Tact', 'Login'), $scope.loginData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.isLoggedIn = true;
                    swal({
                        title: "成功",
                        text: "登入成功",
                        icon: "success"
                    });
                } else {
                    swal({
                        title: "登入失敗",
                        text: response.data.msg || "登入失敗",
                        icon: "error"
                    });
                }
            }, function (error) {
                $scope.isLoading = false;
                swal({
                    title: "錯誤",
                    text: "登入發生錯誤",
                    icon: "error"
                });
                console.error(error);
            });
    };

    // 統一查詢方法（根據查詢類型）
    $scope.queryUnified = function () {
        if (!$scope.isLoggedIn) {
            swal({
                title: "錯誤",
                text: "請先登入",
                icon: "error"
            });
            return;
        }

        if ($scope.selectedQueryType === 'hwb') {
            // 分提單號查詢
            $scope.query();
        } else if ($scope.selectedQueryType === 'mwb') {
            // 主號查詢
            $scope.queryMain();
        } else if ($scope.selectedQueryType === 'bag') {
            // 併袋號查詢
            $scope.queryBag();
        }
    };

    // 查詢（分提單號）
    $scope.query = function () {
        if (!$scope.queryData.inputList || $scope.queryData.inputList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入查詢資料",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var request = {
            hwbNoList: $scope.queryData.inputList
        };

        $http.post(Router.action('Tact', 'Query'), request)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.results = response.data.ReturnObject || [];
                    $scope.mainResults = []; // 清除主號查詢結果
                    if ($scope.results.length === 0) {
                        swal({
                            title: "查詢結果",
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
                console.error(error);
            });
    };

    // 主號查詢
    $scope.queryMain = function () {
        if (!$scope.queryData.inputList || $scope.queryData.inputList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入主號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var request = {
            mwb: $scope.queryData.inputList
        };

        $http.post(Router.action('Tact', 'QueryMain'), request)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.mainResults = response.data.ReturnObject || [];
                    $scope.results = []; // 清除分提單號查詢結果
                    if ($scope.mainResults.length === 0) {
                        swal({
                            title: "查詢結果",
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
                console.error(error);
            });
    };

    // 查詢（併袋號）
    $scope.queryBag = function () {
        if (!$scope.queryData.inputList || $scope.queryData.inputList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入查詢資料",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var request = {
            bagNoList: $scope.queryData.inputList
        };

        $http.post(Router.action('Tact', 'QueryBag'), request)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.bagResults = response.data.ReturnObject || [];
                    $scope.results = []; // 清除分提單號查詢結果
                    if ($scope.bagResults.length === 0) {
                        swal({
                            title: "查詢結果",
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
                console.error(error);
            });
    };

    // 清除結果
    $scope.clearResults = function () {
        $scope.results = [];
        $scope.mainResults = [];
        $scope.bagResults = [];
        $scope.queryData.inputList = '';
    };

    // 取得所有不重複的派件公司名稱
    $scope.getDistinctTransNames = function () {
        if (!$scope.mainResults || $scope.mainResults.length === 0) {
            return [];
        }

        var transNamesSet = {};
        $scope.mainResults.forEach(function (item) {
            if (item.NotGciDetails && item.NotGciDetails.length > 0) {
                item.NotGciDetails.forEach(function (detail) {
                    if (!detail.B6F && detail.TransName) {
                        transNamesSet[detail.TransName] = true;
                    }
                });
            }
        });

        return Object.keys(transNamesSet);
    };

    // 計算派件公司的數量
    $scope.getTransNameCount = function (item, transName) {
        if (!item.NotGciDetails || item.NotGciDetails.length === 0) {
            return 0;
        }

        var trackingnoCount = 0;
        var bagnoCount = 0;

        // 件數（沒有袋號的分提單號）
        item.NotGciDetails.forEach(function (detail) {
            if (!detail.B6F && !detail.BagNumber && detail.TransName === transName) {
                trackingnoCount++;
            }
        });

        // 袋數（去重）
        var uniqueBagNos = {};
        item.NotGciDetails.forEach(function (detail) {
            if (!detail.B6F && detail.BagNumber && detail.TransName === transName) {
                var key = detail.BagNumber + '_' + detail.TransName;
                uniqueBagNos[key] = true;
            }
        });
        bagnoCount = Object.keys(uniqueBagNos).length;

        var totalCount = trackingnoCount + bagnoCount;
        return totalCount > 0 ? totalCount : '';
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        if (!$scope.queryData.inputList || $scope.queryData.inputList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入查詢資料",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        if ($scope.selectedQueryType === 'hwb') {
            // 分提單號匯出
            var request = {
                hwbNoList: $scope.queryData.inputList
            };

            $http.post(Router.action('Tact', 'ExportExcel'), request)
                .then(function (response) {
                    $scope.isLoading = false;

                    var fileGuid = response.data.fileGuid;
                    var fileName = response.data.fileName;

                    // 建立下載連結
                    var downloadUrl = Router.action('Download', 'DownloadFile') +
                        '?fileGuid=' + encodeURIComponent(fileGuid) +
                        '&filename=' + encodeURIComponent(fileName);

                    // 觸發下載
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                }, function (error) {
                    $scope.isLoading = false;
                    swal({
                        title: "錯誤",
                        text: "匯出發生錯誤",
                        icon: "error"
                    });
                    console.error(error);
                });
        } else if ($scope.selectedQueryType === 'mwb') {
            // 主號匯出
            var request = {
                mwb: $scope.queryData.inputList
            };

            $http.post(Router.action('Tact', 'ExportMainExcel'), request)
                .then(function (response) {
                    $scope.isLoading = false;

                    var fileGuid = response.data.fileGuid;
                    var fileName = response.data.fileName;

                    // 建立下載連結
                    var downloadUrl = Router.action('Download', 'DownloadFile') +
                        '?fileGuid=' + encodeURIComponent(fileGuid) +
                        '&filename=' + encodeURIComponent(fileName);

                    // 觸發下載
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                }, function (error) {
                    $scope.isLoading = false;
                    swal({
                        title: "錯誤",
                        text: "匯出發生錯誤",
                        icon: "error"
                    });
                    console.error(error);
                });
        } else if ($scope.selectedQueryType === 'bag') {
            // 併袋號匯出
            var request = {
                bagNoList: $scope.queryData.inputList
            };

            $http.post(Router.action('Tact', 'ExportBagExcel'), request)
                .then(function (response) {
                    $scope.isLoading = false;

                    var fileGuid = response.data.fileGuid;
                    var fileName = response.data.fileName;

                    // 建立下載連結
                    var downloadUrl = Router.action('Download', 'DownloadFile') +
                        '?fileGuid=' + encodeURIComponent(fileGuid) +
                        '&filename=' + encodeURIComponent(fileName);

                    // 觸發下載
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                }, function (error) {
                    $scope.isLoading = false;
                    swal({
                        title: "錯誤",
                        text: "匯出發生錯誤",
                        icon: "error"
                    });
                    console.error(error);
                });
        }
    };

    // 初始化
    $scope.init();
}]);
