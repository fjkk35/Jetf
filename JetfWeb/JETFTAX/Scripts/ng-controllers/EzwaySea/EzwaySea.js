/// <reference path="../../types/global.d.ts" />
mainApp.controller('EzwaySeaController', ['$scope', '$http', function ($scope, $http) {
        var defaultLoginProfileKey = 'VirtualZone';
        var loginProfiles = [
            { key: 'VirtualZone', label: '虛擬關區', companyId: '24951752', account: 'ECC0248' },
            { key: 'AllOne', label: '全旺', companyId: '24951752', account: 'ECC0197' },
            { key: 'TPCT', label: 'TPCT', companyId: '82953146', account: 'ECC0091' },
            { key: 'KaohsiungBranch', label: '捷豐高雄分公司', companyId: '90276915', account: 'ECC0188' }
        ];
        function isSeaAccount(account) {
            return !!account && account.Account !== 'ECC0001';
        }
        function setLoginError(message) {
            $scope.loginState.errorMessage = message || '';
        }
        function setQueryError(message) {
            $scope.queryState.queryErrorMessage = message || '';
        }
        function resetResults() {
            $scope.queryState.results = [];
        }
        function clearSeaQueryOptions() {
            $scope.queryState.brokerQueryField = '';
            $scope.queryState.brokerOptions = [];
            $scope.queryState.selectedBrokerValue = '';
            $scope.queryState.consolidatorOptions = [];
            $scope.queryState.selectedConsolidator = '';
            $scope.queryState.selectedConsolidatorUserId = '';
        }
        function clearTermsContainer() {
            var container = document.getElementById('ezwayTermsContent');
            if (container) {
                container.innerHTML = '';
            }
        }
        function needsReinitialize(message) {
            return !!message && (message.indexOf('重新登入') !== -1 ||
                message.indexOf('尚未登入') !== -1 ||
                message.indexOf('業者資訊不存在') !== -1);
        }
        function applyLoginCaptchaState(state) {
            var captchaState = state || {};
            $scope.loginState.captchaRequired = !!captchaState.CaptchaRequired;
            $scope.loginState.captchaImageBase64 = captchaState.CaptchaImageBase64 || '';
            $scope.loginState.captchaCode = captchaState.CaptchaCode || '';
            $scope.loginForm.captcha = '';
        }
        function applyQueryCaptchaState(state) {
            var captchaState = state || {};
            $scope.queryState.queryCaptchaRequired = !!captchaState.CaptchaRequired;
            $scope.queryState.queryCaptchaImageBase64 = captchaState.CaptchaImageBase64 || '';
            $scope.queryState.queryCaptchaCode = captchaState.CaptchaCode || '';
            $scope.queryState.queryCaptcha = '';
        }
        function syncSelectedConsolidatorUserId() {
            var selectedConsolidator = $scope.queryState.selectedConsolidator || '';
            var selectedOption = ($scope.queryState.consolidatorOptions || []).filter(function (item) {
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
        function applySeaQueryOptions(options) {
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
        function showTermsModal(html) {
            $scope.loginState.termsAccepted = false;
            $scope.loginState.termsHtml = html || '';
            var container = document.getElementById('ezwayTermsContent');
            if (container) {
                container.innerHTML = $scope.loginState.termsHtml;
            }
            $('#ezwayTermsModal').modal({
                backdrop: 'static',
                keyboard: false
            });
            $('#ezwayTermsModal').modal('show');
        }
        function hideTermsModal() {
            $('#ezwayTermsModal').modal('hide');
            $scope.loginState.termsAccepted = false;
            $scope.loginState.termsHtml = '';
            clearTermsContainer();
        }
        function getSelectedLoginProfile() {
            var profile = loginProfiles.filter(function (item) {
                return item.key === $scope.selectedLoginProfile;
            })[0];
            return profile || loginProfiles.filter(function (item) {
                return item.key === defaultLoginProfileKey;
            })[0] || loginProfiles[0];
        }
        function applySelectedLoginProfile() {
            var profile = getSelectedLoginProfile();
            if (!profile) {
                return;
            }
            $scope.selectedLoginProfile = profile.key;
            $scope.loginForm.companyId = profile.companyId;
            $scope.loginForm.account = profile.account;
        }
        function applyCurrentLoggedInAccount(account) {
            $scope.activeLoggedInAccount = isSeaAccount(account) ? account : null;
            if (!$scope.activeLoggedInAccount || !$scope.activeLoggedInAccount.CanUseX4) {
                $scope.queryState.activeQueryApi = 'Simple';
            }
        }
        function applyLoggedInAccounts(accounts) {
            $scope.loggedInAccounts = (accounts || []).filter(function (item) {
                return isSeaAccount(item);
            });
        }
        function upsertLoggedInAccount(account) {
            if (!account || !account.AccountSessionKey) {
                return;
            }
            var accounts = ($scope.loggedInAccounts || [])
                .filter(function (item) {
                return item && item.AccountSessionKey !== account.AccountSessionKey;
            });
            accounts.push(account);
            $scope.loggedInAccounts = accounts;
        }
        function applyLoggedOutState(pageState) {
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
        function downloadFile(fileGuid, fileName) {
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
        function requestLoginCaptcha(displayError) {
            return $http.get(Router.action('EzwaySea', 'RefreshLoginCaptcha'))
                .then(function (response) {
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
            }, function () {
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
        function requestQuerySetting(displayError) {
            return $http.get(Router.action('EzwaySea', 'QuerySetting'))
                .then(function (response) {
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
            }, function () {
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
        function requestSeaQueryOptions(displayError) {
            return $http.get(Router.action('EzwaySea', 'QueryOptions'))
                .then(function (response) {
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
            }, function () {
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
        function buildLoginRequest(termsAccepted) {
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
        function buildQueryRequest() {
            var request = {
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
        function getValidHawbNumbers() {
            return ($scope.queryState.hawbNo || '')
                .split(/\r?\n/)
                .map(function (value) {
                return value.trim();
            })
                .filter(function (value) {
                return !!value;
            });
        }
        function validateSingleQuery() {
            var hawbNumbers = getValidHawbNumbers();
            if (!hawbNumbers.length) {
                return '請輸入分提單號';
            }
            var invalidHawb = hawbNumbers.some(function (value) {
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
        $scope.initialize = function () {
            setLoginError('');
            setQueryError('');
            $scope.loading = true;
            $http.get(Router.action('EzwaySea', 'Initialize'))
                .then(function (response) {
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
                .catch(function () {
                setLoginError('Ezway 初始化失敗，請稍後再試');
                swal({
                    title: '初始化失敗',
                    text: 'Ezway 初始化失敗，請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.refreshLoginCaptcha = function () {
            $scope.loading = true;
            requestLoginCaptcha(true)
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.activateLoggedInAccount = function (accountSessionKey) {
            $scope.loading = true;
            setLoginError('');
            return $http.post(Router.action('EzwaySea', 'ActivateAccount'), {
                AccountSessionKey: accountSessionKey
            }).then(function (response) {
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
            }, function () {
                setLoginError('Ezway 切換帳號失敗，請稍後再試');
                swal({
                    title: '切換帳號失敗',
                    text: 'Ezway 切換帳號失敗，請稍後再試',
                    icon: 'error'
                });
            }).finally(function () {
                $scope.loading = false;
            });
        };
        $scope.onLoginProfileChanged = function () {
            applySelectedLoginProfile();
        };
        $scope.returnToAccountSelection = function () {
            $scope.initialize();
        };
        $scope.login = function (termsAccepted) {
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
                .then(function (response) {
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
                        .then(function () {
                        return requestSeaQueryOptions(false);
                    })
                        .then(function () {
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
            }, function () {
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
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.confirmTermsAndLogin = function () {
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
        $scope.logout = function () {
            $scope.loading = true;
            return $http.post(Router.action('EzwaySea', 'Logout'), {})
                .then(function (response) {
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
            }, function () {
                setLoginError('Ezway 登出失敗，請稍後再試');
                swal({
                    title: '登出失敗',
                    text: 'Ezway 登出失敗，請稍後再試',
                    icon: 'error'
                });
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.closeTermsModal = function () {
            hideTermsModal();
        };
        $scope.refreshQueryCaptcha = function () {
            $scope.loading = true;
            requestQuerySetting(true)
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.query = function () {
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
                .then(function (response) {
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
            }, function () {
                setQueryError('Ezway 查詢失敗，請稍後再試');
                swal({
                    title: '查詢失敗',
                    text: 'Ezway 查詢失敗，請稍後再試',
                    icon: 'error'
                });
                return requestQuerySetting(false);
            })
                .finally(function () {
                $scope.loading = false;
            });
        };
        $scope.batchQuery = function () {
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
                .then(function (response) {
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
            }, function () {
                setQueryError('Ezway 整批查詢失敗，請稍後再試');
                swal({
                    title: '整批查詢失敗',
                    text: 'Ezway 整批查詢失敗，請稍後再試',
                    icon: 'error'
                });
                return requestQuerySetting(false);
            }).finally(function () {
                $scope.loading = false;
            });
        };
        $scope.exportExcel = function () {
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
            }).then(function (response) {
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
            }, function () {
                swal({
                    title: '匯出失敗',
                    text: 'Ezway 匯出失敗，請稍後再試',
                    icon: 'error'
                });
            }).finally(function () {
                $scope.loading = false;
            });
        };
        $scope.onHawbInputChanged = function () {
            $scope.queryState.hawbCount = getValidHawbNumbers().length;
            setQueryError('');
            resetResults();
        };
        $scope.clearQueryForm = function () {
            $scope.queryState.hawbNo = '';
            $scope.queryState.hawbCount = 0;
            $scope.queryState.queryCaptcha = '';
            setQueryError('');
            resetResults();
        };
        $scope.onQueryModeChanged = function () {
            setQueryError('');
            resetResults();
        };
        $scope.onConsolidatorChanged = function () {
            syncSelectedConsolidatorUserId();
            setQueryError('');
            resetResults();
        };
        $scope.selectQueryApi = function (queryApiType) {
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
        $scope.getReplyDateTime = function (item) {
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
        $scope.hasResultValue = function (fieldName) {
            return ($scope.queryState.results || []).some(function (item) {
                var value = item && item[fieldName];
                return typeof value !== 'undefined' && value !== null && String(value).trim() !== '';
            });
        };
        $scope.initialize();
    }]);
