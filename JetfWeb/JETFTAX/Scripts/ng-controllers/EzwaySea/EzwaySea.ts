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
    CurrentAccount?: EzwayLoggedInAccountResponse;
    LoggedInAccounts?: EzwayLoggedInAccountResponse[];
}

interface EzwayLoginResultResponse {
    IsLoggedIn?: boolean;
    RequiresTermsAgreement?: boolean;
    TermsHtml?: string;
    CurrentAccount?: EzwayLoggedInAccountResponse;
}

interface EzwayQueryResultResponse {
    ConsolidatorName?: string;
    GroupBrokerUser?: string;
    BrokerUser?: string;
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
    NotificationFlag?: string;
    TotCustomsValueAmt?: string;
    BlockReason?: string;
}

interface EzwayQueryResponse {
    Results?: EzwayQueryResultResponse[];
    QueryCaptchaState?: EzwayCaptchaStateResponse;
}

interface EzwaySeaBrokerOptionResponse {
    Value?: string;
    Label?: string;
}

interface EzwaySeaConsolidatorOptionResponse {
    Value?: string;
    Label?: string;
    UserId?: string | null;
}

interface EzwaySeaQueryOptionsResponse {
    BrokerQueryField?: string;
    BrokerOptions?: EzwaySeaBrokerOptionResponse[];
    SelectedBrokerValue?: string;
    ConsolidatorOptions?: EzwaySeaConsolidatorOptionResponse[];
    SelectedConsolidator?: string;
    SelectedConsolidatorUserId?: string | null;
}

interface EzwayDownloadResponse {
    status?: string;
    msg?: string;
    fileGuid?: string;
    fileName?: string;
}

interface EzwayLoginProfileOption {
    key: string;
    label: string;
    companyId: string;
    account: string;
}

interface EzwayLoggedInAccountResponse {
    AccountSessionKey?: string;
    LoginProfileKey?: string;
    LoginProfileLabel?: string;
    CompanyId?: string;
    Account?: string;
    CanUseX4?: boolean;
}

interface EzwaySeaScope extends ng.IScope {
    loading: boolean;
    isLoggedIn: boolean;
    activeLoggedInAccount: EzwayLoggedInAccountResponse | null;
    loggedInAccounts: EzwayLoggedInAccountResponse[];
    selectedLoginProfile: string;
    loginProfiles: EzwayLoginProfileOption[];
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
        brokerQueryField: string;
        brokerOptions: EzwaySeaBrokerOptionResponse[];
        selectedBrokerValue: string;
        consolidatorOptions: EzwaySeaConsolidatorOptionResponse[];
        selectedConsolidator: string;
        selectedConsolidatorUserId: string | null;
        hawbNo: string;
        hawbCount: number;
        queryCaptcha: string;
        queryCaptchaRequired: boolean;
        queryCaptchaImageBase64: string;
        queryCaptchaCode: string;
        queryErrorMessage: string;
        results: EzwayQueryResultResponse[];
    };
    initialize: () => void;
    refreshLoginCaptcha: () => void;
    onLoginProfileChanged: () => void;
    returnToAccountSelection: () => void;
    activateLoggedInAccount: (accountSessionKey: string) => ng.IPromise<void> | void;
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
    onConsolidatorChanged: () => void;
    selectQueryApi: (queryApiType: string) => void;
    getReplyDateTime: (item: EzwayQueryResultResponse) => string;
    hasResultValue: (fieldName: string) => boolean;
}

mainApp.controller('EzwaySeaController', ['$scope', '$http', function (
    $scope: EzwaySeaScope,
    $http: ng.IHttpService
) {
    var defaultLoginProfileKey = 'VirtualZone';
    var loginProfiles: EzwayLoginProfileOption[] = [
        { key: 'VirtualZone', label: '虛擬關區', companyId: '24951752', account: 'ECC0248' },
        { key: 'AllOne', label: '全旺', companyId: '24951752', account: 'ECC0197' },
        { key: 'TPCT', label: 'TPCT', companyId: '82953146', account: 'ECC0091' },
        { key: 'KaohsiungBranch', label: '捷豐高雄分公司', companyId: '90276915', account: 'ECC0188' }
    ];

    function isSeaAccount(account?: EzwayLoggedInAccountResponse | null): boolean {
        return !!account && account.Account !== 'ECC0001';
    }

    function setLoginError(message: string): void {
        $scope.loginState.errorMessage = message || '';
    }

    function setQueryError(message: string): void {
        $scope.queryState.queryErrorMessage = message || '';
    }

    function resetResults(): void {
        $scope.queryState.results = [];
    }

    function clearSeaQueryOptions(): void {
        $scope.queryState.brokerQueryField = '';
        $scope.queryState.brokerOptions = [];
        $scope.queryState.selectedBrokerValue = '';
        $scope.queryState.consolidatorOptions = [];
        $scope.queryState.selectedConsolidator = '';
        $scope.queryState.selectedConsolidatorUserId = '';
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

    function syncSelectedConsolidatorUserId(): void {
        var selectedConsolidator = $scope.queryState.selectedConsolidator || '';
        var selectedOption = ($scope.queryState.consolidatorOptions || []).filter(function (item: EzwaySeaConsolidatorOptionResponse): boolean {
            return !!item && item.Value === selectedConsolidator;
        })[0];

        if (!selectedConsolidator) {
            $scope.queryState.selectedConsolidatorUserId = 'ALL';
            return;
        }

        if (selectedOption && selectedOption.UserId) {
            $scope.queryState.selectedConsolidatorUserId = selectedOption.UserId;
            return;
        }

        $scope.queryState.selectedConsolidatorUserId = selectedConsolidator === 'null' ? null : 'ALL';
    }

    function applySeaQueryOptions(options?: EzwaySeaQueryOptionsResponse | null): void {
        var seaQueryOptions = options || {};
        $scope.queryState.brokerQueryField = seaQueryOptions.BrokerQueryField || '';
        $scope.queryState.brokerOptions = seaQueryOptions.BrokerOptions || [];
        $scope.queryState.selectedBrokerValue = seaQueryOptions.SelectedBrokerValue
            || (($scope.queryState.brokerOptions[0] && $scope.queryState.brokerOptions[0].Value) || '');
        $scope.queryState.consolidatorOptions = seaQueryOptions.ConsolidatorOptions || [];
        $scope.queryState.selectedConsolidator = seaQueryOptions.SelectedConsolidator
            || (($scope.queryState.consolidatorOptions[0] && $scope.queryState.consolidatorOptions[0].Value) || '');
        $scope.queryState.selectedConsolidatorUserId = typeof seaQueryOptions.SelectedConsolidatorUserId === 'undefined'
            ? ''
            : seaQueryOptions.SelectedConsolidatorUserId;

        syncSelectedConsolidatorUserId();
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

    function getSelectedLoginProfile(): EzwayLoginProfileOption {
        var profile = loginProfiles.filter(function (item: EzwayLoginProfileOption): boolean {
            return item.key === $scope.selectedLoginProfile;
        })[0];

        return profile || loginProfiles.filter(function (item: EzwayLoginProfileOption): boolean {
            return item.key === defaultLoginProfileKey;
        })[0] || loginProfiles[0];
    }

    function applySelectedLoginProfile(): void {
        var profile = getSelectedLoginProfile();
        if (!profile) {
            return;
        }

        $scope.selectedLoginProfile = profile.key;
        $scope.loginForm.companyId = profile.companyId;
        $scope.loginForm.account = profile.account;
    }

    function applyCurrentLoggedInAccount(account?: EzwayLoggedInAccountResponse | null): void {
        $scope.activeLoggedInAccount = isSeaAccount(account) ? account : null;

        if (!$scope.activeLoggedInAccount || !$scope.activeLoggedInAccount.CanUseX4) {
            $scope.queryState.activeQueryApi = 'Simple';
        }
    }

    function applyLoggedInAccounts(accounts?: EzwayLoggedInAccountResponse[] | null): void {
        $scope.loggedInAccounts = (accounts || []).filter(function (item: EzwayLoggedInAccountResponse): boolean {
            return isSeaAccount(item);
        });
    }

    function upsertLoggedInAccount(account?: EzwayLoggedInAccountResponse | null): void {
        if (!account || !account.AccountSessionKey) {
            return;
        }

        var accounts = ($scope.loggedInAccounts || [])
            .filter(function (item: EzwayLoggedInAccountResponse): boolean {
                return item && item.AccountSessionKey !== account.AccountSessionKey;
            });

        accounts.push(account);
        $scope.loggedInAccounts = accounts;
    }

    function applyLoggedOutState(pageState?: EzwayPageStateResponse | null): void {
        $scope.isLoggedIn = false;
        $scope.loginForm.password = '';
        $scope.loginForm.captcha = '';
        clearSeaQueryOptions();
        $scope.queryState.hawbNo = '';
        $scope.queryState.queryCaptcha = '';
        $scope.queryState.queryCaptchaRequired = false;
        $scope.queryState.queryCaptchaImageBase64 = '';
        $scope.queryState.queryCaptchaCode = '';
        $scope.queryState.activeQueryApi = 'Simple';
        applyCurrentLoggedInAccount(null);
        applyLoggedInAccounts(pageState ? pageState.LoggedInAccounts : null);
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
        return $http.get(Router.action('EzwaySea', 'RefreshLoginCaptcha'))
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
        return $http.get(Router.action('EzwaySea', 'QuerySetting'))
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

    function requestSeaQueryOptions(displayError: boolean): ng.IPromise<void> {
        return $http.get(Router.action('EzwaySea', 'QueryOptions'))
            .then(function (response: ng.IHttpResponse<ApiResponse<EzwaySeaQueryOptionsResponse>>) {
                var data = response.data || {};
                if (data.msg) {
                    setQueryError(data.msg);

                    if (displayError) {
                        swal({
                            title: '查詢條件載入失敗',
                            text: data.msg,
                            icon: 'error'
                        });
                    }

                    if (needsReinitialize(data.msg)) {
                        $scope.isLoggedIn = false;
                        resetResults();
                        clearSeaQueryOptions();
                        $scope.initialize();
                    }
                    return;
                }

                applySeaQueryOptions(data.ReturnObject);
                setQueryError('');
            }, function (): void {
                if (displayError) {
                    setQueryError('取得海運查詢下拉失敗，請稍後再試');
                    swal({
                        title: '查詢條件載入失敗',
                        text: '取得海運查詢下拉失敗，請稍後再試',
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
            LoginProfileKey: getSelectedLoginProfile().key,
            LoginProfileLabel: getSelectedLoginProfile().label,
            TermsAccepted: termsAccepted
        };
    }

    function buildQueryRequest(): any {
        var request: any = {
            Manual: $scope.queryState.queryMode === 'Batch' ? 'N' : 'Y',
            QueryApiType: $scope.queryState.activeQueryApi,
            HawbNo: $scope.queryState.hawbNo,
            QueryCaptcha: $scope.queryState.queryCaptcha,
            QueryCaptchaCode: $scope.queryState.queryCaptchaCode,
            QueryCaptchaRequired: $scope.queryState.queryCaptchaRequired
        };

        if ($scope.queryState.selectedBrokerValue) {
            request[$scope.queryState.brokerQueryField || 'GroupUserId'] = $scope.queryState.selectedBrokerValue;
        }

        if ($scope.queryState.selectedConsolidator) {
            request.Consolidator = $scope.queryState.selectedConsolidator;
            request.ConsolidatorUserId = $scope.queryState.selectedConsolidatorUserId;
        }

        return request;
    }

    function getValidHawbNumbers(): string[] {
        return ($scope.queryState.hawbNo || '')
            .split(/\r?\n/)
            .map(function (value: string): string {
                return value.trim();
            })
            .filter(function (value: string): boolean {
                return !!value;
            });
    }

    function validateSingleQuery(): string {
        var hawbNumbers = getValidHawbNumbers();

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

        if (($scope.queryState.brokerOptions || []).length > 0 && !$scope.queryState.selectedBrokerValue) {
            return '請選擇報關業者';
        }

        if (($scope.queryState.consolidatorOptions || []).length > 0 && !$scope.queryState.selectedConsolidator) {
            return '請選擇集運商';
        }

        return '';
    }

    $scope.loading = false;
    $scope.isLoggedIn = false;
    $scope.activeLoggedInAccount = null;
    $scope.loggedInAccounts = [];
    $scope.loginProfiles = loginProfiles;
    $scope.selectedLoginProfile = defaultLoginProfileKey;
    $scope.loginForm = {
        companyId: '',
        account: '',
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
        brokerQueryField: '',
        brokerOptions: [],
        selectedBrokerValue: '',
        consolidatorOptions: [],
        selectedConsolidator: '',
        selectedConsolidatorUserId: '',
        hawbNo: '',
        hawbCount: 0,
        queryCaptcha: '',
        queryCaptchaRequired: false,
        queryCaptchaImageBase64: '',
        queryCaptchaCode: '',
        queryErrorMessage: '',
        results: []
    };
    applySelectedLoginProfile();

    $scope.initialize = function (): void {
        setLoginError('');
        setQueryError('');
        $scope.loading = true;

        $http.get(Router.action('EzwaySea', 'Initialize'))
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
                applyLoggedInAccounts(pageState.LoggedInAccounts);
                applyCurrentLoggedInAccount(pageState.CurrentAccount);

                if ($scope.isLoggedIn) {
                    applyQueryCaptchaState(pageState.QueryCaptchaState);
                    resetResults();
                    setQueryError('');
                    return requestSeaQueryOptions(false);
                }

                applyLoginCaptchaState(pageState.LoginCaptchaState);
                clearSeaQueryOptions();
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

    $scope.activateLoggedInAccount = function (accountSessionKey: string): ng.IPromise<void> | void {
        $scope.loading = true;
        setLoginError('');

        return $http.post(Router.action('EzwaySea', 'ActivateAccount'), {
            AccountSessionKey: accountSessionKey
        }).then(function (response: ng.IHttpResponse<ApiResponse<EzwayPageStateResponse>>) {
            var data = response.data || {};
            if (data.msg) {
                setLoginError(data.msg);
                swal({
                    title: '切換帳號失敗',
                    text: data.msg,
                    icon: 'error'
                });

                if (needsReinitialize(data.msg)) {
                    $scope.initialize();
                }
                return;
            }

            var pageState = data.ReturnObject || {};
            $scope.isLoggedIn = !!pageState.IsLoggedIn;
            applyLoggedInAccounts(pageState.LoggedInAccounts);
            applyCurrentLoggedInAccount(pageState.CurrentAccount);
            applyQueryCaptchaState(pageState.QueryCaptchaState);
            resetResults();
            setQueryError('');
            return requestSeaQueryOptions(false);
        }, function (): void {
            setLoginError('Ezway 切換帳號失敗，請稍後再試');
            swal({
                title: '切換帳號失敗',
                text: 'Ezway 切換帳號失敗，請稍後再試',
                icon: 'error'
            });
        }).finally(function (): void {
            $scope.loading = false;
        });
    };

    $scope.onLoginProfileChanged = function (): void {
        applySelectedLoginProfile();
    };

    $scope.returnToAccountSelection = function (): void {
        $scope.initialize();
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

        return $http.post(Router.action('EzwaySea', 'Login'), buildLoginRequest(!!termsAccepted))
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
                    upsertLoggedInAccount(result.CurrentAccount);
                    applyCurrentLoggedInAccount(result.CurrentAccount);
                    setLoginError('');

                    return requestQuerySetting(false)
                        .then(function (): ng.IPromise<void> {
                            return requestSeaQueryOptions(false);
                        })
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

        return $http.post(Router.action('EzwaySea', 'Logout'), {})
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

        return $http.post(Router.action('EzwaySea', 'Query'), buildQueryRequest())
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

        return $http.post(Router.action('EzwaySea', 'BatchQuery'), buildQueryRequest())
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

        return $http.post(Router.action('EzwaySea', 'ExportExcel'), {
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
        $scope.queryState.hawbCount = getValidHawbNumbers().length;
        setQueryError('');
        resetResults();
    };

    $scope.clearQueryForm = function (): void {
        $scope.queryState.hawbNo = '';
        $scope.queryState.hawbCount = 0;
        $scope.queryState.queryCaptcha = '';
        setQueryError('');
        resetResults();
    };

    $scope.onQueryModeChanged = function (): void {
        setQueryError('');
        resetResults();
    };

    $scope.onConsolidatorChanged = function (): void {
        syncSelectedConsolidatorUserId();
        setQueryError('');
        resetResults();
    };

    $scope.selectQueryApi = function (queryApiType: string): void {
        if (queryApiType === 'X4' && (!$scope.activeLoggedInAccount || !$scope.activeLoggedInAccount.CanUseX4)) {
            return;
        }

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

    $scope.hasResultValue = function (fieldName: string): boolean {
        return ($scope.queryState.results || []).some(function (item: EzwayQueryResultResponse): boolean {
            var value = item && (item as any)[fieldName];
            return typeof value !== 'undefined' && value !== null && String(value).trim() !== '';
        });
    };

    $scope.initialize();
}]);
