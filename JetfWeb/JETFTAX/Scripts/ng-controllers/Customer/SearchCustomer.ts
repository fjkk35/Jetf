// <reference path="../../types/global.d.ts" />

interface CustomerPageOption {
    Value: string;
    Text: string;
}

interface CustomerRow {
    Id: number;
    TranType: string;
    CustId: string;
    Customer: string;
    TransNo: string;
    TransName: string;
    IncludeTax: string;
    IncludeTaxName: string;
    CompanyNo: string;
    Company: string;
    CodFee: number | null;
    IsCainiaoP: boolean;
    IsCainiaoPText: string;
}

interface CustomerFormOptions {
    TranTypes: CustomerPageOption[];
    IncludeTaxes: CustomerPageOption[];
    Companies: CustomerPageOption[];
}

interface CustomerUpsertModel {
    Id: number | null;
    TranType: string | null;
    CustId: string | null;
    Customer: string;
    TransNo: string;
    TransName: string;
    IncludeTax: string | null;
    IncludeTaxName: string;
    CompanyNo: string | null;
    Company: string;
    CodFee: string;
    IsCainiaoP: boolean;
}

interface CustomerQueryResponse {
    TotalCount: number;
    Data: CustomerRow[];
}

interface SearchCustomerScope extends ng.IScope {
    formOptions: {
        tranTypes: CustomerPageOption[];
        includeTaxes: CustomerPageOption[];
        companies: CustomerPageOption[];
    };
    searchForm: {
        tranType: string;
        transKeyword: string;
        includeTax: string;
        companyNo: string;
        isCainiaoP: string;
    };
    currentPage: number;
    pageSize: string;
    totalCount: number;
    totalPages: number;
    recordsInfo: string;
    rows: CustomerRow[];
    loading: boolean;
    isSearched: boolean;
    exporting: boolean;
    customerSelectAll: boolean;
    selectedCustCodes: string[];
    customerDisplayText: string;
    customerDisplayFullText: string;
    modal: {
        mode: string;
        saving: boolean;
        customerOptions: CustomerPageOption[];
        form: CustomerUpsertModel;
    };
    init: () => void;
    search: () => void;
    clearSearch: () => void;
    changePageSize: () => void;
    goToPage: (page: number) => void;
    previousPage: () => void;
    nextPage: () => void;
    getPageNumbers: () => number[];
    openCreateModal: () => void;
    openEditModal: (id: number) => void;
    onModalTranTypeChange: () => void;
    onModalCustomerChange: () => void;
    onModalCompanyChange: () => void;
    saveCustomer: () => void;
    exportExcel: () => void;
}

mainApp.controller('SearchCustomerController', ['$scope', '$http', function (
    $scope: SearchCustomerScope,
    $http: ng.IHttpService
) {
    function setActiveMenu(): void {
        angular.element('#collapseUpload').addClass('show');
        angular.element('#SearchCustomer').addClass('active');
    }

    function createEmptyForm(): CustomerUpsertModel {
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

    function openLoginPage(): void {
        window.location.href = Router.action('Account', 'Login');
    }

    function parseCainiaoFilter(value: string): boolean | null {
        if (value === 'true') {
            return true;
        }

        if (value === 'false') {
            return false;
        }

        return null;
    }

    function buildSearchRequest(includePaging: boolean): any {
        var request: any = {
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

    function updateRecordsInfo(): void {
        if ($scope.totalCount === 0) {
            $scope.recordsInfo = '';
            return;
        }

        var pageSize = parseInt($scope.pageSize, 10);
        var start = ($scope.currentPage - 1) * pageSize + 1;
        var end = Math.min($scope.currentPage * pageSize, $scope.totalCount);
        $scope.recordsInfo = '顯示 ' + start + ' 到 ' + end + ' 筆，共 ' + $scope.totalCount + ' 筆';
    }

    function loadFormOptions(): void {
        $http.get(Router.action('Customer', 'GetFormOptions'))
            .then(function (response: ng.IHttpResponse<CustomerFormOptions & ApiResponse>) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }

                if ((response.data as ApiResponse).status === 'error') {
                    swal({
                        title: (response.data as ApiResponse).msg || '載入選項失敗',
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

    function loadCustomers(page?: number): void {
        if (page && page > 0) {
            $scope.currentPage = page;
        }

        $scope.loading = true;

        $http.post(Router.action('Customer', 'QueryCustomers'), buildSearchRequest(true))
            .then(function (response: ng.IHttpResponse<CustomerQueryResponse & ApiResponse>) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }

                if ((response.data as ApiResponse).status === 'error') {
                    swal({
                        title: (response.data as ApiResponse).msg || '查詢失敗',
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

    function appendHiddenInput(form: HTMLFormElement, name: string, value: string): void {
        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
    }

    function submitExportForm(): void {
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

    function loadModalCustomerOptions(tranType: string, selectedCustId?: string): void {
        if (!tranType) {
            $scope.modal.customerOptions = [];
            $scope.modal.form.CustId = null;
            $scope.modal.form.Customer = '';
            return;
        }

        $http.get(Router.action('Customer', 'GetCustomerOptions'), {
            params: { tranType: tranType }
        })
            .then(function (response: ng.IHttpResponse<CustomerPageOption[] & ApiResponse>) {
                if (response.data && (response.data as ApiResponse).Redirect) {
                    openLoginPage();
                    return;
                }

                if ((response.data as ApiResponse).status === 'error') {
                    swal({
                        title: (response.data as ApiResponse).msg || '載入客戶清單失敗',
                        icon: 'error'
                    });
                    return;
                }

                $scope.modal.customerOptions = response.data as unknown as CustomerPageOption[] || [];

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

    function validateModalForm(): string {
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

    function showModal(): void {
        $('#customerModal').modal('show');
    }

    function hideModal(): void {
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

    $scope.goToPage = function (page: number) {
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
        var pages: number[] = [];
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

    $scope.openEditModal = function (id: number) {
        $http.get(Router.action('Customer', 'GetCustomerDetail'), {
            params: { id: id }
        })
            .then(function (response: ng.IHttpResponse<CustomerUpsertModel & ApiResponse>) {
                if (response.data && response.data.Redirect) {
                    openLoginPage();
                    return;
                }

                if ((response.data as ApiResponse).status === 'error') {
                    swal({
                        title: (response.data as ApiResponse).msg || '載入資料失敗',
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
        var selected = null as CustomerPageOption | null;

        for (var i = 0; i < $scope.modal.customerOptions.length; i++) {
            if ($scope.modal.customerOptions[i].Value === $scope.modal.form.CustId) {
                selected = $scope.modal.customerOptions[i];
                break;
            }
        }

        $scope.modal.form.Customer = selected ? selected.Text : '';
    };

    $scope.onModalCompanyChange = function () {
        var selected = null as CustomerPageOption | null;

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
            .then(function (response: ng.IHttpResponse<ApiResponse>) {
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
        } finally {
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