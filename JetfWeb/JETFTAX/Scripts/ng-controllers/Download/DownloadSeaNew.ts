// <reference path="../../types/global.d.ts" />

interface DownloadSeaNewResponse {
    Redirect?: boolean;
    fileGuid?: string;
    fileName?: string;
    msg?: string;
}

interface DownloadSeaNewScope extends ng.IScope {
    taxTypeList: Array<{ Value: string; Text: string }>;
    form: {
        date: Date;
        taxType: string;
    };
    datePopup: { opened: boolean };
    dateOptions: any;
    loading: boolean;
    openDatePopup: () => void;
    downloadAll: () => void;
}

mainApp.controller('DownloadSeaNewController', ['$scope', '$http', '$q', function (
    $scope: DownloadSeaNewScope,
    $http: ng.IHttpService,
    $q: ng.IQService
) {
    function formatDate(value: Date): string {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + '-' + month + '-' + day;
    }

    function openLoginPage(): void {
        window.location.href = Router.action('Account', 'Login');
    }

    function setActiveMenu(): void {
        angular.element('#collapseUpload').addClass('show');
        angular.element('#DownloadSeaNew').addClass('active');
    }

    function loadTaxTypeList(): void {
        $http.get(Router.action('SeaTaxUpload', 'GetSeaTaxTypeList'))
            .then(function (response: ng.IHttpResponse<Array<{ Value: string; Text: string }>>) {
                $scope.taxTypeList = response.data || [];
                if (!$scope.form.taxType && $scope.taxTypeList.length > 0) {
                    $scope.form.taxType = $scope.taxTypeList[0].Value;
                }
            })
            .catch(function (error) {
                console.error('載入稅金種類失敗:', error);
            });
    }

    function createFormData(): FormData {
        var form = document.getElementById('DownloadSeaNewForm') as HTMLFormElement;
        var formData = new FormData(form);
        formData.set('date', formatDate($scope.form.date));
        formData.set('taxType', $scope.form.taxType);
        return formData;
    }

    function postAction(actionName: string): ng.IPromise<DownloadSeaNewResponse> {
        return $http.post(Router.action('DownloadSeaNew', actionName), createFormData(), {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        }).then(function (response: { data: DownloadSeaNewResponse }) {
            return response.data;
        });
    }

    function openDownloadFile(response: DownloadSeaNewResponse): void {
        if (!response.fileGuid || !response.fileName) {
            return;
        }

        var path = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(response.fileGuid)
            + '&filename=' + encodeURIComponent(response.fileName);
        window.open(path);
    }

    function handleResponse(response: DownloadSeaNewResponse): ng.IPromise<void> {
        if (response.Redirect) {
            openLoginPage();
            return $q.reject('redirect');
        }

        if (response.msg) {
            swal({
                title: response.msg,
                icon: 'error'
            });
        }

        if (response.fileName) {
            openDownloadFile(response);
        }

        return $q.when();
    }

    $scope.taxTypeList = [];
    $scope.form = {
        date: new Date(),
        taxType: ''
    };
    $scope.datePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.loading = false;

    setActiveMenu();
    loadTaxTypeList();

    $scope.openDatePopup = function () {
        $scope.datePopup.opened = true;
    };

    $scope.downloadAll = function () {
        if (!$scope.form.date) {
            swal({
                title: '請選擇日期',
                icon: 'error'
            });
            return;
        }

        if (!$scope.form.taxType) {
            swal({
                title: '請選擇稅金種類',
                icon: 'error'
            });
            return;
        }

        $scope.loading = true;

        postAction('SeaExcel')
            .then(handleResponse)
            .then(function () {
                return postAction('SeaSpecialDExcel');
            })
            .then(handleResponse)
            .then(function () {
                return postAction('SeaErrorExcel');
            })
            .then(handleResponse)
            .catch(function (error) {
                if (error === 'redirect') {
                    return;
                }

                swal({
                    title: '下載失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };
}]);
