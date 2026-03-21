mainApp.controller('ScanCargoCustomerDiffController', ['$scope', '$http', function ($scope, $http) {
    $scope.loading = false;

    $scope.dataTypeList = [];

    $scope.searchForm = {
        startDate: null,
        endDate: null,
        startHour: '00',
        startMinute: '00',
        endHour: '23',
        endMinute: '59',
        startTimeStr: '',
        endTimeStr: '',
        dataType: ''
    };

    $scope.hourOptions = [];
    for (var h = 0; h <= 23; h++) {
        $scope.hourOptions.push((h < 10 ? '0' : '') + h);
    }

    $scope.minuteOptions = [];
    for (var m = 0; m <= 59; m++) {
        $scope.minuteOptions.push((m < 10 ? '0' : '') + m);
    }

    $scope.dateOptions = {
        showWeeks: false
    };

    $scope.startDatePopup = { opened: false };
    $scope.endDatePopup = { opened: false };

    $scope.openStartDatePopup = function () {
        $scope.startDatePopup.opened = true;
    };

    $scope.openEndDatePopup = function () {
        $scope.endDatePopup.opened = true;
    };

    function pad2(n) {
        return (n < 10 ? '0' : '') + n;
    }

    function formatDate(d) {
        if (!d) return '';
        var date = (d instanceof Date) ? d : new Date(d);
        var yyyy = date.getFullYear();
        var MM = pad2(date.getMonth() + 1);
        var dd = pad2(date.getDate());
        return yyyy + '-' + MM + '-' + dd;
    }

    function showAlert(title, text, type) {
        if (window.swal) {
            try {
                return window.swal(title || '', text || '', type || 'info');
            } catch (e) { }
        }
        if (window.Swal && window.Swal.fire) {
            return window.Swal.fire({
                title: title || '',
                text: text || '',
                icon: type || 'info'
            });
        }
        alert((title ? title + (text ? ': ' : '') : '') + (text || ''));
    }

    $scope.onDateChange = function () {
        var startDateStr = formatDate($scope.searchForm.startDate);
        var endDateStr = formatDate($scope.searchForm.endDate);

        $scope.searchForm.startTimeStr = startDateStr
            ? (startDateStr + ' ' + ($scope.searchForm.startHour || '00') + ':' + ($scope.searchForm.startMinute || '00'))
            : '';

        $scope.searchForm.endTimeStr = endDateStr
            ? (endDateStr + ' ' + ($scope.searchForm.endHour || '23') + ':' + ($scope.searchForm.endMinute || '59'))
            : '';
    };

    $scope.init = function () {
        var now = new Date();
        var start = new Date(now);
        start.setHours(0, 0, 0, 0);
        var end = new Date(now);
        end.setHours(23, 59, 0, 0);

        $scope.searchForm.startDate = start;
        $scope.searchForm.endDate = end;
        $scope.searchForm.startHour = '00';
        $scope.searchForm.startMinute = '00';
        $scope.searchForm.endHour = '23';
        $scope.searchForm.endMinute = '59';
        $scope.onDateChange();

        $scope.loadDataTypeList();
    };

    $scope.loadDataTypeList = function () {
        $http.get(Router.action('ScanCargoCustomerDiff', 'GetDataTypeList'))
            .then(function (resp) {
                if (resp.data && resp.data.error) {
                    showAlert('錯誤', '載入作業地區失敗: ' + resp.data.error, 'error');
                    return;
                }
                $scope.dataTypeList = resp.data || [];

                if (!$scope.searchForm.dataType && $scope.dataTypeList.length > 0) {
                    $scope.searchForm.dataType = $scope.dataTypeList[0].Value;
                }
            })
            .catch(function () {
                showAlert('錯誤', '載入作業地區失敗', 'error');
            });
    };

    $scope.exportExcel = function () {
        if (!$scope.searchForm.startTimeStr || !$scope.searchForm.endTimeStr) {
            showAlert('提醒', '請輸入日期區間', 'warning');
            return;
        }

        if (!$scope.searchForm.dataType) {
            showAlert('提醒', '請選擇作業地區', 'warning');
            return;
        }

        $scope.loading = true;

        var req = {
            startTime: $scope.searchForm.startTimeStr,
            endTime: $scope.searchForm.endTimeStr,
            dataType: $scope.searchForm.dataType
        };

        $http.post(Router.action('ScanCargoCustomerDiff', 'ExportExcel'), req)
            .then(function (resp) {
                if (resp.data && resp.data.msg) {
                    showAlert('錯誤', resp.data.msg, 'error');
                    return;
                }

                if (resp.data && resp.data.fileGuid && resp.data.fileName) {
                    var url = Router.action('Download', 'DownloadFile') + '?fileGuid=' + resp.data.fileGuid + '&fileName=' + encodeURIComponent(resp.data.fileName);
                    window.location.href = url;
                }
            })
            .catch(function () {
                showAlert('錯誤', '匯出失敗，請稍後再試', 'error');
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.init();
}]);
