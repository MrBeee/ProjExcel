# ProjExcel Excel API recommendations

## API maturity against SharpProj

ProjExcel exposes the main CRS and coordinate-transform workflows through Excel UDFs, but it is a task-oriented interface rather than a one-to-one wrapper for SharpProj. This assessment covers the compiled `PE.crs.*` and `PE.trf.*` API and separately records archived CRS UDFs. It is not an exhaustive audit of every SharpProj member. Some SharpProj-only operations remain without exact Excel equivalents, notably standalone `CoordinateArea` construction/mutation and direct inspection of a CRS after `PromotedTo3D()` or `DemotedTo2D()`. Transform option parsing is handled inside UDF error boundaries; `Apply*` and `Apply*Ex` report row/candidate/coordinate failures, and `Roundtrip` validates its point, count, and result and accepts a forward/reverse flag. Compiled CRS and transform UDFs take scalar CRS arguments. The shared CRS parser also accepts a 1×1 array and rejects multi-cell CRS ranges for `PE.crs.*` and CRS-pair `PE.trf.*` calls; the separate transform-description path (blank target CRS) does not accept an array. Numeric EPSG inputs in both creation paths are checked before integer conversion.

Explicit candidate selection, 2D/3D/4D input handling, and transform metadata access already exist; they are not outstanding priorities. When transform creation fails with `BestOnly=TRUE`, ProjExcel retains SharpProj's error rather than relabeling every failure as an invalid `BestOnly` option or retrying with `BestOnly=FALSE`. Preserve single-value CRS inputs and the existing transform-description path; do not add two-cell authority/code inputs. `docs/API_ProjExcel.xlsx` is the intended API test workbook for all API groups (deg/lst/trf/...). `ExcelTests` aims to mimic, as far as practical, tests in `D:\GitHub\SharpProj\src\SharpProj.Tests` and also contains many ProjExcel API checks: the current saved `BasicTests.xlsx` has approximately 14,327 formula anchors containing `PE.*`, including its large `WalkReferenceSystems` sheet. This counts formulas in the XLSX, not evaluated spill cells or passing checks. The other six workbooks besides `BasicTests.xlsx`, `AxisHeightsTests.xlsx`, and `DistanceTests.xlsx` still contain only an empty `Sheet1`. The earlier audit for legacy CRS UDF usage covered these nine workbooks, not workbooks outside `ExcelTests`.

## Unit test workbook coverage

`ExcelTests` has nine workbooks corresponding to the nine source files with `[TestMethod]` methods in `D:\GitHub\SharpProj\src\SharpProj.Tests`. The latest saved `BasicTests.xlsx` contains a populated sheet named for **every one of its 32 source methods**. Combined with the previously audited four populated sheets in `AxisHeightsTests.xlsx` and `DistanceTests.xlsx`, that gives **36 populated test sheets for 67 source methods** and **31 methods without a populated corresponding sheet**. A populated sheet is not necessarily a completed test: coverage requires inputs and explicit checks equivalent to the source method's material assertions. Neither the saved formula values nor this inventory establish that any workbook passes after live Excel recalculation.

| Workbook | Populated sheets representing source methods | Source methods | Gaps |
| --- | ---: | ---: | --- |
| `BasicTests.xlsx` | 32 | 32 | All method sheets are populated; assertion parity remains incomplete. |
| `AxisHeightsTests.xlsx` | 2 | 5 | Three methods lack sheets. |
| `DistanceTests.xlsx` | 2 | 3 | `DistanceDemoNormalized` lacks a sheet. |
| `InfoTests.xlsx` | 0 | 9 | Empty `Sheet1` only. |
| `InitTests.xlsx` | 0 | 1 | Empty `Sheet1` only. |
| `PPointTests.xlsx` | 0 | 6 | Empty `Sheet1` only. |
| `ProjectionTypesTests.xlsx` | 0 | 1 | Empty `Sheet1` only. |
| `SridTests.xlsx` | 0 | 4 | Empty `Sheet1` only. |
| `TransformDetailsTests.xlsx` | 0 | 6 | Empty `Sheet1` only. |

In `BasicTests.xlsx`, all 32 method sheets are now populated and their names match the source methods exactly; no new or renamed method sheets are needed. The newer list and metadata UDFs support `GetGeoidModels`, `UnitTest`, `TestAuthorities`, and `TestCoordinateMetadata`; sheet presence and a TRUE summary do not by themselves establish full test equivalence.

The saved BasicTests formulas reveal the following remaining gaps; the cached values are not proof of a fresh recalculation:

- `MissingProjections!P109:Q109` now compares **11** with **11**, whereas `BasicTests.cs` still asserts **10** for the ESRI 53000–54999 count. The sheet also lacks checks for the subsequent explicit EPSG and ESRI cases that the source expects to fail (or marks inconclusive).
- `WalkBodies!E6` only references the first body's `IsEarth` value. Add checks that no later body is Earth and that every body's Name and Authority is nonempty. `TestRemarks!J14:T34` lists candidate-level fields; the source asserts on the *steps* of each candidate except the last (at least one nonempty Remarks and all nonempty MethodName/Identifier.Code). Use `PE.trf.ListTransformSteps` for those checks.
- `PromoteDemote!C36:C37` explicitly notes that promoted axis count and Z-axis details are not tested: transform `PromoteSourceTo3D`/`DemoteSourceTo2D` options cannot expose a modified CRS to `PE.crs.Properties` or `AxisProperties`. `CoordinateAreaTests!E10:E13` checks EPSG:4326 usage-area bounds rather than standalone `CoordinateArea` constructor/setter semantics; document these as partial/SharpProj-only or add focused APIs if needed.
- `TestTime` has saved coordinate differences (for example `G51=-5`) but its `H51` check compares `C51` to zero instead of checking that difference; compare every asserted epoch-dependent coordinate and yearly ordering explicitly. `TestAustralia!A35:D38` displays the 2000/2020 outputs and difference without a pass/fail for the source's `Assert.AreNotEqual`. `WalkAmersfoort` shows area bounds and a grid-size estimate but does not execute the source's nested transform loop.
- `FewEpsg` has cached `#VALUE!` cells outside its passing `G6` summary; classify expected unsupported/invalid cases and check all material failures rather than relying on that summary. `TestNewZealand_defmodel` includes a cached `#NUM!` and a `FALSE` WKT-derived availability value; its summary primarily checks WKT values rather than the year-by-year distance and 2017-step assertions. The WKT issue is separately documented below. `GnomoricBounds` now samples reversed distance-transform extrema; its source extrema assertions remain commented out, so distinguish these diagnostics from active C# assertions.

Other populated sheets also need targeted source-assertion mapping. Saved `FALSE` values on property UDFs can be expected data rather than failed assertions. `AxisHeightsTests.xlsx` has a populated `AxisHeightTests` sheet apparently representing `HeightConversionNLBE` and a populated `DistanceInNL` sheet, but lacks `RecreateViaWKT`, `NL_Wgs84`, and `CreateFromWKT`. Both populated `DistanceTests.xlsx` sheets also need assertion-equivalence review.

### Separate PROJ WKT round-trip issue: New Zealand deformation model

In `BasicTests.xlsx` / `TestNewZealand_defmodel`, the WKT generated for the EPSG:2105 → EPSG:9000 transform parses successfully but cannot be instantiated when reused as a transform description: `PE.trf.Properties(wkt,,"IsAvailable",options)` returns `FALSE`, while the generated PROJ pipeline returns `TRUE`. The WKT-derived transform lists `nz_linz_nzgd2000-20180701.json` as an available grid. `PE.trf.ListTransformSteps` identifies step 1, **Inverse of ITRF96 to NZGD2000 (7)** (method **Inverse of New Zealand Deformation Model**), as `IsAvailable=FALSE` and `HasInverse=FALSE`; the other four steps are available. An older TopoLib WKT definition embeds an explicit `+proj=defmodel` pipeline and works. Treat this as a separate suspected PROJ operation/database or WKT round-trip issue, **not an established ProjExcel API defect**. It may have been corrected in a PROJ/database version later than 9.5.1, but that has not been verified. Reproduce the WKT parse and instantiability check with a later PROJ/database release before attributing the root cause or marking it fixed. Keep coordinate assertions using the working CRS pair or PROJ pipeline separate from the WKT round-trip regression.

Complete the coverage in stages: (1) map each material source assertion and scenario to an Excel-accessible UDF result, expected value, and explicit comparison, documenting SharpProj-only behavior that cannot be reproduced through ProjExcel; (2) fix or document mismatched saved comparisons and avoid relying on display-only output or stale cached results; (3) recalculate with the intended ProjExcel add-in in Excel and verify every comparison, including network/grid-dependent scenarios under their required conditions. Keep the separate `docs/API_ProjExcel.xlsx` API tests distinct from this source-test parity audit.

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
