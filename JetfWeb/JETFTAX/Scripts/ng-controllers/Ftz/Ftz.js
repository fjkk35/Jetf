mainApp.controller('FtzController', ['$scope', '$http', '$window', function ($scope, $http, $window) {

    // 初始化
    $scope.init = function () {
        $scope.loginData = {
            userId: '0335',
            userPd: '24951752'
        };
        $scope.queryData = {
            hwbqList: ''
        };
        $scope.mainQueryData = {
            mwb: ''
        };
        $scope.bagQueryData = {
            bagNoList: ''
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
        $scope.mainQueryResults = []; // 改為陣列
        $scope.bagQueryResults = []; // 併袋號查詢結果
        $scope.mainUploadFile = null;
    };

    // 主號模式的上傳檔會同時供查詢與匯出使用，這裡只保留檔案物件本身。
    $scope.onMainUploadFileChanged = function (input) {
        var file = input && input.files && input.files.length > 0 ? input.files[0] : null;
        $scope.$applyAsync(function () {
            $scope.mainUploadFile = file;
        });
    };

    // 主號查詢有檔案時改用 multipart/form-data，讓文字與檔案可以一起送到後端。
    $scope.buildMainUploadFormData = function () {
        var formData = new FormData();
        formData.append('mwb', ($scope.queryData.hwbqList || '').trim());
        if ($scope.mainUploadFile) {
            formData.append('uploadFile', $scope.mainUploadFile);
        }
        return formData;
    };

    // Content-Type 交給瀏覽器自動帶 boundary，避免 multipart 格式被手動 header 破壞。
    $scope.getMainRequestConfig = function () {
        return {
            transformRequest: angular.identity,
            headers: {
                'Content-Type': undefined
            }
        };
    };

    // 登入
    $scope.login = function () {
        if (!$scope.loginData.userId || !$scope.loginData.userPd) {
            swal({
                title: "錯誤",
                text: "請輸入帳號和密碼",
                icon: "error"
            });
            return;
        }
        $scope.isLoading = true;
        $http.post(Router.action('Ftz', 'Login'), $scope.loginData)
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
            $scope.queryMainUnified();
        } else if ($scope.selectedQueryType === 'bag') {
            // 併袋號查詢
            $scope.queryBag();
        }
    };

    // 查詢（分提單號）
    $scope.query = function () {
        if (!$scope.queryData.hwbqList || $scope.queryData.hwbqList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入查詢資料",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        $http.post(Router.action('Ftz', 'Query'), $scope.queryData)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.results = response.data.ReturnObject || [];
                    $scope.mainQueryResults = []; // 清除主號查詢結果
                    $scope.bagQueryResults = []; // 清除併袋號查詢結果
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
                        text: response.data.Message || "查詢失敗",
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

    // 主號查詢（統一查詢介面使用）
    $scope.queryMainUnified = function () {
        if (!$scope.queryData.hwbqList || $scope.queryData.hwbqList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入主號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var mainQueryRequest = {
            mwb: $scope.queryData.hwbqList.trim()
        };

        var requestPromise = $scope.mainUploadFile
            ? $http.post(Router.action('Ftz', 'QueryMain'), $scope.buildMainUploadFormData(), $scope.getMainRequestConfig())
            : $http.post(Router.action('Ftz', 'QueryMain'), mainQueryRequest);

        requestPromise
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.mainQueryResults = response.data.ReturnObject || [];
                    $scope.results = []; // 清除分提單號查詢結果
                    $scope.bagQueryResults = []; // 清除併袋號查詢結果
                    if ($scope.mainQueryResults.length === 0) {
                        swal({
                            title: "查詢結果",
                            text: "查無資料",
                            icon: "info"
                        });
                    }
                } else {
                    swal({
                        title: "查詢失敗",
                        text: response.data.msg || response.data.Message || "查詢失敗",
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

    // 併袋號查詢
    $scope.queryBag = function () {
        if (!$scope.queryData.hwbqList || $scope.queryData.hwbqList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入併袋號",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        var bagQueryRequest = {
            bagNoList: $scope.queryData.hwbqList
        };

        $http.post(Router.action('Ftz', 'QueryBag'), bagQueryRequest)
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == 'success') {
                    $scope.bagQueryResults = response.data.ReturnObject || [];
                    $scope.results = []; // 清除分提單號查詢結果
                    $scope.mainQueryResults = []; // 清除主號查詢結果
                    if ($scope.bagQueryResults.length === 0) {
                        swal({
                            title: "查詢結果",
                            text: "查無資料",
                            icon: "info"
                        });
                    }
                } else {
                    swal({
                        title: "查詢失敗",
                        text: response.data.msg || response.data.Message || "查詢失敗",
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
        $scope.mainQueryResults = []; // 修改為陣列
        $scope.bagQueryResults = []; // 清除併袋號查詢結果
        $scope.queryData.hwbqList = '';
        $scope.mainUploadFile = null;

        // 同步把原生 file input 清空，避免同一個檔案重新選取時 change 事件不觸發。
        var uploadFileInput = document.getElementById('mainUploadFile');
        if (uploadFileInput) {
            uploadFileInput.value = '';
        }
    };

    // 清除主號查詢結果
    $scope.clearMainResult = function () {
        $scope.mainQueryResults = []; // 修改為陣列
        $scope.mainQueryData.mwb = '';
    };

    // 取得所有不重複的派件公司名稱
    $scope.getDistinctTransNames = function () {
        if (!$scope.mainQueryResults || $scope.mainQueryResults.length === 0) {
            return [];
        }

        var transNamesSet = {};
        $scope.mainQueryResults.forEach(function (item) {
            if (item.NotGciDetails && item.NotGciDetails.length > 0) {
                item.NotGciDetails.forEach(function (detail) {
                    if (!detail.IsB6F && detail.TransName) {
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

        var bagnosCount = 0;
        var trackingnoCount = 0;
        var bagnoCount = 0;

        // 一分號多袋
        item.NotGciDetails.forEach(function (detail) {
            if (!detail.IsB6F && detail.realTotBag && !detail.expBagNo && detail.TransName === transName) {
                bagnosCount += detail.realTotBag.split(',').length;
            }
        });

        // 件數
        item.NotGciDetails.forEach(function (detail) {
            if (!detail.IsB6F && !detail.realTotBag && !detail.expBagNo && detail.TransName === transName) {
                trackingnoCount++;
            }
        });

        // 袋數（去重）
        var uniqueBagNos = {};
        item.NotGciDetails.forEach(function (detail) {
            if (!detail.IsB6F && detail.expBagNo && detail.TransName === transName) {
                var key = detail.expBagNo + '_' + detail.TransName;
                uniqueBagNos[key] = true;
            }
        });
        bagnoCount = Object.keys(uniqueBagNos).length;

        var totalCount = bagnosCount + trackingnoCount + bagnoCount;
        return totalCount > 0 ? totalCount : '';
    };

    // 匯出 Excel
    $scope.exportExcel = function () {
        if (!$scope.queryData.hwbqList || $scope.queryData.hwbqList.trim() === '') {
            swal({
                title: "錯誤",
                text: "請輸入查詢資料",
                icon: "error"
            });
            return;
        }

        $scope.isLoading = true;

        // 根據查詢類型決定使用哪個 API
        var exportAction = '';
        var requestData = null;
        
        if ($scope.selectedQueryType === 'mwb') {
            exportAction = 'ExportMainExcel';
            requestData = { mwb: $scope.queryData.hwbqList.trim() };
        } else if ($scope.selectedQueryType === 'bag') {
            exportAction = 'ExportBagExcel';
            requestData = { bagNoList: $scope.queryData.hwbqList };
        } else {
            exportAction = 'ExportExcel';
            requestData = $scope.queryData;
        }

        var exportPromise = null;
        if ($scope.selectedQueryType === 'mwb' && $scope.mainUploadFile) {
            exportPromise = $http.post(Router.action('Ftz', exportAction), $scope.buildMainUploadFormData(), $scope.getMainRequestConfig());
        } else {
            exportPromise = $http.post(Router.action('Ftz', exportAction), requestData);
        }

        exportPromise
            .then(function (response) {
                $scope.isLoading = false;
                if (response.data.status == "error" || !response.data.fileGuid) {
                    swal({
                        title: "匯出失敗",
                        text: response.data.msg || response.data.Message || "匯出失敗",
                        icon: "error"
                    });
                    return;
                }

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
    };

    // 初始化
    $scope.init();
}]);
