// <reference path="../../types/global.d.ts" />

interface DownloadIncludeTaxResponse {
    Redirect?: boolean;
    fileGuid?: string;
    fileName?: string;
    msg?: string;
}

interface DownloadIncludeTaxScope extends ng.IScope {
    form: {
        startDate: Date;
        endDate: Date;
        source: string;
    };
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    dateOptions: any;
    exporting: boolean;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    exportExcel: () => void;
}

mainApp.controller('DownloadIncludeTaxController', ['$scope', '$http', function (
    $scope: DownloadIncludeTaxScope,
    $http: ng.IHttpService
) {
    function formatDate(value: Date): string {
        return moment(value).format('YYYY-MM-DD');
    }

    function showError(message: string): void {
        swal({ title: message, icon: 'error' });
    }

    function openDownloadFile(response: DownloadIncludeTaxResponse): void {
        if (!response.fileGuid || !response.fileName) {
            return;
        }

        var downloadUrl = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(response.fileGuid)
            + '&fileName=' + encodeURIComponent(response.fileName);
        var link = document.createElement('a');
        link.href = downloadUrl;
        link.download = response.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    var today = new Date();
    today.setHours(0, 0, 0, 0);
    $scope.form = { startDate: today, endDate: today, source: '1' };
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.exporting = false;

    $scope.openStartDatePopup = function (): void {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function (): void {
        $scope.endDatePopup.opened = true;
    };

    $scope.exportExcel = function (): void {
        if (!$scope.form.startDate || !$scope.form.endDate) {
            showError('日期為必填，請選擇開始日期與結束日期');
            return;
        }

        if (moment($scope.form.startDate).isAfter($scope.form.endDate, 'day')) {
            showError('開始日期不可晚於結束日期');
            return;
        }

        $scope.exporting = true;
        $http.post(Router.action('DownloadIncludeTax', 'ExportExcel'), {
            StartDate: formatDate($scope.form.startDate),
            EndDate: formatDate($scope.form.endDate),
            Source: $scope.form.source
        }).then(function (response: ng.IHttpResponse<DownloadIncludeTaxResponse>): void {
            var data = response.data || {};
            if (data.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return;
            }

            if (data.msg) {
                showError(data.msg);
                return;
            }

            openDownloadFile(data);
        }).catch(function (): void {
            showError('檔案下載失敗，請稍後再試');
        }).finally(function (): void {
            $scope.exporting = false;
        });
    };
}]);
