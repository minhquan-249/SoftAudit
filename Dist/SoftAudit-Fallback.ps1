# SoftAudit v2.0.0 - PowerShell fallback
# Used by Run.bat only when SoftAudit.exe cannot run (e.g. .NET Framework 4.8 missing on an old server).
# Same checks as the exe, written for PowerShell 2.0+ (Windows 7 / Server 2008 R2 and later).
# Output: <HOST>_<IP>_<yyyyMMdd>_summary.csv and _software.csv, in C:\SoftAudit and the site share (-SharePath).

param([string]$SharePath = '')

$ErrorActionPreference = 'SilentlyContinue'
$ToolVersion = '2.0.0-ps'
$LocalFolder = 'C:\SoftAudit'
Write-Host "SoftAudit v$ToolVersion (PowerShell fallback)`n"

function Get-Wmi1($class, $prop) {
    $o = Get-WmiObject -Class $class | Select-Object -First 1
    if ($o) { "$($o.$prop)".Trim() } else { '' }
}

# ---------------- machine ----------------
$hostName = $env:COMPUTERNAME
$isServer = @('2', '3') -contains (Get-Wmi1 'Win32_OperatingSystem' 'ProductType')
$machineType = if ($isServer) { 'Server' } else { 'Client' }
$serial = Get-Wmi1 'Win32_BIOS' 'SerialNumber'
if (-not $serial) { $serial = 'Unknown' }
$osCaption = Get-Wmi1 'Win32_OperatingSystem' 'Caption'
$osVersion = Get-Wmi1 'Win32_OperatingSystem' 'Version'
$windowsVersion = "$osCaption ($osVersion)"

# ---------------- network (same rules as NetworkHelper.cs) ----------------
function Test-UsableIp($ip) { $ip -and -not ($ip.StartsWith('127.') -or $ip.StartsWith('169.254.') -or $ip -eq '0.0.0.0') }

function Get-IpFromAdapters {
    $skip = 'virtual', 'vpn', 'vmware', 'hyper-v', 'fortinet', 'bluetooth', 'tap-', 'wan miniport'
    foreach ($ni in [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces()) {
        if ($ni.OperationalStatus -ne 'Up' -or $ni.NetworkInterfaceType -eq 'Loopback') { continue }
        $desc = ($ni.Description + ' ' + $ni.Name).ToLower()
        $isSkipped = $false
        foreach ($s in $skip) { if ($desc.Contains($s)) { $isSkipped = $true } }
        if ($isSkipped) { continue }
        $props = $ni.GetIPProperties()
        $gw = @($props.GatewayAddresses | Where-Object { $_.Address.ToString() -ne '0.0.0.0' })
        if ($gw.Count -eq 0) { continue }
        foreach ($a in $props.UnicastAddresses) {
            $ip = $a.Address.ToString()
            if ($a.Address.AddressFamily -eq 'InterNetwork' -and (Test-UsableIp $ip)) { return $ip }
        }
    }
    return $null
}

function Get-IpFromRoute {
    try {
        $s = New-Object System.Net.Sockets.Socket('InterNetwork', 'Dgram', 'Udp')
        $s.Connect('8.8.8.8', 65530)
        $ip = $s.LocalEndPoint.Address.ToString()
        $s.Close()
        if (Test-UsableIp $ip) { return $ip }
    } catch { }
    return $null
}

if ($isServer) { $ip = Get-IpFromRoute; if (-not $ip) { $ip = Get-IpFromAdapters } }
else { $ip = Get-IpFromAdapters; if (-not $ip) { $ip = Get-IpFromRoute } }
if (-not $ip) { $ip = 'NoIP' }

$mac = 'Unknown'
foreach ($ni in [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces()) {
    foreach ($a in $ni.GetIPProperties().UnicastAddresses) {
        if ($a.Address.ToString() -eq $ip) {
            $bytes = $ni.GetPhysicalAddress().GetAddressBytes()
            if ($bytes.Length -gt 0) { $mac = ($bytes | ForEach-Object { $_.ToString('X2') }) -join '-' }
        }
    }
}

# ---------------- licences (same as LicenseChecker.cs) ----------------
function Get-License($classes, $appId) {
    $products = @()
    foreach ($c in $classes) {
        $products += @(Get-WmiObject -Query "SELECT Description, LicenseStatus FROM $c WHERE ApplicationID='$appId' AND PartialProductKey IS NOT NULL")
    }
    $products = @($products | Where-Object { $_ })
    if ($products.Count -eq 0) { return @('Not Detected', '-') }

    $p = $products | Where-Object { $_.LicenseStatus -eq 1 } | Select-Object -First 1
    if (-not $p) { $p = $products[0] }

    $status = switch ([int]$p.LicenseStatus) {
        0 { 'Not Activated' } 1 { 'Activated' } 2 { 'Grace Period (OOB)' } 3 { 'Grace Period (OOT)' }
        4 { 'Non-Genuine Grace' } 5 { 'Not Activated (Notification)' } 6 { 'Extended Grace' } default { 'Unknown' }
    }
    $d = "$($p.Description)".ToUpper()
    $channel = 'Unknown'
    if ($d.Contains('KMS')) { $channel = 'KMS' }
    elseif ($d.Contains('MAK')) { $channel = 'MAK' }
    elseif ($d.Contains('OEM')) { $channel = 'OEM' }
    elseif ($d.Contains('RETAIL')) { $channel = 'Retail' }
    elseif ($d.Contains('TIMEBASED') -or $d.Contains('SUBSCRIPTION')) { $channel = 'Subscription' }
    return @($status, $channel)
}

$win = Get-License @('SoftwareLicensingProduct') '55c92734-d682-4d71-983e-d6ec3f16059f'
$off = Get-License @('SoftwareLicensingProduct', 'OfficeSoftwareProtectionProduct') '0ff1ce15-a989-479d-af46-f275c6370663'

# ---------------- installed software (same rules as SoftwareScanner.cs) ----------------
$nameMapping = @(
    @('visual studio code', 'Visual Studio Code'), @('google chrome', 'Google Chrome'),
    @('microsoft edge', 'Microsoft Edge'), @('onedrive', 'Microsoft OneDrive'), @('teams', 'Microsoft Teams'),
    @('winrar', 'WinRAR'), @('notepad++', 'Notepad++'), @('java', 'Java Runtime')
)
$inclusions = @('webview2')
$exclusions = @('kb', 'security update', 'hotfix', 'workload', 'manifest', 'mui', 'proofing', 'language',
    'correction', 'setup metadata', 'shared', 'component', 'targeting pack', 'apphost', 'templates',
    'toolset', 'linguistic', 'linguistique', 'corrección')

$regPaths = @(
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKCU:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
)
$entries = @(foreach ($p in $regPaths) { Get-ItemProperty $p | Where-Object { $_.DisplayName } })

# Office suite name, as OfficeCollector.cs
$officeVersion = 'Not Detected'
$notSuite = 'update', 'click-to-run', 'proofing', 'language', 'mui', 'viewer', 'compatibility', 'web components'
foreach ($e in ($entries | Where-Object { $_.PSPath -like '*HKEY_LOCAL_MACHINE*' })) {
    $n = "$($e.DisplayName)".Trim(); $l = $n.ToLower()
    if (($l.StartsWith('microsoft office') -or $l.StartsWith('microsoft 365'))) {
        $bad = $false; foreach ($x in $notSuite) { if ($l.Contains($x)) { $bad = $true } }
        if (-not $bad) { $officeVersion = $n; break }
    }
}
if ($officeVersion -eq 'Not Detected') {
    $c2r = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\Configuration'
    if ($c2r -and $c2r.ProductReleaseIds) { $officeVersion = "Microsoft Office ($($c2r.ProductReleaseIds)) [$($c2r.VersionToReport)]" }
}
if ($off[0] -eq 'Not Detected' -and $officeVersion.Contains('365')) { $off = @('Microsoft 365 (check portal)', 'Subscription') }
if ($officeVersion -eq 'Not Detected') { $off = @('-', '-') }

$seen = @{}
$software = foreach ($e in $entries) {
    $raw = "$($e.DisplayName)".Trim(); $lower = $raw.ToLower()
    $keep = $true
    $forced = $false; foreach ($i in $inclusions) { if ($lower.Contains($i)) { $forced = $true } }
    if (-not $forced) { foreach ($x in $exclusions) { if ($lower.Contains($x)) { $keep = $false } } }
    if (-not $keep) { continue }

    $name = $null
    foreach ($m in $nameMapping) { if (-not $name -and $lower.Contains($m[0])) { $name = $m[1] } }
    if (-not $name) {
        $name = $raw
        foreach ($noise in '(x64)', '(x86)', '(64-bit)', '(32-bit)', '(User)', 'x64', 'Preview', 'preview', 'phiên bản') { $name = $name.Replace($noise, '') }
        $name = (($name -split ' ') | Where-Object { $_ }) -join ' '
    }
    if (-not $name -or $seen.ContainsKey($name.ToLower())) { continue }
    $seen[$name.ToLower()] = $true

    $date = "$($e.InstallDate)"
    if ($date -match '^\d{8}$') { $date = $date.Substring(0, 4) + '-' + $date.Substring(4, 2) + '-' + $date.Substring(6, 2) }
    if (-not $date) { $date = 'N/A' }

    New-Object PSObject -Property @{
        HostName = $hostName; IPv4 = $ip; Application = $name
        Version = "$($e.DisplayVersion)"; Publisher = "$($e.Publisher)"; 'Install Date' = $date
    }
}
$software = @($software | Sort-Object Application | Select-Object HostName, IPv4, Application, Version, Publisher, 'Install Date')

$summary = New-Object PSObject -Property @{
    HostName = $hostName; 'Serial Number' = $serial; 'Machine Type' = $machineType; IPv4 = $ip; MAC = $mac
    'Windows Version' = $windowsVersion; 'Windows Activation' = $win[0]; 'Windows Channel' = $win[1]
    'Office Version' = $officeVersion; 'Office Activation' = $off[0]; 'Office Channel' = $off[1]
    'Check Date' = (Get-Date -Format 'yyyy-MM-dd HH:mm'); 'Tool Version' = $ToolVersion
} | Select-Object HostName, 'Serial Number', 'Machine Type', IPv4, MAC, 'Windows Version', 'Windows Activation',
    'Windows Channel', 'Office Version', 'Office Activation', 'Office Channel', 'Check Date', 'Tool Version'

Write-Host "[SYSTEM]   $hostName | $machineType | $ip | $mac | SN $serial"
Write-Host "[WINDOWS]  $windowsVersion | $($win[0]) ($($win[1]))"
Write-Host "[OFFICE]   $officeVersion | $($off[0]) ($($off[1]))"
Write-Host "[SOFTWARE] $($software.Count) applications`n"

# ---------------- save: local first, then share ----------------
$base = '{0}_{1}_{2}' -f $hostName, $ip.Replace(':', '_'), (Get-Date -Format 'yyyyMMdd')
$saved = $false
foreach ($folder in @($LocalFolder, $SharePath | Where-Object { $_ })) {
    try {
        if (-not (Test-Path $folder)) { New-Item -ItemType Directory -Path $folder -ErrorAction Stop | Out-Null }
        if (-not (Test-Path $folder)) { throw 'not reachable' }
        $summary | Export-Csv -Path (Join-Path $folder "$base`_summary.csv") -NoTypeInformation -Encoding UTF8 -ErrorAction Stop
        $software | Export-Csv -Path (Join-Path $folder "$base`_software.csv") -NoTypeInformation -Encoding UTF8 -ErrorAction Stop
        Write-Host "[EXPORT]   $folder\$base`_*.csv"
        $saved = $true
    } catch {
        Write-Host "[EXPORT]   Failed: $folder ($_)"
    }
}
if (-not $saved) { exit 1 }
exit 0
