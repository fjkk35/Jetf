## ADDED Requirements

### Requirement: 系統必須提供捷利托運資料查詢頁面
系統 MUST 在捷穩通模組中提供「捷利托運資料查詢」頁面，讓使用者查詢已上傳的捷利托運資料。

#### Scenario: 使用者開啟捷利托運資料查詢頁面
- **GIVEN** 使用者具有捷利托運資料上傳權限
- **WHEN** 使用者進入捷利托運資料查詢頁面
- **THEN** 系統顯示日期起、日期迄、運送編號查詢欄位
- **AND** 系統顯示查詢與清除按鈕

### Requirement: 系統必須支援依 CreatedTime 與運送編號查詢資料
系統 MUST 查詢 jetf.dbo.SjlShippingData，並支援依 CreatedTime 日期區間與 JetfSerial 篩選資料。

#### Scenario: 使用日期區間查詢資料
- **GIVEN** SjlShippingData 中存在符合條件資料
- **WHEN** 使用者輸入日期起與日期迄後執行查詢
- **THEN** 系統以 CreatedTime 大於等於日期起且小於日期迄加 1 天查詢資料

#### Scenario: 使用運送編號查詢資料
- **GIVEN** SjlShippingData 中存在指定運送編號的資料
- **WHEN** 使用者輸入運送編號後執行查詢
- **THEN** 系統以 JetfSerial 精準比對查詢資料

#### Scenario: 查詢結果顯示完整欄位
- **WHEN** 系統回傳查詢結果
- **THEN** 每筆資料必須顯示運送編號、單據編號、編號、收件人、派送日、其他費用、代收、地址、品名、件數、材積、重量、收件人電話、派件公司

### Requirement: 系統必須支援分頁查詢結果
系統 MUST 對捷利托運資料查詢結果提供分頁。

#### Scenario: 查詢結果超過單頁筆數
- **GIVEN** 查詢結果超過單頁筆數
- **WHEN** 使用者執行查詢
- **THEN** 系統只回傳指定頁碼資料
- **AND** 系統回傳總筆數供前端顯示分頁

### Requirement: 系統必須允許修改派件公司
系統 MUST 允許使用者在查詢結果中修改單筆資料的派件公司，且派件公司選項僅能為「大榮」或「捷通」。

#### Scenario: 開啟修改派件公司彈窗
- **GIVEN** 查詢結果中存在任一筆資料
- **WHEN** 使用者點擊修改按鈕
- **THEN** 系統顯示彈窗
- **AND** 彈窗下拉選單僅提供「大榮」與「捷通」

#### Scenario: 使用者選擇與原本相同的派件公司
- **GIVEN** 某筆資料的派件公司為「大榮」
- **WHEN** 使用者在彈窗中仍選擇「大榮」
- **THEN** 系統 MUST 不更新資料
- **AND** 系統顯示不需修改訊息

#### Scenario: 使用者成功修改派件公司
- **GIVEN** 某筆資料的派件公司為空白或與新值不同
- **WHEN** 使用者在彈窗中選擇合法派件公司並儲存
- **THEN** 系統更新 SjlShippingData.TransName
- **AND** 系統回傳最新派件公司

### Requirement: 系統必須記錄派件公司修改歷史
系統 MUST 在修改派件公司時，新增一筆 SjlShippingDataTransNameHistory 歷史資料。

#### Scenario: 派件公司修改成功時記錄歷史
- **GIVEN** 使用者成功將某筆資料派件公司由空白改為「捷通」
- **WHEN** 系統完成更新
- **THEN** 系統在 SjlShippingDataTransNameHistory 新增一筆資料
- **AND** 歷史資料必須記錄 SjlShippingDataId、OldTransName、NewTransName、CreatedOpe、CreatedTime