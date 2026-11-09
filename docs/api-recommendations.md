# ProjExcel Excel API recommendations

## API maturity against SharpProj

ProjExcel exposes the main CRS and coordinate-transform workflows through Excel UDFs, but it is a task-oriented interface rather than a one-to-one wrapper for SharpProj. This assessment covers the supported `PE.crs.*` and `PE.trf.*` API only; obsolete `PE.xfm.*` functions are not part of the API and are excluded. It is not an exhaustive audit of every SharpProj member. The priority table lists only gaps requiring attention; implemented behavior and design constraints are not action items.

| Priority | Area | Gap requiring attention | Recommended action |
| --- | --- | --- | --- |
| P1 | Unavailable operations and error reporting | `PE.trf` non-finite result checks cannot explain why a pipeline or grid is unavailable; supported UDFs do not handle errors consistently. | Report the cause of unavailable operations where possible and align error handling across `PE.crs.*` and `PE.trf.*`. |
| P2 | CRS input consistency | `PE.crs.*` accepts one- or two-cell CRS ranges, while `PE.trf.*` uses single-value CRS inputs. | Align supported CRS inputs around compatible single-cell values without breaking existing scalar inputs. |

Explicit candidate selection, 2D/3D/4D input handling, transform metadata access, and an error when `BestOnly` cannot be honored already exist; they are not outstanding priorities. The actions above focus on diagnosing failed operations and aligning CRS inputs rather than mirroring every SharpProj member. Keep existing UDF signatures compatible when doing so.

## Transform parameters

`PE.trf.Properties` and `PE.trf.StepProperties` accept `Parameters`, but format the value generically. `PE.trf.Options` and `PE.trf.ListTransformOperations` describe creation options and PROJ operation tokens, not transform parameters.

1. `PE.trf.ListTransformParameters(sourceCrs, targetCrs, transformOptions, [nullValues], [transpose])` lists parameters of the selected candidate and its steps. Field names occupy the first column and each parameter occupies a subsequent column, matching the other transform list UDFs. The Step Number field identifies the zero-based source step; it shows the null placeholder for parameters belonging to the top-level transform. Repeated names from different steps remain separate. Only a transform with no exposed parameters anywhere produces a field-name-only result.
2. `PE.trf.ListStepParameters(sourceCrs, targetCrs, stepNumber, transformOptions, [nullValues], [transpose])` uses the same orientation to list only one zero-based step's parameters, following the existing `PE.trf.StepProperties` step-index convention. For a transform without a step list, step 0 refers to the transform itself. Use this UDF for focused inspection of a multi-step operation, including CRS-pair transformations such as 4326 → 28992.
3. Consider `PE.trf.ParameterProperties` for indexed single-parameter lookup only if a real use case emerges; settle the table shape first. Return explicit fields, not SharpProj objects.

The list UDFs explicitly map the installed SharpProj `CoordinateTransformParameter` fields: Name, numeric Value, ValueString, UnitName, UnitCategory, UnitConversionFactor, and parameter/unit authority and code. Missing text and non-finite numeric values use the optional null placeholder. A parameter-free transform or step returns only the field-name column, not an error. These are SharpProj parameter records, not an enumeration of every executable PROJ pipeline token. Do not assume parameter metadata includes grid filenames, infer missing steps from empty parameters, rely on reflection or `ToString()` for parameter serialization, or edit `PE.trf.TransformList`.

Every `PE.trf.List*` UDF ends with an optional `transpose` flag (default `FALSE`); omit it to retain the usual table orientation.

## Other transform properties

1. `PE.trf.ListUsageAreaProperties` returns a complete property/value table for the selected transform's usage area. The separate `PE.trf.UsageAreaProperties` selects named fields and returns one value or a single row for comma-separated names, like `PE.trf.Properties` and `PE.trf.StepProperties`. Both expose Name, WestLongitude, SouthLatitude, EastLongitude, NorthLatitude, CenterX, and CenterY with numeric coordinates and missing metadata represented as missing.
2. `PE.trf.ListIdentifiers(sourceCrs, targetCrs, transformOptions, [nullValues], [transpose])` lists identifiers of the selected transform candidate individually. When the transform has no identifiers of its own, it falls back to identifiers on its pipeline steps, as `PE.trf.Properties(..., "Identifier", ...)` does. Each identifier occupies a subsequent column as a single `AUTHORITY:CODE` value mapped explicitly from Authority and Code; the `Step Number` row identifies fallback step identifiers and uses the null placeholder for top-level identifiers. A transform with no identifiers anywhere returns only the field-name column.

Do not add a separate UDF for every scalar property such as Name, Accuracy, or HasInverse: `PE.trf.Properties` already retrieves these by stable names. Do not duplicate the existing grid UDFs.

## Broader API alignment

- Prefer single-cell CRS inputs including `AUTHORITY:CODE` strings; two-cell authority/code inputs are obsolete. Preserve existing scalar inputs when aligning `PE.crs.*` and `PE.trf.*`.
- Order arguments CRS inputs → task inputs → transform options → optional presentation settings. Keep `PE.trf.Apply(source, target, x, y, options)`.
- Use explicit stable scalar and identifier mappings instead of reflection or object serialization; retain discoverable task-oriented UDFs rather than mirroring every CRS or SharpProj member.
- Improve diagnostics for unavailable operations and steps; the non-finite result checks in `ApplyCore` and `ApplyExCore` are safeguards, not explanations.
- Use consistent `#ERR` diagnostics and validate inputs; `ApplyEx` now rejects more than four coordinate columns instead of treating them as two.
- Prioritize input compatibility and unavailable-operation diagnostics.
