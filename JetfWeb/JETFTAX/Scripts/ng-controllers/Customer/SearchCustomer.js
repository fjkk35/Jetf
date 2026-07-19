// <reference path="../../types/global.d.ts" />
mainApp.controller('SearchCustomerController', ['$scope', '$http', function ($scope, $http) {
        function setActiveMenu() {
            angular.element('#collapseUpload').addClass('show');
            angular.element('#SearchCustomer').addClass('active');
        }
        function createEmptyForm() {
            return {
                Id: null,
                TranType: null,
                CustId: null,
                Customer: '',
                TransNo: '',
                TransName: '',
                IncludeTax: null,
                IncludeTaxName: '',
                CompanyNo: null,
                Company: '',
                CodFee: '',
                IsCainiaoP: false
            };
        }
        function openLoginPage() {
            window.location.href = Router.action('Account', 'Login');
        }
        function parseCainiaoFilter(value) {
            if (value === 'true') {
                return true;
            }
            if (value === 'false') {
                return false;
            }
            return null;
        }
        function buildSearchRequest(includePaging) {
            var request = {
                TranType: $scope.searchForm.tranType,
                CustCodes: ($scope.selectedCustCodes && $scope.selectedCustCodes.length > 0) ? $scope.selectedCustCodes : null,
                TransKeyword: $scope.searchForm.transKeyword,
                IncludeTax: $scope.searchForm.includeTax,
                CompanyNo: $scope.searchForm.companyNo,
                IsCainiaoP: parseCainiaoFilter($scope.searchForm.isCainiaoP)
            };
            if (includePaging) {
                request.Page = $scope.currentPage;
                request.PageSize = parseInt($scope.pageSize, 10);
            }
            return request;
        }
        function updateRecordsInfo() {
            if ($scope.totalCount === 0) {
                $scope.recordsInfo = '';
                return;
            }
            var pageSize = parseInt($scope.pageSize, 10);
            var start = ($scope.currentPage - 1) * pageSize + 1;
            var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
            $scope.recordsInfo = '顯示 ' + start + ' 到 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
        }
        function loadFormOptions() {
            $http.get(Router.action('Customer', 'GetFormOptions'))
                .then(function (response) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'error') {
                    swal({
                        title: response.data.msg || '載入選項失敗',
                        icon: 'error'
                    });
                    return;
                }
                $scope.formOptions.tranTypes = response.data.TranTypes || [];
                $scope.formOptions.includeTaxes = response.data.IncludeTaxes || [];
                $scope.formOptions.companies = response.data.Companies || [];
            })
                .catch(function () {
                swal({
                    title: '載入查詢條件失敗',
                    icon: 'error'
                });
            });
        }
        function loadCustomers(page) {
            if (page && page > 0) {
                $scope.currentPage = page;
            }
            $scope.loading = true;
            $http.post(Router.action('Customer', 'QueryCustomers'), buildSearchRequest(true))
                .then(function (response) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'error') {
                    swal({
                        title: response.data.msg || '查詢失敗',
                        icon: 'error'
                    });
                    return;
                }
                $scope.rows = response.data.Data || [];
                $scope.totalCount = response.data.TotalCount || 0;
                $scope.totalPages = Math.ceil($scope.totalCount / parseInt($scope.pageSize, 10)) || 0;
                updateRecordsInfo();
                if ($scope.totalPages > 0 && $scope.currentPage > $scope.totalPages) {
                    loadCustomers($scope.totalPages);
                }
            })
                .catch(function () {
                swal({
                    title: '查詢失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        }
        function appendHiddenInput(form, name, value) {
            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = name;
            input.value = value;
            form.appendChild(input);
        }
        function submitExportForm() {
            var request = buildSearchRequest(false);
            var form = document.createElement('form');
            form.method = 'POST';
            form.action = Router.action('Customer', 'ExportExcel');
            form.target = '_blank';
            Object.keys(request).forEach(function (key) {
                var value = request[key];
                if (value === null || value === undefined || value === '') {
                    return;
                }
                if (Array.isArray(value)) {
                    value.forEach(function (item) {
                        appendHiddenInput(form, key, item);
                    });
                    return;
                }
                appendHiddenInput(form, key, value.toString());
            });
            document.body.appendChild(form);
            form.submit();
            document.body.removeChild(form);
        }
        function loadModalCustomerOptions(tranType, selectedCustId) {
            if (!tranType) {
                $scope.modal.customerOptions = [];
                $scope.modal.form.CustId = null;
                $scope.modal.form.Customer = '';
                return;
            }
            $http.get(Router.action('Customer', 'GetCustomerOptions'), {
                params: { tranType: tranType }
            })
                .then(function (response) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'error') {
                    swal({
                        title: response.data.msg || '載入客戶清單失敗',
                        icon: 'error'
                    });
                    return;
                }
                $scope.modal.customerOptions = response.data || [];
                if (selectedCustId) {
                    $scope.modal.form.CustId = selectedCustId;
                    $scope.onModalCustomerChange();
                }
            })
                .catch(function () {
                swal({
                    title: '載入客戶清單失敗',
                    icon: 'error'
                });
            });
        }
        function validateModalForm() {
            if (!$scope.modal.form.TranType) {
                return '請選擇運送類型';
            }
            if (!$scope.modal.form.CustId) {
                return '請選擇客戶';
            }
            if ($scope.modal.form.TranType === '空運' && !$scope.modal.form.TransNo) {
                return '空運需輸入派件公司編號';
            }
            if (!$scope.modal.form.TransName) {
                return '請輸入派件公司';
            }
            if (!$scope.modal.form.IncludeTax) {
                return '請選擇是否包稅';
            }
            if (!$scope.modal.form.CompanyNo) {
                return '請選擇物流公司';
            }
            if (!$scope.modal.form.CodFee) {
                return '請輸入手續費';
            }
            return '';
        }
        function showModal() {
            $('#customerModal').modal('show');
        }
        function hideModal() {
            $('#customerModal').modal('hide');
        }
        $scope.formOptions = {
            tranTypes: [],
            includeTaxes: [],
            companies: []
        };
        $scope.searchForm = {
            tranType: '',
            transKeyword: '',
            includeTax: '',
            companyNo: '',
            isCainiaoP: ''
        };
        $scope.currentPage = 1;
        $scope.pageSize = '10';
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.recordsInfo = '';
        $scope.rows = [];
        $scope.loading = false;
        $scope.isSearched = false;
        $scope.exporting = false;
        $scope.customerSelectAll = true;
        $scope.selectedCustCodes = [];
        $scope.customerDisplayText = '';
        $scope.customerDisplayFullText = '';
        $scope.modal = {
            mode: 'create',
            saving: false,
            customerOptions: [],
            form: createEmptyForm()
        };
        $scope.init = function () {
            setActiveMenu();
            loadFormOptions();
        };
        $scope.search = function () {
            $scope.currentPage = 1;
            $scope.isSearched = true;
            loadCustomers(1);
        };
        $scope.clearSearch = function () {
            $scope.searchForm = {
                tranType: '',
                transKeyword: '',
                includeTax: '',
                companyNo: '',
                isCainiaoP: ''
            };
            $scope.customerSelectAll = true;
            $scope.selectedCustCodes = [];
            $scope.customerDisplayText = '';
            $scope.customerDisplayFullText = '';
            $scope.rows = [];
            $scope.currentPage = 1;
            $scope.totalCount = 0;
            $scope.totalPages = 0;
            $scope.recordsInfo = '';
            $scope.isSearched = false;
        };
        $scope.changePageSize = function () {
            $scope.currentPage = 1;
            if ($scope.isSearched) {
                loadCustomers(1);
            }
        };
        $scope.goToPage = function (page) {
            if (page < 1 || page > $scope.totalPages || page === $scope.currentPage) {
                return;
            }
            loadCustomers(page);
        };
        $scope.previousPage = function () {
            if ($scope.currentPage > 1) {
                $scope.currentPage--;
                loadCustomers();
            }
        };
        $scope.nextPage = function () {
            if ($scope.currentPage < $scope.totalPages) {
                $scope.currentPage++;
                loadCustomers();
            }
        };
        $scope.getPageNumbers = function () {
            var pages = [];
            var maxVisible = 10;
            var startPage = Math.max(1, $scope.currentPage - Math.floor(maxVisible / 2));
            var endPage = Math.min($scope.totalPages, startPage + maxVisible - 1);
            if (endPage - startPage < maxVisible - 1) {
                startPage = Math.max(1, endPage - maxVisible + 1);
            }
            for (var index = startPage; index <= endPage; index++) {
                pages.push(index);
            }
            return pages;
        };
        $scope.openCreateModal = function () {
            $scope.modal.mode = 'create';
            $scope.modal.saving = false;
            $scope.modal.customerOptions = [];
            $scope.modal.form = createEmptyForm();
            showModal();
        };
        $scope.openEditModal = function (id) {
            $http.get(Router.action('Customer', 'GetCustomerDetail'), {
                params: { id: id }
            })
                .then(function (response) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'error') {
                    swal({
                        title: response.data.msg || '載入資料失敗',
                        icon: 'error'
                    });
                    return;
                }
                $scope.modal.mode = 'edit';
                $scope.modal.saving = false;
                $scope.modal.form = angular.extend(createEmptyForm(), response.data);
                $scope.modal.customerOptions = [];
                loadModalCustomerOptions($scope.modal.form.TranType, $scope.modal.form.CustId);
                showModal();
            })
                .catch(function () {
                swal({
                    title: '載入客戶資料失敗',
                    icon: 'error'
                });
            });
        };
        $scope.onModalTranTypeChange = function () {
            $scope.modal.form.CustId = null;
            $scope.modal.form.Customer = '';
            if ($scope.modal.form.TranType !== '空運') {
                $scope.modal.form.TransNo = '';
            }
            loadModalCustomerOptions($scope.modal.form.TranType);
        };
        $scope.onModalCustomerChange = function () {
            var selected = null;
            for (var i = 0; i < $scope.modal.customerOptions.length; i++) {
                if ($scope.modal.customerOptions[i].Value === $scope.modal.form.CustId) {
                    selected = $scope.modal.customerOptions[i];
                    break;
                }
            }
            $scope.modal.form.Customer = selected ? selected.Text : '';
        };
        $scope.onModalCompanyChange = function () {
            var selected = null;
            for (var i = 0; i < $scope.formOptions.companies.length; i++) {
                if ($scope.formOptions.companies[i].Value === $scope.modal.form.CompanyNo) {
                    selected = $scope.formOptions.companies[i];
                    break;
                }
            }
            $scope.modal.form.Company = selected ? selected.Text : '';
        };
        $scope.saveCustomer = function () {
            var validationMessage = validateModalForm();
            if (validationMessage) {
                swal({
                    title: validationMessage,
                    icon: 'error'
                });
                return;
            }
            $scope.modal.saving = true;
            $http.post(Router.action('Customer', 'SaveCustomer'), $scope.modal.form)
                .then(function (response) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }
                if (response.data.status === 'success') {
                    swal({
                        title: response.data.msg || '儲存成功',
                        icon: 'success'
                    });
                    hideModal();
                    if ($scope.isSearched) {
                        loadCustomers($scope.currentPage);
                    }
                    return;
                }
                swal({
                    title: response.data.msg || '儲存失敗',
                    icon: 'error'
                });
            })
                .catch(function () {
                swal({
                    title: '儲存失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.modal.saving = false;
            });
        };
        $scope.exportExcel = function () {
            $scope.exporting = true;
            try {
                submitExportForm();
            }
            finally {
                $scope.exporting = false;
            }
        };
        $('#customerModal').on('hidden.bs.modal', function () {
            $scope.$applyAsync(function () {
                $scope.modal.saving = false;
                $scope.modal.customerOptions = [];
                $scope.modal.form = createEmptyForm();
            });
        });
        $scope.init();
    }]);
