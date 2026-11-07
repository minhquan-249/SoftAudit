# SoftAudit v2.0.0

Kiểm tra 1 máy Windows (PC hoặc Server) và xuất ra 1 file Excel:

| Sheet | Nội dung |
|---|---|
| **Summary** (1 dòng/máy) | HostName, Serial, Loại máy (Server/Client), IPv4, MAC, Windows (version, activation, channel), Office (version, activation, channel), ngày check, version tool |
| **Software** (1 dòng/phần mềm) | HostName, IPv4, Application, Version, Publisher, Install Date |

Gộp v1.3 (kiểm kê phần mềm, cách lấy IP cho server) và v1.4 (audit license) thành 1 bản.

## Chạy

Copy thư mục `SoftAudit v2.0.0` (4 file: `SoftAudit.exe`, `SoftAudit.exe.config`, `Run.bat`, `SoftAudit-Fallback.ps1`) rồi chạy `Run.bat`. Không cần quyền admin.

- File luôn được lưu vào `C:\SoftAudit\HOST_IP_yyyyMMdd.xlsx`, sau đó copy lên share `\\10.168.109.254\Software\1. Tool Soft Audit\Data`. Site khác thì sửa dòng `SHARE` trong `Run.bat`.
- Nếu `SoftAudit.exe` không chạy được (máy thiếu .NET Framework 4.8, ví dụ Server 2008 R2 / 2012 R2 chưa cài), `Run.bat` tự chạy `SoftAudit-Fallback.ps1`, cùng logic, xuất ra 2 file CSV (`_summary.csv`, `_software.csv`).
- Chạy tay: `SoftAudit.exe ["\\share\folder"] [/nopause]`. Exit code 0 = đã lưu được ít nhất 1 nơi.

## Cách hoạt động

- **Loại máy**: `Win32_OperatingSystem.ProductType` (1 = Client, 2/3 = Server).
- **IP**: Client lấy card mạng đang Up có gateway, bỏ card ảo/VPN (FortiClient, VMware, Hyper-V…). Server lấy IP mà Windows dùng để route ra ngoài (hợp với host Hyper-V, NIC teaming). Mỗi cách lỗi thì thử cách kia, rồi đến DNS.
- **MAC**: MAC của đúng card đang giữ IP ở trên.
- **License Windows / Office**: đọc WMI `SoftwareLicensingProduct` (và `OfficeSoftwareProtectionProduct` cho Office 2010 / Win7), không parse chữ của slmgr/ospp nữa, nên đúng với mọi ngôn ngữ Windows và mọi bản Office 2010+. Microsoft 365 đăng nhập bằng tài khoản không có key trong WMI, tool ghi "Microsoft 365 (check portal)".
- **Phần mềm**: registry Uninstall (HKLM 64/32-bit + HKCU), lọc/chuẩn hoá tên theo bộ rule cũ trong `software_rules.json` (nay nằm trong `Core/SoftwareScanner.cs`), bỏ trùng.

## Build

```
dotnet build -c Release
```

Kết quả ở `bin/Release/net48/` (`SoftAudit.exe` ~3 MB, ClosedXML đã được nhúng bằng Costura). Copy thêm `Dist/Run.bat` và `Dist/SoftAudit-Fallback.ps1` vào cùng thư mục.

Kiểm tra logic + layout Excel:

```
cd Tests/SelfCheck
dotnet build -c Release
bin/Release/net48/SelfCheck.exe
```

## Đổi so với v1.x

- Sửa lỗi: Office "UNLICENSED" bị báo là Activated (v1.x tìm chữ "licensed").
- Sửa lỗi: Windows/Office không phải tiếng Anh luôn ra Unknown.
- Sửa lỗi: chỉ tìm `ospp.vbs` của Office16.
- .NET 8 (exe 79 MB) → .NET Framework 4.8 (exe ~3 MB, có sẵn trong Windows 10/11, Server 2019/2022).
- Không còn cần thư mục `Config\`.
- Bỏ check AutoCAD (đã tắt từ v1.2).
