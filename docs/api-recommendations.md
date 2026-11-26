# ProjExcel Excel API recommendations

## API maturity against SharpProj

ProjExcel exposes the main CRS and coordinate-transform workflows through Excel UDFs, but it is a task-oriented interface rather than a one-to-one wrapper for SharpProj. This assessment covers the compiled `PE.crs.*`, `PE.trf.*`, and `PE.lib.*` API and separately records archived CRS UDFs. It is not an exhaustive audit of every SharpProj member. Standalone `CoordinateArea` construction/mutation is SharpProj object behavior, not a required Excel UDF. The existing `PE.crs.Options()` token can promote or demote a CRS for inspection through `PE.crs.Properties` and `PE.crs.AxisProperties`. Transform option parsing is handled inside UDF error boundaries; `Apply*` and `Apply*Ex` report row/candidate/coordinate failures, and `Roundtrip` validates its point, count, and result and accepts a forward/reverse flag. Compiled CRS and transform UDFs take scalar CRS arguments. The shared CRS parser also accepts a 1×1 array and rejects multi-cell CRS ranges for `PE.crs.*` and CRS-pair `PE.trf.*` calls; the separate transform-description path (blank target CRS) does not accept an array. Numeric EPSG inputs in both creation paths are checked before integer conversion.

Explicit candidate selection, 2D/3D/4D input handling, and transform metadata access already exist; they are not outstanding priorities. When transform creation fails with `BestOnly=TRUE`, ProjExcel retains SharpProj's error rather than relabeling every failure as an invalid `BestOnly` option or retrying with `BestOnly=FALSE`. Preserve single-value CRS inputs and the existing transform-description path; do not add two-cell authority/code inputs. `docs/API_ProjExcel.xlsx` is the intended API test workbook for all API groups (deg/lst/trf/...). `ExcelTests` aims to mimic, as far as practical, tests in `D:\GitHub\SharpProj\src\SharpProj.Tests` and also contains many ProjExcel API checks: the current saved `BasicTests.xlsx` has approximately 14,327 formula anchors containing `PE.*`, including its large `WalkReferenceSystems` sheet. This counts formulas in the XLSX, not evaluated spill cells or passing checks. The other six workbooks besides `BasicTests.xlsx`, `AxisHeightsTests.xlsx`, and `DistanceTests.xlsx` still contain only an empty `Sheet1`. The earlier audit for legacy CRS UDF usage covered these nine workbooks, not workbooks outside `ExcelTests`.

## Unit test workbook coverage

`ExcelTests` has nine workbooks corresponding to the nine source files with `[TestMethod]` methods in `D:\GitHub\SharpProj\src\SharpProj.Tests`. `BasicTests.xlsx` and `AxisHeightsTests.xlsx` each have a populated sheet named for every source method (32 and 5, respectively). Together with the previously audited two populated sheets in `DistanceTests.xlsx`, that gives **39 populated test sheets for 67 source methods** and **28 methods without a populated corresponding sheet**. A populated sheet is not necessarily a completed test: coverage requires inputs and explicit checks equivalent to the source method's material assertions. The user reports that a fresh Excel recalculation of `BasicTests.xlsx` produced no failed tests at `Summary!F9`; this live result is user-reported, not independently reproduced from saved formula caches.

| Workbook | Populated sheets representing source methods | Source methods | Gaps |
| --- | ---: | ---: | --- |
| `BasicTests.xlsx` | 32 | 32 | Complete for the defined ProjExcel workbook scope, with user-reported passing recalculation and documented SharpProj parity exceptions. |
| `AxisHeightsTests.xlsx` | 5 | 5 | All method sheets present. Live recalculation has not been independently verified. |
| `DistanceTests.xlsx` | 2 | 3 | `DistanceDemoNormalized` lacks a sheet. |
| `InfoTests.xlsx` | 0 | 9 | Empty `Sheet1` only. |
| `InitTests.xlsx` | 0 | 1 | Empty `Sheet1` only. |
| `PPointTests.xlsx` | 0 | 6 | Empty `Sheet1` only. |
| `ProjectionTypesTests.xlsx` | 0 | 1 | Empty `Sheet1` only. |
| `SridTests.xlsx` | 0 | 4 | Empty `Sheet1` only. |
| `TransformDetailsTests.xlsx` | 0 | 6 | Empty `Sheet1` only. |

In `BasicTests.xlsx`, all 32 method sheets are populated and their names match the source methods exactly; no new or renamed method sheets are needed. The newer list and metadata UDFs support `GetGeoidModels`, `UnitTest`, `TestAuthorities`, and `TestCoordinateMetadata`. The workbook is complete for its defined task-oriented ProjExcel test scope, not an exception-free reproduction of every SharpProj assertion.

The saved BasicTests formulas now provide explicit checks in several previously incomplete sheets; cached values are not proof of a fresh recalculation:

- `WalkBodies!I9:I12` checks the first body, absence of any later Earth, and nonempty names and authorities; `E6` includes all four. `TestRemarks!G37:G45` checks remarks, method names, and identifiers on candidate steps, excluding the last candidate. `PromoteDemote` checks promoted/demoted CRS axis counts, axis names, and units through `PE.crs.Options()` and the existing property UDFs.
- `TestAustralia!G38` checks that the 2000 and 2020 transformed XY coordinates differ, and `G6` includes it. `FewEpsg!AO10:AO209` classifies all 200 EPSG codes; `G6` checks coverage and that at least one is deprecated. EPSG:2181–2187 return CRS-not-found errors and are expected creation-failure skips in the source test.
- `TestNewZealand_defmodel!D118:D138` checks annual XY changes and geodesic distance thresholds, with the 2017 threshold at row 135; `D139` requires all 21 rows to pass and `G6` includes `D139`. The source asserts years 2000–2019 (20 comparisons); the sheet also checks 2020. The user reports that `=IFERROR(AND(EXACT(O21,"defmodel"),EXACT(U21,"helmert")),FALSE)` is included in the sheet's pass/fail analysis. The WKT-derived availability problem remains separate, as documented below.
- `MissingProjections!G10:G34` checks the 25 explicitly listed EPSG/ESRI cases. `Q139=SUM(Q40:Q138)` counts **11** ESRI 53000–54999 failures, whereas `BasicTests.cs` expects **10**; the workbook summary documents this known discrepancy. `G6` checks `G35` (the explicit cases), not the disputed count. Do not edit the `P109:Q109` property results or force the count to 10; resolve the difference against the same PROJ database before claiming exact source-test parity.
- `WalkAmersfoort` now tests a 10,000 m-spaced grid of 986 unique input points transformed from EPSG:28992 to EPSG:3857; a numeric X/Y output check is included in the sheet result. This practical grid samples the source's nested loop rather than reproducing every point.
- `CoordinateAreaTests` checks EPSG:4326 usage-area bounds; standalone `CoordinateArea` constructors and setters are SharpProj-only, not missing Excel UDF coverage.
- `TestTime!G51` was corrected as a worksheet typo.
- `GnomoricBounds` includes diagnostics for extrema whose source assertions are commented out.

Other populated sheets also need targeted source-assertion mapping. Saved `FALSE` values on property UDFs can be expected data rather than failed assertions.

`AxisHeightsTests.xlsx` now has `HeightConversionNLBE`, `DistanceInNL`, `RecreateViaWKT`, `NL_Wgs84`, and `CreateFromWKT` sheets; `Summary!D13:D17` references their `G6` results and `D18` aggregates them. `NL_Wgs84!G6` includes its `G69:G70` WKT/PROJ round-trip checks. Its PROJ-string comparison is sensitive to whitespace; the source test compares multiline output, while the sheet uses a single-line rendering. `RecreateViaWKT` also contains a source-level `Assert.Inconclusive` when the WKT-reloaded PROJ string differs; distinguish that known limitation from a passing comparison. Saved TRUE caches alone cannot establish a fresh Excel recalculation. Both populated `DistanceTests.xlsx` sheets still need assertion-equivalence review.

### Separate PROJ WKT round-trip issue: New Zealand deformation model

In `BasicTests.xlsx` / `TestNewZealand_defmodel`, the WKT generated for the EPSG:2105 → EPSG:9000 transform parses successfully but cannot be instantiated when reused as a transform description: `PE.trf.Properties(wkt,,"IsAvailable",options)` returns `FALSE`, while the generated PROJ pipeline returns `TRUE`. The WKT-derived transform lists `nz_linz_nzgd2000-20180701.json` as an available grid. `PE.trf.ListTransformSteps` identifies step 1, **Inverse of ITRF96 to NZGD2000 (7)** (method **Inverse of New Zealand Deformation Model**), as `IsAvailable=FALSE` and `HasInverse=FALSE`; the other four steps are available. An older TopoLib WKT definition embeds an explicit `+proj=defmodel` pipeline and works. Treat this as a separate suspected PROJ operation/database or WKT round-trip issue, **not an established ProjExcel API defect**. It may have been corrected in a PROJ/database version later than 9.5.1, but that has not been verified. Reproduce the WKT parse and instantiability check with a later PROJ/database release before attributing the root cause or marking it fixed. Keep coordinate assertions using the working CRS pair or PROJ pipeline separate from the WKT round-trip regression.

For further SharpProj parity work beyond the completed BasicTests workbook scope, compare tests against the same PROJ database, document SharpProj-only behavior, and independently verify network/grid-dependent and WKT scenarios. Keep the separate `docs/API_ProjExcel.xlsx` API tests distinct from this source-test parity audit.

## Transform parameters

`PE.trf.Properties` and `PE.trf.StepProperties` accept `Parameters`, but format the value generically. `PE.trf.Options` and `PE.trf.ListTransformOperations` describe creation options and PROJ operation tokens, not transform parameters.

1. `PE.trf.ListTransformParameters(sourceCrs, targetCrs, transformOptions, [nullValues], [transpose])` lists parameters of the selected candidate and its steps. Field names occupy the first column and each parameter occupies a subsequent column, matching the other transform list UDFs. The Step Number field identifies the zero-based source step; it shows the null placeholder for parameters belonging to the top-level transform. Repeated names from different steps remain separate. Only a transform with no exposed parameters anywhere produces a field-name-only result.
2. `PE.trf.ListStepParameters(sourceCrs, targetCrs, stepNumber, transformOptions, [nullValues], [transpose])` uses the same orientation to list only one zero-based step's parameters, following the existing `PE.trf.StepProperties` step-index convention. For a transform without a step list, step 0 refers to the transform itself. Use this UDF for focused inspection of a multi-step operation, including CRS-pair transformations such as 4326 → 28992.

The list UDFs explicitly map the installed SharpProj `CoordinateTransformParameter` fields: Name, numeric Value, ValueString, UnitName, UnitCategory, UnitConversionFactor, and parameter/unit authority and code. Missing text and non-finite numeric values use the optional null placeholder. A parameter-free transform or step returns only the field-name column, not an error. These are SharpProj parameter records, not an enumeration of every executable PROJ pipeline token. Do not assume parameter metadata includes grid filenames, infer missing steps from empty parameters, rely on reflection or `ToString()` for parameter serialization, or edit `PE.trf.TransformList`.

Most `PE.trf.List*` UDFs end with an optional `transpose` flag (default `FALSE`); omit it to retain the usual table orientation. `PE.trf.ListTransformOptions()` is a fixed reference table without a `transpose` argument.

## Other transform properties

1. `PE.trf.ListUsageAreaProperties` returns a complete property/value table for the selected transform's usage area. The separate `PE.trf.UsageAreaProperties` selects named fields and returns one value or a single row for comma-separated names, like `PE.trf.Properties` and `PE.trf.StepProperties`. Both expose Name, WestLongitude, SouthLatitude, EastLongitude, NorthLatitude, CenterX, and CenterY with numeric coordinates and missing metadata represented as missing.
2. `PE.trf.ListIdentifiers(sourceCrs, targetCrs, transformOptions, [nullValues], [transpose])` lists identifiers of the selected transform candidate individually. It checks the candidate's `Identifiers` collection, then its singular `Identifier`, and falls back to identifiers on its pipeline steps if neither is present. `PE.trf.Properties(..., "Identifier", ...)` also supports step fallback, but uses a different lookup order and presentation. Each listed identifier occupies a subsequent column as a single `AUTHORITY:CODE` value mapped explicitly from Authority and Code; the `Step Number` row identifies fallback step identifiers and uses the null placeholder for top-level identifiers. A transform with no identifiers anywhere returns only the field-name column.

Do not add a separate UDF for every scalar property such as Name, Accuracy, or HasInverse: `PE.trf.Properties` already retrieves these by stable names. Do not duplicate the existing grid UDFs.

## Broader API alignment

- Single-value CRS inputs including `AUTHORITY:CODE` strings are supported across compiled `PE.crs.*` and CRS-pair `PE.trf.*` UDFs; their shared parser accepts 1×1 arrays and rejects multi-cell CRS ranges. A blank target CRS selects a separate transform-description parser that expects a scalar description.
- Order arguments CRS inputs → task inputs → transform options → optional presentation settings. Keep `PE.trf.Apply(source, target, x, y, options)`.
- Use explicit stable scalar and identifier mappings instead of reflection or object serialization; retain discoverable task-oriented UDFs rather than mirroring every CRS or SharpProj member.
- Preserve pre-execution candidate/grid availability diagnostics. Non-finite results in `ApplyCore` and `ApplyExCore` report the candidate, row, and affected coordinate but do not identify the underlying PROJ cause.
- Use consistent `#ERR` diagnostics and validate inputs; `ApplyEx` now rejects more than four coordinate columns instead of treating them as two.
- Prioritize input compatibility and unavailable-operation diagnostics.
- `PE.lst.CoordinateReferenceSystemList` supports optional Authority and CelestialBodyName filtering after its existing Header and AllowDeprecated arguments without changing the eight-column output. `PE.lst.GeoidModelList` exposes per-CRS Authority/Name records, and `PE.lst.UnitOfMeasurementList` exposes category-filtered unit metadata. `PE.lst.AuthorityList` uses SharpProj's `GetAuthorities()` in native order; the separate internal authority lookup used for CRS/transform parsing is unchanged.
- `PE.crs.CoordinateMetadataProperties` exposes metadata Type, CRS presence, and Epoch. `PE.crs.ListDistanceTransformBounds` samples a CRS's area and reports reverse distance-transform extrema; it is a task-oriented diagnostic, not a general-purpose `CoordinateArea` or `CoordinateMetadata` object API.

## Archived PE.crs.* functions (not compiled)

The 54 functions listed below, `PE.crs.UsageArea.Center`, and the three legacy serialization functions are preserved in `ProjExcel/Deprecated/deprecated.cs` but **excluded from `ProjExcel.csproj`**. They are not registered by the current add-in; workbooks outside the audited `ExcelTests` folder that still call them will need migration. Use the compiled `*Properties()` functions for the property values below. Match the old `Normalized` setting where applicable. Missing-value placeholders, error results, and the shape of some older returns may differ, so replacements are not necessarily drop-in equivalent for every input.

### Use `PE.crs.Properties(crs, properties, [normalized], [nullValues])`

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

`PE.crs.UsageArea.Center` can instead use `PE.crs.Properties(crs, "UsageArea.Center.X,UsageArea.Center.Y", normalized)` for the two adjacent values. This is a combined-value UDF rather than a single named property. `normalized` accepts a Boolean or a token from `PE.crs.Options()`; the token also supports promotion to 3D or demotion to 2D before inspection.

### Use `PE.crs.AxisProperties(crs, axisIndex, properties, [coordinateSystemAxis], [normalized], [nullValues])`

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

### Use `PE.crs.IdentifierProperties(crs, identifierIndex, properties, [nullValues])`

Identifier properties read the original (unnormalized) CRS. The archived identifier UDFs defaulted to `Normalized=FALSE`; they could return different results when explicitly called with `Normalized=TRUE`.

- `PE.crs.Identifiers.Authority`
- `PE.crs.Identifiers.Code`

### Use `PE.crs.ToString(crs, stringFormat, [normalized])`

- `PE.crs.AsJsonString` → `stringFormat="Json"`
- `PE.crs.AsProjString` → `stringFormat="Proj"`
- `PE.crs.AsWktString` → `stringFormat="Wkt"`

## Compiled PE.crs.* functions

- `PE.crs.AxisProperties`
- `PE.crs.CoordinateMetadataProperties`
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
- `PE.crs.ListDistanceTransformBounds`
- `PE.crs.ListIdentifierProperties`
- `PE.crs.ListOptions`
- `PE.crs.Options`
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
- `PE.trf.ListTransformOptions`
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
- `PE.trf.ToStringReversed`
- `PE.trf.UsageAreaProperties`

## Compiled PE.lst.* functions

- `PE.lst.AuthorityList`
- `PE.lst.CelestialBodyList`
- `PE.lst.CoordinateReferenceSystemList`
- `PE.lst.GeoidModelList`
- `PE.lst.OperationList`
- `PE.lst.UnitOfMeasurementList`

## Compiled PE.lib.* functions

- `PE.lib.ListLibProperties` lists the stable library property names and indices.
- `PE.lib.Options` accepts one optional Boolean argument per named property (for example, `AddIn.AssemblyVersion` and `Excel.OperatingSystem`); with no inputs, it returns all property tokens as a comma-separated selection.
- `PE.lib.Properties` reads one named/indexed property or a comma-separated selection in adjacent cells. `Excel.OperatingSystem` remains available here; the `OperatingSystem()` source helper is internal and is not a standalone Excel UDF.
- `PE.lib.VersionInfo` retains its existing mode-based version lookup.

`PE.lib.AddinVersion` and `PE.lib.ListAddinVersionInfo` have been removed from the source; use the curated library property API for those version values.
