// <reference path="../../types/global.d.ts" />

interface ReconciliationCustomerSelectorOption {
    Type: string;
    CustCode: string;
    CustName: string;
}

interface ReconciliationCustomerSelectorGroup {
    Id: number;
    Type: string;
    GroupName: string;
    CustCodes: string[];
}

interface ReconciliationCustomerSelectorOptions {
    SeaCustomers: ReconciliationCustomerSelectorOption[];
    AirCustomers: ReconciliationCustomerSelectorOption[];
    Groups: ReconciliationCustomerSelectorGroup[];
}

interface ReconciliationCustomerSelectorMap {
    [custCode: string]: boolean;
}

mainApp.directive('reconciliationCustomerSelector', ['$http', function (
    $http: ng.IHttpService
): ng.IDirective {
    return {
        restrict: 'E',
        scope: {
            selectedCustomerMap: '=',
            optionsUrl: '@',
            disabled: '=?'
        },
        template:
            '<style>' +
            ' .reconciliation-customer-selector-button{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}' +
            ' .reconciliation-customer-selector .customer-type-panel{border:1px solid #d9dee7;border-radius:4px;overflow:hidden;}' +
            ' .reconciliation-customer-selector .customer-type-header{padding:10px 14px;background:#f2f4f8;border-bottom:1px solid #d9dee7;}' +
            ' .reconciliation-customer-selector .customer-group-area{min-height:48px;padding:9px 14px;border-bottom:1px solid #d9dee7;}' +
            ' .reconciliation-customer-selector .customer-list{height:390px;overflow-y:auto;padding:8px 14px;}' +
            ' .reconciliation-customer-selector .customer-option{min-height:42px;padding-top:9px;padding-bottom:8px;border-bottom:1px solid #edf0f5;}' +
            ' .reconciliation-customer-selector .customer-option:last-child{border-bottom:0;}' +
            '</style>' +
            '<button type="button" class="btn btn-outline-secondary btn-block text-left reconciliation-customer-selector-button"' +
            '        ng-click="open()" ng-disabled="loading || disabled" title="{{displayFullText}}">' +
            '  <i class="fa" ng-class="loading ? \'fa-spinner fa-spin\' : \'fa-users\'"></i>' +
            '  {{loading ? \'載入客戶中...\' : displayText}}' +
            '</button>' +
            '<div class="modal fade reconciliation-customer-selector" tabindex="-1" role="dialog">' +
            '  <div class="modal-dialog modal-xl" role="document">' +
            '    <div class="modal-content">' +
            '      <div class="modal-header">' +
            '        <h5 class="modal-title">客戶</h5>' +
            '        <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>' +
            '      </div>' +
            '      <div class="modal-body">' +
            '        <div class="row mb-3">' +
            '          <div class="col-md-8"><input type="text" class="form-control" ng-model="keyword" placeholder="輸入客戶代碼或名稱篩選" /></div>' +
            '          <div class="col-md-4 text-right align-self-center">已選擇 {{selectedCount()}} 位客戶</div>' +
            '        </div>' +
            '        <div ng-show="loading" class="text-center py-5"><i class="fa fa-spinner fa-spin fa-3x"></i><p>載入客戶中...</p></div>' +
            '        <div class="row" ng-show="!loading">' +
            '          <div class="col-lg-6 mb-3 mb-lg-0">' +
            '            <div class="customer-type-panel">' +
            '              <div class="customer-type-header d-flex align-items-center justify-content-between"><strong>海運</strong>' +
            '                <div><button type="button" class="btn btn-outline-primary btn-sm" ng-click="selectAll(\'SEA\')">全選</button>' +
            '                <button type="button" class="btn btn-outline-secondary btn-sm ml-1" ng-click="clear(\'SEA\')">清除</button></div>' +
            '              </div>' +
            '              <div class="customer-group-area"><span class="text-muted mr-2">客戶群組：</span>' +
            '                <button type="button" class="btn btn-outline-primary btn-sm mr-2 mb-1" ng-repeat="group in groupsByType(\'SEA\') track by group.Id" ng-click="selectGroup(group)" title="選取此群組的全部客戶">{{group.GroupName}}</button>' +
            '                <span class="text-muted" ng-if="groupsByType(\'SEA\').length === 0">無群組</span>' +
            '              </div>' +
            '              <div class="customer-list">' +
            '                <div class="text-center text-muted py-3" ng-if="filteredCustomers(\'SEA\').length === 0">查無客戶</div>' +
            '                <div class="custom-control custom-checkbox customer-option" ng-repeat="customer in filteredCustomers(\'SEA\') track by customer.CustCode">' +
            '                  <input type="checkbox" class="custom-control-input" id="seaCustomer_{{selectorId}}_{{$index}}" ng-model="selectedCustomerMap[customer.CustCode]" ng-change="selectionChanged()" />' +
            '                  <label class="custom-control-label d-block" for="seaCustomer_{{selectorId}}_{{$index}}"><strong>{{customer.CustCode}}</strong><span ng-if="customer.CustName"> - {{customer.CustName}}</span></label>' +
            '                </div>' +
            '              </div>' +
            '            </div>' +
            '          </div>' +
            '          <div class="col-lg-6">' +
            '            <div class="customer-type-panel">' +
            '              <div class="customer-type-header d-flex align-items-center justify-content-between"><strong>空運</strong>' +
            '                <div><button type="button" class="btn btn-outline-primary btn-sm" ng-click="selectAll(\'AIR\')">全選</button>' +
            '                <button type="button" class="btn btn-outline-secondary btn-sm ml-1" ng-click="clear(\'AIR\')">清除</button></div>' +
            '              </div>' +
            '              <div class="customer-group-area"><span class="text-muted mr-2">客戶群組：</span>' +
            '                <button type="button" class="btn btn-outline-primary btn-sm mr-2 mb-1" ng-repeat="group in groupsByType(\'AIR\') track by group.Id" ng-click="selectGroup(group)" title="選取此群組的全部客戶">{{group.GroupName}}</button>' +
            '                <span class="text-muted" ng-if="groupsByType(\'AIR\').length === 0">無群組</span>' +
            '              </div>' +
            '              <div class="customer-list">' +
            '                <div class="text-center text-muted py-3" ng-if="filteredCustomers(\'AIR\').length === 0">查無客戶</div>' +
            '                <div class="custom-control custom-checkbox customer-option" ng-repeat="customer in filteredCustomers(\'AIR\') track by customer.CustCode">' +
            '                  <input type="checkbox" class="custom-control-input" id="airCustomer_{{selectorId}}_{{$index}}" ng-model="selectedCustomerMap[customer.CustCode]" ng-change="selectionChanged()" />' +
            '                  <label class="custom-control-label d-block" for="airCustomer_{{selectorId}}_{{$index}}"><strong>{{customer.CustCode}}</strong><span ng-if="customer.CustName"> - {{customer.CustName}}</span></label>' +
            '                </div>' +
            '              </div>' +
            '            </div>' +
            '          </div>' +
            '        </div>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>',
        link: function (scope: any, element: ng.IAugmentedJQuery): void {
            scope.selectorId = Math.random().toString(36).substr(2, 9);
            scope.keyword = '';
            scope.loading = false;
            scope.displayText = '全部客戶';
            scope.displayFullText = '全部客戶';
            scope.options = {
                SeaCustomers: [],
                AirCustomers: [],
                Groups: []
            } as ReconciliationCustomerSelectorOptions;

            if (!scope.selectedCustomerMap) {
                scope.selectedCustomerMap = {};
            }

            function customersByType(type: string): ReconciliationCustomerSelectorOption[] {
                return type === 'AIR'
                    ? (scope.options.AirCustomers || [])
                    : (scope.options.SeaCustomers || []);
            }

            function selectedCodes(): string[] {
                var codes: string[] = [];
                angular.forEach(scope.selectedCustomerMap, function (selected: boolean, code: string): void {
                    if (selected) {
                        codes.push(code);
                    }
                });
                return codes.sort();
            }

            function updateDisplay(): void {
                var codes = selectedCodes();
                if (!codes.length) {
                    scope.displayText = '全部客戶';
                    scope.displayFullText = '全部客戶';
                    return;
                }

                var names: { [custCode: string]: string } = {};
                customersByType('SEA').concat(customersByType('AIR')).forEach(function (
                    customer: ReconciliationCustomerSelectorOption
                ): void {
                    if (!names[customer.CustCode]) {
                        names[customer.CustCode] = customer.CustName;
                    }
                });

                scope.displayText = '已選擇 ' + codes.length + ' 位客戶';
                scope.displayFullText = codes.map(function (code: string): string {
                    return names[code] ? code + ' - ' + names[code] : code;
                }).join('、');
            }

            function showError(message: string): void {
                swal({ title: message, icon: 'error' });
            }

            scope.open = function (): void {
                scope.keyword = '';
                (<any>element.find('.reconciliation-customer-selector')).modal('show');
            };

            scope.filteredCustomers = function (type: string): ReconciliationCustomerSelectorOption[] {
                var keyword = (scope.keyword || '').trim().toLowerCase();
                if (!keyword) {
                    return customersByType(type);
                }

                return customersByType(type).filter(function (
                    customer: ReconciliationCustomerSelectorOption
                ): boolean {
                    return (customer.CustCode || '').toLowerCase().indexOf(keyword) >= 0
                        || (customer.CustName || '').toLowerCase().indexOf(keyword) >= 0;
                });
            };

            scope.groupsByType = function (type: string): ReconciliationCustomerSelectorGroup[] {
                return (scope.options.Groups || []).filter(function (
                    group: ReconciliationCustomerSelectorGroup
                ): boolean {
                    return group.Type === type;
                });
            };

            scope.selectGroup = function (group: ReconciliationCustomerSelectorGroup): void {
                (group.CustCodes || []).forEach(function (code: string): void {
                    scope.selectedCustomerMap[code] = true;
                });
                updateDisplay();
            };

            scope.selectAll = function (type: string): void {
                customersByType(type).forEach(function (
                    customer: ReconciliationCustomerSelectorOption
                ): void {
                    scope.selectedCustomerMap[customer.CustCode] = true;
                });
                updateDisplay();
            };

            scope.clear = function (type: string): void {
                customersByType(type).forEach(function (
                    customer: ReconciliationCustomerSelectorOption
                ): void {
                    delete scope.selectedCustomerMap[customer.CustCode];
                });
                updateDisplay();
            };

            scope.selectionChanged = updateDisplay;
            scope.selectedCount = function (): number {
                return selectedCodes().length;
            };

            scope.$watchCollection('selectedCustomerMap', updateDisplay);

            scope.loading = true;
            $http.get(scope.optionsUrl)
                .then(function (
                    response: ng.IHttpResponse<ApiResponse<ReconciliationCustomerSelectorOptions>>
                ): void {
                    if (response.data && response.data.Redirect) {
                        window.location.href = Router.action('Account', 'Login');
                        return;
                    }

                    if (response.data.status === 'error' || !response.data.ReturnObject) {
                        showError(response.data.msg || '載入客戶資料失敗');
                        return;
                    }

                    scope.options = response.data.ReturnObject;
                    updateDisplay();
                }).catch(function (): void {
                    showError('載入客戶資料失敗，請稍後再試');
                }).finally(function (): void {
                    scope.loading = false;
                });
        }
    };
}]);
