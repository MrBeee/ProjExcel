using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExcelDna.Integration;

// Added Bart
using SharpProj;
using SharpProj.Proj;
// using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable IDE0020 // Use pattern matching
#pragma warning disable IDE0038 // Use pattern matching

namespace ProjExcel
{

    public static class Crs
    {
        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }

        // SharpProj supports the following options; try to find the relevant one automatically based on the input type.
        // The following are the relevant Create() methods in SharpProj:

        // public new unsafe static CoordinateReferenceSystem   Create(string[] from, [Optional] ProjContext ctx)
        // public new unsafe static CoordinateReferenceSystem   Create(string from, [Optional] ProjContext ctx)

        // public new unsafe static CoordinateReferenceSystem   CreateFromWellKnownText(string from, CreateFromWKTOptions options, out string[] warnings, [Optional] ProjContext ctx)
        // public new static CoordinateReferenceSystem          CreateFromWellKnownText(string from, out string[] warnings, [Optional] ProjContext ctx)
        // public new static CoordinateReferenceSystem          CreateFromWellKnownText(string from, CreateFromWKTOptions options, [Optional] ProjContext ctx)
        // public new static CoordinateReferenceSystem          CreateFromWellKnownText(string from, [Optional] ProjContext ctx)

        // public static CoordinateReferenceSystem              CreateFromDatabase(Identifier identifier, [Optional] ProjContext ctx)
        // public static CoordinateReferenceSystem              CreateFromDatabase(string authority, int code, [Optional] ProjContext ctx)
        // public unsafe static CoordinateReferenceSystem       CreateFromDatabase(string authority, string code, [Optional] ProjContext ctx)

        // public static CoordinateReferenceSystem              CreateFromEpsg(int epsgCode, [Optional] ProjContext ctx)

        // The next routine allows for wider input than just epsg integer values; It returns a CoordinateReferenceSystem from various input types
        // It is also used to normalize the input CRS or transform specification into a CoordinateReferenceSystem object.
        // It has implemented all above Creation methods, apart from the stringlist variant: (string[] from).
        internal static CoordinateReferenceSystem CreateNormalizedCrs(in object oCrs, ProjContext pc)
        {
            return CreateCrsFromSingleCell(oCrs, pc).WithNormalizedAxis(pc);
        }

        internal static CoordinateReferenceSystem CreateCrs(in object[,] oCrs, in ProjContext pjContext)
        {
            if (oCrs == null || oCrs.GetLength(0) != 1 || oCrs.GetLength(1) != 1)
                throw new ArgumentException("CRS must be a single cell (for example, EPSG:1234).");

            return CreateCrsFromSingleCell(oCrs[0, 0], pjContext);
        } // CreateCrs

        private static CoordinateReferenceSystem CreateCrsFromSingleCell(object value, ProjContext pjContext)
        {
            if (Optional.IsEmpty(value))
                throw new ArgumentException("CRS specification is empty.");

            int nCrs;
            string sCrs;

            if (value is double)
            {
                nCrs = (int)(double)value;
                return CoordinateReferenceSystem.CreateFromEpsg(nCrs, pjContext);
            }
            else if (value is int)
            {
                nCrs = (int)value;
                return CoordinateReferenceSystem.CreateFromEpsg(nCrs, pjContext);
            }
            else if (value is string)
            {
                sCrs = (string)value;

                bool success = int.TryParse(sCrs, out nCrs);
                if (success)
                    return CoordinateReferenceSystem.CreateFromEpsg(nCrs, pjContext);

                bool hasAuthorityPrefix = Lst.GetAuthorityList()
                    .Any(authority => sCrs.StartsWith(authority + ":", StringComparison.OrdinalIgnoreCase));
                if (hasAuthorityPrefix)
                {
                    string[] parts = sCrs.Split(':');
                    if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
                        throw new ArgumentException($"Invalid authority code format: {sCrs}");

                    if (int.TryParse(parts[1], out nCrs))
                        return CoordinateReferenceSystem.CreateFromDatabase(parts[0], nCrs, pjContext);

                    return CoordinateReferenceSystem.CreateFromDatabase(parts[0], parts[1], pjContext);
                }

                if (IsWellKnownText(sCrs))
                    return CoordinateReferenceSystem.CreateFromWellKnownText(sCrs, pjContext);

                return CoordinateReferenceSystem.Create(sCrs, pjContext);
            }

            throw new ArgumentException("CRS");
        } // CreateCrsFromSingleCell

        private static bool IsWellKnownText(string sCrs)
        {
            return (sCrs.IndexOf("PROJCS") > -1) || (sCrs.IndexOf("GEOGCS") > -1) || (sCrs.IndexOf("PROJCRS") > -1) || (sCrs.IndexOf("GEOGCRS") > -1) || (sCrs.IndexOf("SPHEROID") > -1);
        } // IsWellKnownText

        private static readonly string[] CrsPropertyNames =
        {
            "Axis.Count", "CelestialBodyName", "CoordinateSystem.Axis.Count",
            "CoordinateSystem.CoordinateSystemType", "CoordinateSystem.Name", "CoordinateSystem.Type",
            "Datum.Name", "Datum.Type",
            "Ellipsoid.InverseFlattening", "Ellipsoid.IsSemiMinorComputed", "Ellipsoid.Name",
            "Ellipsoid.SemiMajorMetre", "Ellipsoid.SemiMinorMetre", "Ellipsoid.Type",
            "GeodeticCRS.Name", "GeodeticCRS.Type", "Identifiers.Count", "IsDeprecated", "Name",
            "PrimeMeridian.Longitude", "PrimeMeridian.Name", "PrimeMeridian.UnitConversionFactor", "PrimeMeridian.UnitName",
            "Remarks", "Scope", "Type",
            "UsageArea.Center.HasValues", "UsageArea.Center.X", "UsageArea.Center.Y",
            "UsageArea.EastLongitude", "UsageArea.MaxX", "UsageArea.MaxY", "UsageArea.MinX", "UsageArea.MinY",
            "UsageArea.Name", "UsageArea.NorthLatitude", "UsageArea.SouthLatitude", "UsageArea.WestLongitude"
        };

        private static readonly string[] AxisPropertyNames =
        {
            "Abbreviation", "Direction", "Name", "UnitAuthName", "UnitCode",
            "UnitConversionFactor", "UnitName"
        };

        private static readonly string[] IdentifierPropertyNames = { "Authority", "Code" };

        private static int ParseCrsPropertyIndex(object property, string[] names)
        {
            if (Optional.IsEmpty(property))
                throw new ArgumentException("Property is required.");

            if (property is int number)
                return number;

            if (property is double value)
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value != Math.Truncate(value))
                    throw new ArgumentException("Property index must be an integer.");
                return checked((int)value);
            }

            string name = Convert.ToString(property)?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Property is required.");

            if (int.TryParse(name, out int index))
                return index;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            throw new ArgumentException($"Unknown property. Name: '{name}'.");
        }

        private static object[,] ListCrsPropertyNames(string[] names)
        {
            object[,] result = new object[names.Length + 1, 2];
            result[0, 0] = "Index";
            result[0, 1] = "Property name";
            for (int i = 0; i < names.Length; i++)
            {
                result[i + 1, 0] = i;
                result[i + 1, 1] = names[i];
            }
            return result;
        }

        private static object[,] TransposeCrsValues(object[,] values)
        {
            object[,] result = new object[values.GetLength(1), values.GetLength(0)];
            for (int row = 0; row < values.GetLength(0); row++)
                for (int col = 0; col < values.GetLength(1); col++)
                    result[col, row] = values[row, col];
            return result;
        }

        private static object ExcelCrsValue(object value, string nullValueText)
        {
            if (value == null || value is string text && string.IsNullOrWhiteSpace(text))
                return nullValueText;
            if (value is double number && (double.IsNaN(number) || double.IsInfinity(number)))
                return nullValueText;
            return value;
        }

        private static object GetCrsPropertyValue(CoordinateReferenceSystem crs, CoordinateReferenceSystem originalCrs, string name, string nullValueText)
        {
            object value;
            switch (name)
            {
                case "Name": value = crs.Name; break;
                case "Type": value = crs.Type.ToString(); break;
                case "IsDeprecated": value = crs.IsDeprecated; break;
                case "CelestialBodyName": value = crs.CelestialBodyName; break;
                case "Remarks": value = crs.Remarks; break;
                case "Scope": value = crs.Scope; break;
                case "CoordinateSystem.Name": value = crs.CoordinateSystem?.Name; break;
                case "CoordinateSystem.Type": value = crs.CoordinateSystem?.Type.ToString(); break;
                case "CoordinateSystem.CoordinateSystemType": value = crs.CoordinateSystem?.CoordinateSystemType.ToString(); break;
                case "Axis.Count": value = crs.Axis?.Count; break;
                case "CoordinateSystem.Axis.Count": value = crs.CoordinateSystem?.Axis?.Count; break;
                case "Datum.Name": value = crs.Datum?.Name; break;
                case "Datum.Type": value = crs.Datum?.Type.ToString(); break;
                case "GeodeticCRS.Name": value = crs.GeodeticCRS?.Name; break;
                case "GeodeticCRS.Type": value = crs.GeodeticCRS?.Type.ToString(); break;
                case "Ellipsoid.Name": value = crs.Ellipsoid?.Name; break;
                case "Ellipsoid.Type": value = crs.Ellipsoid?.Type.ToString(); break;
                case "Ellipsoid.InverseFlattening": value = crs.Ellipsoid?.InverseFlattening; break;
                case "Ellipsoid.IsSemiMinorComputed": value = crs.Ellipsoid?.IsSemiMinorComputed; break;
                case "Ellipsoid.SemiMajorMetre": value = crs.Ellipsoid?.SemiMajorMetre; break;
                case "Ellipsoid.SemiMinorMetre": value = crs.Ellipsoid?.SemiMinorMetre; break;
                case "Identifiers.Count": value = originalCrs.Identifiers?.Count ?? 0; break;
                case "PrimeMeridian.Name": value = crs.PrimeMeridian?.Name; break;
                case "PrimeMeridian.Longitude": value = crs.PrimeMeridian?.Longitude; break;
                case "PrimeMeridian.UnitConversionFactor": value = crs.PrimeMeridian?.UnitConversionFactor; break;
                case "PrimeMeridian.UnitName": value = crs.PrimeMeridian?.UnitName; break;
                case "UsageArea.Name": value = crs.UsageArea?.Name; break;
                case "UsageArea.WestLongitude": value = crs.UsageArea?.WestLongitude; break;
                case "UsageArea.EastLongitude": value = crs.UsageArea?.EastLongitude; break;
                case "UsageArea.SouthLatitude": value = crs.UsageArea?.SouthLatitude; break;
                case "UsageArea.NorthLatitude": value = crs.UsageArea?.NorthLatitude; break;
                case "UsageArea.MinX": value = crs.UsageArea?.MinX; break;
                case "UsageArea.MinY": value = crs.UsageArea?.MinY; break;
                case "UsageArea.MaxX": value = crs.UsageArea?.MaxX; break;
                case "UsageArea.MaxY": value = crs.UsageArea?.MaxY; break;
                case "UsageArea.Center.X": value = crs.UsageArea?.CenterX; break;
                case "UsageArea.Center.Y": value = crs.UsageArea?.CenterY; break;
                case "UsageArea.Center.HasValues": value = crs.UsageArea == null ? (object)null : crs.UsageArea.Center.HasValues; break;
                default: throw new ArgumentException($"Unknown property. Name: '{name}'.");
            }
            return ExcelCrsValue(value, nullValueText);
        }

        [ExcelFunction(Name = "PE.crs.ListCrsProperties", Description = "List accessible CRS scalar properties by name and index")]
        public static object ListCrsProperties(
            [ExcelArgument(Name = "transpose", Description = "Transpose the list (FALSE)")] object transpose = null)
        {
            object[,] result = ListCrsPropertyNames(CrsPropertyNames);
            return Optional.Check(transpose, false) ? TransposeCrsValues(result) : result;
        }

        [ExcelFunction(Name = "PE.crs.Properties", Description = "Show CRS scalar properties selected by name or index in adjacent cells")]
        public static object Properties(
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, EPSG code, WKT, JSON or PROJ string")] object[,] oCrs,
            [ExcelArgument(Name = "properties", Description = "Comma-separated property names or indices; see PE.crs.ListCrsProperties()")] object properties,
            [ExcelArgument(Name = "Normalized", Description = "Use normalized axis ordering (TRUE)")] object normalized = null,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Transpose multiple selected properties (FALSE)")] object transpose = null)
        {
            try
            {
                bool multiple = properties is string names && names.Contains(",");
                int[] indices = multiple
                    ? ((string)properties).Split(',').Select(name => ParseCrsPropertyIndex(name.Trim(), CrsPropertyNames)).ToArray()
                    : new[] { ParseCrsPropertyIndex(properties, CrsPropertyNames) };

                if (indices.Any(index => index < 0 || index >= CrsPropertyNames.Length))
                    return "#ERR: Property index out of range.";

                string nullValueText = Optional.Check(nullValues, "<null>");
                using (ProjContext pjContext = CreateContext())
                using (var originalCrs = CreateCrs(oCrs, pjContext))
                using (var normalizedCrs = Optional.Check(normalized, true) ? originalCrs.WithAxisNormalized() : null)
                {
                    var crs = normalizedCrs ?? originalCrs;
                    if (!multiple)
                        return GetCrsPropertyValue(crs, originalCrs, CrsPropertyNames[indices[0]], nullValueText);

                    object[,] result = new object[1, indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                        result[0, i] = GetCrsPropertyValue(crs, originalCrs, CrsPropertyNames[indices[i]], nullValueText);
                    return Optional.Check(transpose, false) ? TransposeCrsValues(result) : result;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(Name = "PE.crs.ListAxisProperties", Description = "List accessible CRS axis properties by name and index")]
        public static object ListAxisProperties(
            [ExcelArgument(Name = "transpose", Description = "Transpose the list (FALSE)")] object transpose = null)
        {
            object[,] result = ListCrsPropertyNames(AxisPropertyNames);
            return Optional.Check(transpose, false) ? TransposeCrsValues(result) : result;
        }

        [ExcelFunction(Name = "PE.crs.AxisProperties", Description = "Show properties of one CRS axis in adjacent cells")]
        public static object AxisProperties(
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, EPSG code, WKT, JSON or PROJ string")] object[,] oCrs,
            [ExcelArgument(Name = "axisIndex", Description = "Zero-based axis index")] object axisIndex,
            [ExcelArgument(Name = "properties", Description = "Comma-separated names or indices; see PE.crs.ListAxisProperties()")] object properties,
            [ExcelArgument(Name = "coordinateSystemAxis", Description = "Read CoordinateSystem.Axis instead of Axis (FALSE)")] object coordinateSystemAxis = null,
            [ExcelArgument(Name = "Normalized", Description = "Use normalized axis ordering (TRUE)")] object normalized = null,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Transpose multiple selected properties (FALSE)")] object transpose = null)
        {
            try
            {
                bool multiple = properties is string names && names.Contains(",");
                int[] indices = multiple
                    ? ((string)properties).Split(',').Select(name => ParseCrsPropertyIndex(name.Trim(), AxisPropertyNames)).ToArray()
                    : new[] { ParseCrsPropertyIndex(properties, AxisPropertyNames) };

                if (indices.Any(index => index < 0 || index >= AxisPropertyNames.Length))
                    return "#ERR: Property index out of range.";

                int nAxis = ParseCrsPropertyIndex(axisIndex, new string[0]);
                string nullValueText = Optional.Check(nullValues, "<null>");
                using (ProjContext pjContext = CreateContext())
                using (var crs = Optional.Check(normalized, true)
                    ? CreateCrs(oCrs, pjContext).WithAxisNormalized()
                    : CreateCrs(oCrs, pjContext))
                {
                    var axes = Optional.Check(coordinateSystemAxis, false) ? crs.CoordinateSystem?.Axis : crs.Axis;
                    if (axes == null)
                        return "#ERR: CRS has no axes.";
                    if (nAxis < 0 || nAxis >= axes.Count)
                        return "#ERR: Axis index out of range.";

                    var axis = axes[nAxis];
                    object GetValue(int index)
                    {
                        object value;
                        switch (AxisPropertyNames[index])
                        {
                            case "Abbreviation": value = axis.Abbreviation; break;
                            case "Direction": value = axis.Direction; break;
                            case "Name": value = axis.Name; break;
                            case "UnitAuthName": value = axis.UnitAuthName; break;
                            case "UnitCode": value = axis.UnitCode; break;
                            case "UnitConversionFactor": value = axis.UnitConversionFactor; break;
                            case "UnitName": value = axis.UnitName; break;
                            default: throw new ArgumentException("Unknown axis property.");
                        }
                        return ExcelCrsValue(value, nullValueText);
                    }

                    if (!multiple)
                        return GetValue(indices[0]);

                    object[,] result = new object[1, indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                        result[0, i] = GetValue(indices[i]);
                    return Optional.Check(transpose, false) ? TransposeCrsValues(result) : result;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(Name = "PE.crs.ListIdentifierProperties", Description = "List accessible CRS identifier properties by name and index")]
        public static object ListIdentifierProperties(
            [ExcelArgument(Name = "transpose", Description = "Transpose the list (FALSE)")] object transpose = null)
        {
            object[,] result = ListCrsPropertyNames(IdentifierPropertyNames);
            return Optional.Check(transpose, false) ? TransposeCrsValues(result) : result;
        }

        [ExcelFunction(Name = "PE.crs.IdentifierProperties", Description = "Show properties of one CRS identifier in adjacent cells")]
        public static object IdentifierProperties(
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, EPSG code, WKT, JSON or PROJ string")] object[,] oCrs,
            [ExcelArgument(Name = "identifierIndex", Description = "Zero-based identifier index")] object identifierIndex,
            [ExcelArgument(Name = "properties", Description = "Comma-separated names or indices; see PE.crs.ListIdentifierProperties()")] object properties,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Transpose multiple selected properties (FALSE)")] object transpose = null)
        {
            try
            {
                bool multiple = properties is string names && names.Contains(",");
                int[] indices = multiple
                    ? ((string)properties).Split(',').Select(name => ParseCrsPropertyIndex(name.Trim(), IdentifierPropertyNames)).ToArray()
                    : new[] { ParseCrsPropertyIndex(properties, IdentifierPropertyNames) };

                if (indices.Any(index => index < 0 || index >= IdentifierPropertyNames.Length))
                    return "#ERR: Property index out of range.";

                int nIdentifier = ParseCrsPropertyIndex(identifierIndex, new string[0]);
                string nullValueText = Optional.Check(nullValues, "<null>");
                using (ProjContext pjContext = CreateContext())
                using (var crs = CreateCrs(oCrs, pjContext))
                {
                    if (crs.Identifiers == null || crs.Identifiers.Count == 0)
                        return "#ERR: CRS has no identifiers.";
                    if (nIdentifier < 0 || nIdentifier >= crs.Identifiers.Count)
                        return "#ERR: Identifier index out of range.";

                    var identifier = crs.Identifiers[nIdentifier];
                    object GetValue(int index)
                    {
                        object value;
                        switch (IdentifierPropertyNames[index])
                        {
                            case "Authority": value = identifier.Authority; break;
                            case "Code": value = identifier.Code; break;
                            default: throw new ArgumentException("Unknown identifier property.");
                        }
                        return ExcelCrsValue(value, nullValueText);
                    }

                    if (!multiple)
                        return GetValue(indices[0]);

                    object[,] result = new object[1, indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                        result[0, i] = GetValue(indices[i]);
                    return Optional.Check(transpose, false) ? TransposeCrsValues(result) : result;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(Name = "PE.crs.ToString", Description = "Create a PROJ, WKT or JSON string representation of a CRS")]
        public static object ToString(
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string")] object[,] oCrs,
            [ExcelArgument(Name = "stringFormat", Description = "Proj, Wkt or Json (Proj)")] object stringFormat,
            [ExcelArgument(Name = "Normalized", Description = "Use normalized axis ordering (TRUE)")] object normalized = null)
        {
            try
            {
                int outputFormat = TransformFunctions.ParseOutputFormatString(stringFormat);

                using (ProjContext pjContext = CreateContext())
                using (var crs = Optional.Check(normalized, true)
                    ? CreateCrs(oCrs, pjContext).WithAxisNormalized()
                    : CreateCrs(oCrs, pjContext))
                {
                    string result;
                    switch (outputFormat)
                    {
                        case 2:
                            result = crs.AsWellKnownText();
                            break;
                        case 3:
                            result = crs.AsProjJson();
                            break;
                        default:
                            result = crs.AsProjString();
                            break;
                    }

                    return string.IsNullOrWhiteSpace(result) ? "#ERR: Invalid input." : result;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(
            Name = "PE.crs.AsJsonString",
            Description = "Describes the Coordinate Reference System as a JSON-string"
            )]
        public static object AsJsonString(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string CrsString ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        CrsString = crs.AsProjJson();

                        if (String.IsNullOrWhiteSpace(CrsString))
                            return "#ERR: Invalid input.";

                        return CrsString;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // AsJsonString

        [ExcelFunction(
            Name = "PE.crs.AsProjString",
            Description = "Describes the Coordinate Reference System as a PROJ-string"
            )]
        public static object AsProjString(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string CrsString ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        CrsString = crs.AsProjString();

                        if (String.IsNullOrWhiteSpace(CrsString))
                            return "#ERR: Invalid input.";

                        return CrsString;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // AsProjString

        [ExcelFunction(
            Name = "PE.crs.AsWktString",
            Description = "Describes a Coordinate Reference System as a WKT-string"
            )]
        public static object AsWktString(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string CrsString ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        CrsString = crs.AsWellKnownText();

                        if (String.IsNullOrWhiteSpace(CrsString))
                            return "#ERR: Invalid input.";

                        return CrsString;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // AsWellKnownText

        [ExcelFunction(
            Name = "PE.crs.Axis.Abbreviation",
            Description = "Get the short name of axis-N of a coordinate reference system"
            )]
        public static object Axis_Abbreviation(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].Abbreviation;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_Abbreviation

        [ExcelFunction(
            Name = "PE.crs.Axis.Count",
            Description = "Gets type of Coordinate System"
            )]
        public static object Axis_Count(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nAxes = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            nAxes = crs.Axis.Count;

                        return nAxes;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex); 
            }

        } // Axis_Count

        [ExcelFunction(
            Name = "PE.crs.Axis.Direction",
            Description = "Get the direction of axis-N of a coordinate reference system"
            )]
        public static object Axis_Direction(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].Direction;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_Direction

        [ExcelFunction(
            Name = "PE.crs.Axis.Name",
            Description = "Get the name of axis-N of a coordinate reference system"
            )]
        public static object Axis_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_Name

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitAuthName",
            Description = "Get the authority name of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitAuthName(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].UnitAuthName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitAuthName

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitCode",
            Description = "Get the authority name of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitCode(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].UnitCode;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitCode

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitConversionFactor",
            Description = "Get the Unit Conversion Factor of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitConversionFactor(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            double factor = 0;

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            factor = crs.Axis[nIndex].UnitConversionFactor;

                        return factor;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitConversionFactor

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitName",
            Description = "Get the unit name of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitName(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].UnitName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitName

        [ExcelFunction(
            Name = "PE.crs.CelestialBodyName",
            Description = "Gets the name of the appropriate celestial body for this Coordinate Reference System"
            )]
        public static object CelestialBodyName(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CelestialBodyName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CelestialBodyName

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Abbreviation",
            Description = "Get the short name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_Abbreviation(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].Abbreviation;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_Abbreviation

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Count",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_Axis_Count(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nAxes = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null & crs.CoordinateSystem != null)
                            nAxes = crs.CoordinateSystem.Axis.Count;

                        return nAxes;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex); 
            }

        } // CoordinateSystem_Axis_Count

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Direction",
            Description = "Get the direction of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_Direction(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].Direction;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_Direction

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Name",
            Description = "Get the name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_Name

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitAuthName",
            Description = "Get the authority name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitAuthName(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].UnitAuthName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitAuthName

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitCode",
            Description = "Get the authority name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitCode(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].UnitCode;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitCode

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitConversionFactor",
            Description = "Get the Unit Conversion Factor of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitConversionFactor(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            double factor = 0;

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            factor = crs.CoordinateSystem.Axis[nIndex].UnitConversionFactor;

                        return factor;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitConversionFactor

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitName",
            Description = "Get the unit name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitName(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].UnitName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitName

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.CoordinateSystemType",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_CoordinateSystemType(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.CoordinateSystem.CoordinateSystemType.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type = "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_CoordinateSystemType

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Name",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Name

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Type",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_Type(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);
            try
            {
                object[,] res = new object [1, 1];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;
                            res[0, 0] = projType.ToString();
                        }

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageAreaCenterX

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center",
            Description = "Gets center point of CRS usage area"
            )]
        public static object UsageArea_Center(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                object[,] res = new object [1, 2];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                        {
                            res[0, 0] = crs.UsageArea.CenterX;
                            res[0, 1] = crs.UsageArea.CenterY;
                            return res;
                        }
                        else 
                            throw new ArgumentException("No Center Point found");  // Will return #VALUE to Excel
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center.HasValues",
            Description = "Confirms whether the center point in the usage area has values"
            )]
        public static object UsageArea_Center_HasValues(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null && crs.UsageArea.Center !=null)
                            return crs.UsageArea.Center.HasValues;

                        return false; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center_HasValues

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center.X",
            Description = "Gets x-value of center point of CRS usage area"
            )]
        public static object UsageArea_Center_X(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.CenterX;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center_X

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center.Y",
            Description = "Gets y-value of center point of CRS usage area"
            )]
        public static object UsageArea_Center_Y(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.CenterY;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center_Y

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MaxX",
            Description = "Gets maximum X-value of CRS usage area"
            )]
        public static object UsageArea_MaxX(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MaxX;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MaxX

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MaxY",
            Description = "Gets maximum Y-value of CRS usage area"
            )]
        public static object UsageArea_MaxY(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MaxY;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MaxY

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MinX",
            Description = "Gets minimum X-value of CRS usage area"
            )]
        public static object UsageArea_MinX(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MinX;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MinX

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MinY",
            Description = "Gets minimum Y-value of CRS usage area"
            )]
        public static object UsageArea_MinY(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MinY;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MinY

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Name",
            Description = "Gets name of CRS usage area"
            )]
        public static object UsageArea_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            name = crs.UsageArea.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Name

        [ExcelFunction(
            Name = "PE.crs.UsageArea.WestLongitude",
            Description = "Gets the west longitude of CRS usage area"
            )]
        public static object UsageArea_WestLongitude(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double west = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            west = crs.UsageArea.WestLongitude;

                        return west;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_WestLongitude

        [ExcelFunction(
            Name = "PE.crs.UsageArea.EastLongitude",
            Description = "Gets east longitude of CRS usage area"
            )]
        public static object UsageArea_EastLongitude(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double east = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            east = crs.UsageArea.EastLongitude;

                        return east;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_EastLongitude

        [ExcelFunction(
            Name = "PE.crs.UsageArea.SouthLatitude",
            Description = "Gets south latitude of CRS usage area"
            )]
        public static object UsageArea_SouthLatitude(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double south = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            south = crs.UsageArea.SouthLatitude;

                        return south;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_SouthLatitude

        [ExcelFunction(
            Name = "PE.crs.UsageArea.NorthLatitude",
            Description = "Gets north latitude of CRS usage area"
            )]
        public static object UsageArea_NorthLatitude(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double north = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            north = crs.UsageArea.NorthLatitude;

                        return north;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_NorthLatitude

        [ExcelFunction(
            Name = "PE.crs.Datum.Name",
            Description = "Gets name of datum of CRS"
            )]
        public static object Datum_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Datum.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Datum_Name

        [ExcelFunction(
            Name = "PE.crs.Datum.Type",
            Description = "Gets type of datum in CRS"
            )]
        public static object Datum_Type(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.Datum.Type.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type = "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Datum_Type

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.InverseFlattening",
            Description = "Gets inverse flattening of ellipsoid"
            )]
        public static object Ellipsoid_InverseFlattening(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.Ellipsoid.InverseFlattening;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_InverseFlattening

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.IsSemiMinorComputed",
            Description = "Confirms whether when size of semi-minor axis has been calculated"
            )]
        public static object Ellipsoid_IsSemiMinorComputed(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            return crs.Ellipsoid.IsSemiMinorComputed;

                        return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EllipsoidIsSemiMinorComputed

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.Name",
            Description = "Gets name of Ellipsoid in Coordinate Reference System"
            )]
        public static object Ellipsoid_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Ellipsoid.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_Name

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.SemiMajorMetre",
            Description = "Gets size of ellipsoid's semi-major axis in metres"
            )]
        public static object Ellipsoid_SemiMajorMetre(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.Ellipsoid.SemiMajorMetre;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_SemiMajorMetre

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.SemiMinorMetre",
            Description = "Gets size of ellipsoid's semi-minor axis in metres"
            )]
        public static object Ellipsoid_SemiMinorMetre(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.Ellipsoid.SemiMinorMetre;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_SemiMinorMetre

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.Type",
            Description = "Gets type of ellipsoid in Coordinate Reference System"
            )]
        public static object Ellipsoid_Type(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.Ellipsoid.Type.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type = "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_Type

        [ExcelFunction(
            Name = "PE.crs.EuclideanArea",
            Description = "Gets Euclidean area defined by 3 or more points in a Projected or Engineering CRS, ignoring elevation differences"
            )]
        public static object EuclideanArea(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Vertical list of points", Name = "PointList")] object[,] Points,
            [ExcelArgument("Normalize area to [m2] (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            int nPointRows = Points.GetLength(0);
            int nPointCols = Points.GetLength(1);

            if (nPointRows < 3 )
                return "#ERR: Invalid input.";

            if (nPointCols < 2 || nPointCols > 4 )
                return "#ERR: Invalid input.";

            // Now determine number of points in (poly)line
            int nPoints = nPointRows + 1;

            double[,] points = new double[nPoints, 2];

            // First, get all the points from Points
            for (int i = 0; i < nPointRows; i++)
            {
                points[i, 0] = (double)Points[i, 0];
                points[i, 1] = (double)Points[i, 1];
            }

            // Make sure we close the polygon
            points[nPointRows, 0] = points[0, 0];
            points[nPointRows, 1] = points[0, 1];

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;

                            if (projType == ProjType.ProjectedCrs || projType == ProjType.EngineeringCrs)
                            {
                                double sum1 = 0;
                                double sum2 = 0;

                                // so far now

                                for (int i = 0; i < nPoints - 1; i++)
                                {
                                    sum1 += points[i, 0] * points[i + 1, 1];
                                    sum2 += points[i, 1] * points[i + 1, 0];
                                }

                                double area = Math.Abs(0.5 * (sum1 - sum2));

                                // See : https://github.com/tracyharton/Proj4/blob/master/pj_units.c
                                double conversion0 = crs.Axis[0].UnitConversionFactor;
                                double conversion1 = crs.Axis[1].UnitConversionFactor;

                                if (bNorm)
                                {
                                    area *= (conversion0 * conversion1);
                                }

                                return area;
                            }
                            else
                                throw new Exception("Euclidean Area requires a Projected or Engineering CRS");
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EuclideanArea

        [ExcelFunction(
            Name = "PE.crs.EuclideanDistance",
            Description = "Gets Euclidean distance between two points (or between multiple points in a poly-line), defined in a Projected or Engineering CRS, ignoring elevation differences"
            )]
        public static object EuclideanDistance(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Normalize distance to [m] (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            int nPoint1Rows = Point1.GetLength(0);
            int nPoint1Cols = Point1.GetLength(1);

            int nPoint2Rows = Point2.GetLength(0);
            int nPoint2Cols = Point2.GetLength(1);

            if (nPoint1Rows < 1 )
                return "#ERR: Invalid input.";

            if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                return "#ERR: Invalid input.";

            // Check use of nPoint2; we need it when nPoint1Rows == 1
            if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                return "#ERR: Invalid input.";

            // Do the check on nPoint2Cols only when we need Point2
            if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                return "#ERR: Invalid input.";

            // Now determine number of points in (poly)line
            int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

            double[,] points = new double[nPoints, 2];

            // First, get all the points from Point1
            for (int i = 0; i < nPoint1Rows; i++)
            {
                points[i, 0] = (double)Point1[i, 0];
                points[i, 1] = (double)Point1[i, 1];
            }

            // Now get the last point from Point2 if needed
            if (nPoints > nPoint1Rows)
            {
                points[nPoints - 1, 0] = (double)Point2[0, 0];
                points[nPoints - 1, 1] = (double)Point2[0, 1];
            }

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;

                            if (projType == ProjType.ProjectedCrs || projType == ProjType.EngineeringCrs)
                            {
                                double length = 0;

                                for (int i = 0; i < nPoints - 1; i++)
                                {
                                    length += Math.Sqrt(Math.Pow(points[i + 1, 0] - points[i, 0], 2) + Math.Pow(points[i + 1, 1] - points[i, 1], 2));
                                }
                                double conversion0 = crs.Axis[0].UnitConversionFactor;

                                if (bNorm)
                                {
                                    length *= conversion0;
                                }

                                return length;
                            }
                            else
                                throw new Exception("Euclidean Distance requires a Projected or Engineering CRS");
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EuclideanDistance

        [ExcelFunction(
            Name = "PE.crs.EuclideanDistanceZ",
            Description = "Gets Euclidean distance between two points (or between multiple points in a poly-line), defined in a Projected or Engineering CRS, honoring elevation differences"
            )]
        public static object EuclideanDistanceZ(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y,z points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Normalize distance to [m] (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                int nPoint2Rows = Point2.GetLength(0);
                int nPoint2Cols = Point2.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                // Check use of nPoint2; we need it when nPoint1Rows == 1
                if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                    return "#ERR: Invalid input.";

                // Do the check on nPoint2Cols only when we need Point2
                if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                    return "#ERR: Invalid input.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                PPoint[] points = new PPoint[nPoints];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    switch (nPoint1Cols)
                    {
                        default:
                        case 2:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1]);
                            break;
                        case 3:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2]);
                            break;
                        case 4:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2], (double)Point1[i,3]);
                            break;
                    }
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    switch (nPoint2Cols)
                    {
                        default:
                        case 2:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1]);
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2]);
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2], (double)Point2[0, 3]);
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;

                            if (projType == ProjType.ProjectedCrs || projType == ProjType.EngineeringCrs)
                            {
                                double length = 0;
                                for (int i = 0; i < nPoints - 1; i++)
                                {
                                    length += Math.Sqrt(
                                        Math.Pow(points[i + 1].X - points[i].X, 2)
                                        + Math.Pow(points[i + 1].Y - points[i].Y, 2)
                                        + Math.Pow(points[i + 1].Z - points[i].Z, 2));
                                }

                                double conversion0 = crs.Axis[0].UnitConversionFactor;

                                if (bNorm)
                                {
                                    length *= conversion0;
                                }

                                return length;
                            }
                            else
                            {
                                throw new Exception("Euclidean Distance requires a Projected or Engineering CRS");
                            }
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EuclideanDistanceZ

        [ExcelFunction(
            Name = "PE.crs.GeodesicArea",
            Description = "Gets surface area defined by multiple (at least 3) points in a polygon defined in a Coordinate Reference System"
            )]
        public static object GeodesicArea(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Vertical list of points in a polygon)", Name = "startPointOrPointList")] object[,] Points,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            int nPointRows = Points.GetLength(0);
            int nPointCols = Points.GetLength(1);

            if (nPointRows < 3 )
                return "#ERR: Invalid input.";

            if (nPointCols < 2 || nPointCols > 4 )
                return "#ERR: Invalid input.";

            // Now determine number of points in (poly)line
            int nPoints = nPointRows;

            PPoint[] points = new PPoint[nPoints];

            // First, get all the points from Point1
            for (int i = 0; i < nPoints; i++)
            {
                switch (nPointCols)
                {
                    default:
                    case 2:
                        points[i] = new PPoint((double)Points[i, 0], (double)Points[i, 1]);
                        break;
                    case 3:
                        points[i] = new PPoint((double)Points[i, 0], (double)Points[i, 1], (double)Points[i, 2]);
                        break;
                    case 4:
                        points[i] = new PPoint((double)Points[i, 0], (double)Points[i, 1], (double)Points[i, 2], (double)Points[i,3]);
                        break;
                }
            }

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            double distance = Math.Abs(crs.GeoArea(points));
                            return Optional.CheckNan(distance);
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodesicArea

        [ExcelFunction(
            Name = "PE.crs.GeodesicDistance",
            Description = "Gets geodesic distance between two points (or between multiple points in a poly-line), defined in a Coordinate Reference System, ignoring elevation differences"
            )]
        public static object GeodesicDistance(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list of points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                int nPoint2Rows = Point2.GetLength(0);
                int nPoint2Cols = Point2.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                // Check use of nPoint2; we need it when nPoint1Rows == 1
                if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                    return "#ERR: Invalid input.";

                // Do the check on nPoint2Cols only when we need Point2
                if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                    return "#ERR: Invalid input.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                PPoint[] points = new PPoint[nPoints];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    switch (nPoint1Cols)
                    {
                        default:
                        case 2:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1]);
                            break;
                        case 3:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2]);
                            break;
                        case 4:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2], (double)Point1[i,3]);
                            break;
                    }
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    switch (nPoint2Cols)
                    {
                        default:
                        case 2:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1]);
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2]);
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2], (double)Point2[0, 3]);
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            double distance = crs.GeoDistance(points);
                            return Optional.CheckNan(distance);
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodesicDistance

        [ExcelFunction(
            Name = "PE.crs.GeodesicDistanceZ",
            Description = "Gets geodesic distance between two points (or between multiple points in a poly-line), defined in a Coordinate Reference System, honoring elevation differences"
            )]
        public static object GeodesicDistanceZ(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y,z points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                int nPoint2Rows = Point2.GetLength(0);
                int nPoint2Cols = Point2.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                // Check use of nPoint2; we need it when nPoint1Rows == 1
                if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                    return "#ERR: Invalid input.";

                // Do the check on nPoint2Cols only when we need Point2
                if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                    return "#ERR: Invalid input.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                PPoint[] points = new PPoint[nPoints];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    switch (nPoint1Cols)
                    {
                        default:
                        case 2:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1]);
                            break;
                        case 3:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2]);
                            break;
                        case 4:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2], (double)Point1[i,3]);
                            break;
                    }
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    switch (nPoint2Cols)
                    {
                        default:
                        case 2:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1]);
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2]);
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2], (double)Point2[0, 3]);
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            double distance = crs.GeoDistanceZ(points);
                            return Optional.CheckNan(distance);
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeoDistanceZ

        [ExcelFunction(
            Name = "PE.crs.GeodeticCRS.Name",
            Description = "Gets name of Geodetic Coordinate Reference System"
            )]
        public static object GeodeticCRS_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.GeodeticCRS.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodeticCRS_Name

        [ExcelFunction(
            Name = "PE.crs.GeodeticCRS.Type",
            Description = "Gets type of Geodetic Coordinate Reference System"
            )]
        public static object GeodeticCRS_Type(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.GeodeticCRS.Type.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type= "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodeticCRS_Type

        [ExcelFunction(
            Name = "PE.crs.Identifiers.Authority",
            Description = "Gets Authority of Identifier N"
            )]
        public static object Identifiers_Authority(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (false)", Name = "Normalized")] object normalized
             )
        {
            bool bNorm  = Optional.Check(normalized, false);

            try
            {
                int nIndex  = (int)Optional.Check(index , 0.0);

                int Count = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    // for Identifiers we CANNOT normalize the Axes, as this will NULL the Identifiers
                    // using (var crs = CreateCrs(oCrs, pjContext).WithAxisNormalized())
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.Identifiers != null)
                        {
                            Count = crs.Identifiers.Count;

                            if (nIndex > Count - 1 || nIndex < 0)
                                return "<index out of range>";

                            return crs.Identifiers[nIndex].Authority;
                        }
                        else
                            return "Unknown";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Identifiers_Authority

        [ExcelFunction(
            Name = "PE.crs.Identifiers.Code",
            Description = "Gets Code of Identifier N"
            )]
        public static object Identifiers_Code(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (false)", Name = "Normalized")] object normalized
             )
        {
            bool bNorm  = Optional.Check(normalized, false);

            try
            {
                int nIndex  = (int)Optional.Check(index , 0.0);

                using (ProjContext pjContext = CreateContext())
                {
                    //for Identifiers we CANNOT normalize the Axes, as this will NULL the Identifiers
                    // using (var crs = CreateCrs(oCrs, pjContext).WithAxisNormalized())
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.Identifiers != null)
                        {
                            int Count = crs.Identifiers.Count;

                            if (nIndex > Count - 1 || nIndex < 0)
                                return "<index out of range>";

                            return crs.Identifiers[nIndex].Code;
                        }
                        else
                            return "Unknown";

                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Identifiers_Code

        [ExcelFunction(
            Name = "PE.crs.Identifiers.Count",
            Description = "Gets number of Identifiers"
            )]
        public static object Identifiers_Count(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (false)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, false);

            try
            {
                int Count = -0;

                using (ProjContext pjContext = CreateContext())
                {
                    //for Identifiers we CANNOT normalize the Axes, as this will NULL the Identifiers
                    // using (var crs = CreateCrs(oCrs, pjContext).WithAxisNormalized())
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.Identifiers != null)
                            Count = crs.Identifiers.Count;

                        return Count;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Identifiers_Count

        [ExcelFunction(
            Name = "PE.crs.IsDeprecated",
            Description = "Confirms whether a CRS has been deprecated"
            )]
        public static object IsDeprecated(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            return crs.IsDeprecated;
                        else
                            return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // IsDeprecated

        [ExcelFunction(
            Name = "PE.crs.IsEquivalentTo",
            Description = "Confirms whether two different CRSs are equivalent"
            )]
        public static object IsEquivalentTo(
            [ExcelArgument("CRS-1, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs1")] object[,] oCrs1,
            [ExcelArgument("CRS-2, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs2")] object[,] oCrs2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs1 = bNorm ? CreateCrs(oCrs1, pjContext).WithAxisNormalized() : CreateCrs(oCrs1, pjContext))
                    using (var crs2 = bNorm ? CreateCrs(oCrs2, pjContext).WithAxisNormalized() : CreateCrs(oCrs2, pjContext))
                    {
                        if (crs1 != null && crs2 != null )
                            return crs1.IsEquivalentTo(crs2, pjContext);
                        else
                            return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // IsEquivalentTo

        [ExcelFunction(
            Name = "PE.crs.IsEquivalentToRelaxed",
            Description = "Confirms whether two different CRSs are equivalent with the axes in any order"
            )]
        public static object IsEquivalentToRelaxed(
            [ExcelArgument("CRS-1, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs1")] object[,] oCrs1,
            [ExcelArgument("CRS-2, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs2")] object[,] oCrs2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs1 = bNorm ? CreateCrs(oCrs1, pjContext).WithAxisNormalized() : CreateCrs(oCrs1, pjContext))
                    using (var crs2 = bNorm ? CreateCrs(oCrs2, pjContext).WithAxisNormalized() : CreateCrs(oCrs2, pjContext))
                    {
                        if (crs1 != null && crs2 != null )
                            return crs1.IsEquivalentToRelaxed(crs2, pjContext);
                        else
                            return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // IsEquivalentToRelaxed

        [ExcelFunction(
            Name = "PE.crs.Name",
            Description = "Gets name of Coordinate Reference System"
            )]
        public static object Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Name

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.Longitude",
            Description = "Gets longitude of prime miridian in degrees"
            )]
        public static object PrimeMeridian_Longitude(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.PrimeMeridian.Longitude;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_Longitude

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.Name",
            Description = "Gets name of prime meridean of coordinate reference system"
            )]
        public static object PrimeMeridian_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.PrimeMeridian.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_Name

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.UnitConversionFactor",
            Description = "Gets unit conversion factor of prime miridian in degrees"
            )]
        public static object PrimeMeridian_UnitConversionFactor(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.PrimeMeridian.UnitConversionFactor;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_UnitConversionFactor

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.UnitName",
            Description = "Gets name of prime meridean of coordinate reference system"
            )]
        public static object PrimeMeridian_UnitName(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.PrimeMeridian.UnitName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_UnitName

        [ExcelFunction(
            Name = "PE.crs.Remarks",
            Description = "Gets scope of Coordinate Reference System"
            )]
        public static object Remarks(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string remarks ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            remarks = crs.Remarks;

                        if (String.IsNullOrWhiteSpace(remarks))
                            remarks= "<NotFound>";

                        return remarks;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Remarks

        [ExcelFunction(
            Name = "PE.crs.Scope",
            Description = "Gets scope of Coordinate Reference System"
            )]
        public static object Scope(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string scope ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            scope = crs.Scope;

                        if (String.IsNullOrWhiteSpace(scope))
                            scope = "<NotFound>";

                        return scope;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Scope

        [ExcelFunction(
            Name = "PE.crs.Type",
            Description = "Gets type of coordinate reference system"
            )]
        public static object Type(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);
            try
            {
                object[,] res = new object [1, 1];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;
                            res[0, 0] = projType.ToString();
                        }

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageAreaCenterX

    } // class

} // namespace
