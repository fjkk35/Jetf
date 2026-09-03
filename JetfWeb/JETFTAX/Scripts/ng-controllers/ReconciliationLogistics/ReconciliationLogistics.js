// <reference path="../../types/global.d.ts" />
mainApp.controller('ReconciliationLogisticsController', ['$scope', '$http', function ($scope, $http) {
        function today() {
            var value = new Date();
            value.setHours(0, 0, 0, 0);
            return value;
        }
        function showError(message) {
            swal({ title: '錯誤', text: message, icon: 'error' });
        }
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        function formatDate(value) {
            return value ? moment(value).format('YYYY-MM-DD') : null;
        }
        function parseNullableNumber(value) {
            return value ? parseInt(value, 10) : null;
        }
        function validateSearchDates() {
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
        function validateRetryDates() {
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
        function clearSelectedFile() {
            var fileInput = document.getElementById('reconciliationLogisticsFile');
            if (fileInput) {
                fileInput.value = '';
            }
        }
        function clearSelectedComparisonFile() {
            var fileInput = document.getElementById('reconciliationLogisticsComparisonFile');
            if (fileInput) {
                fileInput.value = '';
            }
        }
        function setUploadModalStaticBackdrop(enabled) {
            // Bootstrap 4 會在點擊背景時讀取目前設定，因此可依上傳結果切換是否允許關閉。
            var modalInstance = $('#reconciliationLogisticsUploadModal')
                .data('bs.modal');
            if (modalInstance && modalInstance._config) {
                modalInstance._config.backdrop = enabled ? 'static' : true;
            }
        }
        function getSelectedUploadCompany() {
            for (var index = 0; index < $scope.companies.length; index++) {
                if ($scope.companies[index].Value === $scope.uploadForm.company) {
                    return $scope.companies[index];
                }
            }
            return null;
        }
        function getSelectedComparisonCompany() {
            for (var index = 0; index < $scope.companies.length; index++) {
                if ($scope.companies[index].Value === $scope.comparisonForm.company) {
                    return $scope.companies[index];
                }
            }
            return null;
        }
        function getExpectedFileExtensions(company) {
            if (!company || !company.FileExtension) {
                return [];
            }
            return company.FileExtension.split(',').map(function (extension) {
                return extension.trim().replace(/^\./, '').toLowerCase();
            });
        }
        function resetUploadResult() {
            $scope.uploadResult = null;
            $scope.uploadSummary = null;
            $scope.excelErrorMessage = '';
            $scope.uploadData = [];
            $scope.resultRows = [];
            $scope.downloadInfo = null;
            setUploadModalStaticBackdrop(false);
        }
        function buildSearchRequest() {
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
        function updateRecordsInfo() {
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
        function loadData() {
            $scope.loading = true;
            $http.post(Router.action('ReconciliationLogistics', 'Search'), buildSearchRequest()).then(function (response) {
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
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize, 10)) || 0;
                $scope.isSearched = true;
                updateRecordsInfo();
            }).catch(function () {
                showError('查詢失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
        }
        function loadCompanies() {
            $scope.optionsLoading = true;
            $http.get(Router.action('ReconciliationLogistics', 'GetCompanies'))
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '載入物流公司失敗');
                    return;
                }
                $scope.companies = response.data.ReturnObject;
            }).catch(function () {
                showError('載入物流公司失敗，請稍後再試');
            }).finally(function () {
                loadStatuses();
            });
        }
        function loadStatuses() {
            $http.get(Router.action('ReconciliationLogistics', 'GetStatuses'))
                .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '載入狀態選項失敗');
                    return;
                }
                $scope.statuses = response.data.ReturnObject;
            }).catch(function () {
                showError('載入狀態選項失敗，請稍後再試');
            }).finally(function () {
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
        $scope.init = function () {
            angular.element('#ReconciliationLogistics').addClass('active');
            loadCompanies();
        };
        $scope.openSearchStartDatePopup = function () {
            $scope.searchStartDatePopup.opened = true;
        };
        $scope.openSearchEndDatePopup = function () {
            $scope.searchEndDatePopup.opened = true;
        };
        $scope.openUploadDatePopup = function () {
            $scope.uploadDatePopup.opened = true;
        };
        $scope.openRetryStartDatePopup = function () {
            $scope.retryStartDatePopup.opened = true;
        };
        $scope.openRetryEndDatePopup = function () {
            $scope.retryEndDatePopup.opened = true;
        };
        $scope.search = function () {
            if (!validateSearchDates()) {
                return;
            }
            $scope.currentPage = 1;
            loadData();
        };
        $scope.clearSearch = function () {
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
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            if ($scope.isSearched) {
                loadData();
            }
        };
        $scope.goToPage = function (page) {
            if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
                return;
            }
            $scope.currentPage = page;
            loadData();
        };
        $scope.previousPage = function () {
            $scope.goToPage($scope.currentPage - 1);
        };
        $scope.nextPage = function () {
            $scope.goToPage($scope.currentPage + 1);
        };
        $scope.getPageNumbers = function () {
            var pages = [];
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
        $scope.openUploadModal = function () {
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
        $scope.openComparisonModal = function () {
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
        $scope.$watch('comparisonForm.company', function (newValue, oldValue) {
            var selectedCompany = getSelectedComparisonCompany();
            $scope.comparisonAcceptedFileExtension = selectedCompany
                ? selectedCompany.FileExtension
                : '.xlsx,.csv';
            if (newValue !== oldValue) {
                clearSelectedComparisonFile();
                $scope.comparisonDownloadInfo = null;
            }
        });
        $scope.compareDetail = function () {
            if (!$scope.comparisonForm.company) {
                showError('請選擇物流公司');
                return;
            }
            var fileInput = document.getElementById('reconciliationLogisticsComparisonFile');
            var file = fileInput && fileInput.files && fileInput.files.length
                ? fileInput.files[0]
                : null;
            if (!file) {
                showError('請選擇檔案');
                return;
            }
            var selectedCompany = getSelectedComparisonCompany();
            var expectedExtensions = getExpectedFileExtensions(selectedCompany);
            var expectedExtension = expectedExtensions.join(', ');
            var extension = file.name.split('.').pop().toLowerCase();
            if (!expectedExtensions.length || expectedExtensions.indexOf(extension) < 0) {
                clearSelectedComparisonFile();
                showError((selectedCompany ? selectedCompany.Text : '物流公司') +
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
            }).then(function (response) {
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
            }).catch(function () {
                showError('比對明細產生失敗，請稍後再試');
            }).finally(function () {
                $scope.comparing = false;
                clearSelectedComparisonFile();
            });
        };
        $scope.downloadComparisonExcel = function () {
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
        $scope.openRetryModal = function () {
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
        $scope.retryReconcile = function () {
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
            }).then(function (response) {
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
            }).catch(function () {
                showError('重新銷帳失敗，請稍後再試');
            }).finally(function () {
                $scope.retrying = false;
            });
        };
        $scope.$watch('uploadForm.company', function (newValue, oldValue) {
            var selectedCompany = getSelectedUploadCompany();
            $scope.acceptedFileExtension = selectedCompany
                ? selectedCompany.FileExtension
                : '.xlsx,.csv';
            if (newValue !== oldValue) {
                clearSelectedFile();
                resetUploadResult();
            }
        });
        $scope.uploadFile = function () {
            if (!$scope.uploadForm.repaymentDate) {
                showError('請選擇回款日期');
                return;
            }
            if (!$scope.uploadForm.company) {
                showError('請選擇物流公司');
                return;
            }
            var fileInput = document.getElementById('reconciliationLogisticsFile');
            var file = fileInput && fileInput.files && fileInput.files.length
                ? fileInput.files[0]
                : null;
            if (!file) {
                showError('請選擇檔案');
                return;
            }
            var selectedCompany = getSelectedUploadCompany();
            var expectedExtensions = getExpectedFileExtensions(selectedCompany);
            var expectedExtension = expectedExtensions.join(', ');
            var extension = file.name.split('.').pop().toLowerCase();
            if (!expectedExtensions.length || expectedExtensions.indexOf(extension) < 0) {
                clearSelectedFile();
                showError((selectedCompany ? selectedCompany.Text : '物流公司') +
                    '上傳檔案副檔名需為 ' +
                    expectedExtension);
                return;
            }
            var formData = new FormData();
            formData.append('file', file);
            formData.append('company', $scope.uploadForm.company);
            formData.append('repaymentDate', moment($scope.uploadForm.repaymentDate).format('YYYY-MM-DD'));
            $scope.uploading = true;
            resetUploadResult();
            $http.post(Router.action('ReconciliationLogistics', 'Upload'), formData, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                var hasUploadResult = !!response.data.ReturnObject;
                var result = response.data.ReturnObject || {};
                // 欄位驗證失敗與銷帳結果分開保存，彈窗只顯示需要處理的異常資料。
                $scope.uploadData = result.Data || [];
                $scope.resultRows = (result.Results || []).filter(function (item) {
                    return item.IsException;
                });
                // 統計數字使用本次完整上傳結果，不受畫面僅顯示異常資料影響。
                $scope.uploadSummary = hasUploadResult
                    ? {
                        Count: result.Count || 0,
                        UpdatedCount: result.UpdatedCount || 0,
                        ExceptionCount: (result.FailCount || 0) +
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
                var message = result.Message || response.data.msg || (isSuccess ? '上傳成功' : '上傳失敗');
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
            }).catch(function () {
                $scope.uploadSummary = null;
                $scope.excelErrorMessage = '';
                $scope.uploadResult = {
                    success: false,
                    message: '系統發生錯誤，請稍後再試'
                };
                showError($scope.uploadResult.message);
            }).finally(function () {
                $scope.uploading = false;
                clearSelectedFile();
            });
        };
        $scope.downloadExcel = function () {
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
