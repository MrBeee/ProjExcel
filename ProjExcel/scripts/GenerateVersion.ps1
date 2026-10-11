param(
    [string]$OutputFile
)

$now = Get-Date
$version = "1.0.{0}.{1}" -f $now.ToString("yyMMdd"), $now.ToString("HHmm")

$content = @"
using System.Reflection;

[assembly: AssemblyFileVersion("$version")]
[assembly: AssemblyInformationalVersion("$version")]
"@

$dir = Split-Path $OutputFile -Parent
if (!(Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir | Out-Null
}

Set-Content -Path $OutputFile -Value $content -Encoding UTF8