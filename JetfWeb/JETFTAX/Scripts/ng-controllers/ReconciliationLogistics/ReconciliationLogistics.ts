// <reference path="../../types/global.d.ts" />

interface ReconciliationLogisticsCompanyOption {
    Value: string;
    Text: string;
    FileExtension: string;
}

interface ReconciliationLogisticsStatusOption {
    Value: string;
    Text: string;
}

interface ReconciliationLogisticsSearchRow {
    Id: number;
    RepaymentDate: string;
    Company: string;
    CustomerCode: string;
    TrackingNo: string;
    DlvInv: string;
    ReceivedAmount: number;
    DifferenceAmount: number;
    Status: string;
}

interface ReconciliationLogisticsQueryResponse {
    TotalCount: number;
    Data: ReconciliationLogisticsSearchRow[];
}

interface ReconciliationLogisticsUploadRow {
    RowNo: number;
    TrackingNo: string;
    DlvInv: string;
    ReceivedAmountText: string;
    FailReason: string;
}

interface ReconciliationLogisticsResultItem {
    RepaymentDate: string;
    Company: string;
    TrackingNo: string;
    DlvInv: string;
    ReceivableAmount: number;
    RepaymentAmount: number;
    Difference: number;
    Status: number;
    StatusName: string;
    IsSuccess: boolean;
    IsException: boolean;
}

interface ReconciliationLogisticsUploadResult {
    Count: number;
    FailCount: number;
    UpdatedCount: number;
    UnmatchedCount: number;
    ExceptionCount: number;
    UpdatedDetailCount: number;
    Message: string;
    Data: ReconciliationLogisticsUploadRow[];
    Results: ReconciliationLogisticsResultItem[];
    FileGuid: string;
    FileName: string;
    ExcelErrorMessage: string;
}

interface ReconciliationLogisticsUploadSummary {
    Count: number;
    UpdatedCount: number;
    ExceptionCount: number;
    UpdatedDetailCount: number;
}

interface ReconciliationLogisticsRetrySummary {
    Count: number;
    UpdatedCount: number;
    UnmatchedCount: number;
    Message: string;
}

interface ReconciliationLogisticsComparisonFileResult {
    FileGuid: string;
    FileName: string;
}

interface ReconciliationLogisticsScope extends ng.IScope {
    searchForm: {
        repaymentDateStart: Date | null;
        repaymentDateEnd: Date | null;
        company: string;
        status: string;
        trackingNo: string;
        dlvInv: string;
    };
    uploadForm: {
        repaymentDate: Date | null;
        company: string;
    };
    comparisonForm: {
        company: string;
    };
    retryForm: {
        repaymentDateStart: Date | null;
        repaymentDateEnd: Date | null;
        company: string;
    };
    dateOptions: any;
    searchStartDatePopup: { opened: boolean };
    searchEndDatePopup: { opened: boolean };
    uploadDatePopup: { opened: boolean };
    retryStartDatePopup: { opened: boolean };
    retryEndDatePopup: { opened: boolean };
    companies: ReconciliationLogisticsCompanyOption[];
    statuses: ReconciliationLogisticsStatusOption[];
    acceptedFileExtension: string;
    optionsLoading: boolean;
    loading: boolean;
    uploading: boolean;
    isSearched: boolean;
    rows: ReconciliationLogisticsSearchRow[];
    currentPage: number;
    pageSize: string;
    totalCount: number;
    totalPages: number;
    recordsInfo: string;
    uploadResult: {
        success: boolean;
        message: string;
    } | null;
    uploadSummary: ReconciliationLogisticsUploadSummary | null;
    excelErrorMessage: string;
    uploadData: ReconciliationLogisticsUploadRow[];
    resultRows: ReconciliationLogisticsResultItem[];
    downloadInfo: {
        fileGuid: string;
        fileName: string;
    } | null;
    comparisonAcceptedFileExtension: string;
    comparing: boolean;
    comparisonDownloadInfo: {
        fileGuid: string;
        fileName: string;
    } | null;
    retrying: boolean;
    retrySummary: ReconciliationLogisticsRetrySummary | null;
    init: () => void;
    openSearchStartDatePopup: () => void;
    openSearchEndDatePopup: () => void;
    openUploadDatePopup: () => void;
    openRetryStartDatePopup: () => void;
    openRetryEndDatePopup: () => void;
    search: () => void;
    clearSearch: () => void;
    changePageSize: () => void;
    goToPage: (page: number) => void;
    previousPage: () => void;
    nextPage: () => void;
    getPageNumbers: () => number[];
    openUploadModal: () => void;
    uploadFile: () => void;
    downloadExcel: () => void;
    openComparisonModal: () => void;
    compareDetail: () => void;
    downloadComparisonExcel: () => void;
    openRetryModal: () => void;
    retryReconcile: () => void;
}

mainApp.controller('ReconciliationLogisticsController', ['$scope', '$http', function (
    $scope: ReconciliationLogisticsScope,
    $http: ng.IHttpService
) {
    function today(): Date {
        var value = new Date();
        value.setHours(0, 0, 0, 0);
        return value;
    }

    function showError(message: string): void {
        swal({ title: '錯誤', text: message, icon: 'error' });
    }

    function redirectIfNeeded(response: ApiResponse): boolean {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }

        return false;
    }

    function formatDate(value: Date | null): string | null {
        return value ? moment(value).format('YYYY-MM-DD') : null;
    }

    function parseNullableNumber(value: string): number | null {
        return value ? parseInt(value, 10) : null;
    }

    function validateSearchDates(): boolean {
        if (!$scope.searchForm.repaymentDateStart || !$scope.searchForm.repaymentDateEnd) {
            showError('回款日期為必填，請選擇開始日期與結束日期');
            return false;
        }

        if (moment($scope.searchForm.repaymentDateStart)
            .isAfter($scope.searchForm.repaymentDateEnd, 'day')) {
            showError('開始日期不可晚於結束日期');
            return false;
        }

        return true;
    }

    function validateRetryDates(): boolean {
        if (!$scope.retryForm.repaymentDateStart || !$scope.retryForm.repaymentDateEnd) {
            showError('回款日期為必填，請選擇開始日期與結束日期');
            return false;
        }

        if (moment($scope.retryForm.repaymentDateStart)
            .isAfter($scope.retryForm.repaymentDateEnd, 'day')) {
            showError('開始日期不可晚於結束日期');
            return false;
        }

        return true;
    }

    function clearSelectedFile(): void {
        var fileInput = document.getElementById(
            'reconciliationLogisticsFile') as HTMLInputElement;
        if (fileInput) {
            fileInput.value = '';
        }
    }

    function clearSelectedComparisonFile(): void {
        var fileInput = document.getElementById(
            'reconciliationLogisticsComparisonFile') as HTMLInputElement;
        if (fileInput) {
            fileInput.value = '';
        }
    }

    function setUploadModalStaticBackdrop(enabled: boolean): void {
        // Bootstrap 4 會在點擊背景時讀取目前設定，因此可依上傳結果切換是否允許關閉。
        var modalInstance = $('#reconciliationLogisticsUploadModal')
            .data('bs.modal') as any;
        if (modalInstance && modalInstance._config) {
            modalInstance._config.backdrop = enabled ? 'static' : true;
        }
    }

    function getSelectedUploadCompany(): ReconciliationLogisticsCompanyOption | null {
        for (var index = 0; index < $scope.companies.length; index++) {
            if ($scope.companies[index].Value === $scope.uploadForm.company) {
                return $scope.companies[index];
            }
        }

        return null;
    }

    function getSelectedComparisonCompany(): ReconciliationLogisticsCompanyOption | null {
        for (var index = 0; index < $scope.companies.length; index++) {
            if ($scope.companies[index].Value === $scope.comparisonForm.company) {
                return $scope.companies[index];
            }
        }

        return null;
    }

    function resetUploadResult(): void {
        $scope.uploadResult = null;
        $scope.uploadSummary = null;
        $scope.excelErrorMessage = '';
        $scope.uploadData = [];
        $scope.resultRows = [];
        $scope.downloadInfo = null;
        setUploadModalStaticBackdrop(false);
    }

    function buildSearchRequest(): any {
        return {
            RepaymentDateStart: formatDate($scope.searchForm.repaymentDateStart),
            RepaymentDateEnd: formatDate($scope.searchForm.repaymentDateEnd),
            Company: parseNullableNumber($scope.searchForm.company),
            Status: parseNullableNumber($scope.searchForm.status),
            TrackingNo: $scope.searchForm.trackingNo,
            DlvInv: $scope.searchForm.dlvInv,
            Page: $scope.currentPage,
            PageSize: parseInt($scope.pageSize, 10)
        };
    }

    function updateRecordsInfo(): void {
        if ($scope.totalCount === 0) {
            $scope.recordsInfo = '共 0 筆';
            return;
        }

        var pageSize = parseInt($scope.pageSize, 10);
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo =
            '顯示 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    }

    function loadData(): void {
        $scope.loading = true;
        $http.post(
            Router.action('ReconciliationLogistics', 'Search'),
            buildSearchRequest()
        ).then(function (
            response: ng.IHttpResponse<ApiResponse<ReconciliationLogisticsQueryResponse>>
        ): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error' || !response.data.ReturnObject) {
                showError(response.data.msg || '查詢失敗');
                return;
            }

            var result = response.data.ReturnObject;
            $scope.rows = result.Data || [];
            $scope.totalCount = result.TotalCount || 0;
            $scope.totalPages = Math.ceil(
                $scope.totalCount / parseInt($scope.pageSize, 10)) || 0;
            $scope.isSearched = true;
            updateRecordsInfo();
        }).catch(function (): void {
            showError('查詢失敗，請稍後再試');
        }).finally(function (): void {
            $scope.loading = false;
        });
    }

    function loadCompanies(): void {
        $scope.optionsLoading = true;
        $http.get(Router.action('ReconciliationLogistics', 'GetCompanies'))
            .then(function (
                response: ng.IHttpResponse<ApiResponse<ReconciliationLogisticsCompanyOption[]>>
            ): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '載入物流公司失敗');
                    return;
                }

                $scope.companies = response.data.ReturnObject;
            }).catch(function (): void {
                showError('載入物流公司失敗，請稍後再試');
            }).finally(function (): void {
                loadStatuses();
            });
    }

    function loadStatuses(): void {
        $http.get(Router.action('ReconciliationLogistics', 'GetStatuses'))
            .then(function (
                response: ng.IHttpResponse<ApiResponse<ReconciliationLogisticsStatusOption[]>>
            ): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '載入狀態選項失敗');
                    return;
                }

                $scope.statuses = response.data.ReturnObject;
            }).catch(function (): void {
                showError('載入狀態選項失敗，請稍後再試');
            }).finally(function (): void {
                $scope.optionsLoading = false;
            });
    }

    $scope.searchForm = {
        repaymentDateStart: today(),
        repaymentDateEnd: today(),
        company: '',
        status: '',
        trackingNo: '',
        dlvInv: ''
    };
    $scope.uploadForm = {
        repaymentDate: today(),
        company: ''
    };
    $scope.comparisonForm = {
        company: ''
    };
    $scope.retryForm = {
        repaymentDateStart: today(),
        repaymentDateEnd: today(),
        company: ''
    };
    $scope.dateOptions = {
        startingDay: 1,
        showWeeks: false
    };
    $scope.searchStartDatePopup = { opened: false };
    $scope.searchEndDatePopup = { opened: false };
    $scope.uploadDatePopup = { opened: false };
    $scope.retryStartDatePopup = { opened: false };
    $scope.retryEndDatePopup = { opened: false };
    $scope.companies = [];
    $scope.statuses = [];
    $scope.acceptedFileExtension = '.xlsx,.csv';
    $scope.optionsLoading = false;
    $scope.loading = false;
    $scope.uploading = false;
    $scope.isSearched = false;
    $scope.rows = [];
    $scope.currentPage = 1;
    $scope.pageSize = '20';
    $scope.totalCount = 0;
    $scope.totalPages = 0;
    $scope.recordsInfo = '';
    $scope.uploadResult = null;
    $scope.uploadSummary = null;
    $scope.excelErrorMessage = '';
    $scope.uploadData = [];
    $scope.resultRows = [];
    $scope.downloadInfo = null;
    $scope.comparisonAcceptedFileExtension = '.xlsx,.csv';
    $scope.comparing = false;
    $scope.comparisonDownloadInfo = null;
    $scope.retrying = false;
    $scope.retrySummary = null;

    $scope.init = function (): void {
        angular.element('#ReconciliationLogistics').addClass('active');
        loadCompanies();
    };

    $scope.openSearchStartDatePopup = function (): void {
        $scope.searchStartDatePopup.opened = true;
    };

    $scope.openSearchEndDatePopup = function (): void {
        $scope.searchEndDatePopup.opened = true;
    };

    $scope.openUploadDatePopup = function (): void {
        $scope.uploadDatePopup.opened = true;
    };

    $scope.openRetryStartDatePopup = function (): void {
        $scope.retryStartDatePopup.opened = true;
    };

    $scope.openRetryEndDatePopup = function (): void {
        $scope.retryEndDatePopup.opened = true;
    };

    $scope.search = function (): void {
        if (!validateSearchDates()) {
            return;
        }

        $scope.currentPage = 1;
        loadData();
    };

    $scope.clearSearch = function (): void {
        $scope.searchForm = {
            repaymentDateStart: today(),
            repaymentDateEnd: today(),
            company: '',
            status: '',
            trackingNo: '',
            dlvInv: ''
        };
        $scope.rows = [];
        $scope.isSearched = false;
        $scope.currentPage = 1;
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.recordsInfo = '';
    };

    $scope.changePageSize = function (): void {
        $scope.currentPage = 1;
        if ($scope.isSearched) {
            loadData();
        }
    };

    $scope.goToPage = function (page: number): void {
        if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
            return;
        }

        $scope.currentPage = page;
        loadData();
    };

    $scope.previousPage = function (): void {
        $scope.goToPage($scope.currentPage - 1);
    };

    $scope.nextPage = function (): void {
        $scope.goToPage($scope.currentPage + 1);
    };

    $scope.getPageNumbers = function (): number[] {
        var pages: number[] = [];
        var maxVisible = 10;
        var start = Math.max(1, $scope.currentPage - Math.floor(maxVisible / 2));
        var end = Math.min($scope.totalPages, start + maxVisible - 1);
        if (end - start < maxVisible - 1) {
            start = Math.max(1, end - maxVisible + 1);
        }

        for (var page = start; page <= end; page++) {
            pages.push(page);
        }

        return pages;
    };

    $scope.openUploadModal = function (): void {
        $scope.uploadForm = {
            repaymentDate: today(),
            company: ''
        };
        $scope.uploadDatePopup.opened = false;
        $scope.acceptedFileExtension = '.xlsx,.csv';
        $scope.uploading = false;
        resetUploadResult();
        clearSelectedFile();

        $('#reconciliationLogisticsUploadModal').modal({
            backdrop: true,
            keyboard: false,
            show: true
        });
    };

    $scope.openComparisonModal = function (): void {
        $scope.comparisonForm = {
            company: ''
        };
        $scope.comparisonAcceptedFileExtension = '.xlsx,.csv';
        $scope.comparing = false;
        $scope.comparisonDownloadInfo = null;
        clearSelectedComparisonFile();

        $('#reconciliationLogisticsComparisonModal').modal({
            backdrop: true,
            keyboard: false,
            show: true
        });
    };

    $scope.$watch('comparisonForm.company', function (
        newValue: string,
        oldValue: string
    ): void {
        var selectedCompany = getSelectedComparisonCompany();
        $scope.comparisonAcceptedFileExtension = selectedCompany
            ? selectedCompany.FileExtension
            : '.xlsx,.csv';

        if (newValue !== oldValue) {
            clearSelectedComparisonFile();
            $scope.comparisonDownloadInfo = null;
        }
    });

    $scope.compareDetail = function (): void {
        if (!$scope.comparisonForm.company) {
            showError('請選擇物流公司');
            return;
        }

        var fileInput = document.getElementById(
            'reconciliationLogisticsComparisonFile') as HTMLInputElement;
        var file = fileInput && fileInput.files && fileInput.files.length
            ? fileInput.files[0]
            : null;
        if (!file) {
            showError('請選擇檔案');
            return;
        }

        var selectedCompany = getSelectedComparisonCompany();
        var expectedExtension = selectedCompany
            ? selectedCompany.FileExtension.replace('.', '').toLowerCase()
            : '';
        var extension = file.name.split('.').pop().toLowerCase();
        if (!expectedExtension || extension !== expectedExtension) {
            clearSelectedComparisonFile();
            showError(
                (selectedCompany ? selectedCompany.Text : '物流公司') +
                '上傳檔案副檔名需為 ' + expectedExtension);
            return;
        }

        var formData = new FormData();
        formData.append('file', file);
        formData.append('company', $scope.comparisonForm.company);
        $scope.comparing = true;
        $scope.comparisonDownloadInfo = null;
        $http.post(Router.action('ReconciliationLogistics', 'CompareDetail'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (
            response: ng.IHttpResponse<ApiResponse<ReconciliationLogisticsComparisonFileResult>>
        ): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error' || !response.data.ReturnObject) {
                showError(response.data.msg || '比對明細產生失敗');
                return;
            }

            var result = response.data.ReturnObject;
            $scope.comparisonDownloadInfo = {
                fileGuid: result.FileGuid,
                fileName: result.FileName
            };
            $scope.downloadComparisonExcel();
            swal({
                title: '成功',
                text: '比對明細已產生並開始下載。',
                icon: 'success'
            });
        }).catch(function (): void {
            showError('比對明細產生失敗，請稍後再試');
        }).finally(function (): void {
            $scope.comparing = false;
            clearSelectedComparisonFile();
        });
    };

    $scope.downloadComparisonExcel = function (): void {
        if (!$scope.comparisonDownloadInfo) {
            showError('沒有可下載的比對明細');
            return;
        }

        var downloadInfo = $scope.comparisonDownloadInfo;
        $scope.comparisonDownloadInfo = null;
        var downloadUrl = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(downloadInfo.fileGuid)
            + '&fileName=' + encodeURIComponent(downloadInfo.fileName);
        var link = document.createElement('a');
        link.href = downloadUrl;
        link.download = downloadInfo.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

    $scope.openRetryModal = function (): void {
        $scope.retryForm = {
            repaymentDateStart: $scope.searchForm.repaymentDateStart || today(),
            repaymentDateEnd: $scope.searchForm.repaymentDateEnd || today(),
            company: ''
        };
        $scope.retryStartDatePopup.opened = false;
        $scope.retryEndDatePopup.opened = false;
        $scope.retrying = false;
        $scope.retrySummary = null;

        $('#reconciliationLogisticsRetryModal').modal({
            backdrop: true,
            keyboard: false,
            show: true
        });
    };

    $scope.retryReconcile = function (): void {
        if (!validateRetryDates()) {
            return;
        }

        if (!$scope.retryForm.company) {
            showError('請選擇物流公司');
            return;
        }

        $scope.retrying = true;
        $scope.retrySummary = null;
        $http.post(Router.action('ReconciliationLogistics', 'RetryNotFound'), {
            RepaymentDateStart: formatDate($scope.retryForm.repaymentDateStart),
            RepaymentDateEnd: formatDate($scope.retryForm.repaymentDateEnd),
            Company: parseNullableNumber($scope.retryForm.company)
        }).then(function (
            response: ng.IHttpResponse<ApiResponse<ReconciliationLogisticsUploadResult>>
        ): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error' || !response.data.ReturnObject) {
                showError(response.data.msg || '重新銷帳失敗');
                return;
            }

            var result = response.data.ReturnObject;
            $scope.retrySummary = {
                Count: result.Count || 0,
                UpdatedCount: result.UpdatedCount || 0,
                UnmatchedCount: result.UnmatchedCount || 0,
                Message: result.Message || response.data.msg || '重新銷帳完成'
            };
            swal({
                title: '完成',
                text: $scope.retrySummary.Message,
                icon: 'success'
            });

            if ($scope.isSearched) {
                loadData();
            }
        }).catch(function (): void {
            showError('重新銷帳失敗，請稍後再試');
        }).finally(function (): void {
            $scope.retrying = false;
        });
    };

    $scope.$watch('uploadForm.company', function (
        newValue: string,
        oldValue: string
    ): void {
        var selectedCompany = getSelectedUploadCompany();
        $scope.acceptedFileExtension = selectedCompany
            ? selectedCompany.FileExtension
            : '.xlsx,.csv';

        if (newValue !== oldValue) {
            clearSelectedFile();
            resetUploadResult();
        }
    });

    $scope.uploadFile = function (): void {
        if (!$scope.uploadForm.repaymentDate) {
            showError('請選擇回款日期');
            return;
        }

        if (!$scope.uploadForm.company) {
            showError('請選擇物流公司');
            return;
        }

        var fileInput = document.getElementById(
            'reconciliationLogisticsFile') as HTMLInputElement;
        var file = fileInput && fileInput.files && fileInput.files.length
            ? fileInput.files[0]
            : null;
        if (!file) {
            showError('請選擇檔案');
            return;
        }

        var selectedCompany = getSelectedUploadCompany();
        var expectedExtension = selectedCompany
            ? selectedCompany.FileExtension.replace('.', '').toLowerCase()
            : '';
        var extension = file.name.split('.').pop().toLowerCase();
        if (!expectedExtension || extension !== expectedExtension) {
            clearSelectedFile();
            showError(
                (selectedCompany ? selectedCompany.Text : '物流公司') +
                '上傳檔案副檔名需為 ' +
                expectedExtension
            );
            return;
        }

        var formData = new FormData();
        formData.append('file', file);
        formData.append('company', $scope.uploadForm.company);
        formData.append(
            'repaymentDate',
            moment($scope.uploadForm.repaymentDate).format('YYYY-MM-DD'));

        $scope.uploading = true;
        resetUploadResult();
        $http.post(Router.action('ReconciliationLogistics', 'Upload'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (
            response: ng.IHttpResponse<ApiResponse<ReconciliationLogisticsUploadResult>>
        ): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            var hasUploadResult = !!response.data.ReturnObject;
            var result =
                response.data.ReturnObject || {} as ReconciliationLogisticsUploadResult;
            // 欄位驗證失敗與銷帳結果分開保存，彈窗只顯示需要處理的異常資料。
            $scope.uploadData = result.Data || [];
            $scope.resultRows = (result.Results || []).filter(function (
                item: ReconciliationLogisticsResultItem
            ): boolean {
                return item.IsException;
            });

            // 統計數字使用本次完整上傳結果，不受畫面僅顯示異常資料影響。
            $scope.uploadSummary = hasUploadResult
                ? {
                    Count: result.Count || 0,
                    UpdatedCount: result.UpdatedCount || 0,
                    ExceptionCount:
                        (result.FailCount || 0) +
                        (result.UnmatchedCount || 0) +
                        (result.ExceptionCount || 0),
                    UpdatedDetailCount: result.UpdatedDetailCount || 0
                }
                : null;

            // 只有後端成功產生 Excel 時才顯示下載按鈕。
            $scope.downloadInfo = result.FileGuid && result.FileName
                ? { fileGuid: result.FileGuid, fileName: result.FileName }
                : null;
            $scope.excelErrorMessage = result.ExcelErrorMessage || '';
            var isSuccess = response.data.status === 'success';
            var message =
                result.Message || response.data.msg || (isSuccess ? '上傳成功' : '上傳失敗');
            $scope.uploadResult = {
                success: isSuccess,
                message: message
            };
            setUploadModalStaticBackdrop(true);

            swal({
                title: $scope.excelErrorMessage ? '提醒' : (isSuccess ? '成功' : '錯誤'),
                text: $scope.excelErrorMessage || message,
                icon: $scope.excelErrorMessage
                    ? 'warning'
                    : (isSuccess ? 'success' : 'error')
            });

            if (isSuccess && $scope.isSearched) {
                loadData();
            }
        }).catch(function (): void {
            $scope.uploadSummary = null;
            $scope.excelErrorMessage = '';
            $scope.uploadResult = {
                success: false,
                message: '系統發生錯誤，請稍後再試'
            };
            showError($scope.uploadResult.message);
        }).finally(function (): void {
            $scope.uploading = false;
            clearSelectedFile();
        });
    };

    $scope.downloadExcel = function (): void {
        if (!$scope.downloadInfo) {
            showError('目前沒有可下載的結果');
            return;
        }

        // TempData 下載後即會移除；先清除按鈕，避免同一份結果重複點擊下載。
        var downloadInfo = $scope.downloadInfo;
        $scope.downloadInfo = null;
        var downloadUrl = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(downloadInfo.fileGuid)
            + '&fileName=' + encodeURIComponent(downloadInfo.fileName);
        var link = document.createElement('a');
        link.href = downloadUrl;
        link.download = downloadInfo.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };
}]);
