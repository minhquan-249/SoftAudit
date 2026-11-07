# SoftAudit v2.0.0

Kiểm tra 1 máy Windows (PC hoặc Server) và xuất ra 1 file Excel:

| Sheet | Nội dung |
|---|---|
| **Summary** (1 dòng/máy) | HostName, Serial, Loại máy (Server/Client), IPv4, MAC, Windows (version, activation, channel), Office (version, activation, channel), ngày check, version tool |
| **Software** (1 dòng/phần mềm) | HostName, IPv4, Application, Version, Publisher, Install Date |

Gộp v1.3 (kiểm kê phần mềm, cách lấy IP cho server) và v1.4 (audit license) thành 1 bản.

## Chạy

Mỗi site một gói riêng (giống SM-Tool): `SoftAudit-LITV`, `SoftAudit-HOT`, `SoftAudit-LDS`, `SoftAudit-Coralis`.
Copy thư mục gói (hoặc giải nén zip) rồi chạy `SoftAudit-<SITE>.bat`. Không cần quyền admin, không phải chọn gì.

Kết quả được lưu theo 3 tầng như SM-Tool:
1. Luôn lưu `C:\SoftAudit\HOST_IP_yyyyMMdd.xlsx`.
2. Đẩy lên Google Drive qua Apps Script chung với SM-Tool → `DATA/<SITE>/SoftAudit/`.
3. Drive lỗi thì copy lên share của site (`SharePath` trong `SoftAudit.ini`, thư mục cuối tự tạo nếu chưa có).

Nếu `SoftAudit.exe` không chạy được (máy thiếu .NET Framework 4.8, ví dụ Server 2008 R2 / 2012 R2 chưa cài), file bat tự chạy `SoftAudit-Fallback.ps1`: cùng logic, cùng 3 tầng, xuất 2 file CSV (`_summary.csv`, `_software.csv`).

Chạy tay: `SoftAudit.exe [/nopause]`. Thiếu `SoftAudit.ini` thì chỉ lưu `C:\SoftAudit`. Exit code 0 = đã lưu được ít nhất 1 nơi.

## Cấu hình site

`Sites\<SITE>.ini` (copy từ `Sites\_example.ini`), cùng giá trị với `SM-Tool\Sites\<SITE>.psd1`:

```
SiteName=HOT
DriveUploadUrl=https://script.google.com/macros/s/.../exec
SharePath=\\10.172.25.45\Public\It\2. Setup\Quanbm\DATA\SoftAudit
```

**Không commit file site thật** (repo public, file có URL Drive) — `.gitignore` đã chặn, chỉ `_example.ini` được commit.

## Apps Script (dùng chung với SM-Tool)

`AppsScript/Code.gs` nhận thêm `subFolder` và lưu theo site:

```
DATA/<SITE>/PC-Info/    CSV thông tin máy (SM-Tool)
DATA/<SITE>/SoftAudit/  Excel / CSV của SoftAudit
```

Cập nhật: dán code vào project Apps Script cũ → Save → **Deploy → Manage deployments → Edit → Version: New version → Deploy**. Không tạo deployment mới (URL `/exec` sẽ đổi, SM-Tool phải build lại).

## Cách hoạt động

- **Loại máy**: `Win32_OperatingSystem.ProductType` (1 = Client, 2/3 = Server).
- **IP**: Client lấy card mạng đang Up có gateway, bỏ card ảo/VPN (FortiClient, VMware, Hyper-V…). Server lấy IP mà Windows dùng để route ra ngoài (hợp với host Hyper-V, NIC teaming). Mỗi cách lỗi thì thử cách kia, rồi đến DNS.
- **MAC**: MAC của đúng card đang giữ IP ở trên.
- **License Windows / Office**: đọc WMI `SoftwareLicensingProduct` (và `OfficeSoftwareProtectionProduct` cho Office 2010 / Win7), không parse chữ của slmgr/ospp nữa, nên đúng với mọi ngôn ngữ Windows và mọi bản Office 2010+. Microsoft 365 đăng nhập bằng tài khoản không có key trong WMI, tool ghi "Microsoft 365 (check portal)".
- **Phần mềm**: registry Uninstall (HKLM 64/32-bit + HKCU), lọc/chuẩn hoá tên theo bộ rule cũ trong `software_rules.json` (nay nằm trong `Core/SoftwareScanner.cs`), bỏ trùng.

## Build

```
.\Build.ps1            # tất cả Sites\*.ini
.\Build.ps1 -Site HOT  # 1 site
```

Kết quả ở `Dist\SoftAudit-<SITE>\` + `Dist\SoftAudit-<SITE>.zip` (exe ~3 MB, ClosedXML nhúng bằng Costura). Cần .NET SDK (`dotnet`).

Kiểm tra:

```
cd Tests/SelfCheck && dotnet build -c Release && bin/Release/net48/SelfCheck.exe   # logic, ini, form Drive, layout Excel
node Tests/AppsScript/code-gs.test.js                                               # routing thư mục của Code.gs
```

## Đổi so với v1.x

- Sửa lỗi: Office "UNLICENSED" bị báo là Activated (v1.x tìm chữ "licensed").
- Sửa lỗi: Windows/Office không phải tiếng Anh luôn ra Unknown.
- Sửa lỗi: chỉ tìm `ospp.vbs` của Office16.
- .NET 8 (exe 79 MB) → .NET Framework 4.8 (exe ~3 MB, có sẵn trong Windows 10/11, Server 2019/2022).
- Không còn cần thư mục `Config\`.
- Mỗi site 1 gói (`Sites\*.ini` + `Build.ps1`), upload Drive → share giống SM-Tool, thêm cột Site.
- Bỏ check AutoCAD (đã tắt từ v1.2).
