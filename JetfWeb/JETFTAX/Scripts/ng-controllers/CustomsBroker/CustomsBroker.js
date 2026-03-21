mainApp.controller('CustomsBrokerController', function ($scope, $http) {
    // 初始化變數
    $scope.searchParams = {
        Name: '',
        ContactPerson: '',
        PortArea: '',
        Category: ''
    };

    $scope.editModel = {
        Id: 0,
        Name: '',
        PortArea: ''
    };

    $scope.contactModel = {
        Id: 0,
        CustomsBrokerId: 0,
        ContactPerson: '',
        Email: '',
        Phone: '',
        Category: ''
    };

    $scope.portAreaOptions = [];
    $scope.customsBrokerOptions = [];
    $scope.dataList = [];
    $scope.isSearched = false;
    $scope.loading = false;
    $scope.modalTitle = '新增報驗公司';
    $scope.contactModalTitle = '新增聯絡人';

    // 分頁相關
    $scope.currentPage = 1;
    $scope.pageSize = 10;
    $scope.totalCount = 0;
    $scope.totalPages = 0;
    $scope.pageSizeOptions = [10, 25, 50, 100];

    // 初始化
    $scope.init = function () {
        $scope.loadPortAreaList();
        $scope.loadCustomsBrokerList();
    };

    // 載入港區選項
    $scope.loadPortAreaList = function () {
        $.ajax({
            url: Router.action('CustomsBroker', 'GetPortAreaList'),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $scope.$apply(function () {
                    $scope.portAreaOptions = data || [];
                });
            },
            error: function () {
                console.error('載入港區選項失敗');
            }
        });
    };

    // 載入報驗公司選項
    $scope.loadCustomsBrokerList = function () {
        $.ajax({
            url: Router.action('CustomsBroker', 'GetAllForDropdown'),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $scope.$apply(function () {
                    $scope.customsBrokerOptions = data || [];
                });
            },
            error: function () {
                console.error('載入報驗公司選項失敗');
            }
        });
    };

    // 查詢
    $scope.performSearch = function () {
        $scope.currentPage = 1;
        $scope.isSearched = true;
        $scope.loadData();
    };

    // 載入資料
    $scope.loadData = function () {
        $scope.loading = true;

        var request = {
            Name: $scope.searchParams.Name,
            ContactPerson: $scope.searchParams.ContactPerson,
            PortArea: $scope.searchParams.PortArea,
            Category: $scope.searchParams.Category,
            Page: $scope.currentPage,
            PageSize: $scope.pageSize
        };

        $.ajax({
            url: Router.action('CustomsBroker', 'GetData'),
            type: 'POST',
            data: request,
            dataType: 'json',
            success: function (response) {
                $scope.$apply(function () {
                    if (response.Redirect) {
                        window.location = Router.action('Account', 'Login');
                        return;
                    }

                    if (response.error) {
                        swal({
                            title: "查詢失敗",
                            text: response.error,
                            icon: "error"
                        });
                        return;
                    }

                    $scope.dataList = response.Data || response.data || [];
                    $scope.totalCount = response.TotalCount || 0;
                    $scope.totalPages = Math.ceil($scope.totalCount / $scope.pageSize);
                    $scope.initPagination();
                });
            },
            error: function () {
                swal({
                    title: "查詢失敗",
                    text: "請稍後再試或聯繫系統管理員",
                    icon: "error"
                });
            },
            complete: function () {
                $scope.$apply(function () {
                    $scope.loading = false;
                });
            }
        });
    };

    // 初始化分頁
    $scope.initPagination = function () {
        var $pagination = $('#pagination-twbs');

        if ($pagination.data('twbs-pagination')) {
            $pagination.twbsPagination('destroy');
        }

        if ($scope.totalPages <= 1) {
            $pagination.empty();
            return;
        }

        try {
            $pagination.twbsPagination({
                totalPages: $scope.totalPages,
                visiblePages: 10,
                startPage: $scope.currentPage,
                initiateStartPageClick: false,
                hideOnlyOnePage: true,
                first: '第一頁',
                prev: '上一頁',
                next: '下一頁',
                last: '最後一頁',
                onPageClick: function (event, page) {
                    if (page !== $scope.currentPage) {
                        $scope.$apply(function () {
                            $scope.currentPage = page;
                            $scope.loadData();
                        });
                    }
                }
            });
        } catch (error) {
            console.error('Error initializing twbsPagination:', error);
        }
    };

    // 更新記錄資訊
    $scope.getRecordsInfo = function () {
        if (!$scope.isSearched || $scope.totalCount === 0) {
            return '顯示第 0 到 0 筆資料，共 0 筆';
        }
        var start = ($scope.currentPage - 1) * $scope.pageSize + 1;
        var end = Math.min($scope.currentPage * $scope.pageSize, $scope.totalCount);
        return '顯示第 ' + start + ' 到 ' + end + ' 筆資料，共 ' + $scope.totalCount + ' 筆';
    };

    // 改變每頁筆數
    $scope.changePageSize = function () {
        $scope.currentPage = 1;
        if ($scope.isSearched) {
            $scope.loadData();
        }
    };

    // 清除查詢表單
    $scope.clearSearch = function () {
        $scope.searchParams = {
            Name: '',
            ContactPerson: '',
            PortArea: '',
            Category: ''
        };
        $scope.isSearched = false;
        $scope.dataList = [];
        $scope.totalCount = 0;
        $scope.totalPages = 0;
        $scope.currentPage = 1;

        var $pagination = $('#pagination-twbs');
        if ($pagination.data('twbs-pagination')) {
            $pagination.twbsPagination('destroy');
        }
    };

    // 顯示新增報驗公司 Modal
    $scope.showAddModal = function () {
        $scope.clearForm();
        $scope.modalTitle = '新增報驗公司';
        $('#customsBrokerModal').modal('show');
    };

    // 編輯報驗公司
    $scope.editCustomsBroker = function (id) {
        $scope.loading = true;

        $.ajax({
            url: Router.action('CustomsBroker', 'GetById'),
            type: 'POST',
            data: { id: id },
            dataType: 'json',
            success: function (response) {
                $scope.$apply(function () {
                    if (response.status === 'success' && response.ReturnObject) {
                        var data = response.ReturnObject;

                        $scope.editModel = {
                            Id: data.Id,
                            Name: data.Name,
                            PortArea: data.PortArea
                        };

                        $scope.modalTitle = '編輯報驗公司';
                        $('#customsBrokerModal').modal('show');
                    } else {
                        swal({
                            title: "錯誤",
                            text: response.msg || "取得資料失敗",
                            icon: "error"
                        });
                    }
                });
            },
            error: function () {
                swal({
                    title: "錯誤",
                    text: "取得資料失敗",
                    icon: "error"
                });
            },
            complete: function () {
                $scope.$apply(function () {
                    $scope.loading = false;
                });
            }
        });
    };

    // 刪除報驗公司
    $scope.deleteCustomsBroker = function (id, name) {
        swal({
            title: "確認刪除",
            text: '確定要刪除 "' + name + '" 嗎？',
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    value: null,
                    visible: true,
                    className: "",
                    closeModal: true,
                },
                confirm: {
                    text: "確定刪除",
                    value: true,
                    visible: true,
                    className: "btn-danger",
                    closeModal: true
                }
            }
        }).then(function (willDelete) {
            if (willDelete) {
                $scope.loading = true;

                $.ajax({
                    url: Router.action('CustomsBroker', 'Delete'),
                    type: 'POST',
                    data: { id: id },
                    dataType: 'json',
                    success: function (response) {
                        if (response.status === 'success') {
                            swal({
                                title: "成功",
                                text: response.msg,
                                icon: "success"
                            }).then(function () {
                                if ($scope.isSearched) {
                                    $scope.$apply(function () {
                                        $scope.loadData();
                                    });
                                }
                            });
                        } else {
                            swal({
                                title: "錯誤",
                                text: response.msg,
                                icon: "error"
                            });
                        }
                    },
                    error: function () {
                        swal({
                            title: "錯誤",
                            text: "刪除失敗",
                            icon: "error"
                        });
                    },
                    complete: function () {
                        $scope.$apply(function () {
                            $scope.loading = false;
                        });
                    }
                });
            }
        });
    };

    // 儲存報驗公司
    $scope.saveCustomsBroker = function () {
        // 表單驗證
        if (!$scope.editModel.Name || !$scope.editModel.Name.trim()) {
            swal({
                title: "驗證錯誤",
                text: "請輸入公司名稱",
                icon: "warning"
            });
            return;
        }

        $scope.loading = true;

        var url = $scope.editModel.Id > 0 ?
            Router.action('CustomsBroker', 'Update') :
            Router.action('CustomsBroker', 'Insert');

        $.ajax({
            url: url,
            type: 'POST',
            data: $scope.editModel,
            dataType: 'json',
            success: function (response) {
                if (response.status === 'success') {
                    swal({
                        title: "成功",
                        text: response.msg,
                        icon: "success"
                    }).then(function () {
                        $('#customsBrokerModal').modal('hide');
                        // 重新載入下拉選單
                        $scope.loadCustomsBrokerList();
                        if ($scope.isSearched) {
                            $scope.$apply(function () {
                                $scope.loadData();
                            });
                        }
                    });
                } else {
                    swal({
                        title: "錯誤",
                        text: response.msg,
                        icon: "error"
                    });
                }
            },
            error: function () {
                swal({
                    title: "錯誤",
                    text: "儲存失敗",
                    icon: "error"
                });
            },
            complete: function () {
                $scope.$apply(function () {
                    $scope.loading = false;
                });
            }
        });
    };

    // 清除表單
    $scope.clearForm = function () {
        $scope.editModel = {
            Id: 0,
            Name: '',
            PortArea: ''
        };
    };

    // 顯示新增聯絡人 Modal
    $scope.showAddContactModal = function () {
        $scope.clearContactForm();
        $scope.contactModalTitle = '新增聯絡人';
        $('#contactModal').modal('show');
    };

    // 編輯聯絡人
    $scope.editContact = function (id) {
        $scope.loading = true;

        $.ajax({
            url: Router.action('CustomsBroker', 'GetContactById'),
            type: 'POST',
            data: { id: id },
            dataType: 'json',
            success: function (response) {
                $scope.$apply(function () {
                    if (response.status === 'success' && response.ReturnObject) {
                        var data = response.ReturnObject;

                        $scope.contactModel = {
                            Id: data.Id,
                            CustomsBrokerId: String(data.CustomsBrokerId), // 轉換為字串以配合 select
                            ContactPerson: data.ContactPerson || '',
                            Email: data.Email || '',
                            Phone: data.Phone || '',
                            Category: data.Category || ''
                        };

                        $scope.contactModalTitle = '編輯聯絡人';
                        $('#contactModal').modal('show');
                    } else {
                        swal({
                            title: "錯誤",
                            text: response.msg || "取得資料失敗",
                            icon: "error"
                        });
                    }
                });
            },
            error: function () {
                swal({
                    title: "錯誤",
                    text: "取得資料失敗",
                    icon: "error"
                });
            },
            complete: function () {
                $scope.$apply(function () {
                    $scope.loading = false;
                });
            }
        });
    };

    // 刪除聯絡人
    $scope.deleteContact = function (id, name) {
        swal({
            title: "確認刪除",
            text: '確定要刪除聯絡人 "' + name + '" 嗎？',
            icon: "warning",
            buttons: {
                cancel: {
                    text: "取消",
                    value: null,
                    visible: true,
                    className: "",
                    closeModal: true,
                },
                confirm: {
                    text: "確定刪除",
                    value: true,
                    visible: true,
                    className: "btn-danger",
                    closeModal: true
                }
            }
        }).then(function (willDelete) {
            if (willDelete) {
                $scope.loading = true;

                $.ajax({
                    url: Router.action('CustomsBroker', 'DeleteContact'),
                    type: 'POST',
                    data: { id: id },
                    dataType: 'json',
                    success: function (response) {
                        if (response.status === 'success') {
                            swal({
                                title: "成功",
                                text: response.msg,
                                icon: "success"
                            }).then(function () {
                                if ($scope.isSearched) {
                                    $scope.$apply(function () {
                                        $scope.loadData();
                                    });
                                }
                            });
                        } else {
                            swal({
                                title: "錯誤",
                                text: response.msg,
                                icon: "error"
                            });
                        }
                    },
                    error: function () {
                        swal({
                            title: "錯誤",
                            text: "刪除失敗",
                            icon: "error"
                        });
                    },
                    complete: function () {
                        $scope.$apply(function () {
                            $scope.loading = false;
                        });
                    }
                });
            }
        });
    };

    // 儲存聯絡人
    $scope.saveContact = function () {
        // 表單驗證
        if (!$scope.contactModel.CustomsBrokerId || $scope.contactModel.CustomsBrokerId === '0') {
            swal({
                title: "驗證錯誤",
                text: "請選擇報驗公司",
                icon: "warning"
            });
            return;
        }

        if (!$scope.contactModel.ContactPerson || !$scope.contactModel.ContactPerson.trim()) {
            swal({
                title: "驗證錯誤",
                text: "請輸入聯絡人",
                icon: "warning"
            });
            return;
        }

        $scope.loading = true;

        var url = $scope.contactModel.Id > 0 ?
            Router.action('CustomsBroker', 'UpdateContact') :
            Router.action('CustomsBroker', 'InsertContact');

        // 將 CustomsBrokerId 轉換為數字，確保 Email 和 Phone 欄位被傳送
        var submitData = {
            Id: $scope.contactModel.Id,
            CustomsBrokerId: parseInt($scope.contactModel.CustomsBrokerId),
            ContactPerson: $scope.contactModel.ContactPerson || '',
            Email: $scope.contactModel.Email || '',  // 確保有預設值
            Phone: $scope.contactModel.Phone || '',   // 確保有預設值
            Category: $scope.contactModel.Category || ''
        };

        // 除錯用：顯示要傳送的資料
        console.log('儲存聯絡人資料:', submitData);

        $.ajax({
            url: url,
            type: 'POST',
            data: submitData,
            dataType: 'json',
            success: function (response) {
                if (response.status === 'success') {
                    swal({
                        title: "成功",
                        text: response.msg,
                        icon: "success"
                    }).then(function () {
                        $('#contactModal').modal('hide');
                        if ($scope.isSearched) {
                            $scope.$apply(function () {
                                $scope.loadData();
                            });
                        }
                    });
                } else {
                    swal({
                        title: "錯誤",
                        text: response.msg,
                        icon: "error"
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('儲存聯絡人失敗:', error);
                console.error('回應內容:', xhr.responseText);
                swal({
                    title: "錯誤",
                    text: "儲存失敗：" + error,
                    icon: "error"
                });
            },
            complete: function () {
                $scope.$apply(function () {
                    $scope.loading = false;
                });
            }
        });
    };

    // 清除聯絡人表單
    $scope.clearContactForm = function () {
        $scope.contactModel = {
            Id: 0,
            CustomsBrokerId: '0', // 改為字串
            ContactPerson: '',
            Email: '',
            Phone: '',
            Category: ''
        };
    };

    // 格式化日期
    $scope.formatDate = function (dateString) {
        if (!dateString) return '';

        var date = new Date(dateString);
        if (isNaN(date.getTime())) {
            var match = dateString.match(/\/Date\((\d+)\)\//);
            if (match) {
                date = new Date(parseInt(match[1]));
            } else {
                return dateString;
            }
        }

        var year = date.getFullYear();
        var month = ('0' + (date.getMonth() + 1)).slice(-2);
        var day = ('0' + date.getDate()).slice(-2);
        var hours = ('0' + date.getHours()).slice(-2);
        var minutes = ('0' + date.getMinutes()).slice(-2);

        return year + '/' + month + '/' + day + ' ' + hours + ':' + minutes;
    };

    // 截斷按鈕處理
    $scope.setupTruncateButtons = function () {
        setTimeout(function () {
            $('#customsBrokerTable .truncate-cell').each(function () {
                var span = $(this).find('.text');
                if (span.length && span[0].scrollWidth > span[0].clientWidth) {
                    $(this).find('.expand-btn').show();
                }
            });
        }, 100);
    };

    // 展開/收起文字
    $scope.toggleExpand = function ($event) {
        var btn = $($event.currentTarget);
        var container = btn.closest('.truncate-cell');
        container.toggleClass('expanded');
        btn.text(container.hasClass('expanded') ? '收起' : '展開');
    };

    // Modal 關閉時清除表單
    $('#customsBrokerModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.clearForm();
        });
    });

    $('#contactModal').on('hidden.bs.modal', function () {
        $scope.$apply(function () {
            $scope.clearContactForm();
        });
    });

    // 初始化
    $scope.init();
});
