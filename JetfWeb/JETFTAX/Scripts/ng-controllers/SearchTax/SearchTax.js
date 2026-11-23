mainApp.controller('SearchTaxController', ['$scope', '$http', function ($scope, $http) {
        $scope.keyword = '';
        $scope.data = [];
        $scope.loading = false;
        $scope.isSearched = false;
        $scope.detail = null;
        $scope.detailDlvInv = '';
        $scope.detailLoading = false;
        $scope.logs = [];
        $scope.logsLoading = false;
        $scope.showLogs = false;
        $scope.search = function () {
            var keyword = normalizeText($scope.keyword);
            if (!keyword) {
                alert('請輸入物流貨號、分提單號或袋號');
                return;
            }
            $scope.loading = true;
            $http.post(Router.action('SearchTax', 'Search'), { keyword: keyword })
                .then(function (response) {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢失敗: ' + result.error);
                    return;
                }
                $scope.data = result.Data || [];
                $scope.isSearched = true;
            })
                .catch(function () {
                alert('查詢失敗，請稍後再試');
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.clearSearch = function () {
            $scope.keyword = '';
            $scope.data = [];
            $scope.isSearched = false;
        };
        $scope.openDetail = function (dlvInv) {
            dlvInv = normalizeText(dlvInv);
            if (!dlvInv) {
                return;
            }
            $scope.detailDlvInv = dlvInv;
            $scope.detail = null;
            $scope.detailLoading = true;
            $scope.logs = [];
            $scope.showLogs = false;
            $('#searchTaxDetailModal').modal('show');
            $http.post(Router.action('SearchTax', 'GetDetail'), { dlvInv: dlvInv })
                .then(function (response) {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢明細失敗: ' + result.error);
                    return;
                }
                $scope.detail = result.Data || { Fee: null, CargoStatuses: [] };
            })
                .catch(function () {
                alert('查詢明細失敗，請稍後再試');
            })
                .finally(function () {
                $scope.detailLoading = false;
            });
        };
        // 明細視窗內展開/收合查詢紀錄；每次展開重新讀取
        $scope.toggleLogs = function () {
            if ($scope.showLogs) {
                $scope.showLogs = false;
                return;
            }
            if (!$scope.detailDlvInv) {
                return;
            }
            $scope.showLogs = true;
            $scope.logs = [];
            $scope.logsLoading = true;
            $http.post(Router.action('SearchTax', 'GetSearchLogs'), { dlvInv: $scope.detailDlvInv })
                .then(function (response) {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢紀錄失敗: ' + result.error);
                    return;
                }
                $scope.logs = result.Data || [];
            })
                .catch(function () {
                alert('查詢紀錄失敗，請稍後再試');
            })
                .finally(function () {
                $scope.logsLoading = false;
            });
        };
        function normalizeText(value) {
            return value ? value.toString().trim() : '';
        }
    }]);
