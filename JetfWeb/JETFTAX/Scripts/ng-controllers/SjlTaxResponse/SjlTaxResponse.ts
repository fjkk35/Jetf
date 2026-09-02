interface SjlTaxResponseForm {
    Type: string;
    DeliveryNumbers: string;
}

interface SjlTaxResponseResultItem {
    DeliveryNumber: string;
    Result: string;
    Code?: string;
    Message?: string;
}

interface SjlTaxResponseResult {
    RequestedCount: number;
    MatchedCount: number;
    SuccessCount: number;
    NoTaxCount: number;
    FailureCount: number;
    Items: SjlTaxResponseResultItem[];
}

interface SjlTaxResponseApiResponse<T> {
    status?: string;
    msg?: string;
    ReturnObject?: T;
}

mainApp.controller('SjlTaxResponseController', ['$scope', '$http', function ($scope: any, $http: ng.IHttpService) {
    $scope.sjlTaxForm = <SjlTaxResponseForm>{
        Type: '海運',
        DeliveryNumbers: ''
    };
    $scope.sjlTaxResult = null;
    $scope.sjlTaxResultMessage = '';
    $scope.sendingSjlTax = false;

    $scope.sendSjlTax = function (): void {
        if (!$scope.sjlTaxForm.DeliveryNumbers || $scope.sjlTaxForm.DeliveryNumbers.trim() === '') {
            swal({
                title: '錯誤',
                text: '請輸入物流貨號',
                icon: 'error'
            });
            return;
        }

        $scope.sendingSjlTax = true;
        $scope.sjlTaxResult = null;
        $scope.sjlTaxResultMessage = '';

        $http.post(Router.action('SjlTaxResponse', 'SendSjlTax'), {
            Type: $scope.sjlTaxForm.Type,
            DeliveryNumbers: $scope.sjlTaxForm.DeliveryNumbers
        })
            .then(function (response: ng.IHttpPromiseCallbackArg<SjlTaxResponseApiResponse<SjlTaxResponseResult>>): void {
                if (response.data && response.data.status === 'success') {
                    $scope.sjlTaxResult = response.data.ReturnObject || null;
                    $scope.sjlTaxResultMessage = response.data.msg || '捷利稅金回傳完成';
                    swal({
                        title: '回傳完成',
                        text: $scope.sjlTaxResultMessage,
                        icon: $scope.sjlTaxResult && $scope.sjlTaxResult.FailureCount > 0 ? 'warning' : 'success'
                    });
                } else {
                    swal({
                        title: '回傳失敗',
                        text: response.data.msg || '捷利稅金回傳失敗',
                        icon: 'error'
                    });
                }
            })
            .catch(function (): void {
                swal({
                    title: '回傳失敗',
                    text: '捷利稅金回傳發生錯誤，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function (): void {
                $scope.sendingSjlTax = false;
            });
    };
}]);
