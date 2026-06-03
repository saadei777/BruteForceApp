# Drives the running WPF app via UI Automation and captures screenshots
# of each key state for the test report.
$ErrorActionPreference = 'SilentlyContinue'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

$sig = @'
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
'@
Add-Type -MemberDefinition $sig -Name Native -Namespace Win32

$AE  = [System.Windows.Automation.AutomationElement]
$TS  = [System.Windows.Automation.TreeScope]::Descendants
$IDP = [System.Windows.Automation.AutomationElement]::AutomationIdProperty

$shotDir = Join-Path $PSScriptRoot 'screenshots'
New-Item -ItemType Directory -Force -Path $shotDir | Out-Null

$global:hwnd = [IntPtr]::Zero

function Get-El($id) {
  # Re-fetch root from the live handle every time so we never use a stale tree.
  $root = $AE::FromHandle($global:hwnd)
  if ($null -eq $root) { return $null }
  $cond = New-Object System.Windows.Automation.PropertyCondition($IDP, $id)
  for ($k = 0; $k -lt 20; $k++) {
    $el = $root.FindFirst($TS, $cond)
    if ($null -ne $el) { return $el }
    Start-Sleep -Milliseconds 150
  }
  return $null
}
function Click-El($id) {
  $el = Get-El $id
  if ($null -eq $el) { Write-Host "WARN: $id not found"; return $false }
  $p = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
  $p.Invoke()
  return $true
}
function Text-El($id) {
  $el = Get-El $id
  if ($null -eq $el) { return '' }
  return $el.Current.Name
}
function Value-El($id) {
  $el = Get-El $id
  if ($null -eq $el) { return '' }
  $p = $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
  return $p.Current.Value
}
function Capture($name) {
  [Win32.Native]::ShowWindow($global:hwnd, 9) | Out-Null
  [Win32.Native]::SetForegroundWindow($global:hwnd) | Out-Null
  Start-Sleep -Milliseconds 500
  $r = New-Object Win32.Native+RECT
  [Win32.Native]::GetWindowRect($global:hwnd, [ref]$r) | Out-Null
  $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
  if ($w -le 0 -or $h -le 0) { Write-Host "bad rect for $name"; return }
  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left, $r.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
  $path = Join-Path $shotDir "$name.png"
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Host "saved $name.png"
}
function Alive($proc) { $proc.Refresh(); return -not $proc.HasExited }

# --- launch app ---
Get-Process BruteForceApp -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
$exe = Join-Path $PSScriptRoot 'bin\Debug\net8.0-windows\BruteForceApp.exe'
$proc = Start-Process -FilePath $exe -PassThru
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Milliseconds 250
  $proc.Refresh()
  if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { break }
}
$global:hwnd = $proc.MainWindowHandle
Write-Host "hwnd=$($global:hwnd)  alive=$(Alive $proc)"

# 1) initial state
Capture '01_initial'

# 2) generate password
Click-El 'BtnGenerate' | Out-Null
Start-Sleep -Milliseconds 700
$plain = Text-El 'TxtPlainPassword'
$hash  = Text-El 'TxtHash'
Write-Host "PLAIN=$plain"
Write-Host "HASH=$hash"
Capture '02_password_generated'

# 3) start attack, wait until found
Click-El 'BtnStart' | Out-Null
$found = ''
for ($i = 0; $i -lt 480; $i++) {
  Start-Sleep -Milliseconds 250
  if (-not (Alive $proc)) { Write-Host "APP EXITED during attack"; break }
  $found = Text-El 'TxtFoundPassword'
  if ($found -eq $plain -and $plain -ne '') { break }
}
Start-Sleep -Milliseconds 500
$elapsed = Text-El 'TxtElapsed'
$checked = Text-El 'TxtChecked'
$prog    = Text-El 'TxtProgress'
Write-Host "FOUND=$found ELAPSED=$elapsed CHECKED=$checked PROGRESS=$prog ALIVE=$(Alive $proc)"
Capture '03_password_found'

# 4) benchmark single vs multi thread
Click-El 'BtnBenchmark' | Out-Null
$log = ''
for ($i = 0; $i -lt 480; $i++) {
  Start-Sleep -Milliseconds 500
  if (-not (Alive $proc)) { Write-Host "APP EXITED during benchmark"; break }
  $log = Value-El 'TxtLog'
  if ($log -match 'Speedup') { break }
}
Start-Sleep -Milliseconds 700
Capture '04_benchmark'
Write-Host '----- LOG -----'
Write-Host $log
Write-Host '----- END -----'
Write-Host "DONE alive=$(Alive $proc)"
