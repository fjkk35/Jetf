// <reference path="../../types/global.d.ts" />

interface ReconciliationCustomerGroupRow {
    Id: number;
    Type: string;
    TypeName: string;
    GroupName: string;
    CustomerDisplay: string;
}

interface ReconciliationCustomerGroupOption {
    Id: number;
    GroupName: string;
}

interface ReconciliationCustomerOption {
    CustCode: string;
    CustName: string;
    IsSelected: boolean;
    IsDisabled: boolean;
    AssignedGroupName: string;
}

interface ReconciliationCustomerGroupForm {
    Id: number | null;
    Type: string;
    GroupName: string;
    CustCodes: string[];
}

interface ReconciliationCustomerGroupScope extends ng.IScope {
    searchForm: {
        Type: string;
        GroupName: string;
    };
    groupOptions: ReconciliationCustomerGroupOption[];
    rows: ReconciliationCustomerGroupRow[];
    loading: boolean;
    modal: {
        form: ReconciliationCustomerGroupForm;
        customers: ReconciliationCustomerOption[];
        customerKeyword: string;
        loading: boolean;
        saving: boolean;
    };
    init: () => void;
    search: () => void;
    clearSearch: () => void;
    onSearchTypeChange: () => void;
    openCreateModal: () => void;
    openEditModal: (id: number) => void;
    onModalTypeChange: () => void;
    getSelectedCount: () => number;
    saveGroup: () => void;
    deleteGroup: (row: ReconciliationCustomerGroupRow) => void;
}

mainApp.controller('ReconciliationCustomerGroupController', ['$scope', '$http', function (
    $scope: ReconciliationCustomerGroupScope,
    $http: ng.IHttpService
) {
    function emptyForm(): ReconciliationCustomerGroupForm {
        return {
            Id: null,
            Type: '',
            GroupName: '',
            CustCodes: []
        };
    }

    function redirectIfNeeded(response: ApiResponse): boolean {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }

        return false;
    }

    function showError(message: string): void {
        swal({ title: message, icon: 'error' });
    }

    function loadGroupOptions(): void {
        $http.get(Router.action('ReconciliationCustomerGroup', 'GetGroupOptions'), {
            params: { type: $scope.searchForm.Type }
        }).then(function (response: ng.IHttpResponse<ApiResponse<ReconciliationCustomerGroupOption[]>>): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error') {
                showError(response.data.msg || '載入客戶群組失敗');
                return;
            }

            $scope.groupOptions = response.data.ReturnObject || [];
        }).catch(function (): void {
            showError('載入客戶群組失敗');
        });
    }

    function loadCustomerOptions(type: string, id: number | null): void {
        if (!type) {
            $scope.modal.customers = [];
            return;
        }

        $scope.modal.loading = true;
        $http.get(Router.action('ReconciliationCustomerGroup', 'GetCustomerOptions'), {
            params: { type: type, id: id }
        }).then(function (response: ng.IHttpResponse<ApiResponse<ReconciliationCustomerOption[]>>): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error') {
                showError(response.data.msg || '載入客戶資料失敗');
                return;
            }

            $scope.modal.customers = response.data.ReturnObject || [];
        }).catch(function (): void {
            showError('載入客戶資料失敗');
        }).finally(function (): void {
            $scope.modal.loading = false;
        });
    }

    function hideModal(): void {
        $('#reconciliationCustomerGroupModal').modal('hide');
    }

    function validateForm(): string {
        if (!$scope.modal.form.Type) {
            return '請選擇類型';
        }

        if (!$scope.modal.form.GroupName || !$scope.modal.form.GroupName.trim()) {
            return '請輸入群組名稱';
        }

        if ($scope.modal.form.GroupName.trim().length > 20) {
            return '群組名稱不可超過 20 個字元';
        }

        if ($scope.getSelectedCount() === 0) {
            return '請至少選擇一個客戶';
        }

        return '';
    }

    $scope.searchForm = { Type: '', GroupName: '' };
    $scope.groupOptions = [];
    $scope.rows = [];
    $scope.loading = false;
    $scope.modal = {
        form: emptyForm(),
        customers: [],
        customerKeyword: '',
        loading: false,
        saving: false
    };

    $scope.init = function (): void {
        angular.element('#ReconciliationCustomerGroup').addClass('active');
        loadGroupOptions();
        $scope.search();
    };

    $scope.search = function (): void {
        $scope.loading = true;
        $http.post(Router.action('ReconciliationCustomerGroup', 'Search'), $scope.searchForm)
            .then(function (response: ng.IHttpResponse<ApiResponse<ReconciliationCustomerGroupRow[]>>): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status === 'error') {
                    showError(response.data.msg || '查詢失敗');
                    return;
                }

                $scope.rows = response.data.ReturnObject || [];
            }).catch(function (): void {
                showError('查詢失敗，請稍後再試');
            }).finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.clearSearch = function (): void {
        $scope.searchForm = { Type: '', GroupName: '' };
        loadGroupOptions();
        $scope.search();
    };

    $scope.onSearchTypeChange = function (): void {
        $scope.searchForm.GroupName = '';
        loadGroupOptions();
    };

    $scope.openCreateModal = function (): void {
        $scope.modal.form = emptyForm();
        $scope.modal.customers = [];
        $scope.modal.customerKeyword = '';
        $scope.modal.loading = false;
        $scope.modal.saving = false;
        $('#reconciliationCustomerGroupModal').modal('show');
    };

    $scope.openEditModal = function (id: number): void {
        $scope.modal.loading = true;
        $http.get(Router.action('ReconciliationCustomerGroup', 'GetDetail'), {
            params: { id: id }
        }).then(function (response: ng.IHttpResponse<ApiResponse<ReconciliationCustomerGroupForm>>): void {
            if (redirectIfNeeded(response.data)) {
                $scope.modal.loading = false;
                return;
            }

            if (response.data.status === 'error' || !response.data.ReturnObject) {
                $scope.modal.loading = false;
                showError(response.data.msg || '載入客戶群組失敗');
                return;
            }

            $scope.modal.form = response.data.ReturnObject;
            $scope.modal.customers = [];
            $scope.modal.customerKeyword = '';
            $scope.modal.saving = false;
            $('#reconciliationCustomerGroupModal').modal('show');
            loadCustomerOptions($scope.modal.form.Type, $scope.modal.form.Id);
        }).catch(function (): void {
            $scope.modal.loading = false;
            showError('載入客戶群組失敗');
        });
    };

    $scope.onModalTypeChange = function (): void {
        $scope.modal.customers = [];
        $scope.modal.customerKeyword = '';
        loadCustomerOptions($scope.modal.form.Type, $scope.modal.form.Id);
    };

    $scope.getSelectedCount = function (): number {
        return $scope.modal.customers.filter(function (customer): boolean {
            return customer.IsSelected && !customer.IsDisabled;
        }).length;
    };

    $scope.saveGroup = function (): void {
        var validationMessage = validateForm();
        if (validationMessage) {
            showError(validationMessage);
            return;
        }

        $scope.modal.form.CustCodes = $scope.modal.customers
            .filter(function (customer): boolean {
                return customer.IsSelected && !customer.IsDisabled;
            })
            .map(function (customer): string {
                return customer.CustCode;
            });
        $scope.modal.saving = true;

        $http.post(Router.action('ReconciliationCustomerGroup', 'Save'), $scope.modal.form)
            .then(function (response: ng.IHttpResponse<ApiResponse>): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status !== 'success') {
                    showError(response.data.msg || '儲存失敗');
                    return;
                }

                swal({ title: response.data.msg || '儲存成功', icon: 'success' });
                $scope.searchForm.Type = $scope.modal.form.Type;
                $scope.searchForm.GroupName = $scope.modal.form.GroupName.trim();
                hideModal();
                loadGroupOptions();
                $scope.search();
            }).catch(function (): void {
                showError('儲存失敗，請稍後再試');
            }).finally(function (): void {
                $scope.modal.saving = false;
            });
    };

    $scope.deleteGroup = function (row: ReconciliationCustomerGroupRow): void {
        if (!window.confirm('確定要刪除客戶群組「' + row.GroupName + '」？')) {
            return;
        }

        $http.post(Router.action('ReconciliationCustomerGroup', 'Delete'), { id: row.Id })
            .then(function (response: ng.IHttpResponse<ApiResponse>): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status !== 'success') {
                    showError(response.data.msg || '刪除失敗');
                    return;
                }

                swal({ title: response.data.msg || '刪除成功', icon: 'success' });
                if ($scope.searchForm.GroupName === row.GroupName) {
                    $scope.searchForm.GroupName = '';
                }
                loadGroupOptions();
                $scope.search();
            }).catch(function (): void {
                showError('刪除失敗，請稍後再試');
            });
    };

    $('#reconciliationCustomerGroupModal').on('hidden.bs.modal', function (): void {
        $scope.$applyAsync(function (): void {
            $scope.modal.form = emptyForm();
            $scope.modal.customers = [];
            $scope.modal.customerKeyword = '';
            $scope.modal.loading = false;
            $scope.modal.saving = false;
        });
    });
}]);
