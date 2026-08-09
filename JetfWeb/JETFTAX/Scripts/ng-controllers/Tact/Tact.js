mainApp.controller('TactController', ['$scope', '$http', function ($scope, $http) {

    $scope.init = function () {
        $scope.loginData = {
            acctId: '3027',
            acctPw: '24951752'
        };
        $scope.queryData = { inputList: '' };
        $scope.queryTypes = [
            { value: 'hwb', label: '分提單號' },
            { value: 'mwb', label: '主號' },
            { value: 'bag', label: '併袋號' }
        ];
        $scope.selectedQueryType = 'hwb';
        $scope.isLoggedIn = false;
        $scope.isLoading = false;
        $scope.results = [];
        $scope.bagResults = [];
        $scope.mainUploadFile = null;
    };

    $scope.onMainUploadFileChanged = function (input) {
        var file = input && input.files && input.files.length > 0 ? input.files[0] : null;
        $scope.$applyAsync(function () {
            $scope.mainUploadFile = file;
        });
    };

    $scope.buildMainUploadFormData = function () {
        var formData = new FormData();
        formData.append('mwb', ($scope.queryData.inputList || '').trim());
        if ($scope.mainUploadFile) {
            formData.append('uploadFile', $scope.mainUploadFile);
        }
        return formData;
    };

    $scope.getMainRequestConfig = function () {
        return {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        };
    };

    $scope.login = function () {
        if (!$scope.loginData.acctId || !$scope.loginData.acctPw) {
            showMessage('錯誤', '請輸入帳號和密碼', 'error');
            return;
        }

        $scope.isLoading = true;
        $http.post(Router.action('Tact', 'Login'), $scope.loginData).then(function (response) {
            $scope.isLoading = false;
            if (response.data.status === 'success') {
                $scope.isLoggedIn = true;
                showMessage('成功', '登入成功', 'success');
                return;
            }
            showMessage('登入失敗', response.data.msg || '登入失敗', 'error');
        }, function (error) {
            $scope.isLoading = false;
            showMessage('錯誤', '登入發生錯誤', 'error');
            console.error(error);
        });
    };

    $scope.queryUnified = function () {
        if (!$scope.isLoggedIn) {
            showMessage('錯誤', '請先登入', 'error');
            return;
        }
        if ($scope.selectedQueryType === 'hwb') {
            query('Query', { hwbNoList: $scope.queryData.inputList }, 'results');
        } else if ($scope.selectedQueryType === 'bag') {
            query('QueryBag', { bagNoList: $scope.queryData.inputList }, 'bagResults');
        }
    };

    function query(action, request, resultProperty) {
        if (!$scope.queryData.inputList || $scope.queryData.inputList.trim() === '') {
            showMessage('錯誤', '請輸入查詢資料', 'error');
            return;
        }

        $scope.isLoading = true;
        $http.post(Router.action('Tact', action), request).then(function (response) {
            $scope.isLoading = false;
            if (response.data.status !== 'success') {
                showMessage('查詢失敗', response.data.msg || '查詢失敗', 'error');
                return;
            }

            $scope.results = [];
            $scope.bagResults = [];
            $scope[resultProperty] = response.data.ReturnObject || [];
            if ($scope[resultProperty].length === 0) {
                showMessage('查詢結果', '查無資料', 'info');
            }
        }, function (error) {
            $scope.isLoading = false;
            showMessage('錯誤', '查詢發生錯誤', 'error');
            console.error(error);
        });
    }

    $scope.clearResults = function () {
        $scope.results = [];
        $scope.bagResults = [];
        $scope.queryData.inputList = '';
        $scope.mainUploadFile = null;

        var uploadFileInput = document.getElementById('mainUploadFile');
        if (uploadFileInput) {
            uploadFileInput.value = '';
        }
    };

    $scope.exportExcel = function () {
        if (!$scope.queryData.inputList || $scope.queryData.inputList.trim() === '') {
            showMessage('錯誤', '請輸入查詢資料', 'error');
            return;
        }

        var action;
        var request;
        if ($scope.selectedQueryType === 'mwb') {
            action = 'ExportMainExcel';
            request = { mwb: $scope.queryData.inputList.trim() };
        } else if ($scope.selectedQueryType === 'bag') {
            action = 'ExportBagExcel';
            request = { bagNoList: $scope.queryData.inputList };
        } else {
            action = 'ExportExcel';
            request = { hwbNoList: $scope.queryData.inputList };
        }

        $scope.isLoading = true;
        var exportPromise = $scope.selectedQueryType === 'mwb' && $scope.mainUploadFile
            ? $http.post(Router.action('Tact', action), $scope.buildMainUploadFormData(), $scope.getMainRequestConfig())
            : $http.post(Router.action('Tact', action), request);

        exportPromise.then(function (response) {
            $scope.isLoading = false;
            if (response.data.status === 'error' || !response.data.fileGuid) {
                showMessage('匯出失敗', response.data.msg || response.data.Message || '匯出失敗', 'error');
                return;
            }

            var fileGuid = response.data.fileGuid;
            var fileName = response.data.fileName;
            var downloadUrl = Router.action('Download', 'DownloadFile')
                + '?fileGuid=' + encodeURIComponent(fileGuid)
                + '&filename=' + encodeURIComponent(fileName);
            var link = document.createElement('a');
            link.href = downloadUrl;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }, function (error) {
            $scope.isLoading = false;
            showMessage('錯誤', '匯出發生錯誤', 'error');
            console.error(error);
        });
    };

    function showMessage(title, text, icon) {
        swal({ title: title, text: text, icon: icon });
    }

    $scope.init();
}]);
