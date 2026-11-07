# SoftAudit v2.0.1 - PowerShell fallback
# Used by Run.bat only when SoftAudit.exe cannot run (e.g. .NET Framework 4.8 missing on an old server).
# Same checks as the exe, written for PowerShell 2.0+ (Windows 7 / Server 2008 R2 and later).
# Output: <HOST>_<IP>_<yyyyMMdd>_summary.csv and _software.csv.
# Same tiers as the exe / SM-Tool: always C:\SoftAudit, then Google Drive, then the site share if Drive failed.
# Site settings come from SoftAudit.ini next to this script (SiteName, DriveUploadUrl, SharePath).

$ErrorActionPreference = 'SilentlyContinue'
$ToolVersion = '2.0.1-ps'
$LocalFolder = 'C:\SoftAudit'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path   # $PSScriptRoot does not exist in PS 2.0

$Site = @{ SiteName = ''; DriveUploadUrl = ''; SharePath = '' }
$iniPath = Join-Path $ScriptDir 'SoftAudit.ini'
if (Test-Path $iniPath) {
    foreach ($line in Get-Content $iniPath) {
        $line = $line.Trim()
        if (-not $line -or $line.StartsWith(';') -or $line.StartsWith('#') -or $line.IndexOf('=') -lt 1) { continue }
        $key = $line.Substring(0, $line.IndexOf('=')).Trim()
        $value = $line.Substring($line.IndexOf('=') + 1).Trim().Trim('"', "'")
        if ($Site.ContainsKey($key)) { $Site[$key] = $value }
    }
}

# ---------------- log: same file and format as SoftAudit.exe / SM-Tool, marked [PS] ----------------
$Started = Get-Date
$LogFile = $null
foreach ($d in @('C:\SoftAudit', (Join-Path ([IO.Path]::GetTempPath()) 'SoftAudit'))) {
    try {
        if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -ErrorAction Stop | Out-Null }
        $f = Join-Path $d ('{0}_{1}.log' -f $env:COMPUTERNAME, (Get-Date -Format 'yyyyMMdd'))
        Add-Content -Path $f -Value '' -ErrorAction Stop
        $LogFile = $f; break
    } catch { }
}

function Write-Log($Message, $Level = 'INFO') {
    if (-not $LogFile) { return }
    try {
        $tag = if ($Site.SiteName) { "$($Site.SiteName) " } else { '' }
        $line = '{0} [{1}] [{2}v{3}] [PS] {4}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level.PadRight(7), $tag, $ToolVersion, (("$Message" -replace '\s*\r?\n\s*', ' ').Trim())
        Add-Content -Path $LogFile -Value $line -ErrorAction Stop
    } catch { }   # logging must never stop the audit
}

# console + log
function Say($Message, $Level = 'INFO') { Write-Host $Message; Write-Log $Message $Level }

function Finish($Code) {
    Write-Log ('===== END ===== exit code {0}, {1:0}s' -f $Code, ((Get-Date) - $Started).TotalSeconds)
    if ($LogFile) { Write-Host "`nLog: $LogFile" }
    exit $Code
}

Write-Log "===== START ===== user $env:USERDOMAIN\$env:USERNAME, PowerShell $($PSVersionTable.PSVersion), script $ScriptDir"
if (Test-Path $iniPath) { Write-Log "Config: $iniPath | Drive: $(if ($Site.DriveUploadUrl) { 'on' } else { 'off' }) | Share: $(if ($Site.SharePath) { $Site.SharePath } else { 'off' })" }
else { Write-Log "Config: $iniPath not found -> local only" 'WARNING' }

Write-Host "SoftAudit v$ToolVersion (PowerShell fallback) - site: $(if ($Site.SiteName) { $Site.SiteName } else { 'none, local only' })`n"

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

function Get-IpsFromAdapters {
    $ips = @()
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
            if ($a.Address.AddressFamily -eq 'InterNetwork' -and (Test-UsableIp $ip)) { $ips += $ip; break }
        }
    }
    return ,$ips
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

# Client: routed IP (respects LAN/Wi-Fi metric) only if it is on a physical adapter with a gateway.
$routeIp = Get-IpFromRoute
$adapterIps = Get-IpsFromAdapters
if ($isServer) { $ip = $routeIp; if (-not $ip) { $ip = $adapterIps | Select-Object -First 1 } }
elseif ($routeIp -and ($adapterIps -contains $routeIp)) { $ip = $routeIp }
elseif ($adapterIps.Count -gt 0) { $ip = $adapterIps[0] }
else { $ip = $routeIp }
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
    Site = $Site.SiteName; HostName = $hostName; 'Serial Number' = $serial; 'Machine Type' = $machineType; IPv4 = $ip; MAC = $mac
    'Windows Version' = $windowsVersion; 'Windows Activation' = $win[0]; 'Windows Channel' = $win[1]
    'Office Version' = $officeVersion; 'Office Activation' = $off[0]; 'Office Channel' = $off[1]
    'Check Date' = (Get-Date -Format 'yyyy-MM-dd HH:mm'); 'Tool Version' = $ToolVersion
} | Select-Object Site, HostName, 'Serial Number', 'Machine Type', IPv4, MAC, 'Windows Version', 'Windows Activation',
    'Windows Channel', 'Office Version', 'Office Activation', 'Office Channel', 'Check Date', 'Tool Version'

Say "[SYSTEM]   $hostName | $machineType | $ip | $mac | SN $serial"
Say "[WINDOWS]  $windowsVersion | $($win[0]) ($($win[1]))"
Say "[OFFICE]   $officeVersion | $($off[0]) ($($off[1]))"
Write-Log "Software: $($entries.Count) registry entries -> $($software.Count) after filter/dedup"
Say "[SOFTWARE] $($software.Count) applications`n"

# ---------------- save: local, then Drive, then share (same as SM-Tool) ----------------
$base = '{0}_{1}_{2}' -f $hostName, $ip.Replace(':', '_'), (Get-Date -Format 'yyyyMMdd')
$files = @("$base`_summary.csv", "$base`_software.csv")

function Save-Csvs($folder) {
    if (-not (Test-Path $folder)) { New-Item -ItemType Directory -Path $folder -ErrorAction Stop | Out-Null }
    $summary | Export-Csv -Path (Join-Path $folder $files[0]) -NoTypeInformation -Encoding UTF8 -ErrorAction Stop
    $software | Export-Csv -Path (Join-Path $folder $files[1]) -NoTypeInformation -Encoding UTF8 -ErrorAction Stop
}

# Apps Script web app shared with SM-Tool (AppsScript\Code.gs): form POST, overwrites same name -> safe to retry
function Send-ToDrive($path) {
    if ((Get-Item $path).Length -eq 0) { return $true }   # empty CSV (no software): Apps Script rejects empty fileData
    try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072 } catch { }  # TLS 1.2
    $form = New-Object System.Collections.Specialized.NameValueCollection
    $form.Add('site', $Site.SiteName)
    $form.Add('subFolder', 'SoftAudit')
    $form.Add('fileName', [IO.Path]::GetFileName($path))
    $form.Add('fileData', [Convert]::ToBase64String([IO.File]::ReadAllBytes($path)))
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $wc = New-Object System.Net.WebClient
            $resp = [Text.Encoding]::UTF8.GetString($wc.UploadValues($Site.DriveUploadUrl, 'POST', $form))
            if ($resp.Replace(' ', '') -match '"success":true') { Write-Log "Drive upload OK: $([IO.Path]::GetFileName($path)), attempt $attempt, response $resp"; return $true }
            Say "           Drive refused (attempt $attempt/3): $resp" 'WARNING'
        } catch {
            Say "           Drive error (attempt $attempt/3): $_" 'WARNING'
        }
        if ($attempt -lt 3) { Start-Sleep -Seconds (5 * $attempt) }
    }
    return $false
}

$localOk = $false
try {
    Save-Csvs $LocalFolder
    $localOk = $true
    Say "[EXPORT]   Local:   $LocalFolder\$base`_*.csv"
} catch {
    Say "[EXPORT]   Local failed: $_" 'ERROR'
}

if ($localOk -and $Site.DriveUploadUrl) {
    Say '[EXPORT]   Uploading to Google Drive...'
    $driveOk = $true
    foreach ($f in $files) { if (-not (Send-ToDrive (Join-Path $LocalFolder $f))) { $driveOk = $false } }
    if ($driveOk) { Say "[EXPORT]   Drive:   $($Site.SiteName)/SoftAudit/$base`_*.csv"; Finish 0 }
    Say '[EXPORT]   Drive failed.' 'WARNING'
}

if ($Site.SharePath) {
    try {
        Save-Csvs $Site.SharePath
        Say "[EXPORT]   Share:   $($Site.SharePath)\$base`_*.csv"
        Finish 0
    } catch {
        Say "[EXPORT]   Share failed: $_" 'WARNING'
    }
}

if ($localOk) { Say "[EXPORT]   Only saved locally. Please send $LocalFolder\$base`_*.csv to IT." 'WARNING'; Finish 0 }
Finish 1
