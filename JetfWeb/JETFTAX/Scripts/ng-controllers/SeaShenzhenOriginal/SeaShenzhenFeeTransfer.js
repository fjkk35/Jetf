mainApp.controller('SeaShenzhenFeeTransferController', ['$scope', '$http', function ($scope, $http) {
    function formatDataDate(value) {
        var month = ('0' + (value.getMonth() + 1)).slice(-2);
        var day = ('0' + value.getDate()).slice(-2);
        return value.getFullYear() + month + day;
    }

    $scope.form = {
        dataDate: new Date()
    };
    $scope.datePopup = { opened: false };
    $scope.dateOptions = {
        formatYear: 'yyyy',
        maxDate: new Date(2099, 11, 31),
        minDate: new Date(2000, 0, 1),
        startingDay: 0,
        showWeeks: false
    };
    $scope.transferring = false;
    $scope.hasResult = false;
    $scope.resultMessage = '';
    $scope.result = createEmptyResult();

    $scope.openDatePopup = function () {
        $scope.datePopup.opened = true;
    };

    $scope.transfer = function () {
        if (!$scope.form.dataDate) {
            showMessage('error', '請選擇日期');
            return;
        }

        if (!window.confirm('確定要執行稅金轉檔？')) {
            return;
        }

        $scope.transferring = true;
        $scope.resultMessage = '';

        $http.post(Router.action('SeaShenzhenFeeTransfer', 'Transfer'), {
            DataDate: formatDataDate($scope.form.dataDate)
        })
            .then(function (response) {
                var responseData = response.data || {};
                if (responseData.status !== 'success' || !responseData.ReturnObject) {
                    showMessage('error', responseData.msg || '轉檔失敗');
                    return;
                }

                $scope.result = responseData.ReturnObject;
                $scope.result.Exceptions = $scope.result.Exceptions || [];
                $scope.resultMessage = $scope.result.message || responseData.msg || '轉檔完成';
                $scope.hasResult = true;
                showMessage('success', $scope.resultMessage);
            })
            .catch(function () {
                showMessage('error', '轉檔失敗，請稍後再試');
            })
            .finally(function () {
                $scope.transferring = false;
            });
    };

    function createEmptyResult() {
        return {
            DataDate: '',
            SourceCount: 0,
            DeletedCount: 0,
            CreatedCount: 0,
            ExceptionCount: 0,
            Exceptions: []
        };
    }

    function showMessage(type, text) {
        if (typeof swal === 'function') {
            swal({
                title: type === 'success' ? '成功' : '錯誤',
                text: text,
                icon: type
            });
            return;
        }

        alert(text);
    }
}]);
