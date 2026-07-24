SIDECAR - START HERE

1. Put Sidecar.exe in a normal user-writable folder and run it.
2. Sign into ChatGPT inside Sidecar when requested.
3. Hold the "Attach" handle, drag the cursor over the real Codex window, and release.
4. Sidecar pins itself to that exact window. Use the Follow toggle to control whether it continues following the attached window.
5. Choose a theme, select the correct saved Codex thread, and enter a Plan, Debug, Review, or General request.
6. Use "Preview" to inspect the local context package, then select "Prepare in ChatGPT" and send it.

RETURN COMPLETED WORK TO CODEX

7. After the ChatGPT work is complete, select "Prepare handoff".
8. Send the populated handoff request in ChatGPT.
9. When ChatGPT finishes the handoff response, select "Copy latest reply".
10. Paste that detailed prompt into Codex so it can continue implementation without repeating the finished analysis.

UPDATES

Sidecar checks GitHub Releases when it starts. You can also use the "Updates" button in the header.
When a newer release is available, Sidecar downloads it, verifies GitHub's SHA-256 asset digest and a valid trusted Windows Authenticode signature, then replaces the executable and restarts. A failed integrity/signature check does not replace the running copy.

Sidecar populates prompts but does not auto-submit them.

STARTUP REPORT

%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\startup-crash.log

RUNTIME DIAGNOSTICS

%LOCALAPPDATA%\ChatGPTSidecar\Diagnostics\sidecar-dock.log

The Microsoft Edge WebView2 Runtime is required for the embedded ChatGPT panel. Sidecar.exe is self-contained for .NET and does not require the .NET SDK.
