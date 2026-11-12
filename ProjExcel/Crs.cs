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
        internal static CoordinateReferenceSystem CreateCrs(in object oCrs, in ProjContext pjContext)
        {
            if (oCrs is object[,] range)
            {
                // the single cell oCrs may contain a range object returned from another Excel function,
                // but it must be a single cell range (1x1) to be valid for CRS creation.
                if (range.GetLength(0) != 1 || range.GetLength(1) != 1)
                    throw new ArgumentException("CRS must be a single cell (for example, EPSG:1234).");

                return CreateCrsFromSingleCell(range[0, 0], pjContext);
            }

            return CreateCrsFromSingleCell(oCrs, pjContext);
        } // CreateCrs

        internal static int ParseNumericEpsgCode(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) ||
                value != Math.Truncate(value) || value < int.MinValue || value > int.MaxValue)
                throw new ArgumentException("Numeric EPSG code must be a finite integer within the Int32 range.");

            return (int)value;
        }

        private static CoordinateReferenceSystem CreateCrsFromSingleCell(object value, ProjContext pjContext)
        {
            if (Optional.IsEmpty(value))
                throw new ArgumentException("CRS specification is empty.");

            int nCrs;
            string sCrs;

            if (value is double)
            {
                nCrs = ParseNumericEpsgCode((double)value);
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
                if (string.IsNullOrWhiteSpace(sCrs))
                    throw new ArgumentException("CRS specification is empty.");

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

            throw new ArgumentException("CRS specification must be an EPSG code, AUTHORITY:CODE, WKT, JSON or PROJ string.");
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
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, EPSG code, WKT, JSON or PROJ string")] object oCrs,
            [ExcelArgument(Name = "properties", Description = "Comma-separated property names or indices; see PE.crs.ListCrsProperties()")] object properties,
            [ExcelArgument(Name = "Normalized", Description = "Use normalized axis ordering (TRUE)")] object normalized = null,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null)
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
                    return result;
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
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, EPSG code, WKT, JSON or PROJ string")] object oCrs,
            [ExcelArgument(Name = "axisIndex", Description = "Zero-based axis index")] object axisIndex,
            [ExcelArgument(Name = "properties", Description = "Comma-separated names or indices; see PE.crs.ListAxisProperties()")] object properties,
            [ExcelArgument(Name = "coordinateSystemAxis", Description = "Read CoordinateSystem.Axis instead of Axis (FALSE)")] object coordinateSystemAxis = null,
            [ExcelArgument(Name = "Normalized", Description = "Use normalized axis ordering (TRUE)")] object normalized = null,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null)
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
                using (var originalCrs = CreateCrs(oCrs, pjContext))
                using (var normalizedCrs = Optional.Check(normalized, true) ? originalCrs.WithAxisNormalized() : null)
                {
                    var crs = normalizedCrs ?? originalCrs;
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
                    return result;
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
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, EPSG code, WKT, JSON or PROJ string")] object oCrs,
            [ExcelArgument(Name = "identifierIndex", Description = "Zero-based identifier index")] object identifierIndex,
            [ExcelArgument(Name = "properties", Description = "Comma-separated names or indices; see PE.crs.ListIdentifierProperties()")] object properties,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null)
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
                    return result;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(Name = "PE.crs.ToString", Description = "Create a PROJ, WKT or JSON string representation of a CRS")]
        public static object ToString(
            [ExcelArgument(Name = "Crs", Description = "Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string")] object oCrs,
            [ExcelArgument(Name = "stringFormat", Description = "Proj, Wkt or Json (Proj)")] object stringFormat,
            [ExcelArgument(Name = "Normalized", Description = "Use normalized axis ordering (TRUE)")] object normalized = null)
        {
            try
            {
                int outputFormat = TransformFunctions.ParseOutputFormatString(stringFormat);

                using (ProjContext pjContext = CreateContext())
                using (var originalCrs = CreateCrs(oCrs, pjContext))
                using (var normalizedCrs = Optional.Check(normalized, true) ? originalCrs.WithAxisNormalized() : null)
                {
                    var crs = normalizedCrs ?? originalCrs;
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

        private static double MeasurementCoordinate(object[,] points, int row, int column, string argument)
        {
            object value = points[row, column];
            double coordinate;
            if (value is double number)
                coordinate = number;
            else if (value is int integer)
                coordinate = integer;
            else
                throw new ArgumentException($"{argument} row {row + 1}, coordinate {column + 1} must be a finite number.");

            if (double.IsNaN(coordinate) || double.IsInfinity(coordinate))
                throw new ArgumentException($"{argument} row {row + 1}, coordinate {column + 1} must be a finite number.");
            return coordinate;
        }

        [ExcelFunction(
            Name = "PE.crs.EuclideanArea",
            Description = "Gets Euclidean area defined by 3 or more points in a Projected or Engineering CRS, ignoring elevation differences"
            )]
        public static object EuclideanArea(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object oCrs,
            [ExcelArgument("Vertical list of points", Name = "PointList")] object[,] Points,
            [ExcelArgument("Normalize area to [m2] (true)", Name = "Normalized")] object normalized
            )
        {
            try
            {
                bool bNorm = Optional.Check(normalized, true);

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
                    points[i, 0] = MeasurementCoordinate(Points, i, 0, "PointList");
                    points[i, 1] = MeasurementCoordinate(Points, i, 1, "PointList");
                }

                // Make sure we close the polygon
                points[nPointRows, 0] = points[0, 0];
                points[nPointRows, 1] = points[0, 1];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs = CreateCrs(oCrs, pjContext))
                    using (var normalizedCrs = bNorm ? originalCrs.WithAxisNormalized() : null)
                    {
                        var crs = normalizedCrs ?? originalCrs;
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Normalize distance to [m] (true)", Name = "Normalized")] object normalized
            )
        {
            try
            {
                bool bNorm = Optional.Check(normalized, true);

                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input. Need at least one point.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input. Each point must have 2 to 4 coordinates.";

                if (nPoint1Rows == 1 &&
                    (Point2 == null || Point2.GetLength(0) != 1 || Point2.GetLength(1) < 2 || Point2.GetLength(1) > 4))
                    return "#ERR: End point must be one row with 2 to 4 coordinates.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                double[,] points = new double[nPoints, 2];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    points[i, 0] = MeasurementCoordinate(Point1, i, 0, "startPointOrPointList");
                    points[i, 1] = MeasurementCoordinate(Point1, i, 1, "startPointOrPointList");
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    points[nPoints - 1, 0] = MeasurementCoordinate(Point2, 0, 0, "endPointOrNul");
                    points[nPoints - 1, 1] = MeasurementCoordinate(Point2, 0, 1, "endPointOrNul");
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs = CreateCrs(oCrs, pjContext))
                    using (var normalizedCrs = bNorm ? originalCrs.WithAxisNormalized() : null)
                    {
                        var crs = normalizedCrs ?? originalCrs;
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y,z points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Normalize distance to [m] (true)", Name = "Normalized")] object normalized
            )
        {
            try
            {
                bool bNorm = Optional.Check(normalized, true);
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                int nPoint2Cols = 0;
                if (nPoint1Rows == 1)
                {
                    if (Point2 == null || Point2.GetLength(0) != 1 || Point2.GetLength(1) < 2 || Point2.GetLength(1) > 4)
                        return "#ERR: End point must be one row with 2 to 4 coordinates.";
                    nPoint2Cols = Point2.GetLength(1);
                }

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
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"));
                            break;
                        case 3:
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 2, "startPointOrPointList"));
                            break;
                        case 4:
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 2, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 3, "startPointOrPointList"));
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
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"));
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 2, "endPointOrNul"));
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 2, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 3, "endPointOrNul"));
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs = CreateCrs(oCrs, pjContext))
                    using (var normalizedCrs = bNorm ? originalCrs.WithAxisNormalized() : null)
                    {
                        var crs = normalizedCrs ?? originalCrs;
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object oCrs,
            [ExcelArgument("Vertical list of points in a polygon)", Name = "startPointOrPointList")] object[,] Points,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            try
            {
                bool bNorm = Optional.Check(normalized, true);

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
                            points[i] = new PPoint(MeasurementCoordinate(Points, i, 0, "startPointOrPointList"), MeasurementCoordinate(Points, i, 1, "startPointOrPointList"));
                            break;
                        case 3:
                            points[i] = new PPoint(MeasurementCoordinate(Points, i, 0, "startPointOrPointList"), MeasurementCoordinate(Points, i, 1, "startPointOrPointList"), MeasurementCoordinate(Points, i, 2, "startPointOrPointList"));
                            break;
                        case 4:
                            points[i] = new PPoint(MeasurementCoordinate(Points, i, 0, "startPointOrPointList"), MeasurementCoordinate(Points, i, 1, "startPointOrPointList"), MeasurementCoordinate(Points, i, 2, "startPointOrPointList"), MeasurementCoordinate(Points, i, 3, "startPointOrPointList"));
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs = CreateCrs(oCrs, pjContext))
                    using (var normalizedCrs = bNorm ? originalCrs.WithAxisNormalized() : null)
                    {
                        var crs = normalizedCrs ?? originalCrs;
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object oCrs,
            [ExcelArgument("Start point (or vertical list of points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            try
            {
                bool bNorm = Optional.Check(normalized, true);
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                int nPoint2Cols = 0;
                if (nPoint1Rows == 1)
                {
                    if (Point2 == null || Point2.GetLength(0) != 1 || Point2.GetLength(1) < 2 || Point2.GetLength(1) > 4)
                        return "#ERR: End point must be one row with 2 to 4 coordinates.";
                    nPoint2Cols = Point2.GetLength(1);
                }

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
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"));
                            break;
                        case 3:
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 2, "startPointOrPointList"));
                            break;
                        case 4:
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 2, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 3, "startPointOrPointList"));
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
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"));
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 2, "endPointOrNul"));
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 2, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 3, "endPointOrNul"));
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs = CreateCrs(oCrs, pjContext))
                    using (var normalizedCrs = bNorm ? originalCrs.WithAxisNormalized() : null)
                    {
                        var crs = normalizedCrs ?? originalCrs;
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y,z points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            try
            {
                bool bNorm = Optional.Check(normalized, true);
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                int nPoint2Cols = 0;
                if (nPoint1Rows == 1)
                {
                    if (Point2 == null || Point2.GetLength(0) != 1 || Point2.GetLength(1) < 2 || Point2.GetLength(1) > 4)
                        return "#ERR: End point must be one row with 2 to 4 coordinates.";
                    nPoint2Cols = Point2.GetLength(1);
                }

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
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"));
                            break;
                        case 3:
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 2, "startPointOrPointList"));
                            break;
                        case 4:
                            points[i] = new PPoint(MeasurementCoordinate(Point1, i, 0, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 1, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 2, "startPointOrPointList"), MeasurementCoordinate(Point1, i, 3, "startPointOrPointList"));
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
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"));
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 2, "endPointOrNul"));
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint(MeasurementCoordinate(Point2, 0, 0, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 1, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 2, "endPointOrNul"), MeasurementCoordinate(Point2, 0, 3, "endPointOrNul"));
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs = CreateCrs(oCrs, pjContext))
                    using (var normalizedCrs = bNorm ? originalCrs.WithAxisNormalized() : null)
                    {
                        var crs = normalizedCrs ?? originalCrs;
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
            Name = "PE.crs.IsEquivalentTo",
            Description = "Confirms whether two different CRSs are equivalent"
            )]
        public static object IsEquivalentTo(
            [ExcelArgument("CRS-1, string with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs1")] object oCrs1,
            [ExcelArgument("CRS-2, string with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs2")] object oCrs2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs1 = CreateCrs(oCrs1, pjContext))
                    using (var normalizedCrs1 = bNorm ? originalCrs1.WithAxisNormalized() : null)
                    using (var originalCrs2 = CreateCrs(oCrs2, pjContext))
                    using (var normalizedCrs2 = bNorm ? originalCrs2.WithAxisNormalized() : null)
                    {
                        var crs1 = normalizedCrs1 ?? originalCrs1;
                        var crs2 = normalizedCrs2 ?? originalCrs2;
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
            [ExcelArgument("CRS-1, string with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs1")] object oCrs1,
            [ExcelArgument("CRS-2, string with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs2")] object oCrs2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var originalCrs1 = CreateCrs(oCrs1, pjContext))
                    using (var normalizedCrs1 = bNorm ? originalCrs1.WithAxisNormalized() : null)
                    using (var originalCrs2 = CreateCrs(oCrs2, pjContext))
                    using (var normalizedCrs2 = bNorm ? originalCrs2.WithAxisNormalized() : null)
                    {
                        var crs1 = normalizedCrs1 ?? originalCrs1;
                        var crs2 = normalizedCrs2 ?? originalCrs2;
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

    } // class

} // namespace
