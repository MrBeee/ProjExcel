$sourcePath = 'D:\Source\VS22\ProjExcel\ProjExcel\TopoLib\Crs.cs'
$targetPath = 'D:\Source\VS22\ProjExcel\ProjExcel\Crs.cs'

$methodNames = @(
    'Datum_Name',
    'Datum_Type',
    'Ellipsoid_InverseFlattening',
    'Ellipsoid_IsSemiMinorComputed',
    'Ellipsoid_Name',
    'Ellipsoid_SemiMajorMetre',
    'Ellipsoid_SemiMinorMetre',
    'Ellipsoid_Type',
    'EuclideanArea',
    'EuclideanDistance',
    'EuclideanDistanceZ',
    'GeodesicArea',
    'GeodesicDistance',
    'GeodesicDistanceZ',
    'GeodeticCRS_Name',
    'GeodeticCRS_Type',
    'Identifiers_Authority',
    'Identifiers_Code',
    'Identifiers_Count',
    'IsDeprecated',
    'IsEquivalentTo',
    'IsEquivalentToRelaxed',
    'Name',
    'PrimeMeridian_Longitude',
    'PrimeMeridian_Name',
    'PrimeMeridian_UnitConversionFactor',
    'PrimeMeridian_UnitName',
    'Remarks',
    'Scope',
    'Type'
)

$source = Get-Content -Raw -Path $sourcePath
$target = Get-Content -Raw -Path $targetPath
$parts = [regex]::Split($source, '(?=^\s*\[ExcelFunctionDoc\()', [System.Text.RegularExpressions.RegexOptions]::Multiline)
$inserted = New-Object System.Collections.Generic.List[string]

foreach ($methodName in $methodNames) {
    $targetMethodPattern = 'public\s+static\s+.*?\s+' + [regex]::Escape($methodName) + '\s*\('
    if ([regex]::IsMatch($target, $targetMethodPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        continue
    }

    $chunk = $parts | Where-Object { $_ -match ('public\s+static\s+.*?\s+' + [regex]::Escape($methodName) + '\s*\(') } | Select-Object -First 1
    if (-not $chunk) {
        throw "Method not found in source: $methodName"
    }

    $chunk = [regex]::Replace($chunk, '\[ExcelFunctionDoc\((?<body>[\s\S]*?)\)\]', {
        param($m)

        $body = $m.Groups['body'].Value
        $nameMatch = [regex]::Match($body, 'Name\s*=\s*"([^"]+)"')
        $descriptionMatch = [regex]::Match($body, 'Description\s*=\s*"([^"]+)"')

        if (-not $nameMatch.Success -or -not $descriptionMatch.Success) {
            throw "Unable to convert attribute for $methodName"
        }

        $excelName = $nameMatch.Groups[1].Value -replace '^TL\.', 'PE.'
        $description = $descriptionMatch.Groups[1].Value

        return "[ExcelFunction(`r`n            Name = `"$excelName`",`r`n            Description = `"$description`"`r`n            )]"
    }, [System.Text.RegularExpressions.RegexOptions]::Singleline)

    $chunk = $chunk.Trim()
    $inserted.Add($chunk)
}

if ($inserted.Count -eq 0) {
    Write-Output 'No missing methods to restore.'
    exit 0
}

$addition = "`r`n`r`n        " + ($inserted -join "`r`n`r`n        ") + "`r`n"
$target = [regex]::Replace($target, '\r?\n\s*\}\s*// class\s*\r?\n\s*\}\s*// namespace\s*$', "$addition`r`n    } // class`r`n`r`n} // namespace`r`n")
[System.IO.File]::WriteAllText($targetPath, $target)
Write-Output "Restored $($inserted.Count) methods to Crs.cs"
