// <reference path="../../types/global.d.ts" />

interface SeaTaxGUploadResponse {
    Redirect?: boolean;
    status?: string;
    msg?: string;
}

interface SeaTaxGUploadScope extends ng.IScope {
    form: {
        date: Date;
    };
    datePopup: { opened: boolean };
    dateOptions: any;
    loading: boolean;
    openDatePopup: () => void;
    uploadFile: () => void;
}

mainApp.controller('SeaTaxGUploadController', ['$scope', '$http', function (
    $scope: SeaTaxGUploadScope,
    $http: ng.IHttpService
) {
    function formatDate(value: Date): string {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + '-' + month + '-' + day;
    }

    function resetFileInput(): void {
        var fileElement = document.getElementById('seaTaxGFile') as HTMLInputElement;
        if (fileElement) {
            fileElement.value = '';
        }
    }

    function openLoginPage(): void {
        window.location.href = Router.action('Account', 'Login');
    }

    angular.element('#collapseUpload').addClass('show');
    angular.element('#SeataxG').addClass('active');

    $scope.form = {
        date: new Date()
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

    $scope.openDatePopup = function () {
        $scope.datePopup.opened = true;
    };

    $scope.uploadFile = function () {
        var fileElement = document.getElementById('seaTaxGFile') as HTMLInputElement;
        if (!$scope.form.date) {
            swal({
                title: '請選擇日期',
                icon: 'error'
            });
            return;
        }

        if (!fileElement || !fileElement.files || fileElement.files.length === 0) {
            swal({
                title: '未選擇檔案',
                icon: 'error'
            });
            return;
        }

        var file = fileElement.files[0];
        if (!/\.xlsx$/i.test(file.name)) {
            swal({
                title: '副檔名需為 xlsx',
                icon: 'error'
            });
            return;
        }

        var form = document.getElementById('SeaTaxGUploadForm') as HTMLFormElement;
        var formData = new FormData(form);
        formData.set('date', formatDate($scope.form.date));
        $scope.loading = true;

        $http.post(Router.action('SeaTaxGUpload', 'UploadFile'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response: { data: SeaTaxGUploadResponse }) {
                if (response.data.Redirect) {
                    openLoginPage();
                    return;
                }

                if (response.data.status === 'success') {
                    swal({
                        title: response.data.msg || '上傳成功',
                        icon: 'success'
                    });
                    resetFileInput();
                    return;
                }

                swal({
                    title: response.data.msg || '上傳失敗',
                    icon: 'error'
                });
            })
            .catch(function () {
                swal({
                    title: '上傳失敗',
                    text: '請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function () {
                $scope.loading = false;
            });
    };
}]);
