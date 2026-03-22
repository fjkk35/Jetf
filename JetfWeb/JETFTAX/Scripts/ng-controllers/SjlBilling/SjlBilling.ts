interface SjlBillingSearchData {
    startDate: Date | null;
    endDate: Date | null;
    transName: string;
}

interface SjlBillingDownloadResponse {
    Redirect?: boolean;
    msg?: string;
    fileGuid?: string;
    fileName?: string;
}

mainApp.controller('SjlBillingController', ['$scope', '$http', function ($scope: any, $http: ng.IHttpService) {
    $scope.downloading = false;
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };

    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };

    $scope.searchData = <SjlBillingSearchData>{
        startDate: new Date(),
        endDate: new Date(),
        transName: ''
    };

    $scope.openStartDatePopup = function (): void {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function (): void {
        $scope.endDatePopup.opened = true;
    };

    $scope.formatDate = function (date: Date | null): string {
        if (!date) {
            return '';
        }

        var parsedDate = new Date(date);
        var year = parsedDate.getFullYear();
        var month = String(parsedDate.getMonth() + 1).padStart(2, '0');
        var day = String(parsedDate.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    };

    $scope.downloadExcel = function (): void {
        // 下載前先做前端必填與日期區間檢查，避免無效請求直接打到後端。
        if (!$scope.searchData.startDate || !$scope.searchData.endDate || !$scope.searchData.transName) {
            swal({
                title: '提醒',
                text: '請完整輸入日期起、日期迄與派件公司',
                icon: 'warning'
            });
            return;
        }

        if (new Date($scope.searchData.startDate) > new Date($scope.searchData.endDate)) {
            swal({
                title: '提醒',
                text: '日期起不可大於日期迄',
                icon: 'warning'
            });
            return;
        }

        $scope.downloading = true;

        // 後端會先產出暫存檔資訊，再由前端轉成實際下載動作。
        $http({
            method: 'POST',
            url: Router.action('SjlBilling', 'DownloadExcel'),
            data: {
                StartDate: $scope.formatDate($scope.searchData.startDate),
                EndDate: $scope.formatDate($scope.searchData.endDate),
                TransName: $scope.searchData.transName
            },
            headers: { 'Content-Type': 'application/json' }
        }).then(function (response: ng.IHttpPromiseCallbackArg<SjlBillingDownloadResponse>) {
            var result = response.data || {};

            if (result.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return;
            }

            if (result.msg) {
                swal({
                    title: '錯誤',
                    text: result.msg,
                    icon: 'error'
                });
                return;
            }

            if (result.fileGuid && result.fileName) {
                // 沿用既有 DownloadController 的 fileGuid/fileName 下載模式。
                var path = Router.action('Download', 'DownloadFile') +
                    '?fileGuid=' + encodeURIComponent(result.fileGuid) +
                    '&fileName=' + encodeURIComponent(result.fileName);

                var link = document.createElement('a');
                link.href = path;
                link.download = result.fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            }
        }).catch(function (): void {
            swal({
                title: '錯誤',
                text: '下載失敗，請稍後再試',
                icon: 'error'
            });
        }).finally(function (): void {
            $scope.downloading = false;
        });
    };
}]);