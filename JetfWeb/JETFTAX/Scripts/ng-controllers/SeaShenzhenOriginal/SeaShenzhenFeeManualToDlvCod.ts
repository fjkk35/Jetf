interface SeaShenzhenFeeManualToDlvCodUploadReturnObject {
    data?: SeaShenzhenFeeManualToDlvCodUploadFailRow[];
    message?: string;
}

interface SeaShenzhenFeeManualToDlvCodUploadResponse {
    status?: string;
    msg?: string;
    ReturnObject?: SeaShenzhenFeeManualToDlvCodUploadReturnObject;
}

interface SeaShenzhenFeeManualToDlvCodUploadFailRow {
    RowNo: number;
    FailFieldName: string;
    FailReason: string;
    TrackingNo: string;
    DlvInv: string;
    ToDlvCodText: string;
}

interface SeaShenzhenFeeManualToDlvCodQueryRow {
    Id: number;
    TrackingNo: string;
    ToDlvCod: number;
    CreatedTimeText: string;
    CreatedUser: string;
}

interface SeaShenzhenFeeManualToDlvCodQueryResponse {
    Data?: SeaShenzhenFeeManualToDlvCodQueryRow[];
    TotalCount?: number;
    error?: string;
}

interface SeaShenzhenFeeManualToDlvCodScope extends ng.IScope {
    data: SeaShenzhenFeeManualToDlvCodQueryRow[];
    uploading: boolean;
    loading: boolean;
    isSearched: boolean;
    recordsInfo: string;
    currentPage: number;
    pageSize: string;
    totalCount: number;
    totalPages: number;
    searchForm: {
        trackingNo: string;
    };
    uploadFailData: SeaShenzhenFeeManualToDlvCodUploadFailRow[];
    uploadResult: {
        success: boolean;
        message: string;
    } | null;
    openUploadModal: () => void;
    uploadFile: () => void;
    search: () => void;
    clearSearch: () => void;
    loadData: () => void;
    changePageSize: () => void;
    changePage: (page: number) => void;
    previousPage: () => void;
    nextPage: () => void;
    getPages: () => number[];
    parsePageSize: () => number;
}

mainApp.controller('SeaShenzhenFeeManualToDlvCodController', ['$scope', '$http', function (
    $scope: SeaShenzhenFeeManualToDlvCodScope,
    $http: ng.IHttpService
) {
    function clearSelectedFile(fileInput: HTMLInputElement): void {
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

    $scope.openUploadModal = function (): void {
        $scope.uploadResult = null;
        $scope.uploadFailData = [];
        ($('#seaShenzhenFeeManualToDlvCodUploadModal') as any).modal('show');
    };

    $scope.search = function (): void {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    $scope.clearSearch = function (): void {
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

    $scope.loadData = function (): void {
        $scope.loading = true;

        $http.post(Router.action('SeaShenzhenFeeManualToDlvCod', 'SearchData'), buildRequest($scope.currentPage, $scope.parsePageSize()))
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeManualToDlvCodQueryResponse>): void {
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
            .catch(function (): void {
                showError('查詢失敗，請稍後再試');
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.changePageSize = function (): void {
        $scope.currentPage = 1;
        $scope.loadData();
    };

    $scope.changePage = function (page: number): void {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }

        $scope.currentPage = page;
        $scope.loadData();
    };

    $scope.previousPage = function (): void {
        if ($scope.currentPage > 1) {
            $scope.currentPage--;
            $scope.loadData();
        }
    };

    $scope.nextPage = function (): void {
        if ($scope.currentPage < $scope.totalPages) {
            $scope.currentPage++;
            $scope.loadData();
        }
    };

    $scope.getPages = function (): number[] {
        var pages: number[] = [];
        var startPage = Math.max(1, $scope.currentPage - 2);
        var endPage = Math.min($scope.totalPages, $scope.currentPage + 2);

        for (var i = startPage; i <= endPage; i++) {
            pages.push(i);
        }

        return pages;
    };

    $scope.parsePageSize = function (): number {
        return parseInt($scope.pageSize, 10);
    };

    $scope.uploadFile = function (): void {
        var fileInput = document.getElementById('seaShenzhenFeeManualToDlvCodFileInput') as HTMLInputElement;
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
            .then(function (response: ng.IHttpResponse<SeaShenzhenFeeManualToDlvCodUploadResponse>): void {
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
                    ($('#seaShenzhenFeeManualToDlvCodUploadModal') as any).modal('hide');
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
            .catch(function (): void {
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
            .finally(function (): void {
                $scope.uploading = false;
            });
    };

    function showError(message: string): void {
        swal({
            title: '錯誤',
            text: message,
            icon: 'error'
        });
    }

    function buildRequest(pageIndex: number, pageSize: number): any {
        return {
            TrackingNo: $scope.searchForm.trackingNo,
            PageIndex: pageIndex,
            PageSize: pageSize
        };
    }

    function updateRecordsInfo(): void {
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
