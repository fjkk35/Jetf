//<reference path="../../types/global.d.ts" />

// 設定 Moment.js 中文
moment.locale('zh-tw');

interface DropdownOption {
    Value: string | number;
    Text: string;
}

interface ApprovalCategoryOption {
    Id: number;
    CategoryName: string;
}

interface AuthorizationFormOption {
    Id: number;
    FormName: string;
}

interface AbnormalStateOption {
    Id: number;
    AbnormalStateName: string;
}

interface StepOption {
    Id: number;
    StepName: string;
    IsMultiple?: boolean;
}

interface SeaOrderOriginal {
    CreateDate?: string | null;
    Modifyby?: string | null;
    Post_Entry?: string | null;
    Cust_Name?: string | null;
    Jetf_Serial?: string | null;
    Piece?: number | null;
    Eta?: string | null;
    Importer?: string | null;
    Importer_Id?: string | null;
    Im_Phoneno?: string | null;
}

interface SeaClearanceDetailData {
    Id: number;
    SeaOrderOriginals: SeaOrderOriginal[];
    CurrentStepId?: number | null;
    CurrentAbnormalStateId?: number | null;
    CustomsBrokerId?: number | null;
    CustomsBrokerageId?: number | null;
    ContactEmail?: string | null;
    ContactChangeData?: string | null;
    SignInTime?: string | null;
    SignOutTime?: string | null;
    DeclNo?: string | null;
    IsCustomsHold?: boolean;
    CustomsHold?: string | null;
    MainNumber?: string | null;
    TrackingNo?: string | null;
    ProDateTime?: string | null;
    [key: string]: any;
}

interface UpdateSignInOutTimeResult {
    Updated: boolean;
    SignInTime?: string | null;
    SignOutTime?: string | null;
}

interface SaveSeaClearanceStepResult {
    NextStepId?: number;
    AutoJumped?: boolean;
    AutoJumpMessage?: string;
}

interface UpdateImportDateResult {
    ImportDate?: string | null;
    CustomerDeadline?: string | null;
    CloseDate?: string | null;
    ProDateTimeDeadline?: string | null;
    LateDeclarationFee?: number | null;
}

interface SeaClearanceTempValues {
    selectedStepId: string | number | null;
    selectedAbnormalStateId: string | number | null;
    selectedStepDetailId?: string | number | null;
    ProcessingPersonnel?: string;
    CustomsBrokerId?: string | number | null;
    CustomsBrokerageId?: string;
    Post_Entry?: string;
    SignInTime?: Date | null;
    SignOutTime?: Date | null;
    ContactEmail?: string;
    ContactChangeData?: string;
    DeclNo?: string;
    Importer?: string;
    Importer_Id?: string;
    IsCustomsHold?: boolean;
    CustomsHold?: string;
    ApprovalCategories?: Record<number, boolean>;
    AuthorizationForms?: Record<number, boolean>;
    StepDetails?: Record<number, boolean>;
    AbnormalStateDetails?: Record<number, boolean>;
    newRemark?: string;
    [key: string]: any;
}

interface SeaClearanceDetailScope extends ng.IScope {
    detailId: string | null;
    detailData: SeaClearanceDetailData | null;
    customsBrokerOptions: DropdownOption[];
    customsBrokerageOptions: DropdownOption[];
    postEntryOptions: DropdownOption[];
    approvalCategoryOptions: ApprovalCategoryOption[];
    authorizationFormOptions: AuthorizationFormOption[];
    abnormalStateOptions: AbnormalStateOption[];
    stepOptions: StepOption[];
    selectedCategories: ApprovalCategoryOption[];
    selectedReceivedOriginalForms: AuthorizationFormOption[];
    selectedDocumentDeliveryForms: AuthorizationFormOption[];
    editMode: Record<string, boolean>;
    currentEditField: string | null;
    tempValues: SeaClearanceTempValues;
    processor: string;
    showFullContactEmail: boolean;
    savingStep: boolean;
    savingAbnormalState: boolean;
    remarks: any[];
    allSteps: any[];
    allAbnormalStates: any[];
    availableStepDetails: any[];
    availableAbnormalStateDetails: any[];
    cptData: any;
    isLoadingCptData: boolean;
    currentStepId: number | null;
    currentAuthFormType: number;
    dateOptions: any;
    datePopup: Record<string, boolean>;
    isUpdatingEta: boolean;
    isUpdatingImportDate: boolean;
    [key: string]: any;
}

// Angular.js 應用程式 - 加入 ui.bootstrap 依賴
const app = angular.module('seaClearanceDetailApp', ['commonFilters', 'ui.bootstrap']);

// Controller
app.controller('SeaClearanceDetailController', ['$scope', '$http', '$filter', function (
    $scope: SeaClearanceDetailScope,
    $http: any,
    $filter: any
) {
    // 初始化資料
    $scope.detailId = getQueryStringId();
    $scope.detailData = null;
    $scope.customsBrokerOptions = [];
    $scope.customsBrokerageOptions = [];
    $scope.postEntryOptions = [];
    $scope.approvalCategoryOptions = [];
    $scope.authorizationFormOptions = [];
    $scope.selectedCategories = [];
    $scope.selectedReceivedOriginalForms = [];
    $scope.selectedDocumentDeliveryForms = [];
    $scope.editHistory = [];
    $scope.authorizationFormHistory = [];
    $scope.currentAuthFormType = 1; // 1=收到正本選單、2=寄文件選單
    $scope.showFullContactEmail = false; // 控制聯繫人信箱的顯示/隱藏狀態
    $scope.processor = ''; // 負責人

    // CPT 關貿資料
    $scope.cptData = null;
    $scope.isLoadingCptData = false; // CPT 資料載入狀態

    // 步驟相關資料
    $scope.stepOptions = [];
    $scope.availableStepDetails = [];
    $scope.allSteps = []; // 所有步驟資料
    $scope.savingStep = false;
    $scope.currentStepId = null; // 當前步驟ID

    // 異常狀態相關資料
    $scope.abnormalStateOptions = [];
    $scope.availableAbnormalStateDetails = [];
    $scope.allAbnormalStates = []; // 所有異常狀態資料
    $scope.savingAbnormalState = false;

    // 備註相關資料
    $scope.remarks = [];

    // 編輯狀態管理
    $scope.editMode = {};
    $scope.currentEditField = null;
    $scope.tempValues = {
        selectedStepId: null,  // 步驟選擇
        selectedAbnormalStateId: null  // 異常狀態選擇
    };

    $scope.isEditLocked = function (fieldName) {
        return $scope.currentEditField && $scope.currentEditField !== fieldName;
    };

    $scope.isAnyEditing = function () {
        return !!$scope.currentEditField;
    };

    // 日期選擇器設定
    $scope.dateOptions = {
        formatYear: 'yyyy',
        minDate: new Date(1900, 1, 1),
        startingDay: 0,
        showWeeks: false
    };

    // 日期彈出視窗狀態
    $scope.datePopup = {};

    // 切換聯繫人信箱顯示狀態的函數
    $scope.toggleContactEmailDisplay = function (show) {
        console.log('切換聯繫人信箱顯示狀態:', show);
        $scope.showFullContactEmail = show;
        console.log('當前顯示狀態:', $scope.showFullContactEmail);
    };

    // 取得 query string 的 id 值
    function getQueryStringId() {
        var urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('id');
    }

    function getApiErrorMessage(error, fallbackMessage) {
        var data = error && error.data !== undefined ? error.data : error;

        if (data) {
            if (typeof data === 'string') {
                return data;
            }

            if (data.msg) {
                return data.msg;
            }

            if (data.error) {
                return data.error;
            }

            if (data.Message) {
                return data.Message;
            }

            if (data.ExceptionMessage) {
                return data.ExceptionMessage;
            }
        }

        if (error && error.statusText) {
            return fallbackMessage + "：" + error.statusText;
        }

        return fallbackMessage;
    }

    function showApiError(fallbackMessage, error?) {
        swal({
            title: "錯誤",
            text: getApiErrorMessage(error, fallbackMessage),
            icon: "error"
        });
    }

    function hasApiError(response) {
        var data = response && response.data !== undefined ? response.data : response;

        if (!data || typeof data !== 'object' || Array.isArray(data)) {
            return false;
        }

        if (data.Redirect) {
            return false;
        }

        if (data.status === 'error' || data.IsSuccess === false || data.error) {
            return true;
        }

        return !!data.msg && (data.ReturnObject === null || data.ReturnObject === undefined);
    }

    // 載入基礎資料
    $scope.loadInitialData = function () {
        var promises = [
            $http.get(Router.action('CustomsBroker', 'GetAllForDropdown')),
            $http.get(Router.action('SeaClearance', 'GetCustomsBrokerageOptions')),
            $http.get(Router.action('DropDownList', 'GetPostEntryTypeList')),
            $http.get(Router.action('ApprovalCategory', 'GetAll')),
            $http.get(Router.action('AuthorizationForm', 'GetAll')),
            $http.get(Router.action('AbnormalState', 'GetAllAbnormalStates'))
        ];

        Promise.all(promises).then(function (responses) {
            var failedResponse = responses.find(function (response) {
                return hasApiError(response);
            });

            if (failedResponse) {
                showApiError("載入基礎資料失敗", failedResponse);
                return;
            }

            $scope.customsBrokerOptions = responses[0].data || [];
            $scope.customsBrokerageOptions = responses[1].data || [];
            $scope.postEntryOptions = responses[2].data || [];
            $scope.approvalCategoryOptions = responses[3].data || [];
            $scope.authorizationFormOptions = responses[4].data || [];
            $scope.abnormalStateOptions = responses[5].data || [];
            $scope.loadData();
            $scope.$apply();
        }).catch(function (error) {
            console.error('載入基礎資料失敗:', error);
            showApiError("載入基礎資料失敗", error);
        });
    };

    // 載入明細資料（僅載入基本資料）
    $scope.loadDetailData = function () {
        let detailId = $scope.detailId;
        return $http.post(Router.action('SeaClearance', 'GetDetail'), { id: detailId })
            .then(function (response: { data: ApiResponse<SeaClearanceDetailData> }) {
                if (response.data.Redirect) {
                    window.location.href = Router.action('Account', 'Login');
                    return;
                }

                if (hasApiError(response) || !response.data.ReturnObject) {
                    showApiError("載入明細資料失敗", response);
                    return;
                }

                $scope.detailData = response.data.ReturnObject;
                //異常狀態Id
                $scope.tempValues.selectedAbnormalStateId = $scope.detailData.CurrentAbnormalStateId;
                // 每次載入新資料時重置顯示狀態
                $scope.showFullContactEmail = false;
                console.log('載入資料後，聯繫人信箱:', $scope.detailData.ContactEmail);
                console.log('信箱長度:', $scope.detailData.ContactEmail ? $scope.detailData.ContactEmail.length : 0);

                return response.data.ReturnObject;
            })
            .catch(function (error) {
                console.error('載入明細資料失敗:', error);
                showApiError("載入明細資料失敗", error);
            });
    };

    // 更新入倉與出倉時間
    $scope.updateSignInOutTime = function () {
        $http.post(Router.action('SeaClearance', 'UpdateSignInOutTime'), { id: $scope.detailData.Id })
            .then(function (response: { data: ApiResponse<UpdateSignInOutTimeResult> }) {
                if (hasApiError(response)) {
                    showApiError("更新入倉與出倉時間失敗", response);
                    return;
                }

                if (response.data.status === 'success' || response.data.ReturnObject) {
                    var result = response.data.ReturnObject;
                    if (result && result.Updated) {
                        console.log('入倉與出倉時間已更新');
                        $scope.detailData.SignInTime = result.SignInTime;
                        $scope.detailData.SignOutTime = result.SignOutTime;
                    }
                }
            })
            .catch(function (error) {
                console.error('更新入倉與出倉時間失敗:', error);
                showApiError("更新入倉與出倉時間失敗", error);
            });
    };

    // 載入完整資料（包含所有相關資料）
    $scope.loadData = function () {
        $scope.loadDetailData()
            .then(function (detailData) {
                // 檢查是否成功載入資料
                if (!detailData) {
                    return;
                }
                let detailId = detailData.Id;
                $scope.loadDetailApprovalCategories(detailId);
                $scope.loadDetailAuthorizationForms(detailId);
                $scope.loadRemarks(detailId);

                // 更新入倉與出倉時間
                $scope.updateSignInOutTime();

                // 載入可用步驟（基於跳轉規則）
                $scope.loadAvailableSteps(detailData.CurrentStepId);

                // 載入負責人
                $scope.loadProcessor(detailId);

                // 載入 CPT 關貿資料
                $scope.loadCptData();

            })
            .catch(function (error) {
                console.error('載入完整資料失敗:', error);
                showApiError("載入完整資料失敗", error);
            });
    };

    // 載入 CPT 關貿資料
    $scope.loadCptData = function () {
        if (!$scope.detailData) {
            console.log('沒有明細資料，無法載入 CPT 資料');
            return;
        }

        $scope.isLoadingCptData = true; // 開始載入

        $http.get(Router.action('SeaClearance', 'GetCptData'), {
            params: {
                SeaClearanceDetailId: $scope.detailData.Id,
                MainNumber: $scope.detailData.MainNumber,
                TrackingNo: $scope.detailData.TrackingNo,
            }
        }).then(function (response: { data: ApiResponse<any> }) {
            if (hasApiError(response)) {
                showApiError("載入 CPT 資料失敗", response);
                $scope.cptData = null;
                return;
            }

            if (response.data && response.data.ReturnObject) {
                $scope.cptData = response.data.ReturnObject;
                console.log('CPT 資料載入成功:', $scope.cptData);
                // 更新資料
                if ($scope.cptData.IsUpdate) {
                    if ($scope.cptData.UpdatedDeclNo !== undefined && $scope.cptData.UpdatedDeclNo !== null) {
                        $scope.detailData.DeclNo = $scope.cptData.UpdatedDeclNo;
                    }

                    if ($scope.cptData.UpdatedProDateTime !== undefined && $scope.cptData.UpdatedProDateTime !== null) {
                        $scope.detailData.ProDateTime = $scope.cptData.UpdatedProDateTime;
                    }
                }

            } else {
                console.log('CPT 資料載入失敗或無資料');
                $scope.cptData = null;
            }
        }).catch(function (error) {
            console.error('載入 CPT 資料失敗:', error);
            $scope.cptData = null;
            showApiError("載入 CPT 資料失敗", error);
        }).finally(function () {
            $scope.isLoadingCptData = false; // 結束載入
        });


    };

    // 載入明細的簽審類別
    $scope.loadDetailApprovalCategories = function (detailId) {
        $http.get(Router.action('SeaClearance', 'GetDetailApprovalCategories'), {
            params: { seaClearanceDetailId: detailId }
        }).then(function (response) {
            var selectedIds = response.data;
            if (hasApiError(response) || !Array.isArray(selectedIds)) {
                $scope.selectedCategories = [];
                showApiError("載入明細簽審類別失敗", response);
                return;
            }

            $scope.selectedCategories = $scope.approvalCategoryOptions.filter(function (category) {
                return selectedIds.includes(category.Id);
            });
        }).catch(function (error) {
            console.error('載入明細簽審類別失敗:', error);
            showApiError("載入明細簽審類別失敗", error);
        });
    };

    // 載入明細的授權表單
    $scope.loadDetailAuthorizationForms = function (detailId) {
        // 載入收到正本選單
        $http.get(Router.action('SeaClearance', 'GetDetailAuthorizationForms'), {
            params: { seaClearanceDetailId: detailId, type: 1 }
        }).then(function (response) {
            var selectedIds = response.data;
            if (hasApiError(response) || !Array.isArray(selectedIds)) {
                $scope.selectedReceivedOriginalForms = [];
                showApiError("載入收到正本選單失敗", response);
                return;
            }

            $scope.selectedReceivedOriginalForms = $scope.authorizationFormOptions.filter(function (form) {
                return selectedIds.includes(form.Id);
            });
        }).catch(function (error) {
            console.error('載入收到正本選單失敗:', error);
            showApiError("載入收到正本選單失敗", error);
        });

        // 載入寄文件選單
        $http.get(Router.action('SeaClearance', 'GetDetailAuthorizationForms'), {
            params: { seaClearanceDetailId: detailId, type: 2 }
        }).then(function (response) {
            var selectedIds = response.data;
            if (hasApiError(response) || !Array.isArray(selectedIds)) {
                $scope.selectedDocumentDeliveryForms = [];
                showApiError("載入寄文件選單失敗", response);
                return;
            }

            $scope.selectedDocumentDeliveryForms = $scope.authorizationFormOptions.filter(function (form) {
                return selectedIds.includes(form.Id);
            });
        }).catch(function (error) {
            console.error('載入寄文件選單失敗:', error);
            showApiError("載入寄文件選單失敗", error);
        });
    };

    // 載入授權表單歷史記錄
    $scope.loadAuthorizationFormHistory = function (detailId, type) {
        var params: any = { seaClearanceDetailId: detailId };

        // 如果有指定 type，則加入參數
        if (type !== undefined && type !== null) {
            params.type = type;
        }

        $http.get(Router.action('SeaClearance', 'GetAuthorizationFormHistory'), {
            params: params
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.authorizationFormHistory = [];
                showApiError("載入授權表單歷史記錄失敗", response);
                return;
            }

            $scope.authorizationFormHistory = response.data || [];
        }).catch(function (error) {
            console.error('載入授權表單歷史記錄失敗:', error);
            showApiError("載入授權表單歷史記錄失敗", error);
        });
    };

    // 取得報驗公司名稱
    $scope.getCustomsBrokerName = function (customsBrokerId) {
        if (!customsBrokerId) return '';
        var broker = $scope.customsBrokerOptions.find(function (b) {
            return b.Value == customsBrokerId;
        });
        return broker ? broker.Text : '';
    };

    $scope.getCustomsBrokerageName = function (customsBrokerageId) {
        if (!customsBrokerageId) return '';
        var brokerage = $scope.customsBrokerageOptions.find(function (item) {
            return parseInt(String(item.Value), 10) === parseInt(String(customsBrokerageId), 10);
        });
        return brokerage ? brokerage.Text : '';
    };

    // 開啟日期彈出視窗
    $scope.openDatePopup = function (fieldName) {
        $scope.datePopup[fieldName] = true;
    };

    // 編輯欄位
    $scope.editField = function (fieldName) {
        if ($scope.isEditLocked(fieldName) || $scope.editMode[fieldName]) {
            return;
        }

        $scope.currentEditField = fieldName;
        $scope.editMode[fieldName] = true;

        // 設定暫存值
        switch (fieldName) {
            case 'ProcessingPersonnel':
                $scope.tempValues.ProcessingPersonnel = $scope.detailData.ProcessingPersonnel || '';
                break;
            case 'CustomsBroker':
                $scope.tempValues.CustomsBrokerId = $scope.detailData.CustomsBrokerId;
                break;
            case 'CustomsBrokerage':
                $scope.tempValues.CustomsBrokerageId = $scope.detailData.CustomsBrokerageId
                    ? $scope.detailData.CustomsBrokerageId.toString()
                    : '';
                break;
            case 'Post_Entry':
                $scope.tempValues.Post_Entry = $scope.detailData.SeaOrderOriginals[0].Post_Entry || '';
                break;
            case 'SignInTime':
                // 轉換日期為Date物件以供日期選擇器使用
                var date = moment($scope.detailData.SignInTime);
                $scope.tempValues.SignInTime = date.isValid() ? date.toDate() : null;
                break;
            case 'SignOutTime':
                // 轉換日期為Date物件以供日期選擇器使用
                var date = moment($scope.detailData.SignOutTime);
                $scope.tempValues.SignOutTime = date.isValid() ? date.toDate() : null;
                break;
            case 'ContactEmail':
                $scope.tempValues.ContactEmail = $scope.detailData.ContactEmail || '';
                break;
            case 'ContactChangeData':
                $scope.tempValues.ContactChangeData = $scope.detailData.ContactChangeData || '';
                break;
            case 'DeclNo':
                $scope.tempValues.DeclNo = $scope.detailData.DeclNo || '';
                break;
            case 'Importer':
                $scope.tempValues.Importer = $scope.detailData.SeaOrderOriginals[0].Importer || '';
                break;
            case 'Importer_Id':
                $scope.tempValues.Importer_Id = $scope.detailData.SeaOrderOriginals[0].Importer_Id || '';
                break;
            case 'IsCustomsHold':
                $scope.tempValues.IsCustomsHold = $scope.detailData.IsCustomsHold || false;
                break;
            case 'CustomsHold':
                $scope.tempValues.CustomsHold = $scope.detailData.CustomsHold || '';
                break;
        }
    };

    // 顯示簽審類別 Modal
    $scope.showApprovalCategoryModal = function () {
        // 初始化暫存值
        $scope.tempValues.ApprovalCategories = {};
        $scope.selectedCategories.forEach(function (category) {
            $scope.tempValues.ApprovalCategories[category.Id] = true;
        });

        // 使用 Bootstrap Modal 方法開啟
        $('#approvalCategoryModal').modal('show');
    };

    // 顯示授權表單 Modal
    $scope.showAuthorizationFormModal = function (type) {
        $scope.currentAuthFormType = type;

        // 初始化暫存值
        $scope.tempValues.AuthorizationForms = {};

        // 載入當前類型的歷史記錄
        $scope.loadAuthorizationFormHistory($scope.detailData.Id, type);

        // 使用 Bootstrap Modal 方法開啟
        $('#authorizationFormModal').modal('show');
    };

    // 儲存欄位
    $scope.saveField = function (fieldName) {
        var newValue;
        var apiField;

        switch (fieldName) {
            case 'ProcessingPersonnel':
                newValue = $scope.tempValues.ProcessingPersonnel;
                apiField = 'ProcessingPersonnel';
                break;
            case 'CustomsBroker':
                newValue = $scope.tempValues.CustomsBrokerId;
                apiField = 'CustomsBrokerId';
                break;
            case 'CustomsBrokerage':
                newValue = $scope.tempValues.CustomsBrokerageId;
                apiField = 'CustomsBrokerageId';
                break;
            case 'Post_Entry':
                newValue = $scope.tempValues.Post_Entry;
                apiField = 'Post_Entry';
                break;
            case 'SignInTime':
                // 將Date物件轉換為ISO字串
                newValue = $filter('customDate')($scope.tempValues.SignInTime);
                apiField = 'SignInTime';
                break;
            case 'SignOutTime':
                // 將Date物件轉換為ISO字串
                newValue = $filter('customDate')($scope.tempValues.SignOutTime);
                apiField = 'SignOutTime';
                break;
            case 'ContactEmail':
                newValue = $scope.tempValues.ContactEmail;
                apiField = 'ContactEmail';

                // 驗證信箱格式
                if (newValue && newValue.trim() !== '') {
                    var validation = $scope.validateEmails(newValue);
                    if (!validation.valid) {
                        var invalidEmailsList = validation.invalidEmails.join('\n');
                        swal({
                            title: "信箱格式錯誤",
                            text: "以下信箱格式不正確：\n" + invalidEmailsList + "\n\n請使用逗號分隔多個信箱",
                            icon: "error"
                        });
                        return;
                    }
                }

                // 保存後重置顯示狀態
                $scope.showFullContactEmail = false;
                break;
            case 'ContactChangeData':
                newValue = $scope.tempValues.ContactChangeData;
                apiField = 'ContactChangeData';
                break;
            case 'DeclNo':
                newValue = $scope.tempValues.DeclNo;
                apiField = 'DeclNo';

                // 驗證報單號碼格式
                if (newValue && newValue.trim() !== '') {
                    var declNoPattern = /^[A-Za-z]{2}  [A-Za-z0-9]{10}$/;
                    if (!declNoPattern.test(newValue)) {
                        swal({
                            title: "格式錯誤",
                            text: "報單號碼格式不正確！\n格式：前2碼英文 + 2個半型空白 + 後10碼英數，共14碼",
                            icon: "error"
                        });
                        return;
                    }
                }
                break;
            case 'Importer':
                newValue = $scope.tempValues.Importer;
                apiField = 'Importer';
                break;
            case 'Importer_Id':
                newValue = $scope.tempValues.Importer_Id;
                apiField = 'Importer_Id';
                break;
            case 'IsCustomsHold':
                newValue = $scope.tempValues.IsCustomsHold ? 'true' : 'false';
                apiField = 'IsCustomsHold';
                break;
            case 'CustomsHold':
                newValue = $scope.tempValues.CustomsHold;
                apiField = 'CustomsHold';

                // 驗證扣倉項次格式
                if (newValue && newValue.trim() !== '') {
                    // 只允許數字和半形逗號
                    var customsHoldPattern = /^[0-9,]+$/;
                    if (!customsHoldPattern.test(newValue)) {
                        swal({
                            title: "格式錯誤",
                            text: "扣倉項次只能輸入數字和半形逗號(,)！",
                            icon: "error"
                        });
                        return;
                    }

                    // 檢查是否有連續的逗號
                    if (/,,/.test(newValue)) {
                        swal({
                            title: "格式錯誤",
                            text: "扣倉項次不能包含連續的逗號！",
                            icon: "error"
                        });
                        return;
                    }

                    // 檢查開頭或結尾是否為逗號
                    if (newValue.startsWith(',') || newValue.endsWith(',')) {
                        swal({
                            title: "格式錯誤",
                            text: "扣倉項次不能以逗號開頭或結尾！",
                            icon: "error"
                        });
                        return;
                    }
                }
                break;
        }

        $http.post(Router.action('SeaClearance', 'UpdateField'), {
            id: $scope.detailData.Id,
            field: apiField,
            newValue: newValue || null
        }).then(function (response) {
            if (!hasApiError(response) && response.data.status === 'success') {
                // 更新資料模型
                switch (fieldName) {
                    case 'DeclNo':
                        $scope.detailData.DeclNo = newValue;
                        break;
                    case 'Post_Entry':
                        if ($scope.detailData.SeaOrderOriginals && $scope.detailData.SeaOrderOriginals[0]) {
                            $scope.detailData.SeaOrderOriginals[0].Post_Entry = newValue;
                        }
                        break;
                    case 'Importer':
                        if ($scope.detailData.SeaOrderOriginals && $scope.detailData.SeaOrderOriginals[0]) {
                            $scope.detailData.SeaOrderOriginals[0].Importer = newValue;
                        }
                        break;
                    case 'Importer_Id':
                        if ($scope.detailData.SeaOrderOriginals && $scope.detailData.SeaOrderOriginals[0]) {
                            $scope.detailData.SeaOrderOriginals[0].Importer_Id = newValue;
                        }
                        break;
                    case 'IsCustomsHold':
                        $scope.detailData.IsCustomsHold = $scope.tempValues.IsCustomsHold;
                        break;
                    case 'CustomsHold':
                        $scope.detailData.CustomsHold = newValue;
                        break;
                    case 'CustomsBrokerage':
                        $scope.detailData.CustomsBrokerageId = newValue ? parseInt(newValue, 10) : null;
                        break;
                    default:
                        $scope.detailData[apiField] = newValue;
                        break;
                }
                $scope.editMode[fieldName] = false;
                $scope.currentEditField = null;

                swal({
                    title: "成功",
                    text: "更新成功",
                    icon: "success"
                });
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "更新失敗"),
                    icon: "error"
                });
            }
        }).catch(function (error) {
            showApiError("更新失敗", error);
        });
    };

    // 儲存簽審類別
    $scope.saveApprovalCategories = function () {
        var selectedIds = [];
        angular.forEach($scope.tempValues.ApprovalCategories, function (isSelected, categoryId) {
            if (isSelected) {
                selectedIds.push(parseInt(categoryId));
            }
        });

        $http.post(Router.action('SeaClearance', 'UpdateDetailApprovalCategories'), {
            seaClearanceDetailId: $scope.detailData.Id,
            categoryIds: selectedIds
        }).then(function (response) {
            if (!hasApiError(response) && response.data.status === 'success') {
                $scope.selectedCategories = $scope.approvalCategoryOptions.filter(function (category) {
                    return selectedIds.includes(category.Id);
                });
                // 使用 Bootstrap Modal 方法關閉
                $('#approvalCategoryModal').modal('hide');

                swal({
                    title: "成功",
                    text: "簽審類別更新成功",
                    icon: "success"
                });
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "更新失敗"),
                    icon: "error"
                });
            }
        }).catch(function (error) {
            showApiError("更新失敗", error);
        });
    };

    // 檢查是否有選中的授權表單
    $scope.hasSelectedAuthorizationForms = function () {
        if (!$scope.tempValues.AuthorizationForms) {
            return false;
        }

        var hasSelected = false;
        angular.forEach($scope.tempValues.AuthorizationForms, function (isSelected, formId) {
            if (isSelected) {
                hasSelected = true;
            }
        });

        return hasSelected;
    };

    // 儲存授權表單
    $scope.saveAuthorizationForms = function () {
        var selectedIds = [];
        angular.forEach($scope.tempValues.AuthorizationForms, function (isSelected, formId) {
            if (isSelected) {
                selectedIds.push(parseInt(formId));
            }
        });

        $http.post(Router.action('SeaClearance', 'UpdateDetailAuthorizationForms'), {
            seaClearanceDetailId: $scope.detailData.Id,
            type: $scope.currentAuthFormType,
            formIds: selectedIds
        }).then(function (response) {
            if (!hasApiError(response) && response.data.status === 'success') {
                var selectedForms = $scope.authorizationFormOptions.filter(function (form) {
                    return selectedIds.includes(form.Id);
                });

                if ($scope.currentAuthFormType === 1) {
                    $scope.selectedReceivedOriginalForms = selectedForms;
                } else {
                    $scope.selectedDocumentDeliveryForms = selectedForms;
                }

                // 使用 Bootstrap Modal 方法關閉
                $('#authorizationFormModal').modal('hide');

                var typeName = $scope.currentAuthFormType === 1 ? "收到正本選單" : "寄文件選單";
                swal({
                    title: "成功",
                    text: typeName + "更新成功",
                    icon: "success"
                });

                // 重新載入歷史記錄
                $scope.loadAuthorizationFormHistory($scope.detailData.Id, $scope.currentAuthFormType);
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "更新失敗"),
                    icon: "error"
                });
            }
        }).catch(function (error) {
            showApiError("更新失敗", error);
        });
    };

    // 取消編輯
    $scope.cancelEdit = function (fieldName) {
        $scope.editMode[fieldName] = false;
        delete $scope.tempValues[fieldName];
        $scope.currentEditField = null;
        // 關閉日期彈出視窗
        if ($scope.datePopup[fieldName]) {
            $scope.datePopup[fieldName] = false;
        }
    };

    // 清空日期
    $scope.clearDate = function (fieldName) {
        $scope.tempValues[fieldName] = null;
        $scope.saveField(fieldName);
    };

    // 顯示編輯紀錄 Modal
    $scope.showEditHistoryModal = function () {
        if (!$scope.detailData || !$scope.detailData.Id) {
            swal({
                title: "錯誤",
                text: "無法取得明細ID",
                icon: "error"
            });
            return;
        }

        $http.post(Router.action('SeaClearance', 'GetEditHistory'), {
            seaClearanceDetailId: $scope.detailData.Id
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.editHistory = [];
                showApiError("載入編輯紀錄失敗", response);
                return;
            }

            $scope.editHistory = response.data || [];
            // 使用 Bootstrap Modal 方法開啟
            $('#editHistoryModal').modal('show');
        }).catch(function (error) {
            showApiError("載入編輯紀錄失敗", error);
        });
    };

    // 初始化
    $scope.loadInitialData();

    // 載入負責人
    $scope.loadProcessor = function (detailId) {
        $http.get(Router.action('SeaClearance', 'GetProcessor'), {
            params: { seaClearanceDetailId: detailId }
        }).then(function (response: { data: ApiResponse<string> }) {
            if (hasApiError(response)) {
                $scope.processor = '';
                showApiError("載入負責人失敗", response);
                return;
            }

            if (response.data && response.data.ReturnObject !== undefined) {
                $scope.processor = response.data.ReturnObject || '';
                console.log('負責人:', $scope.processor);
            }
        }).catch(function (error) {
            console.error('載入負責人失敗:', error);
            $scope.processor = '';
            showApiError("載入負責人失敗", error);
        });
    };

    // 載入當前步驟
    $scope.loadStepHistory = function (detailId) {
        $http.get(Router.action('SeaClearance', 'GetSeaClearanceStepHistory'), {
            params: { seaClearanceDetailId: detailId }
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.allSteps = [];
                showApiError("載入步驟失敗", response);
                return;
            }

            $scope.allSteps = response.data || [];
            console.log('全部步驟:', $scope.allSteps);
        }).catch(function (error) {
            console.error('載入步驟失敗:', error);
            showApiError("載入步驟失敗", error);
        });
    };

    // 載入步驟詳細
    $scope.loadStepDetails = function () {
        console.log('載入步驟詳細, tempValues.selectedStepId:', $scope.tempValues.selectedStepId);
        console.log('selectedStepId 類型:', typeof $scope.tempValues.selectedStepId);

        // 確保 selectedStepId 是有效的數字
        var stepId = parseInt(String($scope.tempValues.selectedStepId), 10);
        if (!stepId || isNaN(stepId)) {
            console.log('無效的步驟ID，清空步驟詳細');
            $scope.availableStepDetails = [];
            return null;
        }

        return $http.get(Router.action('SeaClearance', 'GetStepDetails'), {
            params: { stepId: stepId }
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.availableStepDetails = [];
                showApiError("載入步驟詳細失敗", response);
                return false;
            }

            $scope.availableStepDetails = response.data || [];
            // 初始化選擇狀態
            $scope.tempValues.StepDetails = {};
            $scope.tempValues.selectedStepDetailId = null; // 初始化單選值
            console.log('載入步驟詳細成功，數量:', $scope.availableStepDetails.length);
            return true;
        }).catch(function (error) {
            console.error('載入步驟詳細失敗:', error);
            $scope.availableStepDetails = [];
            showApiError("載入步驟詳細失敗", error);
            return false;
        });
    };

    // 取得選中的步驟名稱
    $scope.getSelectedStepName = function () {
        if (!$scope.tempValues.selectedStepId) return '';
        var step = $scope.stepOptions.find(function (s) {
            return s.Id == $scope.tempValues.selectedStepId;
        });
        return step ? step.StepName : '';
    };

    // 取得選中的步驟是否可多選
    $scope.getSelectedStepIsMultiple = function () {
        if (!$scope.tempValues.selectedStepId) return false;
        var step = $scope.stepOptions.find(function (s) {
            return s.Id == $scope.tempValues.selectedStepId;
        });
        return step ? step.IsMultiple : false;
    };

    // 判斷步驟詳細是否應該被禁用
    $scope.isStepDetailDisabled = function () {
        //7:(六)待通知否補件
        //18:(七)待等扣繳稅金放行
        const selectedStepId = parseInt(String($scope.tempValues.selectedStepId), 10);
        if (![7, 18].includes(selectedStepId)) {
            return false;
        }

        // 取得報關方式
        const postEntry = $scope.detailData.SeaOrderOriginals[0].Post_Entry;
        
        // 如果報關方式是 X2、X3、G1、轉G1，則禁用
        const disabledPostEntries = ['X2', 'X3', 'G1', '轉G1'];
        return disabledPostEntries.includes(postEntry);
    };

    // 顯示步驟詳細 Modal
    $scope.showStepDetailModal = function () {
        console.log('開啟步驟詳細 Modal');
        console.log('原始 tempValues.selectedStepId:', $scope.tempValues.selectedStepId, '類型:', typeof $scope.tempValues.selectedStepId);

        // 確保 selectedStepId 是有效的數字
        var stepId = parseInt(String($scope.tempValues.selectedStepId), 10);
        if (!stepId || isNaN(stepId)) {
            swal({
                title: "提示",
                text: "請先選擇步驟",
                icon: "warning"
            });
            return;
        }

        // 確保有載入步驟詳細
        if ($scope.availableStepDetails.length === 0) {
            console.log('步驟詳細為空，重新載入...');
            var loadStepDetailsPromise = $scope.loadStepDetails();
            if (loadStepDetailsPromise && loadStepDetailsPromise.then) {
                loadStepDetailsPromise.then(function (isLoaded) {
                    if (isLoaded !== false) {
                        $('#stepDetailModal').modal('show');
                    }
                });
            }
        } else {
            $('#stepDetailModal').modal('show');
        }
    };

    // 檢查是否有選中的步驟詳細
    $scope.hasSelectedStepDetails = function () {
        if (!$scope.tempValues.StepDetails) {
            return false;
        }

        var hasSelected = false;
        angular.forEach($scope.tempValues.StepDetails, function (isSelected, detailId) {
            if (isSelected) {
                hasSelected = true;
            }
        });

        return hasSelected;
    };

    // 儲存步驟和步驟詳細
    $scope.saveStepAndDetails = function () {
        // 確保 selectedStepId 是有效的數字
        var stepId = parseInt(String($scope.tempValues.selectedStepId), 10);
        if (!stepId || isNaN(stepId)) {
            swal({
                title: "錯誤",
                text: "請選擇步驟",
                icon: "error"
            });
            return;
        }

        $scope.savingStep = true;

        // 取得選中的步驟詳細
        var selectedDetailIds = [];

        // 判斷是多選還是單選
        if ($scope.getSelectedStepIsMultiple()) {
            // 多選模式：從 checkbox 取得選中的項目
            angular.forEach($scope.tempValues.StepDetails, function (isSelected, detailId) {
                if (isSelected) {
                    selectedDetailIds.push(parseInt(detailId));
                }
            });
        } else {
            // 單選模式：從 radio 取得選中的項目
            if ($scope.tempValues.selectedStepDetailId) {
                selectedDetailIds.push(parseInt(String($scope.tempValues.selectedStepDetailId), 10));
            }
        }

        // 一次儲存步驟和步驟詳細
        $http.post(Router.action('SeaClearance', 'SaveSeaClearanceStep'), {
            seaClearanceDetailId: $scope.detailData.Id,
            stepId: stepId,
            stepDetailIds: selectedDetailIds
        }).then(function (response: { data: ApiResponse<SaveSeaClearanceStepResult | number> }) {
            if (!hasApiError(response) && (response.data.status === 'success' || response.data.ReturnObject)) {
                $scope.completeStepSave(response.data.ReturnObject);
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "儲存步驟失敗"),
                    icon: "error"
                });
            }
            $scope.savingStep = false;
        }).catch(function (error) {
            console.error('儲存步驟失敗:', error);
            showApiError("儲存步驟失敗", error);
            $scope.savingStep = false;
        });
    };

    // 完成步驟儲存
    $scope.completeStepSave = function (stepResult: SaveSeaClearanceStepResult | number) {
        $('#stepDetailModal').modal('hide');

        var normalizedStepResult = typeof stepResult === 'number'
            ? { NextStepId: stepResult }
            : (stepResult || {});

        var nextStepId = normalizedStepResult.NextStepId || null;
        var successMessage = "步驟儲存成功";

        if (normalizedStepResult.AutoJumpMessage) {
            successMessage = normalizedStepResult.AutoJumpMessage;
        }

        swal({
            title: "成功",
            text: successMessage,
            icon: "success",
        });

        // 清除選擇
        $scope.tempValues.selectedStepId = null;
        $scope.availableStepDetails = [];
        $scope.tempValues.StepDetails = {};
        $scope.tempValues.selectedStepDetailId = null; // 清除單選值

        $scope.savingStep = false;

        // 重新載入可用步驟和當前步驟
        $scope.loadAvailableSteps(nextStepId);

        // 重新載入負責人（因為步驟可能已改變）
        $scope.loadProcessor($scope.detailData.Id);
    };

    // 顯示步驟歷史記錄 Modal
    $scope.showStepHistoryModal = function () {
        $scope.loadStepHistory($scope.detailId)
        $('#stepHistoryModal').modal('show');
    };

    // ==================== 步驟跳轉規則相關功能 ====================

    // 載入可用步驟（基於跳轉規則）
    $scope.loadAvailableSteps = function (stepId) {
        $http.get(Router.action('SeaClearance', 'GetAvailableSteps'), {
            params:
            {
                stepId: stepId
            }
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.stepOptions = [];
                $scope.tempValues.selectedStepId = null;
                showApiError("載入可用步驟失敗", response);
                return;
            }

            $scope.stepOptions = response.data || [];
            $scope.tempValues.selectedStepId = $scope.stepOptions.length > 0
                ? $scope.stepOptions[$scope.stepOptions.length - 1].Id
                : null;
            console.log('載入可用步驟，數量:', $scope.stepOptions.length);
        }).catch(function (error) {
            console.error('載入可用步驟失敗:', error);
            showApiError("載入可用步驟失敗", error);
        });
    };


    // 處理鍵盤事件
    $scope.handleKeyDown = function (event, fieldName) {
        // Enter 鍵 (keyCode 13)
        if (event.keyCode === 13) {
            event.preventDefault(); // 防止表單提交
            $scope.saveField(fieldName);
        }
        // Escape 鍵 (keyCode 27)
        else if (event.keyCode === 27) {
            event.preventDefault();
            $scope.cancelEdit(fieldName);
        }
    };

    // 處理下拉選單鍵盤事件
    $scope.handleSelectKeyDown = function (event, fieldName) {
        // Enter 鍵 (keyCode 13)
        if (event.keyCode === 13) {
            event.preventDefault(); // 防止表單提交
            $scope.saveField(fieldName);
        }
        // Escape 鍵 (keyCode 27)
        else if (event.keyCode === 27) {
            event.preventDefault();
            $scope.cancelEdit(fieldName);
        }
    };

    // ==================== 異常狀態相關功能 ====================

    // 載入異常狀態歷史
    $scope.loadAbnormalStateHistory = function (detailId) {
        $http.get(Router.action('SeaClearance', 'GetSeaClearanceAbnormalStateHistory'), {
            params: { seaClearanceDetailId: detailId }
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.allAbnormalStates = [];
                showApiError("載入異常狀態失敗", response);
                return;
            }

            $scope.allAbnormalStates = response.data || [];
            console.log('全部異常狀態:', $scope.allAbnormalStates);
        }).catch(function (error) {
            console.error('載入異常狀態失敗:', error);
            showApiError("載入異常狀態失敗", error);
        });
    };

    // 載入異常狀態詳細
    $scope.loadAbnormalStateDetails = function () {
        console.log('載入異常狀態詳細, tempValues.selectedAbnormalStateId:', $scope.tempValues.selectedAbnormalStateId);
        console.log('selectedAbnormalStateId 類型:', typeof $scope.tempValues.selectedAbnormalStateId);

        // 確保 selectedAbnormalStateId 是有效的數字
        var abnormalStateId = parseInt(String($scope.tempValues.selectedAbnormalStateId), 10);
        if (!abnormalStateId || isNaN(abnormalStateId)) {
            console.log('無效的異常狀態ID，清空異常狀態詳細');
            $scope.availableAbnormalStateDetails = [];
            return null;
        }

        return $http.get(Router.action('SeaClearance', 'GetAbnormalStateDetails'), {
            params: { abnormalStateId: abnormalStateId }
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.availableAbnormalStateDetails = [];
                showApiError("載入異常狀態詳細失敗", response);
                return false;
            }

            $scope.availableAbnormalStateDetails = response.data || [];
            // 初始化選擇狀態
            $scope.tempValues.AbnormalStateDetails = {};
            console.log('載入異常狀態詳細成功，數量:', $scope.availableAbnormalStateDetails.length);
            return true;
        }).catch(function (error) {
            console.error('載入異常狀態詳細失敗:', error);
            $scope.availableAbnormalStateDetails = [];
            showApiError("載入異常狀態詳細失敗", error);
            return false;
        });
    };

    // 取得選中的異常狀態名稱
    $scope.getSelectedAbnormalStateName = function () {
        if (!$scope.tempValues.selectedAbnormalStateId) return '';
        var abnormalState = $scope.abnormalStateOptions.find(function (s) {
            return s.Id == $scope.tempValues.selectedAbnormalStateId;
        });
        return abnormalState ? abnormalState.AbnormalStateName : '';
    };

    // 顯示異常狀態詳細 Modal
    $scope.showAbnormalStateDetailModal = function () {
        console.log('開啟異常狀態詳細 Modal');
        console.log('原始 tempValues.selectedAbnormalStateId:', $scope.tempValues.selectedAbnormalStateId, '類型:', typeof $scope.tempValues.selectedAbnormalStateId);

        // 確保 selectedAbnormalStateId 是有效的數字
        var abnormalStateId = parseInt(String($scope.tempValues.selectedAbnormalStateId), 10);
        if (!abnormalStateId || isNaN(abnormalStateId)) {
            swal({
                title: "提示",
                text: "請先選擇異常狀態",
                icon: "warning"
            });
            return;
        }

        // 確保有載入異常狀態詳細
        if ($scope.availableAbnormalStateDetails.length === 0) {
            console.log('異常狀態詳細為空，重新載入...');
            var loadAbnormalStateDetailsPromise = $scope.loadAbnormalStateDetails();
            if (loadAbnormalStateDetailsPromise && loadAbnormalStateDetailsPromise.then) {
                loadAbnormalStateDetailsPromise.then(function (isLoaded) {
                    if (isLoaded !== false) {
                        $('#abnormalStateDetailModal').modal('show');
                    }
                });
            }
        } else {
            $('#abnormalStateDetailModal').modal('show');
        }
    };

    // 檢查是否有選中的異常狀態詳細
    $scope.hasSelectedAbnormalStateDetails = function () {
        if (!$scope.tempValues.AbnormalStateDetails) {
            return false;
        }

        var hasSelected = false;
        angular.forEach($scope.tempValues.AbnormalStateDetails, function (isSelected, detailId) {
            if (isSelected) {
                hasSelected = true;
            }
        });

        return hasSelected;
    };

    // 儲存異常狀態和異常狀態詳細
    $scope.saveAbnormalStateAndDetails = function () {
        var abnormalStateId = parseInt(String($scope.tempValues.selectedAbnormalStateId), 10);
        if (!abnormalStateId || isNaN(abnormalStateId)) {
            swal({
                title: "錯誤",
                text: "請選擇異常狀態",
                icon: "error"
            });
            return;
        }

        $scope.savingAbnormalState = true;

        var selectedDetailIds = [];
        angular.forEach($scope.tempValues.AbnormalStateDetails, function (isSelected, detailId) {
            if (isSelected) {
                selectedDetailIds.push(parseInt(detailId));
            }
        });

        $http.post(Router.action('SeaClearance', 'SaveSeaClearanceAbnormalState'), {
            seaClearanceDetailId: $scope.detailData.Id,
            abnormalStateId: abnormalStateId,
            abnormalStateDetailIds: selectedDetailIds
        }).then(function (response) {
            if (!hasApiError(response) && (response.data.status === 'success' || response.data.ReturnObject)) {
                $scope.detailData.CurrentAbnormalStateId = abnormalStateId;
                $scope.completeAbnormalStateSave();
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "儲存異常狀態失敗"),
                    icon: "error"
                });
            }
            $scope.savingAbnormalState = false;
        }).catch(function (error) {
            console.error('儲存異常狀態失敗:', error);
            showApiError("儲存異常狀態失敗", error);
            $scope.savingAbnormalState = false;
        });
    };

    // 完成異常狀態儲存
    $scope.completeAbnormalStateSave = function () {
        $('#abnormalStateDetailModal').modal('hide');

        swal({
            title: "成功",
            text: "異常狀態儲存成功",
            icon: "success",
            timer: 2000
        });

        // 清除選擇
        $scope.availableAbnormalStateDetails = [];
        $scope.tempValues.AbnormalStateDetails = {};

        $scope.savingAbnormalState = false;
    };

    // 顯示異常狀態歷史記錄 Modal
    $scope.showAbnormalStateHistoryModal = function () {
        $scope.loadAbnormalStateHistory($scope.detailId);
        $('#abnormalStateHistoryModal').modal('show');
    };

    // ==================== 備註相關功能 ====================

    // 載入備註記錄
    $scope.loadRemarks = function (detailId) {
        $http.get(Router.action('SeaClearance', 'GetSeaClearanceRemarks'), {
            params: { seaClearanceDetailId: detailId }
        }).then(function (response) {
            if (hasApiError(response) || !Array.isArray(response.data)) {
                $scope.remarks = [];
                showApiError("載入備註記錄失敗", response);
                return;
            }

            $scope.remarks = response.data || [];
            console.log('載入備註記錄，數量:', $scope.remarks.length);
        }).catch(function (error) {
            console.error('載入備註記錄失敗:', error);
            showApiError("載入備註記錄失敗", error);
        });
    };

    // 新增備註
    $scope.addRemark = function () {
        // 驗證備註內容
        if (!$scope.tempValues.newRemark || $scope.tempValues.newRemark.trim() === '') {
            swal({
                title: "提示",
                text: "請輸入備註內容",
                icon: "warning"
            });
            return;
        }

        $http.post(Router.action('SeaClearance', 'AddSeaClearanceRemark'), {
            seaClearanceDetailId: $scope.detailData.Id,
            remark: $scope.tempValues.newRemark.trim()
        }).then(function (response) {
            if (!hasApiError(response) && (response.data.status === 'success' || response.data.ReturnObject)) {
                swal({
                    title: "成功",
                    text: "備註新增成功",
                    icon: "success",
                    timer: 2000
                });

                // 清空輸入框
                $scope.tempValues.newRemark = '';

                // 重新載入備註列表
                $scope.loadRemarks($scope.detailData.Id);
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "新增備註失敗"),
                    icon: "error"
                });
            }
        }).catch(function (error) {
            console.error('新增備註失敗:', error);
            showApiError("新增備註失敗", error);
        });
    };

    // 驗證信箱格式的函數（支援多個信箱，使用逗號分隔）
    $scope.validateEmails = function (emailString) {
        if (!emailString || emailString.trim() === '') {
            return { valid: true, emails: [] };
        }

        var raw = emailString.trim();

        // 禁止連續逗號，避免出現 "a@a.com,,b@b.com" 也被視為通過
        if (raw.indexOf(',,') >= 0) {
            return {
                valid: false,
                invalidEmails: ['(信箱間隔符號不可連續出現逗號 ",,")'],
                emails: []
            };
        }

        // 以逗號分隔信箱
        var emails = raw.split(',').map(function (email) {
            return email.trim();
        }).filter(function (email) {
            return email !== '';
        });

        // Email 正規表達式
        var emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

        var invalidEmails = [];

        // 若原始字串包含逗號，但分割後只有一個 email，代表格式不正確(例如僅有逗號、或逗號旁為空)
        // 同時也避免像 "a@a.com," 或 ",a@a.com" 的情況
        if (raw.indexOf(',') >= 0) {
            // 開頭或結尾逗號視為錯誤
            if (raw.startsWith(',') || raw.endsWith(',')) {
                invalidEmails.push('(信箱不可用逗號開頭或結尾)');
            }
        }

        for (var i = 0; i < emails.length; i++) {
            if (!emailPattern.test(emails[i])) {
                invalidEmails.push(emails[i]);
            }
        }

        if (invalidEmails.length > 0) {
            return {
                valid: false,
                invalidEmails: invalidEmails,
                emails: emails
            };
        }

        return {
            valid: true,
            emails: emails
        };
    };

    $scope.hasSignInTime = function () {
        if (!$scope.detailData) {
            return false;
        }

        var signInTime = $scope.detailData.SignInTime;
        if (!signInTime) {
            return false;
        }

        if (typeof signInTime === 'string' && signInTime.trim() === '') {
            return false;
        }

        return moment(signInTime).isValid();
    };

    $scope.isUpdatingEta = false;

    $scope.updateEta = function () {
        if ($scope.isUpdatingEta) {
            return;
        }

        if (!$scope.detailData || !$scope.detailData.Id) {
            swal({
                title: "錯誤",
                text: "無法取得明細ID",
                icon: "error"
            });
            return;
        }

        $scope.isUpdatingEta = true;

        $http.post(Router.action('SeaClearance', 'UpdateEta'), {
            id: $scope.detailData.Id
        }).then(function (response: { data: ApiResponse<string> }) {
            if (!hasApiError(response) && response.data.status === 'success') {
                if ($scope.detailData.SeaOrderOriginals && $scope.detailData.SeaOrderOriginals[0]) {
                    $scope.detailData.SeaOrderOriginals[0].Eta = response.data.ReturnObject;
                }

                swal({
                    title: "成功",
                    text: "更新成功",
                    icon: "success"
                });
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "更新失敗"),
                    icon: "error"
                });
            }
        }).catch(function (error) {
            showApiError("更新失敗", error);
        }).finally(function () {
            $scope.isUpdatingEta = false;
        });
    };

    $scope.isUpdatingImportDate = false;

    $scope.updateImportDate = function () {
        if ($scope.isUpdatingImportDate) {
            return;
        }

        if (!$scope.detailData || !$scope.detailData.Id) {
            swal({
                title: "錯誤",
                text: "無法取得明細ID",
                icon: "error"
            });
            return;
        }

        $scope.isUpdatingImportDate = true;

        $http.post(Router.action('SeaClearance', 'UpdateImportDate'), {
            id: $scope.detailData.Id
        }).then(function (response: { data: ApiResponse<UpdateImportDateResult> }) {
            if (!hasApiError(response) && response.data.status === 'success' && response.data.ReturnObject) {
                var updatedData = response.data.ReturnObject;
                $scope.detailData.ImportDate = updatedData.ImportDate;
                $scope.detailData.CustomerDeadline = updatedData.CustomerDeadline;
                $scope.detailData.CloseDate = updatedData.CloseDate;
                $scope.detailData.ProDateTimeDeadline = updatedData.ProDateTimeDeadline;
                $scope.detailData.LateDeclarationFee = updatedData.LateDeclarationFee;

                swal({
                    title: "成功",
                    text: "更新成功",
                    icon: "success"
                });
            } else {
                swal({
                    title: "錯誤",
                    text: getApiErrorMessage(response, "更新失敗"),
                    icon: "error"
                });
            }
        }).catch(function (error) {
            showApiError("更新失敗", error);
        }).finally(function () {
            $scope.isUpdatingImportDate = false;
        });
    };
}]);
