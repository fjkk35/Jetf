// <reference path="../../types/global.d.ts" />
mainApp.controller('ReceivableController', ['$scope', '$http', function ($scope, $http) {
        function redirectIfNeeded(response) {
            if (response && response.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return true;
            }
            return false;
        }
        function showError(message) {
            swal({ title: message, icon: 'error' });
        }
        function toAmount(value) {
            var amount = Number(value);
            return isFinite(amount) ? amount : 0;
        }
        function validateEditAmounts(form) {
            var fields = [
                { name: '跟廠商收', value: form.CustomerCod },
                { name: '跟派件收', value: form.TransCod },
                { name: '捷豐支付', value: form.JetfPayment },
                { name: '報關費', value: form.Ccfee },
                { name: '到付款', value: form.Cod },
                { name: '手續費', value: form.Fee }
            ];
            for (var index = 0; index < fields.length; index++) {
                var rawValue = fields[index].value;
                if (rawValue === null || rawValue === undefined || rawValue === '') {
                    showError(fields[index].name + '金額必須為非負整數');
                    return false;
                }
                var value = Number(rawValue);
                if (!isFinite(value) || value < 0 || Math.floor(value) !== value) {
                    showError(fields[index].name + '金額必須為非負整數');
                    return false;
                }
            }
            var total = Number(form.CustomerCod) + Number(form.TransCod) + Number(form.JetfPayment);
            if (total !== form.OriginalCollectionTotal) {
                showError('跟廠商收、跟派件收、捷豐支付合計必須與原始金額 ' +
                    form.OriginalCollectionTotal.toLocaleString() + ' 相同');
                return false;
            }
            return true;
        }
        function parseNullableNumber(value) {
            return value ? parseInt(value, 10) : null;
        }
        function today() {
            var value = new Date();
            value.setHours(0, 0, 0, 0);
            return value;
        }
        function formatDate(value) {
            return value ? moment(value).format('YYYY-MM-DD') : null;
        }
        function validateDates() {
            if (!$scope.searchForm.outDateStart || !$scope.searchForm.outDateEnd) {
                showError('日期為必填，請選擇開始日期與結束日期');
                return false;
            }
            if (moment($scope.searchForm.outDateStart).isAfter($scope.searchForm.outDateEnd, 'day')) {
                showError('開始日期不可晚於結束日期');
                return false;
            }
            return true;
        }
        function selectedCodes(selectionMap) {
            var codes = [];
            angular.forEach(selectionMap, function (selected, code) {
                if (selected) {
                    codes.push(code);
                }
            });
            return codes.sort();
        }
        function buildRequest(includePaging) {
            var codes = selectedCodes($scope.selectedCustomerMap);
            var request = {
                OutDateStart: formatDate($scope.searchForm.outDateStart),
                OutDateEnd: formatDate($scope.searchForm.outDateEnd),
                CustomerCodes: codes.length ? codes : null,
                TrackingNo: $scope.searchForm.trackingNo,
                DlvInv: $scope.searchForm.dlvInv,
                Status: parseNullableNumber($scope.searchForm.status),
                CollectionType: parseNullableNumber($scope.searchForm.collectionType)
            };
            if (includePaging) {
                request.Page = $scope.currentPage;
                request.PageSize = parseInt($scope.pageSize, 10);
            }
            return request;
        }
        function updateRecordsInfo() {
            if ($scope.totalCount === 0) {
                $scope.recordsInfo = '共 0 筆';
                return;
            }
            var pageSize = parseInt($scope.pageSize, 10);
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示 ' + start + ' 至 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
        }
        function loadData() {
            $scope.loading = true;
            $http.post(Router.action('Receivable', 'Search'), buildRequest(true))
                .then(function (response) {
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
                if ($scope.totalPages > 0 && $scope.currentPage > $scope.totalPages) {
                    $scope.currentPage = $scope.totalPages;
                    loadData();
                }
            }).catch(function () {
                showError('查詢失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
        }
        $scope.searchForm = {
            outDateStart: today(),
            outDateEnd: today(),
            trackingNo: '',
            dlvInv: '',
            status: '',
            collectionType: ''
        };
        $scope.dateOptions = {
            startingDay: 1,
            showWeeks: false
        };
        $scope.startDatePopup = { opened: false };
        $scope.endDatePopup = { opened: false };
        $scope.rows = [];
        $scope.loading = false;
        $scope.exporting = false;
        $scope.isSearched = false;
        $scope.currentPage = 1;
        $scope.pageSize = '20';
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.recordsInfo = '';
        $scope.selectedCustomerMap = {};
        $scope.editingRow = null;
        $scope.editForm = null;
        $scope.savingEdit = false;
        $scope.init = function () {
            angular.element('#Receivable').addClass('active');
        };
        $scope.openStartDatePopup = function () {
            $scope.startDatePopup.opened = true;
        };
        $scope.openEndDatePopup = function () {
            $scope.endDatePopup.opened = true;
        };
        $scope.search = function () {
            if (!validateDates()) {
                return;
            }
            $scope.currentPage = 1;
            loadData();
        };
        $scope.clearSearch = function () {
            $scope.searchForm = {
                outDateStart: today(),
                outDateEnd: today(),
                trackingNo: '',
                dlvInv: '',
                status: '',
                collectionType: ''
            };
            $scope.selectedCustomerMap = {};
            $scope.currentPage = 1;
            loadData();
        };
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            loadData();
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
        $scope.exportExcel = function () {
            if (!validateDates()) {
                return;
            }
            var request = buildRequest(false);
            $scope.exporting = true;
            $http.post(Router.action('Receivable', 'ExportExcel'), request)
                .then(function (response) {
                var data = response.data || {};
                if (redirectIfNeeded(data)) {
                    return;
                }
                if (data.msg) {
                    showError(data.msg);
                    return;
                }
                if (data.fileGuid && data.fileName) {
                    var downloadUrl = Router.action('Download', 'DownloadFile')
                        + '?fileGuid=' + data.fileGuid
                        + '&fileName=' + encodeURIComponent(data.fileName);
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = data.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
            }).catch(function () {
                showError('下載失敗，請稍後再試');
            }).finally(function () {
                $scope.exporting = false;
            });
        };
        $scope.openEdit = function (row) {
            $scope.editingRow = row;
            $scope.editForm = {
                Id: row.Id,
                CustomerCod: toAmount(row.CustomerCod),
                TransCod: toAmount(row.TransCod),
                JetfPayment: toAmount(row.JetfPayment),
                Ccfee: toAmount(row.Ccfee),
                Cod: toAmount(row.Cod),
                Fee: toAmount(row.Fee),
                UnreceivedReason: row.UnreceivedReason || '',
                OriginalCollectionTotal: toAmount(row.CustomerCod) +
                    toAmount(row.TransCod) +
                    toAmount(row.JetfPayment)
            };
            $scope.savingEdit = false;
            $('#receivableEditModal').modal('show');
        };
        $scope.closeEdit = function () {
            $('#receivableEditModal').modal('hide');
            $scope.editingRow = null;
            $scope.editForm = null;
        };
        $scope.saveEdit = function () {
            var form = $scope.editForm;
            if (!form || !validateEditAmounts(form)) {
                return;
            }
            $scope.savingEdit = true;
            $http.post(Router.action('Receivable', 'Update'), {
                Id: form.Id,
                CustomerCod: form.CustomerCod,
                TransCod: form.TransCod,
                JetfPayment: form.JetfPayment,
                Ccfee: form.Ccfee,
                Cod: form.Cod,
                Fee: form.Fee,
                UnreceivedReason: form.UnreceivedReason
            }).then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '修改失敗');
                    return;
                }
                $scope.closeEdit();
                swal({ title: '修改成功', icon: 'success' });
                loadData();
            }).catch(function () {
                showError('修改失敗，請稍後再試');
            }).finally(function () {
                $scope.savingEdit = false;
            });
        };
    }]);
