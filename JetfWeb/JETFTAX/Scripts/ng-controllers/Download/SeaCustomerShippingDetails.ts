// <reference path="../../types/global.d.ts" />

interface SeaCustomerShippingDetailsDownloadFile {
    fileGuid?: string;
    fileName?: string;
}

interface SeaCustomerShippingDetailsResponse extends SeaCustomerShippingDetailsDownloadFile {
    Redirect?: boolean;
    files?: SeaCustomerShippingDetailsDownloadFile[];
    msg?: string;
}

interface SeaCustomerShippingDetailsRequest {
    SDate: string;
    EDate: string;
    DataType: string;
    DespatchName: string;
}

interface SeaCustomerShippingDetailsScope extends ng.IScope {
    warehouseList: Array<{ Value: string; Text: string }>;
    customerList: Array<{ Value: string; Text: string }>;
    form: {
        sDate: Date;
        eDate: Date;
        dataType: string;
        despatchName: string;
    };
    startDatePopup: { opened: boolean };
    endDatePopup: { opened: boolean };
    dateOptions: any;
    loading: boolean;
    openStartDatePopup: () => void;
    openEndDatePopup: () => void;
    download: () => void;
}

mainApp.controller('SeaCustomerShippingDetailsController', ['$scope', '$http', function (
    $scope: SeaCustomerShippingDetailsScope,
    $http: ng.IHttpService
) {
    function formatDate(value: Date): string {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + '-' + month + '-' + day;
    }

    function showError(message: string): void {
        swal({
            title: message,
            icon: 'error'
        });
    }

    function setActiveMenu(): void {
        angular.element('#collapseUpload').addClass('show');
        angular.element('#SeaCustomerShippingDetails').addClass('active');
    }

    function loadWarehouseList(): void {
        $http.get(Router.action('SeaCustomerShippingDetails', 'GetWarehouseList'))
            .then(function (response: ng.IHttpResponse<Array<{ Value: string; Text: string }>>) {
                $scope.warehouseList = response.data || [];
                if (!$scope.form.dataType && $scope.warehouseList.length > 0) {
                    $scope.form.dataType = $scope.warehouseList[0].Value;
                }
            })
            .catch(function () {
                showError('倉別載入失敗');
            });
    }

    function loadCustomerList(): void {
        $http.get(Router.action('SeaCustomerShippingDetails', 'GetCustomerList'))
            .then(function (response: ng.IHttpResponse<Array<{ Value: string; Text: string }>>) {
                $scope.customerList = response.data || [];
            })
            .catch(function () {
                showError('客戶載入失敗');
            });
    }

    function createRequest(): SeaCustomerShippingDetailsRequest {
        return {
            SDate: formatDate($scope.form.sDate),
            EDate: formatDate($scope.form.eDate),
            DataType: $scope.form.dataType,
            DespatchName: $scope.form.despatchName
        };
    }

    function downloadFile(file: SeaCustomerShippingDetailsDownloadFile): void {
        if (!file.fileGuid || !file.fileName) {
            return;
        }

        var path = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(file.fileGuid)
            + '&filename=' + encodeURIComponent(file.fileName);
        var link = document.createElement('a');
        link.href = path;
        link.download = file.fileName;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    function downloadFiles(response: SeaCustomerShippingDetailsResponse): void {
        var files = response.files && response.files.length
            ? response.files
            : [response];

        angular.forEach(files, function (file: SeaCustomerShippingDetailsDownloadFile, index: number) {
            window.setTimeout(function () {
                downloadFile(file);
            }, index * 300);
        });
    }

    var today = new Date();
    $scope.warehouseList = [];
    $scope.customerList = [];
    $scope.form = {
        sDate: today,
        eDate: today,
        dataType: '',
        despatchName: ''
    };
    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.loading = false;

    setActiveMenu();
    loadWarehouseList();
    loadCustomerList();

    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    $scope.download = function () {
        if (!$scope.form.sDate || !$scope.form.eDate) {
            showError('請選擇出倉日');
            return;
        }

        if ($scope.form.sDate > $scope.form.eDate) {
            showError('出倉日起日不可大於迄日');
            return;
        }

        if (!$scope.form.dataType) {
            showError('請選擇倉別');
            return;
        }

        if (!$scope.form.despatchName) {
            showError('請選擇客戶');
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('SeaCustomerShippingDetails', 'DownloadExcel'), createRequest())
            .then(function (response: { data: SeaCustomerShippingDetailsResponse }) {
            var data = response.data || {};
            if (data.Redirect) {
                window.location.href = Router.action('Account', 'Login');
                return;
            }

            if (data.msg) {
                showError(data.msg);
                return;
            }

            downloadFiles(data);
        }).catch(function () {
            showError('檔案下載失敗，請稍後再試');
        }).finally(function () {
            $scope.loading = false;
        });
    };
}]);
