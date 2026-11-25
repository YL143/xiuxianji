# 修仙纪 · 启动脚本（在 Windows PowerShell 中以隐藏控制台方式运行同款桌面窗口）
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$logPath = Join-Path $root "运行日志.log"

try {
    . (Join-Path $root "lib.ps1")
    Build-GameTypes -SrcDir (Join-Path $root "src")

    [Xiuxian.MainForm]::BaseDir = $root

    Add-Type -MemberDefinition @'
[DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow();
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
'@ -Name Win -Namespace Native -PassThru | Out-Null

    # 收回控制台窗口，只留游戏窗口
    $cw = [Native.Win]::GetConsoleWindow()
    if ($cw -ne [IntPtr]::Zero) { [void][Native.Win]::ShowWindow($cw, 0) }

    # 让窗口在高 DPI 屏上保持清晰
    Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name Dpi -Namespace Native -PassThru | Out-Null
    [void][Native.Dpi]::SetProcessDPIAware()

    [System.Windows.Forms.Application]::EnableVisualStyles()
    [System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)

    $form = New-Object Xiuxian.MainForm
    $form.add_Shown({
        [void][Native.Win]::ShowWindow($form.Handle, 5)
        [void][Native.Win]::SetForegroundWindow($form.Handle)
        $form.Activate()
    })

    [System.Windows.Forms.Application]::Run($form)
}
catch {
    $msg = $_.Exception.ToString()
    [System.IO.File]::WriteAllText($logPath, $msg, (New-Object System.Text.UTF8Encoding($false)))
    exit 1
}
