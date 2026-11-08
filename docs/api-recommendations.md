# ProjExcel Excel API recommendations

## Transform parameters

`PE.trf.Properties` and `PE.trf.StepProperties` already accept `Parameters`, but format the value generically; no structured parameter-listing UDF exists. `PE.trf.Options` and `PE.trf.ListTransformOperations` describe creation options and PROJ operation tokens, not transform parameters.

1. Add `PE.trf.ListTransformParameters(sourceCrs, targetCrs, transformOptions, ...)` to list the selected candidate's parameters in an Excel table with stable explicit columns and one row per parameter. Document how concatenated operations are represented rather than silently flattening steps.
2. Add `PE.trf.ListStepParameters(sourceCrs, targetCrs, stepNumber, transformOptions, ...)` to list one step's parameters, using the existing `PE.trf.StepProperties` index convention. This helps diagnose multi-step WKT operations.
3. Consider `PE.trf.ParameterProperties` for indexed single-parameter lookup only if a real use case emerges; settle the table shape first. Return explicit fields, not SharpProj objects.

Before implementation, verify the element type and supported fields of `CoordinateTransform.Parameters` in the installed SharpProj version. Explicitly map available names, values, units, and identifiers where supported; do not assume grid filename fields exist. Handle absent parameters and unavailable operations clearly. Do not rely on reflection or `ToString()` for Excel-facing parameter serialization, and do not edit `PE.trf.TransformList`.

## Other transform properties

1. Add `PE.trf.UsageAreaProperties` for the selected transform's usage area. Expose Name, WestLongitude, SouthLatitude, EastLongitude, NorthLatitude, CenterX, and CenterY as explicitly mapped, Excel-friendly fields; return bounds and center coordinates as numbers, not a string requiring parsing. Report missing area metadata as missing rather than deriving it from the CRS or input coordinates. `PE.trf.Properties` already formats UsageArea as a single string, and transform and step listings show its individual fields, but neither provides direct structured access to the selected transform's numeric area fields.
2. Consider `PE.trf.ListIdentifiers` if users need to inspect multiple identifiers individually. Return each identifier in an Excel-friendly table, explicitly mapping Authority and Code (or formatting them as `AUTHORITY:CODE` in a single cell), instead of joining all identifiers into one property value.

Do not add a separate UDF for every scalar property such as Name, Accuracy, or HasInverse: `PE.trf.Properties` already retrieves these by stable names. Do not duplicate the existing grid UDFs.

## Broader API alignment

- Prefer single-cell CRS inputs including `AUTHORITY:CODE` strings; two-cell authority/code inputs are obsolete. Preserve existing scalar inputs when aligning `PE.crs.*` and `PE.trf.*`.
- Order arguments CRS inputs → task inputs → transform options → optional presentation settings. Keep `PE.trf.Apply(source, target, x, y, options)`.
- Use explicit stable scalar and identifier mappings instead of reflection or object serialization; retain discoverable task-oriented UDFs rather than mirroring every CRS or SharpProj member.
- Improve diagnostics for unavailable operations and steps; the non-finite result checks in `ApplyCore` and `ApplyExCore` are safeguards, not explanations.
- Use consistent `#ERR` diagnostics and validate inputs, including rejecting more than four `ApplyEx` columns instead of treating them as two.
- Prioritize input compatibility and unavailable-operation diagnostics.
