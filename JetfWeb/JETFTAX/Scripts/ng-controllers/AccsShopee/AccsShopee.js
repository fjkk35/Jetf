// Controller
mainApp.controller('AccsShopeeController', function ($scope, $http) {
    // 初始化資料
    $scope.loading = false;
    $scope.verifyCodeImage = '';
    $scope.isLoggedIn = false;
    $scope.token = '';
    $scope.sessionCookie = '';
    
    // 登入表單
    $scope.loginForm = {
        userId: 'GUEST',
        userWd: 'GUEST',
        verifyCode: ''
    };
    
    // 查詢表單
    $scope.queryForm = {
        mawbNumbers: ''
    };
    
    // 查詢結果
    $scope.queryResults = [];
    $scope.showResults = false;

    // 載入驗證碼
    $scope.loadVerifyCode = function () {
        $scope.loading = true;
        
        $http.get(Router.action('AccsShopee', 'GetVerifyCode'))
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.verifyCodeImage = response.data.ReturnObject.ImageBase64;
                    $scope.sessionCookie = response.data.ReturnObject.SessionId;
                } else if (response.data && response.data.msg) {
                    swal({
                        title: "錯誤",
                        text: response.data.msg,
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('載入驗證碼失敗:', error);
                swal({
                    title: "錯誤",
                    text: "載入驗證碼失敗",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 重新載入驗證碼
    $scope.refreshVerifyCode = function () {
        $scope.loginForm.verifyCode = '';
        $scope.loadVerifyCode();
    };

    // 登入
    $scope.login = function () {
        // 驗證輸入
        if (!$scope.loginForm.verifyCode || $scope.loginForm.verifyCode.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入驗證碼",
                icon: "warning"
            });
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('AccsShopee', 'Login'), $scope.loginForm)
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.isLoggedIn = true;
                    $scope.token = response.data.ReturnObject.Token;
                    $scope.sessionCookie = response.data.ReturnObject.SessionCookie;
                    
                    swal({
                        title: "登入成功",
                        text: "已成功登入 Accs 系統",
                        icon: "success",
                        timer: 2000
                    });
                } else if (response.data && response.data.msg) {
                    swal({
                        title: "登入失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                    // 重新載入驗證碼
                    $scope.refreshVerifyCode();
                }
            })
            .catch(function (error) {
                console.error('登入失敗:', error);
                swal({
                    title: "錯誤",
                    text: "登入失敗，請稍後再試",
                    icon: "error"
                });
                $scope.refreshVerifyCode();
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 查詢
    $scope.query = function () {
        // 驗證輸入
        if (!$scope.queryForm.mawbNumbers || $scope.queryForm.mawbNumbers.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入主提單號",
                icon: "warning"
            });
            return;
        }

        if (!$scope.isLoggedIn) {
            swal({
                title: "提示",
                text: "請先登入系統",
                icon: "warning"
            });
            return;
        }

        $scope.loading = true;

        var queryRequest = {
            MawbNumbers: $scope.queryForm.mawbNumbers,
            Token: $scope.token
        };

        $http.post(Router.action('AccsShopee', 'Query'), queryRequest)
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.queryResults = response.data.ReturnObject;
                    $scope.showResults = true;
                    
                    if ($scope.queryResults.length === 0) {
                        swal({
                            title: "提示",
                            text: "查無資料",
                            icon: "info"
                        });
                    }
                } else if (response.data && response.data.msg) {
                    swal({
                        title: "查詢失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('查詢失敗:', error);
                swal({
                    title: "錯誤",
                    text: "查詢失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        // 驗證輸入
        if (!$scope.queryForm.mawbNumbers || $scope.queryForm.mawbNumbers.trim() === '') {
            swal({
                title: "驗證錯誤",
                text: "請輸入主提單號",
                icon: "warning"
            });
            return;
        }

        if (!$scope.isLoggedIn) {
            swal({
                title: "提示",
                text: "請先登入系統",
                icon: "warning"
            });
            return;
        }

        $scope.loading = true;

        var queryRequest = {
            MawbNumbers: $scope.queryForm.mawbNumbers,
            Token: $scope.token
        };

        $http.post(Router.action('AccsShopee', 'ExportExcel'), queryRequest)
            .then(function (response) {
                if (response.data.status == "error") {
                    swal({
                        title: "匯出失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                    return;
                }

                if (response.data) {
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
                    
                    swal({
                        title: "匯出成功",
                        text: "檔案已開始下載",
                        icon: "success",
                        timer: 2000
                    });
                } else if (response.data && response.data.msg) {
                    swal({
                        title: "匯出失敗",
                        text: response.data.msg,
                        icon: "error"
                    });
                }
            })
            .catch(function (error) {
                console.error('匯出失敗:', error);
                swal({
                    title: "錯誤",
                    text: "匯出失敗，請稍後再試",
                    icon: "error"
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    // 清除結果
    $scope.clearResults = function () {
        $scope.queryForm.mawbNumbers = '';
        $scope.queryResults = [];
        $scope.showResults = false;
    };

    // 取得狀態顯示文字
    $scope.getStatusText = function (status) {
        switch (status) {
            case 'Success':
                return '成功';
            case 'Error':
                return '錯誤';
            case 'NoData':
                return '查無資料';
            case 'ParseError':
                return '解析錯誤';
            default:
                return status;
        }
    };

    // 取得狀態樣式
    $scope.getStatusClass = function (status) {
        switch (status) {
            case 'Success':
                return 'badge-success';
            case 'Error':
            case 'ParseError':
                return 'badge-danger';
            case 'NoData':
                return 'badge-warning';
            default:
                return 'badge-secondary';
        }
    };

    // 初始化載入驗證碼
    $scope.loadVerifyCode();
});
