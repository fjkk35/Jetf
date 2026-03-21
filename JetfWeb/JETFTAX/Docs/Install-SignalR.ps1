# SignalR 套件安裝腳本
# 請在 Visual Studio 的 Package Manager Console 中執行此腳本

Write-Host "開始安裝 SignalR 相關套件..." -ForegroundColor Green

# 安裝主要的 SignalR 套件
Install-Package Microsoft.AspNet.SignalR -Version 2.4.3 -ProjectName JETFTAX

Write-Host "SignalR 套件安裝完成！" -ForegroundColor Green
Write-Host "請重新建置專案以確認安裝成功。" -ForegroundColor Yellow
