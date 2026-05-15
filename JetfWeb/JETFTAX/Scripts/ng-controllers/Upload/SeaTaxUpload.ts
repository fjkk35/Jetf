// <reference path="../../types/global.d.ts" />

interface SeaTaxUploadResponse {
    Redirect?: boolean;
    status?: string;
    msg?: string;
}

interface SeaTaxUploadScope extends ng.IScope {
    taxTypeList: Array<{ Value: string; Text: string }>;
    form: {
        date: Date;
        taxType: string;
    };
    datePopup: { opened: boolean };
    dateOptions: any;
    loading: boolean;
    openDatePopup: () => void;
    uploadFile: () => void;
}

mainApp.controller('SeaTaxUploadController', ['$scope', '$http', function (
    $scope: SeaTaxUploadScope,
    $http: ng.IHttpService
) {
    function formatDate(value: Date): string {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + '-' + month + '-' + day;
    }

    function resetFileInput(): void {
        var fileElement = document.getElementById('fileTax') as HTMLInputElement;
        if (fileElement) {
            fileElement.value = '';
        }
    }

    function openLoginPage(): void {
        window.location.href = Router.action('Account', 'Login');
    }

    function setActiveMenu(): void {
        angular.element('#collapseUpload').addClass('show');
        angular.element('#SeaTaxUploadNew').addClass('active');
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
                console.error('載入稅金類型失敗:', error);
            });
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

    $scope.uploadFile = function () {
        var fileElement = document.getElementById('fileTax') as HTMLInputElement;
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

        if (!fileElement || !fileElement.files || fileElement.files.length === 0) {
            swal({
                title: '未選擇檔案',
                icon: 'error'
            });
            return;
        }

        var form = document.getElementById('SeaTaxUploadForm') as HTMLFormElement;
        var formData = new FormData(form);
        formData.set('date', formatDate($scope.form.date));
        formData.set('taxType', $scope.form.taxType);

        $scope.loading = true;

        $http.post(Router.action('SeaTaxUpload', 'UploadFile'), formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        })
            .then(function (response: { data: SeaTaxUploadResponse }) {
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
                resetFileInput();
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
