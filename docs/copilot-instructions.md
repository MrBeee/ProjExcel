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
- Avoid adding Excel UDFs that are merely wrappers around existing property-based UDFs without adding capability; keep the UDF list concise and avoid redundant one-line aliases.

## Code Style
- Avoid superfluous one-line helper methods; prefer direct calls when they do not reduce duplication meaningfully.