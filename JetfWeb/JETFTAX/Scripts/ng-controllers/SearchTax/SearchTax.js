mainApp.controller('SearchTaxController', ['$scope', '$http', function ($scope, $http) {
        $scope.keyword = '';
        $scope.data = [];
        $scope.loading = false;
        $scope.isSearched = false;
        $scope.detail = { Fee: null, CargoStatuses: [] };
        $scope.detailDlvInv = '';
        $scope.logs = [];
        $scope.search = function () {
            $scope.loading = true;
            $http.post(Router.action('SearchTax', 'Search'), { keyword: normalizeText($scope.keyword) })
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
        // 物流貨號按鈕：載入明細後開啟視窗
        $scope.openDetail = function (dlvInv) {
            dlvInv = normalizeText(dlvInv);
            if (!dlvInv) {
                return;
            }
            $http.post(Router.action('SearchTax', 'GetDetail'), { dlvInv: dlvInv })
                .then(function (response) {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢明細失敗: ' + result.error);
                    return;
                }
                $scope.detailDlvInv = dlvInv;
                $scope.detail = result.Data || { Fee: null, CargoStatuses: [] };
                $('#searchTaxDetailModal').modal('show');
            })
                .catch(function () {
                alert('查詢明細失敗，請稍後再試');
            });
        };
        // 查詢紀錄按鈕：載入紀錄後開啟第二層視窗
        $scope.openLogs = function () {
            $http.post(Router.action('SearchTax', 'GetSearchLogs'), { dlvInv: $scope.detailDlvInv })
                .then(function (response) {
                var result = response.data || {};
                if (result.error) {
                    alert('查詢紀錄失敗: ' + result.error);
                    return;
                }
                $scope.logs = result.Data || [];
                $('#searchTaxLogModal').modal('show');
            })
                .catch(function () {
                alert('查詢紀錄失敗，請稍後再試');
            });
        };
        function normalizeText(value) {
            return value ? value.toString().trim() : '';
        }
        // 查詢紀錄疊在明細視窗上，關閉後保留明細視窗的捲動狀態
        $('#searchTaxLogModal').on('hidden.bs.modal', function () {
            if ($('#searchTaxDetailModal').hasClass('show')) {
                $('body').addClass('modal-open');
            }
        });
    }]);
