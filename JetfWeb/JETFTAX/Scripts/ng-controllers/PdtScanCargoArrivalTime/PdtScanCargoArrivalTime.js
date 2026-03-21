mainApp.controller('PdtScanCargoArrivalTimeController', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
    $scope.loading = false;
    $scope.isSearched = false;

    $scope.dataTypeList = [];
    $scope.transList = [];

    $scope.searchForm = {
        startDate: null,
        endDate: null,
        startHour: '00',
        startMinute: '00',
        endHour: '23',
        endMinute: '59',
        startTimeStr: '',
        endTimeStr: '',
        dataType: '',
        transNo: ''
    };

    $scope.data = [];

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
            // sweetalert v1
            try {
                return window.swal(title || '', text || '', type || 'info');
            } catch (e) { }
        }
        if (window.Swal && window.Swal.fire) {
            // sweetalert2
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

    $scope.detail = {
        loading: false,
        transName: '',
        rows: []
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
        $scope.loadTransList();
    };

    $scope.loadDataTypeList = function () {
        $http.get(Router.action('PdtScanCargoArrivalTime', 'GetDataTypeList'))
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

    $scope.loadTransList = function () {
        $http.get(Router.action('PdtScanCargoArrivalTime', 'GetTransList'))
            .then(function (resp) {
                if (resp.data && resp.data.error) {
                    showAlert('錯誤', '載入派件公司失敗: ' + resp.data.error, 'error');
                    return;
                }
                $scope.transList = resp.data || [];

                if (!$scope.searchForm.transNo && $scope.transList.length > 0) {
                    $scope.searchForm.transNo = $scope.transList[0].Value;
                }
            })
            .catch(function () {
                showAlert('錯誤', '載入派件公司失敗', 'error');
            });
    };

    $scope.search = function () {
        if (!$scope.searchForm.startTimeStr || !$scope.searchForm.endTimeStr) {
            showAlert('提醒', '請輸入日期區間', 'warning');
            return;
        }

        $scope.loading = true;

        var req = {
            StartTime: $scope.searchForm.startTimeStr,
            EndTime: $scope.searchForm.endTimeStr,
            TransNo: $scope.searchForm.transNo,
            DataType: $scope.searchForm.dataType
        };

        $http.post(Router.action('PdtScanCargoArrivalTime', 'SearchData'), req)
            .then(function (resp) {
                if (resp.data && resp.data.error) {
                    showAlert('錯誤', '查詢失敗: ' + resp.data.error, 'error');
                    return;
                }

                $scope.data = (resp.data && resp.data.Data) ? resp.data.Data : [];
                $scope.data.forEach(function (r) {
                    r.ArrivalTimeInput = '';
                });
                $scope.isSearched = true;
            })
            .catch(function () {
                showAlert('錯誤', '查詢失敗，請稍後再試', 'error');
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.update = function (row) {
        if (!row) return;

        if (!row.ArrivalTimeInput || row.ArrivalTimeInput.trim() === '') {
            showAlert('提醒', '未輸入交倉時間不得更新', 'warning');
            return;
        }

        if (!confirm('確認要更新該派件公司底下所有資料的交倉時間？')) {
            return;
        }

        $scope.loading = true;

        var req = {
            ArrivalTime: row.ArrivalTimeInput,
            TransName: row.TransName,
            Ids: row.Ids || []
        };

        $http.post(Router.action('PdtScanCargoArrivalTime', 'UpdateArrivalTime'), req)
            .then(function (resp) {
                var data = resp.data || {};
                if (data.status && data.status !== 'success') {
                    showAlert('錯誤', data.msg || '更新失敗', 'error');
                    return;
                }

                showAlert('成功', data.msg || '更新成功', 'success');

                if (data.result) {
                    row.ArrivedCount = data.result.ArrivedCount;
                    row.LastArrivalTime = data.result.LastArrivalTime;
                    row.LastUpdateArrivalTime = data.result.LastUpdateArrivalTime;
                    row.LastUpdateArrivalTimeOpe = data.result.LastUpdateArrivalTimeOpe;
                }
            })
            .catch(function () {
                showAlert('錯誤', '更新失敗，請稍後再試', 'error');
            })
            .finally(function () {
                $scope.loading = false;
            });
    };

    $scope.openDetailDialog = function (row) {
        if (!row) return;

        $scope.detail.loading = false;
        $scope.detail.transName = row.TransName;
        $scope.detail.rows = row.Details || [];

        if (window.$ && $('#pdtScanCargoArrivalTimeDetailDialog').modal) {
            $('#pdtScanCargoArrivalTimeDetailDialog').modal('show');
        } else {
            showAlert('錯誤', '未載入 Bootstrap modal', 'error');
        }
    };

    $scope.closeDetailDialog = function () {
        if (window.$ && $('#pdtScanCargoArrivalTimeDetailDialog').modal) {
            $('#pdtScanCargoArrivalTimeDetailDialog').modal('hide');
        }
    };

    $scope.init();
}]);
