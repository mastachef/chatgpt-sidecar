CHATGPT SIDECAR DOCK - START HERE

1. Extract the entire ZIP to a normal folder such as Downloads\Sidecar.Dock.
2. Do not run the executable from inside the compressed ZIP preview.
3. Start ChatGPT.Sidecar.Dock.exe.
4. In Sidecar, hold the "Attach" handle, drag the cursor over the real Codex window, and release.
5. Sidecar will pin itself to that exact window and follow it. Use "Auto target" only to return to automatic detection.
6. If Sidecar appears to do nothing, run START_SIDECAR.cmd instead.

The launcher will show the process exit code and open this report when available:

%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log

Paste that report into the ChatGPT conversation. It contains startup/runtime details, not your Codex conversation or repository context.

The Microsoft Edge WebView2 Runtime is required for the embedded ChatGPT panel. A missing WebView2 Runtime should produce a visible error after the Sidecar window opens.