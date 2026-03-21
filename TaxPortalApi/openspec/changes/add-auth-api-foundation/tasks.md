## 1. 設定與基礎設施

- [x] 1.1 在後端專案加入 SQL Server、JWT 與密碼雜湊驗證所需套件與設定模型
- [x] 1.2 註冊 DbContext、JWT Authentication/Authorization 與全域例外處理管線

## 2. 認證與回應模型實作

- [x] 2.1 建立 TaxPortalUser 實體、登入請求模型、ApiResponse 模型與相關例外類型
- [x] 2.2 實作 AuthService 與 JwtTokenService，完成帳密驗證與 Token 簽發流程

## 3. API 與文件整合

- [x] 3.1 建立 AuthController，提供登入端點與受保護的使用者資訊端點，並補齊 XML summary 註解
- [x] 3.2 更新 Swagger/Knife4j Bearer 設定，使文件可直接授權測試受保護 API

## 4. 驗證

- [x] 4.1 執行 dotnet build 驗證專案可成功編譯
- [x] 4.2 檢查 OpenSpec change artifacts 與實作結果一致