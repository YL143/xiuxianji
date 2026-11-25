# 把 src 下的 C# 源码合并后编译进当前 PowerShell 进程（无需任何 exe 文件）
function Build-GameTypes {
    param([string]$SrcDir)

    if (-not $SrcDir) { $SrcDir = Join-Path $PSScriptRoot "src" }

    $header = @"
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
"@

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine($header)

    foreach ($f in (Get-ChildItem -Path $SrcDir -Filter "*.cs" | Sort-Object Name)) {
        $text = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
        $lines = $text -split "`r?`n"
        foreach ($ln in $lines) {
            if ($ln -match '^\s*using\s+[A-Za-z]') { continue }
            [void]$sb.AppendLine($ln)
        }
    }

    $code = $sb.ToString()

    if (-not ("Xiuxian.GameCanvas" -as [type])) {
        Add-Type -TypeDefinition $code -ReferencedAssemblies @("System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll") -Language CSharp -ErrorAction Stop
    }
}
