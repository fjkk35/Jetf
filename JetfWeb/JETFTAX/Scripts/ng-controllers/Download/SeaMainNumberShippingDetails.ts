// <reference path="../../types/global.d.ts" />

interface SeaMainNumberShippingDetailsDownloadFile {
    fileGuid?: string;
    fileName?: string;
}

interface SeaMainNumberShippingDetailsResponse extends SeaMainNumberShippingDetailsDownloadFile {
    Redirect?: boolean;
    files?: SeaMainNumberShippingDetailsDownloadFile[];
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

    function downloadFile(file: SeaMainNumberShippingDetailsDownloadFile): void {
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

    function downloadFiles(response: SeaMainNumberShippingDetailsResponse): void {
        var files = response.files && response.files.length
            ? response.files
            : [response];

        angular.forEach(files, function (file: SeaMainNumberShippingDetailsDownloadFile, index: number) {
            window.setTimeout(function () {
                downloadFile(file);
            }, index * 300);
        });
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

                downloadFiles(data);
            }).catch(function () {
                showError('檔案下載失敗，請稍後再試');
            }).finally(function () {
                $scope.loading = false;
            });
    };
}]);
