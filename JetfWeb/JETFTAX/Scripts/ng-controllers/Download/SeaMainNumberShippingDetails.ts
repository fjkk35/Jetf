// <reference path="../../types/global.d.ts" />

interface SeaMainNumberShippingDetailsResponse {
    Redirect?: boolean;
    fileGuid?: string;
    fileName?: string;
    msg?: string;
}

interface SeaMainNumberShippingDetailsRequest {
    MainNumbers: string;
}

interface SeaMainNumberShippingDetailsScope extends ng.IScope {
    form: {
        mainNumbers: string;
    };
    loading: boolean;
    download: () => void;
    clearMainNumbers: () => void;
}

mainApp.controller('SeaMainNumberShippingDetailsController', ['$scope', '$http', function (
    $scope: SeaMainNumberShippingDetailsScope,
    $http: ng.IHttpService
) {
    function showError(message: string): void {
        swal({
            title: message,
            icon: 'error'
        });
    }

    function setActiveMenu(): void {
        angular.element('#collapseUpload').addClass('show');
        angular.element('#SeaMainNumberShippingDetails').addClass('active');
    }

    function createRequest(): SeaMainNumberShippingDetailsRequest {
        return {
            MainNumbers: $scope.form.mainNumbers
        };
    }

    function downloadFile(response: SeaMainNumberShippingDetailsResponse): void {
        if (!response.fileGuid || !response.fileName) {
            return;
        }

        var path = Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(response.fileGuid)
            + '&filename=' + encodeURIComponent(response.fileName);
        var link = document.createElement('a');
        link.href = path;
        link.download = response.fileName;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    $scope.form = {
        mainNumbers: ''
    };
    $scope.loading = false;

    setActiveMenu();

    $scope.clearMainNumbers = function () {
        $scope.form.mainNumbers = '';
    };

    $scope.download = function () {
        if (!$scope.form.mainNumbers || !$scope.form.mainNumbers.trim()) {
            showError('請輸入主號');
            return;
        }

        $scope.loading = true;

        $http.post(Router.action('SeaMainNumberShippingDetails', 'DownloadExcel'), createRequest())
            .then(function (response: { data: SeaMainNumberShippingDetailsResponse }) {
                var data = response.data || {};
                if (data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }

                if (data.msg) {
                    showError(data.msg);
                    return;
                }

                downloadFile(data);
            }).catch(function () {
                showError('檔案下載失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
    };
}]);
