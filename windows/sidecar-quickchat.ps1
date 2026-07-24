param(
  [Parameter(Mandatory = $true)][string]$NodePath,
  [Parameter(Mandatory = $true)][string]$RuntimeScript
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

try {
  Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class SidecarWindowNative {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
"@
} catch {
  # The type can already exist when the script is debugged in a reused host.
}

$originHandle = [SidecarWindowNative]::GetForegroundWindow()
$codexHome = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $HOME '.codex' }
$logPath = Join-Path $codexHome 'sidecar-quickchat.log'

function Write-SidecarLog([string]$Message) {
  try {
    Add-Content -LiteralPath $logPath -Value "$(Get-Date -Format o) $Message" -Encoding UTF8
  } catch {}
}

function Show-SidecarError([string]$Message) {
  Write-SidecarLog "ERROR $Message"
  [System.Windows.Forms.MessageBox]::Show(
    $Message,
    'ChatGPT Sidecar',
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Error
  ) | Out-Null
}

function Invoke-NodeJson([string[]]$Arguments) {
  $output = & $NodePath $RuntimeScript @Arguments 2>&1
  if ($LASTEXITCODE -ne 0) {
    throw ($output | Out-String).Trim()
  }
  $text = ($output | Out-String).Trim()
  if ([string]::IsNullOrWhiteSpace($text)) { throw 'Sidecar returned no data.' }
  return $text | ConvertFrom-Json
}

function Get-ProcessForHandle([IntPtr]$Handle) {
  if ($Handle -eq [IntPtr]::Zero) { return $null }
  [uint32]$pidValue = 0
  [SidecarWindowNative]::GetWindowThreadProcessId($Handle, [ref]$pidValue) | Out-Null
  if ($pidValue -eq 0) { return $null }
  return Get-Process -Id $pidValue -ErrorAction SilentlyContinue
}

function Get-ChatGPTWindowHandle {
  $originProcess = Get-ProcessForHandle $originHandle
  if ($originProcess -and (
      $originProcess.ProcessName -match '(?i)chatgpt|codex|openai' -or
      $originProcess.MainWindowTitle -match '(?i)chatgpt|codex'
    )) {
    return $originHandle
  }

  $candidate = Get-Process -ErrorAction SilentlyContinue |
    Where-Object {
      $_.MainWindowHandle -ne 0 -and (
        $_.ProcessName -match '(?i)chatgpt|codex|openai' -or
        $_.MainWindowTitle -match '(?i)chatgpt|codex'
      )
    } |
    Sort-Object StartTime -Descending |
    Select-Object -First 1

  if ($candidate) { return [IntPtr]$candidate.MainWindowHandle }
  return [IntPtr]::Zero
}

function Get-ElementText([System.Windows.Automation.AutomationElement]$Element, [string]$PropertyName) {
  try {
    switch ($PropertyName) {
      'Name' { return [string]$Element.Current.Name }
      'AutomationId' { return [string]$Element.Current.AutomationId }
      'HelpText' { return [string]$Element.Current.HelpText }
    }
  } catch {}
  return ''
}

function Get-Descendants([System.Windows.Automation.AutomationElement]$Root) {
  return $Root.FindAll(
    [System.Windows.Automation.TreeScope]::Descendants,
    [System.Windows.Automation.Condition]::TrueCondition
  )
}

function Invoke-AutomationElement([System.Windows.Automation.AutomationElement]$Element) {
  try {
    $pattern = $null
    if ($Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
      $pattern.Invoke()
      return $true
    }
  } catch {}

  try {
    $Element.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    return $true
  } catch {}
  return $false
}

function Find-NewChatControl([System.Windows.Automation.AutomationElement]$Root) {
  $elements = Get-Descendants $Root
  $best = $null
  $bestScore = -1

  for ($i = 0; $i -lt $elements.Count; $i++) {
    $element = $elements.Item($i)
    try {
      if (-not $element.Current.IsEnabled -or $element.Current.IsOffscreen) { continue }
      $name = Get-ElementText $element 'Name'
      $automationId = Get-ElementText $element 'AutomationId'
      $controlType = $element.Current.ControlType
      $score = 0

      if ($name -match '^(?i)new chat$') { $score += 120 }
      elseif ($name -match '^(?i)quick chat$') { $score += 115 }
      elseif ($name -match '(?i)new chat|quick chat|start a new chat') { $score += 90 }
      if ($automationId -match '(?i)new.?chat|quick.?chat') { $score += 80 }
      if ($controlType -eq [System.Windows.Automation.ControlType]::Button) { $score += 20 }
      elseif ($controlType -eq [System.Windows.Automation.ControlType]::Hyperlink) { $score += 10 }

      if ($score -gt $bestScore) {
        $bestScore = $score
        $best = $element
      }
    } catch {}
  }

  if ($bestScore -lt 80) { return $null }
  return $best
}

function Find-QuickChatComposer([System.Windows.Automation.AutomationElement]$Root) {
  $elements = Get-Descendants $Root
  $rootRect = $Root.Current.BoundingRectangle
  $best = $null
  $bestScore = -1

  for ($i = 0; $i -lt $elements.Count; $i++) {
    $element = $elements.Item($i)
    try {
      if (-not $element.Current.IsEnabled -or $element.Current.IsOffscreen) { continue }
      if ($element.Current.ControlType -ne [System.Windows.Automation.ControlType]::Edit) { continue }

      $name = Get-ElementText $element 'Name'
      $automationId = Get-ElementText $element 'AutomationId'
      $helpText = Get-ElementText $element 'HelpText'
      $rect = $element.Current.BoundingRectangle
      if ($rect.Width -lt 160 -or $rect.Height -lt 24) { continue }

      $score = 10
      $joined = "$name $automationId $helpText"
      if ($joined -match '(?i)message chatgpt|ask anything|send a message|prompt|composer') { $score += 120 }
      elseif ($joined -match '(?i)message|chatgpt|chat') { $score += 80 }
      if ($rect.Bottom -gt ($rootRect.Top + ($rootRect.Height * 0.55))) { $score += 35 }
      $score += [Math]::Min(30, [Math]::Floor($rect.Width / 100))

      if ($score -gt $bestScore) {
        $bestScore = $score
        $best = $element
      }
    } catch {}
  }

  return $best
}

function Open-QuickChatAndSubmit([IntPtr]$WindowHandle) {
  if ($WindowHandle -eq [IntPtr]::Zero) { throw 'The ChatGPT desktop window could not be found.' }
  [SidecarWindowNative]::ShowWindowAsync($WindowHandle, 9) | Out-Null
  Start-Sleep -Milliseconds 200
  [SidecarWindowNative]::SetForegroundWindow($WindowHandle) | Out-Null
  Start-Sleep -Milliseconds 350

  $root = [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
  if (-not $root) { throw 'Windows Accessibility could not inspect the ChatGPT window.' }
  $newChat = Find-NewChatControl $root
  if (-not $newChat) {
    throw 'The ChatGPT “New chat” control was not found. The prepared Sidecar prompt remains on your clipboard.'
  }
  if (-not (Invoke-AutomationElement $newChat)) {
    throw 'The ChatGPT “New chat” control was found but could not be activated.'
  }

  $composer = $null
  for ($attempt = 0; $attempt -lt 24 -and -not $composer; $attempt++) {
    Start-Sleep -Milliseconds 250
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
    if ($root) { $composer = Find-QuickChatComposer $root }
  }

  if ($composer) {
    $composer.SetFocus()
  } else {
    # New Quick Chats normally autofocus their composer. This fallback still avoids
    # interacting with the Codex composer because New chat has already been invoked.
    Write-SidecarLog 'Quick Chat composer was not exposed through UI Automation; using the autofocus fallback.'
  }

  Start-Sleep -Milliseconds 200
  [System.Windows.Forms.SendKeys]::SendWait('^v')
  Start-Sleep -Milliseconds 700
  [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
}

try {
  Write-SidecarLog 'Starting Quick Chat workflow.'
  $sessions = @(Invoke-NodeJson @('list', '10'))
  if ($sessions.Count -eq 0) {
    throw 'No saved Codex conversations were found. Open a Codex project and send at least one normal Codex message first.'
  }

  $form = New-Object System.Windows.Forms.Form
  $form.Text = 'ChatGPT Sidecar'
  $form.Size = New-Object System.Drawing.Size(640, 410)
  $form.StartPosition = 'CenterScreen'
  $form.FormBorderStyle = 'FixedDialog'
  $form.MaximizeBox = $false
  $form.MinimizeBox = $false
  $form.TopMost = $true
  $form.KeyPreview = $true

  $title = New-Object System.Windows.Forms.Label
  $title.Text = 'Send Codex context to ChatGPT Quick Chat'
  $title.Font = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
  $title.Location = New-Object System.Drawing.Point(18, 16)
  $title.AutoSize = $true
  $form.Controls.Add($title)

  $threadLabel = New-Object System.Windows.Forms.Label
  $threadLabel.Text = 'Codex conversation'
  $threadLabel.Location = New-Object System.Drawing.Point(20, 58)
  $threadLabel.AutoSize = $true
  $form.Controls.Add($threadLabel)

  $threadBox = New-Object System.Windows.Forms.ComboBox
  $threadBox.Location = New-Object System.Drawing.Point(20, 80)
  $threadBox.Size = New-Object System.Drawing.Size(590, 30)
  $threadBox.DropDownStyle = 'DropDownList'
  foreach ($session in $sessions) {
    $titleText = if ($session.title) { [string]$session.title } else { 'Untitled Codex thread' }
    $projectText = if ($session.cwd) { Split-Path -Leaf ([string]$session.cwd) } else { 'unknown project' }
    [void]$threadBox.Items.Add("$titleText  —  $projectText")
  }
  $threadBox.SelectedIndex = 0
  $form.Controls.Add($threadBox)

  $modeLabel = New-Object System.Windows.Forms.Label
  $modeLabel.Text = 'Mode'
  $modeLabel.Location = New-Object System.Drawing.Point(20, 122)
  $modeLabel.AutoSize = $true
  $form.Controls.Add($modeLabel)

  $modeBox = New-Object System.Windows.Forms.ComboBox
  $modeBox.Location = New-Object System.Drawing.Point(20, 144)
  $modeBox.Size = New-Object System.Drawing.Size(150, 30)
  $modeBox.DropDownStyle = 'DropDownList'
  [void]$modeBox.Items.AddRange(@('plan', 'debug', 'review', 'general'))
  $modeBox.SelectedIndex = 0
  $form.Controls.Add($modeBox)

  $requestLabel = New-Object System.Windows.Forms.Label
  $requestLabel.Text = 'What should ChatGPT do?'
  $requestLabel.Location = New-Object System.Drawing.Point(20, 186)
  $requestLabel.AutoSize = $true
  $form.Controls.Add($requestLabel)

  $requestBox = New-Object System.Windows.Forms.TextBox
  $requestBox.Location = New-Object System.Drawing.Point(20, 208)
  $requestBox.Size = New-Object System.Drawing.Size(590, 95)
  $requestBox.Multiline = $true
  $requestBox.ScrollBars = 'Vertical'
  $requestBox.Text = 'Summarize the current Codex work and determine the best next implementation step.'
  $form.Controls.Add($requestBox)

  $hint = New-Object System.Windows.Forms.Label
  $hint.Text = 'Ctrl+Enter sends. Sidecar reads local session and Git files; it does not submit a Codex turn.'
  $hint.Location = New-Object System.Drawing.Point(20, 312)
  $hint.AutoSize = $true
  $form.Controls.Add($hint)

  $cancelButton = New-Object System.Windows.Forms.Button
  $cancelButton.Text = 'Cancel'
  $cancelButton.Location = New-Object System.Drawing.Point(430, 336)
  $cancelButton.Size = New-Object System.Drawing.Size(80, 30)
  $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
  $form.Controls.Add($cancelButton)

  $sendButton = New-Object System.Windows.Forms.Button
  $sendButton.Text = 'Open Quick Chat'
  $sendButton.Location = New-Object System.Drawing.Point(515, 336)
  $sendButton.Size = New-Object System.Drawing.Size(95, 30)
  $sendButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
  $form.Controls.Add($sendButton)
  $form.AcceptButton = $sendButton
  $form.CancelButton = $cancelButton

  $form.Add_Shown({ $requestBox.Focus(); $requestBox.SelectAll() })
  $form.Add_KeyDown({
    param($sender, $eventArgs)
    if ($eventArgs.Control -and $eventArgs.KeyCode -eq [System.Windows.Forms.Keys]::Enter) {
      $form.DialogResult = [System.Windows.Forms.DialogResult]::OK
      $form.Close()
    }
  })

  $dialogResult = $form.ShowDialog()
  if ($dialogResult -ne [System.Windows.Forms.DialogResult]::OK) { exit 0 }
  if ([string]::IsNullOrWhiteSpace($requestBox.Text)) { throw 'Enter a request for ChatGPT.' }

  $session = $sessions[$threadBox.SelectedIndex]
  $prepared = Invoke-NodeJson @(
    'prepare',
    [string]$session.sessionId,
    [string]$modeBox.SelectedItem,
    [string]$requestBox.Text
  )

  if (-not $prepared.copied) {
    $promptText = Get-Content -LiteralPath $prepared.handoffPath -Raw -Encoding UTF8
    [System.Windows.Forms.Clipboard]::SetText($promptText)
  }

  Write-SidecarLog "Prepared session $($prepared.selectedSession.sessionId) at $($prepared.handoffPath)."
  $windowHandle = Get-ChatGPTWindowHandle
  Open-QuickChatAndSubmit $windowHandle
  Write-SidecarLog 'Quick Chat opened and prompt submitted.'
} catch {
  Show-SidecarError $_.Exception.Message
  exit 1
}
