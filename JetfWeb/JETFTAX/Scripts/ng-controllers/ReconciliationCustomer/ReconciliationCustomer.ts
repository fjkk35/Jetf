// <reference path="../../types/global.d.ts" />

interface ReconciliationCustomerSelectionMap {
    [custCode: string]: boolean;
}

interface ReconciliationCustomerQueryResult {
    ReceivableAmount: number;
}

interface ReconciliationCustomerScope extends ng.IScope {
    searchForm: {
        outDateStart: Date | null;
        outDateEnd: Date | null;
        dlvInvText: string;
    };
    selectedCustomerMap: ReconciliationCustomerSelectionMap;
    dateOptions: any;
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    result: ReconciliationCustomerQueryResult;
    confirmAmount: number | null;
    loading: boolean;
    confirming: boolean;
    isSearched: boolean;
    init: () => void;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    search: () => void;
    clearSearch: () => void;
    openConfirmModal: () => void;
    confirm: () => void;
}

mainApp.controller('ReconciliationCustomerController', ['$scope', '$http', function (
    $scope: ReconciliationCustomerScope,
    $http: ng.IHttpService
) {
    var lastQuery: any = null;

    function today(): Date {
        var value = new Date();
        value.setHours(0, 0, 0, 0);
        return value;
    }

    function formatDate(value: Date | null): string | null {
        return value ? moment(value).format('YYYY-MM-DD') : null;
    }

    function showError(message: string): void {
        swal({ title: message, icon: 'error' });
    }

    function redirectIfNeeded(response: ApiResponse): boolean {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }

        return false;
    }

    function validateDates(): boolean {
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

    function selectedCodes(): string[] {
        var codes: string[] = [];
        angular.forEach($scope.selectedCustomerMap, function (
            selected: boolean,
            code: string
        ): void {
            if (selected) {
                codes.push(code);
            }
        });
        return codes.sort();
    }

    function buildRequest(): any {
        var customerCodes = selectedCodes();
        return {
            OutDateStart: formatDate($scope.searchForm.outDateStart),
            OutDateEnd: formatDate($scope.searchForm.outDateEnd),
            CustomerCodes: customerCodes.length ? customerCodes : null,
            DlvInvText: ($scope.searchForm.dlvInvText || '').trim() || null
        };
    }

    function loadData(): void {
        var request = buildRequest();
        $scope.loading = true;
        $http.post(Router.action('ReconciliationCustomer', 'Search'), request)
            .then(function (
                response: ng.IHttpResponse<ApiResponse<ReconciliationCustomerQueryResult>>
            ): void {
                if (redirectIfNeeded(response.data)) {
                    return;
                }

                if (response.data.status === 'error' || !response.data.ReturnObject) {
                    showError(response.data.msg || '查詢失敗');
                    return;
                }

                $scope.result = response.data.ReturnObject;
                $scope.isSearched = true;
                lastQuery = angular.copy(request);
            }).catch(function (): void {
                showError('查詢失敗，請稍後再試');
            }).finally(function (): void {
                $scope.loading = false;
            });
    }

    $scope.searchForm = {
        outDateStart: today(),
        outDateEnd: today(),
        dlvInvText: ''
    };
    $scope.selectedCustomerMap = {};
    $scope.dateOptions = {
        startingDay: 1,
        showWeeks: false
    };
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.result = { ReceivableAmount: 0 };
    $scope.confirmAmount = null;
    $scope.loading = false;
    $scope.confirming = false;
    $scope.isSearched = false;

    $scope.init = function (): void {
        angular.element('#ReconciliationCustomer').addClass('active');
    };

    $scope.openStartDatePopup = function (): void {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function (): void {
        $scope.endDatePopup.opened = true;
    };

    $scope.search = function (): void {
        if (!validateDates()) {
            return;
        }

        loadData();
    };

    $scope.clearSearch = function (): void {
        $scope.searchForm = {
            outDateStart: today(),
            outDateEnd: today(),
            dlvInvText: ''
        };
        $scope.selectedCustomerMap = {};
        loadData();
    };

    $scope.openConfirmModal = function (): void {
        if (!$scope.isSearched || !lastQuery || $scope.result.ReceivableAmount <= 0) {
            showError('目前沒有可銷帳的應收金額');
            return;
        }

        $scope.confirmAmount = null;
        (<any>$('#reconciliationCustomerConfirmModal')).modal('show');
    };

    $scope.confirm = function (): void {
        var amount = $scope.confirmAmount;
        if (!amount || amount <= 0 || Math.floor(amount) !== amount) {
            showError('請輸入大於 0 的整數銷帳金額');
            return;
        }

        if (amount !== $scope.result.ReceivableAmount) {
            showError('銷帳金額必須與應收金額相同');
            return;
        }

        $scope.confirming = true;
        $http.post(Router.action('ReconciliationCustomer', 'Confirm'), {
            Query: lastQuery,
            Amount: amount
        }).then(function (response: ng.IHttpResponse<ApiResponse>): void {
            if (redirectIfNeeded(response.data)) {
                return;
            }

            if (response.data.status === 'error') {
                showError(response.data.msg || '銷帳失敗');
                return;
            }

            (<any>$('#reconciliationCustomerConfirmModal')).modal('hide');
            swal({ title: response.data.msg || '客戶銷帳完成', icon: 'success' });
            loadData();
        }).catch(function (): void {
            showError('銷帳失敗，請稍後再試');
        }).finally(function (): void {
            $scope.confirming = false;
        });
    };
}]);
