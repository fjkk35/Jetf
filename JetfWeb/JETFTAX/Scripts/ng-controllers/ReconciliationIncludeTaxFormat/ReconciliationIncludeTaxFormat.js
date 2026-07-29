mainApp.controller('ReconciliationIncludeTaxFormatController', ['$scope', '$http', function ($scope, $http) {
    function redirectIfNeeded(response) {
        if (response && response.Redirect) {
            window.location.href = Router.action('Account', 'Login');
            return true;
        }
        return false;
    }

    function showError(message) {
        swal({ title: message, icon: 'error' });
    }

    function showSuccess(message, callback) {
        swal({ title: message, icon: 'success' }).then(function () {
            if (callback) {
                callback();
            }
        });
    }

    function newColumn() {
        return { ColumnName: '', SourceType: 0, FieldKey: '', DefaultValue: '' };
    }

    function emptyForm() {
        return { Id: null, FormatName: '', Columns: [] };
    }

    function loadFormats() {
        $scope.loading = true;
        $http.get(Router.action('ReconciliationIncludeTaxFormat', 'Search'))
            .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '查詢格式失敗');
                    return;
                }
                $scope.formats = response.data.ReturnObject || [];
            })
            .catch(function () {
                showError('查詢格式失敗，請稍後再試');
            })
            .finally(function () {
                $scope.loading = false;
            });
    }

    function loadFieldOptions() {
        $http.get(Router.action('ReconciliationIncludeTaxFormat', 'GetFieldOptions'))
            .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '載入資料欄位失敗');
                    return;
                }
                $scope.fieldOptions = response.data.ReturnObject || [];
            })
            .catch(function () {
                showError('載入資料欄位失敗，請稍後再試');
            });
    }

    function openModal() {
        $('#reconciliationIncludeTaxFormatModal').modal('show');
    }

    function validateForm() {
        if (!$scope.modal.form.FormatName || !$scope.modal.form.FormatName.trim()) {
            return '請輸入格式名稱';
        }
        if ($scope.modal.form.Columns.length === 0) {
            return '請至少設定一個匯出欄位';
        }
        for (var index = 0; index < $scope.modal.form.Columns.length; index++) {
            var column = $scope.modal.form.Columns[index];
            if (!column.ColumnName || !column.ColumnName.trim()) {
                return '第 ' + (index + 1) + ' 個欄位尚未輸入匯出名稱';
            }
            if (column.SourceType === 0 && !column.FieldKey) {
                return '第 ' + (index + 1) + ' 個欄位尚未選擇資料欄位';
            }
        }
        return '';
    }

    $scope.formats = [];
    $scope.fieldOptions = [];
    $scope.sourceTypes = [
        { value: 0, text: '資料欄位' },
        { value: 1, text: '固定值' }
    ];
    $scope.loading = false;
    $scope.modal = {
        form: emptyForm(),
        loading: false,
        saving: false,
        sortableOptions: {
            items: 'tr.sortable-row',
            handle: '.drag-handle',
            placeholder: 'ui-sortable-placeholder'
        }
    };

    $scope.init = function () {
        angular.element('#ReconciliationIncludeTaxFormat').addClass('active');
        loadFieldOptions();
        loadFormats();
    };

    $scope.openCreateModal = function () {
        $scope.modal.form = emptyForm();
        $scope.modal.form.Columns.push(newColumn());
        openModal();
    };

    $scope.openEditModal = function (id) {
        $scope.modal.loading = true;
        $http.get(Router.action('ReconciliationIncludeTaxFormat', 'GetDetail'), { params: { id: id } })
            .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '載入格式失敗');
                    return;
                }
                $scope.modal.form = response.data.ReturnObject;
                openModal();
            })
            .catch(function () {
                showError('載入格式失敗，請稍後再試');
            })
            .finally(function () {
                $scope.modal.loading = false;
            });
    };

    $scope.addColumn = function () {
        $scope.modal.form.Columns.push(newColumn());
    };

    $scope.removeColumn = function (index) {
        $scope.modal.form.Columns.splice(index, 1);
    };

    $scope.saveFormat = function () {
        var validationMessage = validateForm();
        if (validationMessage) {
            showError(validationMessage);
            return;
        }
        $scope.modal.saving = true;
        $http.post(Router.action('ReconciliationIncludeTaxFormat', 'Save'), $scope.modal.form)
            .then(function (response) {
                if (redirectIfNeeded(response.data)) {
                    return;
                }
                if (response.data.status === 'error') {
                    showError(response.data.msg || '儲存格式失敗');
                    return;
                }
                $('#reconciliationIncludeTaxFormatModal').modal('hide');
                showSuccess('儲存成功', loadFormats);
            })
            .catch(function () {
                showError('儲存格式失敗，請稍後再試');
            })
            .finally(function () {
                $scope.modal.saving = false;
            });
    };

    $scope.deleteFormat = function (format) {
        swal({
            title: '確定要刪除「' + format.FormatName + '」嗎？',
            icon: 'warning',
            buttons: ['取消', '刪除'],
            dangerMode: true
        }).then(function (confirmed) {
            if (!confirmed) {
                return;
            }
            $scope.loading = true;
            $http.post(Router.action('ReconciliationIncludeTaxFormat', 'Delete'), { id: format.Id })
                .then(function (response) {
                    if (redirectIfNeeded(response.data)) {
                        return;
                    }
                    if (response.data.status === 'error') {
                        showError(response.data.msg || '刪除格式失敗');
                        return;
                    }
                    loadFormats();
                })
                .catch(function () {
                    showError('刪除格式失敗，請稍後再試');
                })
                .finally(function () {
                    $scope.loading = false;
                });
        });
    };

    $scope.closeModal = function () {
        if (!$scope.modal.saving) {
            $('#reconciliationIncludeTaxFormatModal').modal('hide');
        }
    };
}]);
