/// <reference path="../../types/global.d.ts" />

interface BatchSearchCargo2ExportRequest {
    trackingNoList: string;
}

interface BatchSearchCargo2ExportResponse extends ApiResponse {
    fileGuid?: string;
    fileName?: string;
}

interface BatchSearchCargo2Scope extends ng.IScope {
    request: BatchSearchCargo2ExportRequest;
    isLoading: boolean;
    clear: () => void;
    exportExcel: () => void;
}

mainApp.controller('BatchSearchCargo2Controller', ['$scope', '$http', '$window', function (
    $scope: BatchSearchCargo2Scope,
    $http: ng.IHttpService,
    $window: ng.IWindowService
) {
    function setActiveMenu(): void {
        angular.element('#collapseSearch').addClass('show');
        angular.element('#BatchSearchCargo2').addClass('active');
    }

    function openLoginPage(): void {
        $window.location.href = Router.action('Account', 'Login');
    }

    function hasTrackingNoList(): boolean {
        return !!($scope.request.trackingNoList && $scope.request.trackingNoList.trim());
    }

    function buildDownloadUrl(fileGuid: string, fileName: string): string {
        return Router.action('Download', 'DownloadFile')
            + '?fileGuid=' + encodeURIComponent(fileGuid)
            + '&fileName=' + encodeURIComponent(fileName);
    }

    $scope.request = {
        trackingNoList: ''
    };
    $scope.isLoading = false;

    setActiveMenu();

    $scope.clear = function (): void {
        $scope.request.trackingNoList = '';
    };

    $scope.exportExcel = function (): void {
        if (!hasTrackingNoList()) {
            swal({
                title: '錯誤',
                text: '請輸入分提單號',
                icon: 'error'
            });
            return;
        }

        $scope.isLoading = true;

        $http.post(Router.action('BatchSearchCargo2', 'ExportExcel'), $scope.request)
            .then(function (response: ng.IHttpResponse<BatchSearchCargo2ExportResponse>): void {
                var result = response.data || {};

                if (result.Redirect) {
                    openLoginPage();
                    return;
                }

                if (result.fileGuid && result.fileName) {
                    $window.location.href = buildDownloadUrl(result.fileGuid, result.fileName);

                    swal({
                        title: '成功',
                        text: '匯出成功',
                        icon: 'success'
                    });
                    return;
                }

                swal({
                    title: '匯出失敗',
                    text: result.msg || '匯出失敗',
                    icon: 'error'
                });
            })
            .catch(function (): void {
                swal({
                    title: '錯誤',
                    text: '匯出發生錯誤',
                    icon: 'error'
                });
            })
            .finally(function (): void {
                $scope.isLoading = false;
            });
    };
}]);