$paths = @(
    'D:\Source\VS22\ProjExcel\ProjExcel\Crs.cs',
    'D:\Source\VS22\ProjExcel\ProjExcel\TopoLib\Crs.cs'
)

$pattern = '\[ExcelFunctionDoc\((?<body>[\s\S]*?)\)\]'
$allowed = '^(Name|Category|Description|HelpTopic|IsVolatile|IsHidden|IsThreadSafe|IsExceptionSafe|IsMacroType|ExplicitRegistration|SuppressOverwriteError)\s*='

foreach ($path in $paths) {
    if (-not (Test-Path $path)) {
        continue
    }

    $content = Get-Content -Raw -Path $path
    if ([string]::IsNullOrEmpty($content)) {
        $sourcePath = 'D:\Source\VS22\ProjExcel\ProjExcel\Crs.cs'
        if ($path -ne $sourcePath -and (Test-Path $sourcePath)) {
            $content = Get-Content -Raw -Path $sourcePath
        }
    }

    $content = $content -replace 'using ExcelDna\.Documentation;\r?\n', ''
    $content = [regex]::Replace($content, $pattern, {
        param($match)

        $body = $match.Groups['body'].Value
        $lines = $body -split "`r?`n"
        $keep = New-Object System.Collections.Generic.List[string]

        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            if ($trimmed -match $allowed) {
                $keep.Add($line.TrimEnd())
            }
        }

        if ($keep.Count -eq 0) {
            return '[ExcelFunction]'
        }

        return "[ExcelFunction(`r`n" + ($keep -join "`r`n") + "`r`n            )]"
    })

    $tmpPath = "$path.tmp"
    Set-Content -Path $tmpPath -Value $content -Encoding UTF8
    Move-Item -Path $tmpPath -Destination $path -Force
}
