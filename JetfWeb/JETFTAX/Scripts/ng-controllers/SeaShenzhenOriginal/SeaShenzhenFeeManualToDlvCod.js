mainApp.controller('SeaShenzhenFeeManualToDlvCodController', ['$scope', '$http', function ($scope, $http) {
    function clearSelectedFile(fileInput) {
        if (fileInput) {
            fileInput.value = '';
        }
    }
    $scope.data = [];
    $scope.uploading = false;
    $scope.loading = false;
    $scope.isSearched = false;
    $scope.recordsInfo = '';
    $scope.currentPage = 1;
    $scope.pageSize = '10';
    $scope.totalCount = 0;
    $scope.totalPages = 0;
    $scope.searchForm = {
        trackingNo: ''
    };
    $scope.uploadFailData = [];
    $scope.uploadResult = null;
    $scope.openUploadModal = function () {
        $scope.uploadResult = null;
        $scope.uploadFailData = [];
        $('#seaShenzhenFeeManualToDlvCodUploadModal').modal('show');
    };
    $scope.search = function () {
        $scope.currentPage = 1;
        $scope.loadData();
    };
    $scope.clearSearch = function () {
        $scope.searchForm = {
            trackingNo: ''
        };
        $scope.data = [];
        $scope.isSearched = false;
        $scope.recordsInfo = '';
        $scope.currentPage = 1;
        $scope.totalCount = 0;
        $scope.totalPages = 0;
    };
    $scope.loadData = function () {
        $scope.loading = true;
        $http.post(Router.action('SeaShenzhenFeeManualToDlvCod', 'SearchData'), buildRequest($scope.currentPage, $scope.parsePageSize()))
            .then(function (response) {
            var result = response.data || {};
            if (result.error) {
                showError('查詢失敗: ' + result.error);
                return;
            }
            $scope.data = result.Data || [];
            $scope.totalCount = result.TotalCount || 0;
            $scope.totalPages = Math.ceil($scope.totalCount / $scope.parsePageSize()) || 0;
            $scope.isSearched = true;
            updateRecordsInfo();
        })
            .catch(function () {
            showError('查詢失敗，請稍後再試');
        })
            .finally(function () {
            $scope.loading = false;
        });
    };
    $scope.changePageSize = function () {
        $scope.currentPage = 1;
        $scope.loadData();
    };
    $scope.changePage = function (page) {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }
        $scope.currentPage = page;
        $scope.loadData();
    };
    $scope.previousPage = function () {
        if ($scope.currentPage > 1) {
            $scope.currentPage--;
            $scope.loadData();
        }
    };
    $scope.nextPage = function () {
        if ($scope.currentPage < $scope.totalPages) {
            $scope.currentPage++;
            $scope.loadData();
        }
    };
    $scope.getPages = function () {
        var pages = [];
        var startPage = Math.max(1, $scope.currentPage - 2);
        var endPage = Math.min($scope.totalPages, $scope.currentPage + 2);
        for (var i = startPage; i <= endPage; i++) {
            pages.push(i);
        }
        return pages;
    };
    $scope.parsePageSize = function () {
        return parseInt($scope.pageSize, 10);
    };
    $scope.uploadFile = function () {
        var fileInput = document.getElementById('seaShenzhenFeeManualToDlvCodFileInput');
        var file = fileInput && fileInput.files && fileInput.files.length > 0
            ? fileInput.files[0]
            : null;
        if (!file) {
            showError('請選擇檔案');
            return;
        }
        var fileExtension = file.name.split('.').pop().toLowerCase();
        if (fileExtension !== 'xlsx') {
            clearSelectedFile(fileInput);
            showError('副檔名需為 xlsx');
            return;
        }
        $scope.uploading = true;
        $scope.uploadResult = null;
        $scope.uploadFailData = [];
        var formData = new FormData();
        formData.append('file', file);
        $http.post(Router.action('SeaShenzhenFeeManualToDlvCod', 'Upload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response) {
            var data = response.data || {};
            var returnObj = data.ReturnObject || {};
            $scope.uploadFailData = returnObj.data || [];
            if (data.status === 'success') {
                $scope.uploadResult = {
                    success: true,
                    message: returnObj.message || data.msg || '上傳成功'
                };
                swal({
                    title: '成功',
                    text: $scope.uploadResult.message,
                    icon: 'success'
                });
                clearSelectedFile(fileInput);
                $('#seaShenzhenFeeManualToDlvCodUploadModal').modal('hide');
                $scope.loadData();
                return;
            }
            $scope.uploadResult = {
                success: false,
                message: returnObj.message || data.msg || '上傳失敗'
            };
            clearSelectedFile(fileInput);
            swal({
                title: '錯誤',
                text: $scope.uploadResult.message,
                icon: 'error'
            });
        })
            .catch(function () {
            $scope.uploadFailData = [];
            $scope.uploadResult = {
                success: false,
                message: '上傳失敗，請稍後再試'
            };
            clearSelectedFile(fileInput);
            swal({
                title: '錯誤',
                text: '上傳失敗，請稍後再試',
                icon: 'error'
            });
        })
            .finally(function () {
            $scope.uploading = false;
        });
    };
    function showError(message) {
        swal({
            title: '錯誤',
            text: message,
            icon: 'error'
        });
    }
    function buildRequest(pageIndex, pageSize) {
        return {
            TrackingNo: $scope.searchForm.trackingNo,
            PageIndex: pageIndex,
            PageSize: pageSize
        };
    }
    function updateRecordsInfo() {
        if ($scope.totalCount === 0) {
            $scope.recordsInfo = '共 0 筆';
            return;
        }
        var pageSize = $scope.parsePageSize();
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo = '顯示第 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    }
    $scope.loadData();
}]);
