// Controller
mainApp.controller('SearchCargoController', function ($scope, $http, $window) {
    // 初始化資料
    $scope.data = [];
    $scope.loading = false;
    
    // 查詢條件
  $scope.searchForm = {
        searchType: 'trackingNo',
        searchValue: ''
    };
    
    // 查詢類型選項
    $scope.searchTypeOptions = [
        { value: 'trackingNo', text: '分提單號' },
        { value: 'invoice', text: '物流貨號' },
        { value: 'phone', text: '手機' },
    { value: 'fieldX', text: '客戶外箱號' },
      { value: 'cainiaoFieldX', text: '客戶外箱號(菜鳥)' },
      { value: 'orderNo', text: '客戶訂單號' }
    ];

    // 快捷鍵提示
    $scope.shortcutHint = '快捷鍵:Ctrl+(1~6)切換查詢類別';

    // 執行查詢
    $scope.search = function () {
        if ($scope.loading) {
            return;
        }

        if (!$scope.searchForm.searchValue || $scope.searchForm.searchValue.trim() === '') {
  swal({
    title: "請輸入查詢內容",
           icon: "warning"
          });
         return;
        }

      $scope.loading = true;
  $scope.data = [];

    var request = {
            SearchType: $scope.searchForm.searchType,
       SearchValue: $scope.searchForm.searchValue.trim()
        };

        $http.post(Router.action('SearchCargo', 'SearchData'), request)
            .then(function (response) {
    if (response.data.Redirect) {
  window.location = Router.action('Account', 'Login');
        return;
     }

    if (!response.data.success) {
    swal({
   title: "查詢失敗",
      text: response.data.error || "查詢時發生錯誤",
       icon: "error"
           });
         return;
        }

$scope.data = response.data.data || [];
        
           if ($scope.data.length === 0) {
  swal({
              title: "查無資料",
     text: "請確認查詢條件後重試",
      icon: "info"
          });
        }
            })
         .catch(function (error) {
      console.error('查詢失敗:', error);
  swal({
        title: "查詢失敗",
    text: "請稍後再試或聯繫系統管理員",
     icon: "error"
      });
            })
            .finally(function () {
           $scope.loading = false;
         });
    };

    // 清除查詢
    $scope.clearSearch = function () {
        $scope.searchForm.searchValue = '';
        $scope.data = [];
    };

    // 開啟明細頁面 (新分頁)
     $scope.openDetail = function (id, source) {
   var url = Router.action('SearchCargo', 'Detail') + '?id=' + encodeURIComponent(id) + '&source=' + encodeURIComponent(source || '');
        $window.open(url, '_blank');
    };

    // Enter鍵觸發查詢
    $scope.handleKeyPress = function (event) {
        if (event.keyCode === 13) {
          $scope.search();
        }
    };

    // 快捷鍵切換查詢類別
angular.element(document).on('keydown', function (event) {
        if (event.ctrlKey) {
            var index = -1;
   switch (event.keyCode) {
   case 49: // Ctrl+1
                index = 0;
break;
         case 50: // Ctrl+2
      index = 1;
       break;
           case 51: // Ctrl+3
        index = 2;
       break;
           case 52: // Ctrl+4
                index = 3;
   break;
        case 53: // Ctrl+5
index = 4;
   break;
        case 54: // Ctrl+6
      index = 5;
         break;
 }

        if (index >= 0 && index < $scope.searchTypeOptions.length) {
   event.preventDefault();
                $scope.$apply(function () {
        $scope.searchForm.searchType = $scope.searchTypeOptions[index].value;
 });
            }
      }
    });

    // 頁面離開時移除事件監聽
    $scope.$on('$destroy', function () {
        angular.element(document).off('keydown');
    });
});
