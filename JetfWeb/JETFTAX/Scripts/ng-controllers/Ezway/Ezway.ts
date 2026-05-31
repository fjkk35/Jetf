/// <reference path="../../types/global.d.ts" />

interface EzwayCaptchaStateResponse {
    CaptchaRequired?: boolean;
    CaptchaImageBase64?: string;
    CaptchaCode?: string;
}

interface EzwayPageStateResponse {
    IsLoggedIn?: boolean;
    LoginCaptchaState?: EzwayCaptchaStateResponse;
    QueryCaptchaState?: EzwayCaptchaStateResponse;
}

interface EzwayLoginResultResponse {
    IsLoggedIn?: boolean;
    RequiresTermsAgreement?: boolean;
    TermsHtml?: string;
}

interface EzwayQueryResultResponse {
    ImportDate?: string;
    DeclNo?: string;
    MawbNo?: string;
    HawbNo?: string;
    TelNo?: string;
    IdNo?: string;
    ReplyDate?: string;
    ReplyTime?: string;
    IsReply?: string;
    AuthorizeDocNo?: string;
    AuthorizeReply?: string;
    AuthorizeDatm?: string;
    BlockReason?: string;
}

interface EzwayQueryResponse {
    Results?: EzwayQueryResultResponse[];
    QueryCaptchaState?: EzwayCaptchaStateResponse;
}

interface EzwayDownloadResponse {
    status?: string;
    msg?: string;
    fileGuid?: string;
    fileName?: string;
}

interface EzwayScope extends ng.IScope {
    loading: boolean;
    isLoggedIn: boolean;
    loginForm: {
        companyId: string;
        account: string;
        password: string;
        captcha: string;
    };
    loginState: {
        captchaRequired: boolean;
        captchaImageBase64: string;
        captchaCode: string;
        errorMessage: string;
        termsAccepted: boolean;
        termsHtml: string;
    };
    queryState: {
        activeQueryApi: string;
        queryMode: string;
        hawbNo: string;
        queryCaptcha: string;
        queryCaptchaRequired: boolean;
        queryCaptchaImageBase64: string;
        queryCaptchaCode: string;
        queryErrorMessage: string;
        results: EzwayQueryResultResponse[];
    };
    initialize: () => void;
    refreshLoginCaptcha: () => void;
    login: (termsAccepted?: boolean) => ng.IPromise<void> | void;
    logout: () => ng.IPromise<void> | void;
    confirmTermsAndLogin: () => void;
    closeTermsModal: () => void;
    refreshQueryCaptcha: () => void;
    query: () => ng.IPromise<void> | void;
    batchQuery: () => ng.IPromise<void> | void;
    exportExcel: () => ng.IPromise<void> | void;
    clearQueryForm: () => void;
    onHawbInputChanged: () => void;
    onQueryModeChanged: () => void;
    selectQueryApi: (queryApiType: string) => void;
    getReplyDateTime: (item: EzwayQueryResultResponse) => string;
}

mainApp.controller('EzwayController', ['$scope', '$http', function (
    $scope: EzwayScope,
    $http: ng.IHttpService
) {
    function setLoginError(message: string): void {
        $scope.loginState.errorMessage = message || '';
    }

    function setQueryError(message: string): void {
        $scope.queryState.queryErrorMessage = message || '';
    }

    function resetResults(): void {
        $scope.queryState.results = [];
    }

    function clearTermsContainer(): void {
        var container = document.getElementById('ezwayTermsContent');
        if (container) {
            container.innerHTML = '';
        }
    }

    function needsReinitialize(message: string): boolean {
        return !!message && (
            message.indexOf('重新登入') !== -1 ||
            message.indexOf('尚未登入') !== -1 ||
            message.indexOf('業者資訊不存在') !== -1
        );
    }

    function applyLoginCaptchaState(state?: EzwayCaptchaStateResponse | null): void {
        var captchaState = state || {};
        $scope.loginState.captchaRequired = !!captchaState.CaptchaRequired;
        $scope.loginState.captchaImageBase64 = captchaState.CaptchaImageBase64 || '';
        $scope.loginState.captchaCode = captchaState.CaptchaCode || '';
        $scope.loginForm.captcha = '';
    }

    function applyQueryCaptchaState(state?: EzwayCaptchaStateResponse | null): void {
        var captchaState = state || {};
        $scope.queryState.queryCaptchaRequired = !!captchaState.CaptchaRequired;
        $scope.queryState.queryCaptchaImageBase64 = captchaState.CaptchaImageBase64 || '';
        $scope.queryState.queryCaptchaCode = captchaState.CaptchaCode || '';
        $scope.queryState.queryCaptcha = '';
    }

    function showTermsModal(html: string): void {
        $scope.loginState.termsAccepted = false;
        $scope.loginState.termsHtml = html || '';

        var container = document.getElementById('ezwayTermsContent');
        if (container) {
            container.innerHTML = $scope.loginState.termsHtml;
        }

        ($('#ezwayTermsModal') as any).modal({
            backdrop: 'static',
            keyboard: false
        });
        ($('#ezwayTermsModal') as any).modal('show');
    }

    function hideTermsModal(): void {
        ($('#ezwayTermsModal') as any).modal('hide');
        $scope.loginState.termsAccepted = false;
        $scope.loginState.termsHtml = '';
        clearTermsContainer();
    }

    function applyLoggedOutState(pageState?: EzwayPageStateResponse | null): void {
        $scope.isLoggedIn = false;
        $scope.loginForm.password = '';
        $scope.loginForm.captcha = '';
        $scope.queryState.hawbNo = '';
        $scope.queryState.queryCaptcha = '';
        $scope.queryState.queryCaptchaRequired = false;
        $scope.queryState.queryCaptchaImageBase64 = '';
        $scope.queryState.queryCaptchaCode = '';
        setLoginError('');
        setQueryError('');
        resetResults();
        hideTermsModal();
        applyLoginCaptchaState(pageState ? pageState.LoginCaptchaState : null);
    }

    function downloadFile(fileGuid: string, fileName: string): void {
        var downloadUrl = Router.action('Download', 'DownloadFile') +
            '?fileGuid=' + encodeURIComponent(fileGuid) +
            '&fileName=' + encodeURIComponent(fileName);

        var link = document.createElement('a');
        link.href = downloadUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    function requestLoginCaptcha(displayError: boolean): ng.IPromise<void> {
        return $http.get(Router.action('Ezway', 'RefreshLoginCaptcha'))
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwayCaptchaStateResponse>>) {
                var data = response.data || {};
                if (data.msg) {
                    setLoginError(data.msg);
                    if (displayError) {
                        swal({
                            title: '登入驗證碼失敗',
                            text: data.msg,
                            icon: 'error'
                        });
                    }
                    return;
                }

                applyLoginCaptchaState(data.ReturnObject);
                setLoginError('');
            }, function (): void {
                if (displayError) {
                    setLoginError('刷新登入驗證碼失敗，請稍後再試');
                    swal({
                        title: '登入驗證碼失敗',
                        text: '刷新登入驗證碼失敗，請稍後再試',
                        icon: 'error'
                    });
                }
            });
    }

    function requestQuerySetting(displayError: boolean): ng.IPromise<void> {
        return $http.get(Router.action('Ezway', 'QuerySetting'))
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwayCaptchaStateResponse>>) {
                var data = response.data || {};
                if (data.msg) {
                    setQueryError(data.msg);

                    if (displayError) {
                        swal({
                            title: '查詢驗證碼失敗',
                            text: data.msg,
                            icon: 'error'
                        });
                    }

                    if (needsReinitialize(data.msg)) {
                        $scope.isLoggedIn = false;
                        resetResults();
                        $scope.initialize();
                    }
                    return;
                }

                applyQueryCaptchaState(data.ReturnObject);
                setQueryError('');
            }, function (): void {
                if (displayError) {
                    setQueryError('取得查詢驗證碼失敗，請稍後再試');
                    swal({
                        title: '查詢驗證碼失敗',
                        text: '取得查詢驗證碼失敗，請稍後再試',
                        icon: 'error'
                    });
                }
            });
    }

    function buildLoginRequest(termsAccepted: boolean): any {
        return {
            CompanyId: $scope.loginForm.companyId,
            Account: $scope.loginForm.account,
            Password: $scope.loginForm.password,
            Captcha: $scope.loginForm.captcha,
            CaptchaCode: $scope.loginState.captchaCode,
            CaptchaRequired: $scope.loginState.captchaRequired,
            TermsAccepted: termsAccepted
        };
    }

    function buildQueryRequest(): any {
        return {
            Manual: $scope.queryState.queryMode === 'Batch' ? 'N' : 'Y',
            QueryApiType: $scope.queryState.activeQueryApi,
            HawbNo: $scope.queryState.hawbNo,
            QueryCaptcha: $scope.queryState.queryCaptcha,
            QueryCaptchaCode: $scope.queryState.queryCaptchaCode,
            QueryCaptchaRequired: $scope.queryState.queryCaptchaRequired
        };
    }

    function validateSingleQuery(): string {
        var hawbNumbers = ($scope.queryState.hawbNo || '')
            .split(/\r?\n/)
            .map(function (value: string): string {
                return value.trim();
            })
            .filter(function (value: string): boolean {
                return !!value;
            });

        if (!hawbNumbers.length) {
            return '請輸入分提單號';
        }

        var invalidHawb = hawbNumbers.some(function (value: string): boolean {
            return value.length > 35;
        });

        if (invalidHawb) {
            return '分提單號碼最長 35 碼';
        }

        if ($scope.queryState.queryMode === 'Single' && hawbNumbers.length > 10) {
            return '查詢超過10筆，請使用整批查詢';
        }

        return '';
    }

    $scope.loading = false;
    $scope.isLoggedIn = false;
    $scope.loginForm = {
        companyId: '24951752',
        account: 'ECC0001',
        password: '',
        captcha: ''
    };
    $scope.loginState = {
        captchaRequired: false,
        captchaImageBase64: '',
        captchaCode: '',
        errorMessage: '',
        termsAccepted: false,
        termsHtml: ''
    };
    $scope.queryState = {
        activeQueryApi: 'Simple',
        queryMode: 'Single',
        hawbNo: '',
        queryCaptcha: '',
        queryCaptchaRequired: false,
        queryCaptchaImageBase64: '',
        queryCaptchaCode: '',
        queryErrorMessage: '',
        results: []
    };

    $scope.initialize = function (): void {
        setLoginError('');
        setQueryError('');
        $scope.loading = true;

        $http.get(Router.action('Ezway', 'Initialize'))
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwayPageStateResponse>>) {
                var data = response.data || {};
                if (data.msg) {
                    setLoginError(data.msg);
                    swal({
                        title: '初始化失敗',
                        text: data.msg,
                        icon: 'error'
                    });
                    return;
                }

                var pageState = data.ReturnObject || {};
                $scope.isLoggedIn = !!pageState.IsLoggedIn;

                if ($scope.isLoggedIn) {
                    applyQueryCaptchaState(pageState.QueryCaptchaState);
                    resetResults();
                    setQueryError('');
                    return;
                }

                applyLoginCaptchaState(pageState.LoginCaptchaState);
                resetResults();
                setLoginError('');
            })
            .catch(function (): void {
                setLoginError('Ezway 初始化失敗，請稍後再試');
                swal({
                    title: '初始化失敗',
                    text: 'Ezway 初始化失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.refreshLoginCaptcha = function (): void {
        $scope.loading = true;
        requestLoginCaptcha(true)
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.login = function (termsAccepted?: boolean): ng.IPromise<void> | void {
        setLoginError('');

        if (!$scope.loginForm.companyId || !$scope.loginForm.companyId.trim()) {
            setLoginError('請輸入統一編號');
            swal({ title: '登入資料不完整', text: '請輸入統一編號', icon: 'warning' });
            return;
        }

        if (!$scope.loginForm.account || !$scope.loginForm.account.trim()) {
            setLoginError('請輸入帳號');
            swal({ title: '登入資料不完整', text: '請輸入帳號', icon: 'warning' });
            return;
        }

        if (!$scope.loginForm.password) {
            setLoginError('請輸入密碼');
            swal({ title: '登入資料不完整', text: '請輸入密碼', icon: 'warning' });
            return;
        }

        if ($scope.loginState.captchaRequired && (!$scope.loginForm.captcha || !$scope.loginForm.captcha.trim())) {
            setLoginError('請輸入登入驗證碼');
            swal({ title: '登入資料不完整', text: '請輸入登入驗證碼', icon: 'warning' });
            return;
        }

        $scope.loading = true;

        return $http.post(Router.action('Ezway', 'Login'), buildLoginRequest(!!termsAccepted))
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwayLoginResultResponse>>) {
                var data = response.data || {};
                var result = data.ReturnObject || {};

                if (result.RequiresTermsAgreement) {
                    showTermsModal(result.TermsHtml || '');
                    return;
                }

                if (result.IsLoggedIn) {
                    $scope.isLoggedIn = true;
                    $scope.loginForm.password = '';
                    $scope.loginForm.captcha = '';
                    setLoginError('');

                    return requestQuerySetting(false)
                        .then(function (): void {
                            hideTermsModal();
                            swal({
                                title: '登入成功',
                                text: '已成功登入 Ezway 系統',
                                icon: 'success'
                            });
                        });
                }

                setLoginError(data.msg || 'Ezway 登入失敗');
                swal({
                    title: '登入失敗',
                    text: data.msg || 'Ezway 登入失敗',
                    icon: 'error'
                });

                if ($scope.loginState.captchaRequired) {
                    return requestLoginCaptcha(false);
                }
            }, function (): ng.IPromise<void> | void {
                setLoginError('Ezway 登入失敗，請稍後再試');
                swal({
                    title: '登入失敗',
                    text: 'Ezway 登入失敗，請稍後再試',
                    icon: 'error'
                });

                if ($scope.loginState.captchaRequired) {
                    return requestLoginCaptcha(false);
                }
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.confirmTermsAndLogin = function (): void {
        if (!$scope.loginState.termsAccepted) {
            swal({
                title: '請先確認服務條款',
                text: '勾選同意後才能完成登入',
                icon: 'warning'
            });
            return;
        }

        $scope.login(true);
    };

    $scope.logout = function (): ng.IPromise<void> | void {
        $scope.loading = true;

        return $http.post(Router.action('Ezway', 'Logout'), {})
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwayPageStateResponse>>) {
                var data = response.data || {};
                if (data.msg) {
                    setLoginError(data.msg);
                    swal({
                        title: '登出失敗',
                        text: data.msg,
                        icon: 'error'
                    });
                    return;
                }

                applyLoggedOutState(data.ReturnObject);
                swal({
                    title: '已登出',
                    text: 'Ezway 登入資訊已清除，請重新登入',
                    icon: 'success'
                });
            }, function (): void {
                setLoginError('Ezway 登出失敗，請稍後再試');
                swal({
                    title: '登出失敗',
                    text: 'Ezway 登出失敗，請稍後再試',
                    icon: 'error'
                });
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.closeTermsModal = function (): void {
        hideTermsModal();
    };

    $scope.refreshQueryCaptcha = function (): void {
        $scope.loading = true;
        requestQuerySetting(true)
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.query = function (): ng.IPromise<void> | void {
        setQueryError('');

        var validationMessage = validateSingleQuery();
        if (validationMessage) {
            setQueryError(validationMessage);
            swal({ title: '查詢資料不完整', text: validationMessage, icon: 'warning' });
            return;
        }

        if ($scope.queryState.queryCaptchaRequired && (!$scope.queryState.queryCaptcha || !$scope.queryState.queryCaptcha.trim())) {
            setQueryError('請輸入查詢驗證碼');
            swal({ title: '查詢資料不完整', text: '請輸入查詢驗證碼', icon: 'warning' });
            return;
        }

        $scope.loading = true;

        return $http.post(Router.action('Ezway', 'Query'), buildQueryRequest())
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwayQueryResponse>>) {
                var data = response.data || {};
                if (data.ReturnObject) {
                    var result = data.ReturnObject;
                    $scope.queryState.results = result.Results || [];
                    applyQueryCaptchaState(result.QueryCaptchaState);
                    setQueryError('');

                    if ($scope.queryState.results.length === 0) {
                        swal({
                            title: '查詢完成',
                            text: '查無資料',
                            icon: 'info'
                        });
                    }
                    return;
                }

                setQueryError(data.msg || 'Ezway 查詢失敗');
                swal({
                    title: '查詢失敗',
                    text: data.msg || 'Ezway 查詢失敗',
                    icon: 'error'
                });

                if (needsReinitialize(data.msg || '')) {
                    $scope.isLoggedIn = false;
                    resetResults();
                    $scope.initialize();
                    return;
                }

                return requestQuerySetting(false);
            }, function (): ng.IPromise<void> {
                setQueryError('Ezway 查詢失敗，請稍後再試');
                swal({
                    title: '查詢失敗',
                    text: 'Ezway 查詢失敗，請稍後再試',
                    icon: 'error'
                });
                return requestQuerySetting(false);
            })
            .finally(function (): void {
                $scope.loading = false;
            });
    };

    $scope.batchQuery = function (): ng.IPromise<void> | void {
        setQueryError('');

        var validationMessage = validateSingleQuery();
        if (validationMessage) {
            setQueryError(validationMessage);
            swal({ title: '查詢資料不完整', text: validationMessage, icon: 'warning' });
            return;
        }

        if ($scope.queryState.queryCaptchaRequired && (!$scope.queryState.queryCaptcha || !$scope.queryState.queryCaptcha.trim())) {
            setQueryError('請輸入查詢驗證碼');
            swal({ title: '查詢資料不完整', text: '請輸入查詢驗證碼', icon: 'warning' });
            return;
        }

        $scope.loading = true;

        return $http.post(Router.action('Ezway', 'BatchQuery'), buildQueryRequest())
        .then(function (response: ng.IHttpResponse<ApiResponse<EzwayQueryResponse>>) {
            var data = response.data || {};
            if (data.ReturnObject) {
                var result = data.ReturnObject;
                $scope.queryState.results = result.Results || [];
                applyQueryCaptchaState(result.QueryCaptchaState);
                setQueryError('');

                if ($scope.queryState.results.length === 0) {
                    swal({
                        title: '整批查詢完成',
                        text: '查無資料',
                        icon: 'info'
                    });
                }
                return;
            }

            setQueryError(data.msg || 'Ezway 整批查詢失敗');
            swal({
                title: '整批查詢失敗',
                text: data.msg || 'Ezway 整批查詢失敗',
                icon: 'error'
            });

            if (needsReinitialize(data.msg || '')) {
                $scope.isLoggedIn = false;
                resetResults();
                $scope.initialize();
                return;
            }

            return requestQuerySetting(false);
        }, function (): ng.IPromise<void> {
            setQueryError('Ezway 整批查詢失敗，請稍後再試');
            swal({
                title: '整批查詢失敗',
                text: 'Ezway 整批查詢失敗，請稍後再試',
                icon: 'error'
            });
            return requestQuerySetting(false);
        }).finally(function (): void {
            $scope.loading = false;
        });
    };

    $scope.exportExcel = function (): ng.IPromise<void> | void {
        if (!$scope.queryState.results.length) {
            var validationMessage = validateSingleQuery();
            if (validationMessage) {
                setQueryError(validationMessage);
                swal({ title: '匯出資料不完整', text: validationMessage, icon: 'warning' });
                return;
            }

            if ($scope.queryState.queryCaptchaRequired && (!$scope.queryState.queryCaptcha || !$scope.queryState.queryCaptcha.trim())) {
                setQueryError('請輸入查詢驗證碼');
                swal({ title: '匯出資料不完整', text: '請輸入查詢驗證碼', icon: 'warning' });
                return;
            }
        }

        $scope.loading = true;

        return $http.post(Router.action('Ezway', 'ExportExcel'), {
            Results: $scope.queryState.results,
            QueryRequest: buildQueryRequest()
        }).then(function (response: ng.IHttpResponse<EzwayDownloadResponse>) {
            var result = response.data || {};
            if (result.status === 'error' || result.msg) {
                swal({
                    title: '匯出失敗',
                    text: result.msg || '匯出失敗',
                    icon: 'error'
                });
                return;
            }

            if (result.fileGuid && result.fileName) {
                downloadFile(result.fileGuid, result.fileName);
                swal({
                    title: '匯出成功',
                    text: '檔案已開始下載',
                    icon: 'success'
                });
            }
        }, function (): void {
            swal({
                title: '匯出失敗',
                text: 'Ezway 匯出失敗，請稍後再試',
                icon: 'error'
            });
        }).finally(function (): void {
            $scope.loading = false;
        });
    };

    $scope.onHawbInputChanged = function (): void {
        setQueryError('');
        resetResults();
    };

    $scope.clearQueryForm = function (): void {
        $scope.queryState.hawbNo = '';
        $scope.queryState.queryCaptcha = '';
        setQueryError('');
        resetResults();
    };

    $scope.onQueryModeChanged = function (): void {
        setQueryError('');
        resetResults();
    };

    $scope.selectQueryApi = function (queryApiType: string): void {
        if ($scope.queryState.activeQueryApi === queryApiType) {
            return;
        }

        $scope.queryState.activeQueryApi = queryApiType;
        $scope.queryState.queryMode = 'Single';
        $scope.clearQueryForm();
    };

    $scope.getReplyDateTime = function (item: EzwayQueryResultResponse): string {
        var replyDate = item && item.ReplyDate ? item.ReplyDate : '';
        var replyTime = item && item.ReplyTime ? item.ReplyTime : '';
        if (!replyDate) {
            return replyTime;
        }

        if (!replyTime) {
            return replyDate;
        }

        return replyDate + ' ' + replyTime;
    };

    $scope.initialize();
}]);