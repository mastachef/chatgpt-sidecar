param(
  [Parameter(Mandatory = $true)][string]$NodePath,
  [Parameter(Mandatory = $true)][string]$RuntimeScript,
  [Parameter(Mandatory = $true)][string]$QuickChatScript
)

$ErrorActionPreference = 'Stop'
$codexHome = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $HOME '.codex' }
$logPath = Join-Path $codexHome 'sidecar-hotkey.log'

function Write-SidecarHotkeyLog([string]$Message) {
  try {
    Add-Content -LiteralPath $logPath -Value "$(Get-Date -Format o) $Message" -Encoding UTF8
  } catch {}
}

try {
  Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class SidecarHotkeyNative {
    public const int WM_HOTKEY = 0x0312;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_NOREPEAT = 0x4000;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
}
"@
} catch {
  Write-SidecarHotkeyLog "Failed to load RegisterHotKey support: $($_.Exception.Message)"
  exit 1
}

$createdNew = $false
$mutex = New-Object System.Threading.Mutex($true, 'Local\ChatGPTSidecarHotkeyListener', [ref]$createdNew)
if (-not $createdNew) {
  Write-SidecarHotkeyLog 'Another Sidecar hotkey listener is already running.'
  exit 0
}

$hotkeyId = 0x5343
$virtualKeyS = 0x53
$modifiers = [SidecarHotkeyNative]::MOD_CONTROL -bor [SidecarHotkeyNative]::MOD_ALT -bor [SidecarHotkeyNative]::MOD_NOREPEAT

try {
  if (-not [SidecarHotkeyNative]::RegisterHotKey([IntPtr]::Zero, $hotkeyId, $modifiers, $virtualKeyS)) {
    $errorCode = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
    Write-SidecarHotkeyLog "Could not register Ctrl+Alt+S. Win32 error: $errorCode"
    exit 2
  }

  Write-SidecarHotkeyLog 'Registered Ctrl+Alt+S successfully.'

  while ($true) {
    $message = New-Object SidecarHotkeyNative+MSG
    $result = [SidecarHotkeyNative]::GetMessage([ref]$message, [IntPtr]::Zero, 0, 0)
    if ($result -le 0) { break }

    if ($message.message -eq [SidecarHotkeyNative]::WM_HOTKEY -and $message.wParam.ToUInt64() -eq [uint64]$hotkeyId) {
      Write-SidecarHotkeyLog 'Ctrl+Alt+S pressed; launching Quick Chat workflow.'
      $argumentList = @(
        '-NoProfile',
        '-STA',
        '-WindowStyle', 'Hidden',
        '-ExecutionPolicy', 'Bypass',
        '-File', ('"' + $QuickChatScript + '"'),
        '-NodePath', ('"' + $NodePath + '"'),
        '-RuntimeScript', ('"' + $RuntimeScript + '"')
      ) -join ' '
      Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -ArgumentList $argumentList | Out-Null
    }
  }
} catch {
  Write-SidecarHotkeyLog "Listener failure: $($_.Exception.Message)"
  exit 1
} finally {
  [SidecarHotkeyNative]::UnregisterHotKey([IntPtr]::Zero, $hotkeyId) | Out-Null
  try { $mutex.ReleaseMutex() } catch {}
  $mutex.Dispose()
}
