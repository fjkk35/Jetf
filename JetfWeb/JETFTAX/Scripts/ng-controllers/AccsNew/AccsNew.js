mainApp.controller('AccsNewController', function ($scope, $http) {
    $scope.loading = false;
    $scope.verifyCodeImage = '';
    $scope.isLoggedIn = false;

    $scope.loginForm = {
        userId: 'GUEST',
        userWd: 'GUEST',
        verifyCode: '',
        captchaId: ''
    };

    $scope.queryForm = {
        mawbNumbers: ''
    };

    $scope.queryResults = [];
    $scope.showResults = false;

    $scope.loadVerifyCode = function () {
        $scope.loading = true;

        $http.get(Router.action('AccsNew', 'GetVerifyCode'))
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.verifyCodeImage = response.data.ReturnObject.ImageBase64;
                    $scope.loginForm.captchaId = response.data.ReturnObject.TransactionId;
                } else if (response.data && response.data.msg) {
                    swal({
                        title: '錯誤',
                        text: response.data.msg,
                        icon: 'error'
                    });
                }
            })
            .catch(function (error) {
                console.error('載入驗證碼失敗:', error);
                swal({
                    title: '錯誤',
                    text: '載入驗證碼失敗',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.refreshVerifyCode = function () {
        $scope.loginForm.verifyCode = '';
        $scope.loginForm.captchaId = '';
        $scope.loadVerifyCode();
    };

    $scope.login = function () {
        if (!$scope.loginForm.verifyCode || $scope.loginForm.verifyCode.trim() === '') {
            swal({
                title: '驗證錯誤',
                text: '請輸入驗證碼',
                icon: 'warning'
            });
            return;
        }

        if (!$scope.loginForm.captchaId) {
            swal({
                title: '驗證錯誤',
                text: '驗證碼識別碼不存在，請重新載入驗證碼',
                icon: 'warning'
            });
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('AccsNew', 'Login'), $scope.loginForm)
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.isLoggedIn = true;
                    $scope.loginForm.verifyCode = '';

                    swal({
                        title: '登入成功',
                        text: '已成功登入 Accs 系統',
                        icon: 'success',
                        timer: 2000
                    });
                } else if (response.data && response.data.msg) {
                    swal({
                        title: '登入失敗',
                        text: response.data.msg,
                        icon: 'error'
                    });
                    $scope.refreshVerifyCode();
                }
            })
            .catch(function (error) {
                console.error('登入失敗:', error);
                swal({
                    title: '錯誤',
                    text: '登入失敗，請稍後再試',
                    icon: 'error'
                });
                $scope.refreshVerifyCode();
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.buildQueryRequest = function () {
        return {
            MawbNumbers: $scope.queryForm.mawbNumbers
        };
    };

    $scope.query = function () {
        if (!$scope.queryForm.mawbNumbers || $scope.queryForm.mawbNumbers.trim() === '') {
            swal({
                title: '驗證錯誤',
                text: '請輸入主號',
                icon: 'warning'
            });
            return;
        }

        if (!$scope.isLoggedIn) {
            swal({
                title: '提示',
                text: '請先登入系統',
                icon: 'warning'
            });
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('AccsNew', 'Query'), $scope.buildQueryRequest())
            .then(function (response) {
                if (response.data && response.data.ReturnObject) {
                    $scope.queryResults = response.data.ReturnObject;
                    $scope.showResults = true;
                } else if (response.data && response.data.msg) {
                    swal({
                        title: '查詢失敗',
                        text: response.data.msg,
                        icon: 'error'
                    });
                }
            })
            .catch(function (error) {
                console.error('查詢失敗:', error);
                swal({
                    title: '錯誤',
                    text: '查詢失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.exportExcel = function () {
        if (!$scope.queryForm.mawbNumbers || $scope.queryForm.mawbNumbers.trim() === '') {
            swal({
                title: '驗證錯誤',
                text: '請輸入主號',
                icon: 'warning'
            });
            return;
        }

        if (!$scope.isLoggedIn) {
            swal({
                title: '提示',
                text: '請先登入系統',
                icon: 'warning'
            });
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('AccsNew', 'ExportExcel'), $scope.buildQueryRequest())
            .then(function (response) {
                if (response.data && response.data.status === 'error') {
                    swal({
                        title: '匯出失敗',
                        text: response.data.msg,
                        icon: 'error'
                    });
                    return;
                }

                if (response.data) {
                    var downloadUrl = Router.action('Download', 'DownloadFile') +
                        '?fileGuid=' + encodeURIComponent(response.data.fileGuid) +
                        '&filename=' + encodeURIComponent(response.data.fileName);

                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = response.data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                    swal({
                        title: '匯出成功',
                        text: '檔案已開始下載',
                        icon: 'success',
                        timer: 2000
                    });
                }
            })
            .catch(function (error) {
                console.error('匯出失敗:', error);
                swal({
                    title: '錯誤',
                    text: '匯出失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.clearResults = function () {
        $scope.queryForm.mawbNumbers = '';
        $scope.queryResults = [];
        $scope.showResults = false;
    };

    $scope.loadVerifyCode();
});