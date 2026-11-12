# ProjExcel Excel API recommendations

## API maturity against SharpProj

ProjExcel exposes the main CRS and coordinate-transform workflows through Excel UDFs, but it is a task-oriented interface rather than a one-to-one wrapper for SharpProj. This assessment covers the compiled `PE.crs.*` and `PE.trf.*` API and separately records archived CRS UDFs; obsolete `PE.xfm.*` functions are not part of the API and are excluded. It is not an exhaustive audit of every SharpProj member. The priority table lists only gaps requiring attention; implemented behavior and design constraints are not action items.

| Priority | Area | Gap requiring attention | Recommended action |
| --- | --- | --- | --- |
| P1 | Unavailable operations and error reporting | Execution UDFs already check candidate and grid availability before applying a transform, but non-finite output checks remain generic and supported UDFs do not handle errors consistently. | Improve diagnostics for failures not explained by availability checks and align error handling across `PE.crs.*` and `PE.trf.*`. |
| P2 | CRS input consistency | `PE.crs.*` now requires a 1×1 CRS range; `PE.trf.*` takes a scalar CRS value. Both support single-value CRS descriptions, but their Excel input shapes still differ. | Align input shapes where practical without reintroducing two-cell authority/code inputs or breaking supported scalar values. |

Explicit candidate selection, 2D/3D/4D input handling, transform metadata access, and an error when `BestOnly` cannot be honored already exist; they are not outstanding priorities. The actions above focus on diagnosing failed operations and aligning CRS inputs rather than mirroring every SharpProj member. The nine workbooks in `ExcelTests` were audited before the legacy CRS UDFs were excluded; workbooks outside that folder were not covered.

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

- Single-cell CRS inputs including `AUTHORITY:CODE` strings are supported; `PE.crs.*` rejects multi-cell CRS ranges. Preserve existing scalar inputs when aligning `PE.crs.*` and `PE.trf.*`.
- Order arguments CRS inputs → task inputs → transform options → optional presentation settings. Keep `PE.trf.Apply(source, target, x, y, options)`.
- Use explicit stable scalar and identifier mappings instead of reflection or object serialization; retain discoverable task-oriented UDFs rather than mirroring every CRS or SharpProj member.
- Preserve pre-execution candidate/grid availability diagnostics and improve remaining failure diagnostics; the non-finite result checks in `ApplyCore` and `ApplyExCore` are safeguards, not explanations.
- Use consistent `#ERR` diagnostics and validate inputs; `ApplyEx` now rejects more than four coordinate columns instead of treating them as two.
- Prioritize input compatibility and unavailable-operation diagnostics.

## Archived PE.crs.* functions (not compiled)

The 54 functions listed below, `PE.crs.UsageArea.Center`, and the three legacy serialization functions are preserved in `ProjExcel/Deprecated/deprecated.cs` but **excluded from `ProjExcel.csproj`**. They are not registered by the current add-in; workbooks outside the audited `ExcelTests` folder that still call them will need migration. Use the compiled `*Properties()` functions for the property values below. Match the old `Normalized` setting where applicable. Missing-value placeholders, error results, and the shape of some older returns may differ, so replacements are not necessarily drop-in equivalent for every input.

### Use `PE.crs.Properties(crs, property, normalized)`

- `PE.crs.Axis.Count`
- `PE.crs.CelestialBodyName`
- `PE.crs.CoordinateSystem.Axis.Count`
- `PE.crs.CoordinateSystem.CoordinateSystemType`
- `PE.crs.CoordinateSystem.Name`
- `PE.crs.CoordinateSystem.Type`
- `PE.crs.Datum.Name`
- `PE.crs.Datum.Type`
- `PE.crs.Ellipsoid.InverseFlattening`
- `PE.crs.Ellipsoid.IsSemiMinorComputed`
- `PE.crs.Ellipsoid.Name`
- `PE.crs.Ellipsoid.SemiMajorMetre`
- `PE.crs.Ellipsoid.SemiMinorMetre`
- `PE.crs.Ellipsoid.Type`
- `PE.crs.GeodeticCRS.Name`
- `PE.crs.GeodeticCRS.Type`
- `PE.crs.Identifiers.Count`
- `PE.crs.IsDeprecated`
- `PE.crs.Name`
- `PE.crs.PrimeMeridian.Longitude`
- `PE.crs.PrimeMeridian.Name`
- `PE.crs.PrimeMeridian.UnitConversionFactor`
- `PE.crs.PrimeMeridian.UnitName`
- `PE.crs.Remarks`
- `PE.crs.Scope`
- `PE.crs.Type`
- `PE.crs.UsageArea.Center.HasValues`
- `PE.crs.UsageArea.Center.X`
- `PE.crs.UsageArea.Center.Y`
- `PE.crs.UsageArea.EastLongitude`
- `PE.crs.UsageArea.MaxX`
- `PE.crs.UsageArea.MaxY`
- `PE.crs.UsageArea.MinX`
- `PE.crs.UsageArea.MinY`
- `PE.crs.UsageArea.Name`
- `PE.crs.UsageArea.NorthLatitude`
- `PE.crs.UsageArea.SouthLatitude`
- `PE.crs.UsageArea.WestLongitude`

`PE.crs.UsageArea.Center` can instead use `PE.crs.Properties(crs, "UsageArea.Center.X,UsageArea.Center.Y", normalized)` for the two adjacent values. This is a combined-value UDF rather than a single named property.

### Use `PE.crs.AxisProperties(crs, axisIndex, property, coordinateSystemAxis, normalized)`

For `PE.crs.Axis.*`, set `coordinateSystemAxis` to `FALSE` (default):

- `PE.crs.Axis.Abbreviation`
- `PE.crs.Axis.Direction`
- `PE.crs.Axis.Name`
- `PE.crs.Axis.UnitAuthName`
- `PE.crs.Axis.UnitCode`
- `PE.crs.Axis.UnitConversionFactor`
- `PE.crs.Axis.UnitName`

For `PE.crs.CoordinateSystem.Axis.*`, set `coordinateSystemAxis` to `TRUE`:

- `PE.crs.CoordinateSystem.Axis.Abbreviation`
- `PE.crs.CoordinateSystem.Axis.Direction`
- `PE.crs.CoordinateSystem.Axis.Name`
- `PE.crs.CoordinateSystem.Axis.UnitAuthName`
- `PE.crs.CoordinateSystem.Axis.UnitCode`
- `PE.crs.CoordinateSystem.Axis.UnitConversionFactor`
- `PE.crs.CoordinateSystem.Axis.UnitName`

### Use `PE.crs.IdentifierProperties(crs, identifierIndex, property)`

Identifier properties read the original (unnormalized) CRS. The archived identifier UDFs defaulted to `Normalized=FALSE`; they could return different results when explicitly called with `Normalized=TRUE`.

- `PE.crs.Identifiers.Authority`
- `PE.crs.Identifiers.Code`

### Use `PE.crs.ToString(crs, stringFormat, normalized)`

- `PE.crs.AsJsonString` → `stringFormat="Json"`
- `PE.crs.AsProjString` → `stringFormat="Proj"`
- `PE.crs.AsWktString` → `stringFormat="Wkt"`

## Compiled PE.crs.* functions

- `PE.crs.AxisProperties`
- `PE.crs.EuclideanArea`
- `PE.crs.EuclideanDistance`
- `PE.crs.EuclideanDistanceZ`
- `PE.crs.GeodesicArea`
- `PE.crs.GeodesicDistance`
- `PE.crs.GeodesicDistanceZ`
- `PE.crs.IdentifierProperties`
- `PE.crs.IsEquivalentTo`
- `PE.crs.IsEquivalentToRelaxed`
- `PE.crs.ListAxisProperties`
- `PE.crs.ListCrsProperties`
- `PE.crs.ListIdentifierProperties`
- `PE.crs.Properties`
- `PE.crs.ToString`

## Compiled PE.trf.* functions

- `PE.trf.Apply`
- `PE.trf.ApplyEx`
- `PE.trf.ApplyReversed`
- `PE.trf.ApplyReversedEx`
- `PE.trf.GridProperties`
- `PE.trf.ListContextSettings`
- `PE.trf.ListIdentifiers`
- `PE.trf.ListStepParameters`
- `PE.trf.ListTransformGrids`
- `PE.trf.ListTransformOperations`
- `PE.trf.ListTransformParameters`
- `PE.trf.ListTransformProperties`
- `PE.trf.ListTransformSteps`
- `PE.trf.ListTransforms`
- `PE.trf.ListUsageAreaProperties`
- `PE.trf.Options`
- `PE.trf.OptionsEx`
- `PE.trf.Properties`
- `PE.trf.Roundtrip`
- `PE.trf.StepCount`
- `PE.trf.StepProperties`
- `PE.trf.ToString`
- `PE.trf.UsageAreaProperties`
