(function () {
    'use strict';

    // 使用 directive 封裝「客戶多選」：
    // 1) 內部自行呼叫 API 載入 custList
    // 2) 自帶樣式與 modal
    // 3) 對外輸出 selectedCustCodes / selectAll / displayText / displayFullText

    mainApp.directive('customerMultiSelect', ['$http', function ($http) {
        return {
            restrict: 'E',
            scope: {
                // 兩向綁定輸出：是否全選
                selectAll: '=?',

                // 兩向綁定輸出：選取的客戶代碼陣列（selectAll=true 時輸出 []）
                selectedCustCodes: '=?',

                // 顯示用（頁面 input 直接用這兩個綁定）
                displayText: '=?',
                displayFullText: '=?'
            },
            template:
                '<style>' +
                '  .customer-multi-select-input{cursor:pointer;background-color:#fff;}' +
                '  .customer-multi-select-input[readonly]{background-color:#fff;}' +
                '  .customer-grid{display:flex;flex-wrap:wrap;gap:8px 12px;}' +
                '  .customer-grid .customer-item{width:calc((100% - (12px * 5))/6);min-width:180px;}' +
                '  .customer-grid .customer-item label{display:flex;align-items:center;gap:6px;margin:0;cursor:pointer;user-select:none;}' +
                '  .customer-grid .customer-item .customer-name{white-space:nowrap;display:inline-block;max-width:100%;}' +
                '  #custSelectModal .modal-dialog{max-width:80vw;width:80vw;}' +
                '  #custSelectModal input[type=checkbox]{transform:scale(1.4);-webkit-transform:scale(1.4);-ms-transform:scale(1.4);transform-origin:center center;-webkit-transform-origin:center center;margin-right:8px;vertical-align:middle;margin-top:0;}' +
                '  #custSelectModal .customer-grid .customer-item label{gap:8px;padding:6px 4px;width:100%;box-sizing:border-box;}' +
                '  #custSelectModal .customer-grid .customer-item{min-height:40px;display:flex;align-items:center;overflow:visible;}' +
                '</style>' +
                '' +
                '<input type="text"' +
                '       class="form-control customer-multi-select-input"' +
                '       ng-model="displayText"' +
                '       ng-attr-title="{{displayFullText}}"' +
                '       ng-click="open()"' +
                '       readonly' +
                '       placeholder="請選擇客戶" />' +
                '' +
                '<div class="modal fade" id="custSelectModal" tabindex="-1" role="dialog" aria-hidden="true">' +
                '  <div class="modal-dialog modal-xl" role="document">' +
                '    <div class="modal-content">' +
                '      <div class="modal-header">' +
                '        <h4 class="modal-title">客戶選擇</h4>' +
                '        <button type="button" class="close" data-dismiss="modal" aria-label="Close">' +
                '          <span aria-hidden="true">&times;</span>' +
                '        </button>' +
                '      </div>' +
                '      <div class="modal-body">' +
                '        <div class="form-group mb-3">' +
                '          <div class="d-flex align-items-center">' +
                '            <div class="custom-control custom-checkbox mr-3">' +
                '              <input type="checkbox" class="custom-control-input" id="custAll_{{id}}" ng-model="selection.selectAll" ng-change="toggleAll()" />' +
                '              <label class="custom-control-label" for="custAll_{{id}}">全選</label>' +
                '            </div>' +
                '            <div class="custom-control custom-checkbox mr-3" ng-repeat="(custType, customers) in custList">' +
                '              <input type="checkbox" class="custom-control-input" id="custGrp_{{id}}_{{custType}}" ng-model="selection[\'select\'+custType]" ng-change="toggleGroup(custType)" />' +
                '              <label class="custom-control-label" for="custGrp_{{id}}_{{custType}}">{{custType}}</label>' +
                '            </div>' +
                '          </div>' +
                '        </div>' +
                '' +
                '        <div style="max-height:600px;overflow-y:auto;">' +
                '          <div class="mb-3" ng-repeat="(custType, customers) in custList">' +
                '            <div class="d-flex align-items-center justify-content-between" style="gap:12px;">' +
                '              <h5 class="mb-0">{{custType}}</h5>' +
                '            </div>' +
                '            <hr class="mt-2 mb-2" />' +
                '            <div class="customer-grid">' +
                '              <div class="customer-item" ng-repeat="cust in customers">' +
                '                <label ng-attr-title="{{cust.Text}}">' +
                '                  <input type="checkbox" ng-model="selection.selectedMap[cust.Value]" ng-change="onItemChanged()" />' +
                '                  <span class="customer-name">{{cust.Text}}</span>' +
                '                </label>' +
                '              </div>' +
                '            </div>' +
                '            <div ng-if="customers.length===0" class="text-center text-muted">查無{{custType}}客戶清單</div>' +
                '          </div>' +
                '' +
                '          <div ng-if="!custList || (custList.SEA.length===0 && custList.AIR.length===0)" class="text-center text-muted">查無客戶清單</div>' +
                '        </div>' +
                '      </div>' +
                '      <div class="modal-footer">' +
                '        <button type="button" class="btn btn-secondary" ng-click="close()">關閉</button>' +
                '      </div>' +
                '    </div>' +
                '  </div>' +
                '</div>',
            link: function (scope, element, attrs) {
                scope.id = (Math.random().toString(36).substr(2, 9));

                scope.custList = {};

                scope.selection = {
                    selectAll: true,
                    selectedMap: {}
                };

                // 預設輸出
                if (typeof scope.selectAll === 'undefined') {
                    scope.selectAll = true;
                }
                if (!scope.selectedCustCodes) {
                    scope.selectedCustCodes = [];
                }
                if (!scope.displayText) {
                    scope.displayText = '全選';
                }
                if (!scope.displayFullText) {
                    scope.displayFullText = '全選';
                }

                function getApiUrl() {
                    // 預設（相容目前 ShipmentInboundRecord）
                    return Router.action('ShipmentInboundRecord', 'GetCustList');
                }

                function initSelectionAsAll() {
                    scope.selection.selectAll = true;
                    scope.selection.selectedMap = {};

                    for (var key in scope.custList) {
                        if (scope.custList.hasOwnProperty(key)) {
                            var list = scope.custList[key] || [];
                            scope.selection['select' + key] = true;
                            for (var i = 0; i < list.length; i++) {
                                scope.selection.selectedMap[list[i].Value] = true;
                            }
                        }
                    }

                    syncOutputs();
                }

                function syncGroupFlags() {
                    var totalCount = 0;
                    var totalSelected = 0;

                    for (var key in scope.custList) {
                        if (scope.custList.hasOwnProperty(key)) {
                            var list = scope.custList[key] || [];
                            totalCount += list.length;

                            var groupAll = (list.length > 0);
                            for (var i = 0; i < list.length; i++) {
                                if (scope.selection.selectedMap[list[i].Value]) {
                                    totalSelected++;
                                } else {
                                    groupAll = false;
                                }
                            }
                            scope.selection['select' + key] = groupAll;
                        }
                    }

                    scope.selection.selectAll = (totalCount === 0) ? true : (totalSelected === totalCount);
                }

                function getSelectedCustomers() {
                    var result = [];
                    for (var key in scope.custList) {
                        if (scope.custList.hasOwnProperty(key)) {
                            var list = scope.custList[key] || [];
                            for (var i = 0; i < list.length; i++) {
                                var cust = list[i];
                                if (scope.selection.selectedMap[cust.Value]) {
                                    result.push(cust);
                                }
                            }
                        }
                    }
                    return result;
                }

                function syncOutputs() {
                    syncGroupFlags();

                    var selected = getSelectedCustomers();

                    // total count
                    var totalCount = 0;
                    for (var key in scope.custList) {
                        if (scope.custList.hasOwnProperty(key)) {
                            totalCount += (scope.custList[key] || []).length;
                        }
                    }

                    // selectAll output
                    scope.selectAll = !!scope.selection.selectAll;

                    if (scope.selectAll || selected.length === totalCount) {
                        scope.displayText = '全選';
                        scope.displayFullText = '全選';
                        scope.selectedCustCodes = [];
                        return;
                    }

                    scope.displayText = '已選擇 ' + selected.length + ' 位客戶';

                    var names = [];
                    var codes = [];
                    for (var i = 0; i < selected.length; i++) {
                        names.push(selected[i].Text);
                        codes.push(selected[i].Value);
                    }
                    scope.displayFullText = names.join('、');
                    scope.selectedCustCodes = codes;
                }

                scope.open = function () {
                    $('#custSelectModal').modal('show');
                };

                scope.close = function () {
                    $('#custSelectModal').modal('hide');
                };

                scope.toggleAll = function () {
                    var val = !!scope.selection.selectAll;
                    for (var key in scope.custList) {
                        if (scope.custList.hasOwnProperty(key)) {
                            var list = scope.custList[key] || [];
                            scope.selection['select' + key] = val;
                            for (var i = 0; i < list.length; i++) {
                                scope.selection.selectedMap[list[i].Value] = val;
                            }
                        }
                    }
                    syncOutputs();
                };

                scope.toggleGroup = function (group) {
                    var list = scope.custList[group] || [];
                    var targetChecked = !!scope.selection['select' + group];

                    for (var i = 0; i < list.length; i++) {
                        scope.selection.selectedMap[list[i].Value] = targetChecked;
                    }

                    syncOutputs();
                };

                scope.onItemChanged = function () {
                    syncOutputs();
                };

                function loadCustList() {
                    var url = getApiUrl();
                    $http.get(url)
                        .then(function (response) {
                            var result = response.data || {};
                            // 相容後端可能回傳 { error: '...' }
                            if (result.error) {
                                alert(result.error);
                                return;
                            }

                            scope.custList = result;
                            initSelectionAsAll();
                        })
                        .catch(function (error) {
                            console.error('載入客戶清單失敗:', error);
                            alert('載入客戶清單失敗');
                        });
                }

                loadCustList();
            }
        };
    }]);
})();
