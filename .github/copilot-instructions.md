# Copilot Instructions

## TopoLib
- Do not edit any function under `TopoLib`.
- Treat `TopoLib` as read-only unless explicitly instructed otherwise.
- Keep changes focused outside `TopoLib`.

## Project Guidelines
- User prefers no custom candidate ranking; expose SharpProj ChooseCoordinateTransform entries directly and let end user choose candidate index.
- Do not edit PE.trf.TransformList unless explicitly requested; user considers it correct and wants changes elsewhere.
- Keep the Excel-facing API deliberate: use explicit, stable property names for scalar CRS and transform values rather than exposing arbitrary .NET members through reflection.
- For ProjExcel Excel-facing UDFs, use explicit stable property names for scalars; expose identifiers as Excel-friendly structured authority/code data mapped explicitly; use task-oriented UDFs for methods and retain discoverable commonly used UDFs instead of generic reflection or ToString-based object serialization.
- Return structured values such as identifiers in an Excel-friendly shape (for example, an `object[,]` with authority and code, or an indexed field accessor); do not return SharpProj objects directly to Excel.
- Map identifier fields explicitly instead of relying on `ToString()` or `Convert.ToString()` to represent identifiers or collections of identifiers.
- Expose methods as task-oriented UDFs, not arbitrary method invocation by name. Retain clearly named UDFs for frequently used operations when they improve discoverability.
- In ProjExcel, prefer transform identifiers formatted as a single AUTHORITY:CODE string in one Excel cell rather than returned as adjacent authority and code cells; explicitly map Identifier.Authority and Identifier.Code instead of using ToString().
- For a PE.trf.GridProperties UDF, return grids involved in the selected transform as a whole; do not require a transform-step index or expose step details unless needed internally to collect grids. The grid-number-indexed Excel UDF should be named PE.trf.GridProperties and resemble PE.trf.StepProperties, not PE.trf.TransformGridProperties.
- PE.trf.OptionsEx should be a standalone Excel UDF with direct arguments for transform and PROJ context settings; do not require end users to nest PE.trf.Options() inside PE.trf.OptionsEx(). User is open to a single-cell compact token combining transform and PROJ context non-default settings rather than a two-row spill, provided defaults are omitted and CandidateIndex is always shown.

## Code Style
- Avoid superfluous one-line helper methods; prefer direct calls when they do not reduce duplication meaningfully.
- In ProjExcel Trf.cs, prefer explicit if/else over a multiline conditional expression for transform candidate selection when it improves readability.