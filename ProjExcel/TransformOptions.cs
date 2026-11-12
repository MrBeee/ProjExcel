using ExcelDna.Integration;
using MathNet.Numerics.IntegralTransforms;
using SharpProj;
using SharpProj.Proj;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;


namespace ProjExcel
{
    public static class TransformOptions
    {
        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }

        // Class CoordinateTransformOptions is defined here: D:\GitHub\SharpProj\src\SharpProj\CoordinateArea.h
        // It contains the following class members:
        //public ref class CoordinateTransformOptions
        //{
        //    public:
        //    property CoordinateArea^ Area;
        //    property String^ Authority;
        //    property Nullable<double> Accuracy;
        //    property bool NoBallparkConversions;
        //    property bool NoDiscardIfMissing;
        //    property bool UsePrimaryGridNames;
        //    property bool UseSuperseded;
        //    property bool StrictContains;
        //    property IntermediateCrsUsage IntermediateCrsUsage;
        //    property bool BestOnly;
        //    property bool ForceOver;
        //};

        // Whereby IntermediateCrsUsage is defined as

        //public enum class IntermediateCrsUsage
        //{
        //    Auto,
        //    Always,
        //    Never
        //};

        // CandidateIndex and AllowDeprecatedCrs are not included in the SharpProj CoordinateTransformOptions class.
        // So I need to check if these are needed in the future. For now, I will include them in the input class.
        internal sealed class CoordinateTransformOptionsInput
        {
            public CoordinateArea Area { get; set; }
            public string Authority { get; set; }
            public double? Accuracy { get; set; }
            public bool NoBallparkConversions { get; set; }
            public bool NoDiscardIfMissing { get; set; }
            public bool UsePrimaryGridNames { get; set; }
            public bool UseSuperseded { get; set; }
            public bool StrictContains { get; set; }
            public IntermediateCrsUsage IntermediateCrsUsage { get; set; }
            public bool BestOnly { get; set; }
            public bool ForceOver { get; set; }

            public bool AllowDeprecatedCrs { get; set; }
            public int CandidateIndex { get; set; }
            public string ErrorMessage { get; set; }

            public bool? EnableNetworkConnections { get; set; }
            public bool? EnableGridCache { get; set; }
            public int? GridCacheSizeMb { get; set; }
            public int? GridCacheTtlSeconds { get; set; }
            public string EndpointUrl { get; set; }
            public string GridCachePath { get; set; }
            public ProjLogLevel? LogLevel { get; set; }

            public CoordinateTransformOptionsInput()
            {
                Authority = null;
                Accuracy = null;
                IntermediateCrsUsage = IntermediateCrsUsage.Auto;
            }

            public static CoordinateTransformOptionsInput Parse(object oOptions)
            {
                var values = new CoordinateTransformOptionsInput
                {
                    NoBallparkConversions = false,
                    NoDiscardIfMissing = false,
                    UsePrimaryGridNames = false,
                    UseSuperseded = false,
                    StrictContains = false,
                    BestOnly = false,
                    ForceOver = false,

                    AllowDeprecatedCrs = false,
                    CandidateIndex = 0
                };

                if (oOptions == null || oOptions == DBNull.Value || oOptions == Type.Missing || oOptions is ExcelMissing || oOptions is ExcelEmpty)
                    return values;

                object raw = oOptions;
                object[,] matrix = raw as object[,];
                if (matrix != null)
                {
                    if (matrix.Length != 1)
                        throw new ArgumentException("transformOptions must be a single cell.");
                    raw = matrix[0, 0];
                }

                if (raw == null || raw == DBNull.Value || raw == Type.Missing || raw is ExcelMissing || raw is ExcelEmpty)
                    return values;
                if (!(raw is string))
                    throw new ArgumentException("transformOptions must contain a transform options token.");

                string token = (string)raw;
                if (string.IsNullOrWhiteSpace(token))
                    return values;

                string[] parts = token.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    string[] keyValue = part.Split(new[] { '=' }, 2);
                    if (keyValue.Length != 2)
                        throw new ArgumentException($"Invalid transform option token '{part.Trim()}': expected Name=Value.");

                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    if (key.Length == 0)
                        throw new ArgumentException($"Invalid transform option token '{part.Trim()}': option name is required.");
                    if (value.Length == 0)
                        throw new ArgumentException($"Transform option '{key}' requires a value.");

                    if (string.Equals(key, "Authority", StringComparison.OrdinalIgnoreCase))
                        values.Authority = string.IsNullOrWhiteSpace(value) ? null : value;
                    else if (string.Equals(key, "Accuracy", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!double.TryParse(value, out double accuracy) || double.IsNaN(accuracy) || double.IsInfinity(accuracy) || accuracy < 0)
                            throw new ArgumentException($"Accuracy must be a non-negative finite number (received '{value}').");
                        values.Accuracy = accuracy;
                    }
                    else if (string.Equals(key, "NoBallparkConversions", StringComparison.OrdinalIgnoreCase))
                        values.NoBallparkConversions = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "NoDiscardIfMissing", StringComparison.OrdinalIgnoreCase))
                        values.NoDiscardIfMissing = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "UsePrimaryGridNames", StringComparison.OrdinalIgnoreCase))
                        values.UsePrimaryGridNames = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "UseSuperseded", StringComparison.OrdinalIgnoreCase))
                        values.UseSuperseded = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "StrictContains", StringComparison.OrdinalIgnoreCase))
                        values.StrictContains = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "BestOnly", StringComparison.OrdinalIgnoreCase))
                        values.BestOnly = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "ForceOver", StringComparison.OrdinalIgnoreCase))
                        values.ForceOver = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "IntermediateCrsUsage", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(value, "Always", StringComparison.OrdinalIgnoreCase))
                            values.IntermediateCrsUsage = IntermediateCrsUsage.Always;
                        else if (string.Equals(value, "Never", StringComparison.OrdinalIgnoreCase))
                            values.IntermediateCrsUsage = IntermediateCrsUsage.Never;
                        else if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
                            values.IntermediateCrsUsage = IntermediateCrsUsage.Auto;
                        else
                            throw new ArgumentException($"IntermediateCrsUsage must be Auto, Always, or Never (received '{value}').");
                    }
                    else if (string.Equals(key, "Area", StringComparison.OrdinalIgnoreCase))
                    {
                        CoordinateArea area = ParseAreaOrDefault(value, null);
                        if (area == null)
                            throw new ArgumentException($"Area must contain four numeric coordinates: west,south,east,north (received '{value}').");
                        values.Area = area;
                    }

                    else if (string.Equals(key, "AllowDeprecatedCrs", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "AllowDeprecatedCRS", StringComparison.OrdinalIgnoreCase))
                        values.AllowDeprecatedCrs = ParseBooleanOption(value, key, false);
                    else if (string.Equals(key, "CandidateIndex", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(value, out int candidateIndex) || candidateIndex < 0)
                            throw new ArgumentException($"CandidateIndex must be a non-negative integer (received '{value}').");
                        values.CandidateIndex = candidateIndex;
                    }
                    else if (string.Equals(key, "EnableNetworkConnections", StringComparison.OrdinalIgnoreCase))
                        values.EnableNetworkConnections = ParseBooleanOption(value, key, ProjService.Default.EnableNetworkConnections);
                    else if (string.Equals(key, "EnableGridCache", StringComparison.OrdinalIgnoreCase))
                        values.EnableGridCache = ParseBooleanOption(value, key, ProjService.Default.EnableGridCache);
                    else if (string.Equals(key, "GridCacheSizeMb", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(value, out int size) || size <= 0)
                            throw new ArgumentException($"GridCacheSizeMb must be a positive integer (received '{value}').");
                        values.GridCacheSizeMb = size;
                    }
                    else if (string.Equals(key, "GridCacheTtlSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(value, out int ttl) || ttl < 0)
                            throw new ArgumentException($"GridCacheTtlSeconds must be a non-negative integer (received '{value}').");
                        values.GridCacheTtlSeconds = ttl;
                    }
                    else if (string.Equals(key, "EndpointUrl", StringComparison.OrdinalIgnoreCase))
                        values.EndpointUrl = string.IsNullOrWhiteSpace(value) ? null : value;
                    else if (string.Equals(key, "GridCachePath", StringComparison.OrdinalIgnoreCase))
                        values.GridCachePath = string.IsNullOrWhiteSpace(value) ? null : value;
                    else if (string.Equals(key, "LogLevel", StringComparison.OrdinalIgnoreCase))
                    {
                        ProjLogLevel logLevel;
                        if (!Enum.TryParse(value, true, out logLevel) || !Enum.IsDefined(typeof(ProjLogLevel), logLevel))
                            throw new ArgumentException($"LogLevel must be a valid PROJ log level (received '{value}').");
                        values.LogLevel = logLevel;
                    }
                    else
                        throw new ArgumentException($"Unknown transform option '{key}'.");
                }
                return values;
            }
        }

        // this function returns the SharpProj CoordinateTransformOptions object, generated through an Excel UDF
        // note that AllowDeprecatedCrs and CandidateIndex are not part of CoordinateTransformOptions
        // so they are not included in the returned object; they put restrictions on CRS and the define the selected transform when multiple transforms are available
        internal static CoordinateTransformOptions CreateCoordinateTransformOptions(CoordinateTransformOptionsInput values)
        {
            var options = new CoordinateTransformOptions();

            if (values == null)
                return options;

            if (!string.IsNullOrWhiteSpace(values.Authority))
                options.Authority = values.Authority;

            if (values.Accuracy.HasValue)
                options.Accuracy = values.Accuracy;

            if (values.Area != null)
                options.Area = values.Area;

            options.NoBallparkConversions = values.NoBallparkConversions;
            options.NoDiscardIfMissing = values.NoDiscardIfMissing;
            options.UsePrimaryGridNames = values.UsePrimaryGridNames;
            options.UseSuperseded = values.UseSuperseded;
            options.StrictContains = values.StrictContains;
            options.IntermediateCrsUsage = values.IntermediateCrsUsage;
            options.BestOnly = values.BestOnly;
            options.ForceOver = values.ForceOver;

            return options;
        }

        //This routine may cause silent data entry errors,
        //so I will not use it. Instead, I will use ParseBooleanOption() which throws an exception if the value is not a valid boolean.
        //internal static bool ParseBooleanOrDefault(object value, bool defaultValue)
        //{
        //    if (value == null || value == DBNull.Value || value == Type.Missing)
        //        return defaultValue;

        //    if (value is bool)
        //        return (bool)value;

        //    if (value is byte)
        //        return (byte)value != 0;

        //    if (value is sbyte)
        //        return (sbyte)value != 0;

        //    if (value is short)
        //        return (short)value != 0;

        //    if (value is ushort)
        //        return (ushort)value != 0;

        //    if (value is int)
        //        return (int)value != 0;

        //    if (value is uint)
        //        return (uint)value != 0;

        //    if (value is long)
        //        return (long)value != 0;

        //    if (value is ulong)
        //        return (ulong)value != 0;

        //    if (value is float)
        //        return Math.Abs((float)value) > 0f;

        //    if (value is double)
        //        return Math.Abs((double)value) > 0d;

        //    if (value is decimal)
        //        return ((decimal)value) != 0m;

        //    string text = Convert.ToString(value);
        //    if (string.IsNullOrWhiteSpace(text))
        //        return defaultValue;

        //    bool parsedBool;
        //    if (bool.TryParse(text, out parsedBool))
        //        return parsedBool;

        //    text = text.Trim();
        //    if (string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
        //        string.Equals(text, "y", StringComparison.OrdinalIgnoreCase) ||
        //        string.Equals(text, "on", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return true;
        //    }

        //    if (string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
        //        string.Equals(text, "n", StringComparison.OrdinalIgnoreCase) ||
        //        string.Equals(text, "off", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return false;
        //    }

        //    double parsedNumber;
        //    if (double.TryParse(text, out parsedNumber))
        //        return parsedNumber != 0d;

        //    return defaultValue;
        //}

        private static bool ParseBooleanOption(object value, string name, bool defaultValue)
        {
            if (value == null || value == DBNull.Value || value == Type.Missing || value is ExcelMissing || value is ExcelEmpty)
                return defaultValue;

            if (value is bool boolean)
                return boolean;

            if (value is string text)
            {
                text = text.Trim();
                if (bool.TryParse(text, out bool parsed))
                    return parsed;
                if (string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "y", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "on", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "n", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text, "off", StringComparison.OrdinalIgnoreCase))
                    return false;
                if (double.TryParse(text, out double number))
                {
                    if (number == 1d)
                        return true;
                    if (number == 0d)
                        return false;
                }
            }
            else if (value is IConvertible && !(value is ExcelError))
            {
                try
                {
                    double number = Convert.ToDouble(value);
                    if (number == 1d)
                        return true;
                    if (number == 0d)
                        return false;
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                {
                }
            }

            throw new ArgumentException($"{name} must be TRUE or FALSE (1 or 0).");
        }

        internal static int ParseIntOrDefault(object value, int defaultValue)
        {
            if (value == null || value == DBNull.Value || value == Type.Missing)
                return defaultValue;

            if (value is int)
                return (int)value;

            if (value is byte)
                return (byte)value;

            if (value is sbyte)
                return (sbyte)value;

            if (value is short)
                return (short)value;

            if (value is ushort)
                return (ushort)value;

            if (value is uint)
                return (int)(uint)value;

            if (value is long)
                return (int)(long)value;

            if (value is ulong)
                return (int)(ulong)value;

            if (value is float)
                return (int)Math.Truncate((float)value);

            if (value is double)
                return (int)Math.Truncate((double)value);

            if (value is decimal)
                return (int)Math.Truncate((decimal)value);

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            int parsedInt;
            if (int.TryParse(text, out parsedInt))
                return parsedInt;

            double parsedNumber;
            if (double.TryParse(text, out parsedNumber))
                return (int)Math.Truncate(parsedNumber);

            return defaultValue;
        }

        internal static string ParseIntermediateCrsUsageOrDefault(object value)
        {
            string usage = Convert.ToString(value);

            if (string.IsNullOrWhiteSpace(usage))
                return "Auto";

            if (string.Equals(usage, "Auto", StringComparison.OrdinalIgnoreCase))
                return "Auto";

            if (string.Equals(usage, "Always", StringComparison.OrdinalIgnoreCase))
                return "Always";

            if (string.Equals(usage, "Never", StringComparison.OrdinalIgnoreCase))
                return "Never";

            return "Auto";
        }

        internal static double? ParseNullableDoubleOrDefault(object value, double? defaultValue)
        {
            if (value == null || value == DBNull.Value || value == Type.Missing)
                return defaultValue;

            if (value is double)
                return (double)value;

            if (value is float)
                return Convert.ToDouble(value);

            if (value is decimal)
                return Convert.ToDouble(value);

            if (value is int)
                return (int)value;

            if (value is long)
                return (long)value;

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            double parsed;
            if (double.TryParse(text, out parsed))
                return parsed;

            return defaultValue;
        }

        private static CoordinateArea ParseAreaOrDefault(object value, CoordinateArea defaultValue)
        {
            if (value == null || value == DBNull.Value || value == Type.Missing)
                return defaultValue;

            if (value is CoordinateArea)
                return (CoordinateArea)value;

            object raw = value;
            object[,] matrix = raw as object[,];
            if (matrix != null && matrix.Length == 4)
            {
                double west = ParseNullableDoubleOrDefault(matrix[0, 0], double.NaN) ?? double.NaN;
                double south = ParseNullableDoubleOrDefault(matrix[0, 1], double.NaN) ?? double.NaN;
                double east = ParseNullableDoubleOrDefault(matrix[0, 2], double.NaN) ?? double.NaN;
                double north = ParseNullableDoubleOrDefault(matrix[0, 3], double.NaN) ?? double.NaN;

                if (!double.IsNaN(west) && !double.IsNaN(south) && !double.IsNaN(east) && !double.IsNaN(north))
                    return new CoordinateArea(west, south, east, north);
            }

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            string[] parts = text.Split(',');
            if (parts.Length != 4)
                return defaultValue;

            double westValue;
            double southValue;
            double eastValue;
            double northValue;

            if (double.TryParse(parts[0].Trim(), out westValue) &&
                double.TryParse(parts[1].Trim(), out southValue) &&
                double.TryParse(parts[2].Trim(), out eastValue) &&
                double.TryParse(parts[3].Trim(), out northValue) &&
                !double.IsNaN(westValue) && !double.IsInfinity(westValue) &&
                !double.IsNaN(southValue) && !double.IsInfinity(southValue) &&
                !double.IsNaN(eastValue) && !double.IsInfinity(eastValue) &&
                !double.IsNaN(northValue) && !double.IsInfinity(northValue) &&
                westValue >= -180 && westValue <= 180 && eastValue >= -180 && eastValue <= 180 &&
                southValue >= -90 && southValue <= 90 && northValue >= -90 && northValue <= 90 &&
                southValue <= northValue)
            {
                return new CoordinateArea(westValue, southValue, eastValue, northValue);
            }

            return defaultValue;
        }

        private static string BuildAreaToken(object westLongitude, object southLatitude, object eastLongitude, object northLatitude)
        {
            double? west = ParseNullableDoubleOrDefault(westLongitude, null);
            double? south = ParseNullableDoubleOrDefault(southLatitude, null);
            double? east = ParseNullableDoubleOrDefault(eastLongitude, null);
            double? north = ParseNullableDoubleOrDefault(northLatitude, null);

            if (!west.HasValue || !south.HasValue || !east.HasValue || !north.HasValue)
                return string.Empty;

            if (west.Value < -180 || west.Value > 180 ||
                east.Value < -180 || east.Value > 180 ||
                south.Value < -90 || south.Value > 90 ||
                north.Value < -90 || north.Value > 90 ||
                south.Value > north.Value)
            {
                return string.Empty;
            }

            return string.Join(",", west.Value, south.Value, east.Value, north.Value);
        }

        // Note: this uUDF creates a textual representation in Excel of the SharpProj class CoordinateTransformOptions
        // This class has been defined here: D:\GitHub\SharpProj\src\SharpProj\CoordinateArea.h

        // CandidateIndex and AllowDeprecatedCrs are not included in the SharpProj CoordinateTransformOptions class.
        // 
        // So I need to check if these are needed in the future. For now, I will include them in the output string.

        [ExcelFunction(Name = "PE.trf.Options", Description = "Creates an options token for CoordinateTransform options")]
        public static string Options(
            [ExcelArgument(Name = "CandidateIndex", Description = "Transform candidate index (0)")] object candidateIndex = null,
            [ExcelArgument(Name = "AllowDeprecatedCrs", Description = "Allow deprecated CRSs (FALSE)")] object allowDeprecatedCrs = null,
            [ExcelArgument(Name = "Authority", Description = "Authority name (blank = no authority filter)")] object authority = null,
            [ExcelArgument(Name = "Accuracy", Description = "Desired accuracy (blank = none)")] object accuracy = null,

            // Area of interest is defined by the bounding box coordinates: west, south, east, north.
            // See proj proj_area_set_bbox() in D:\GitHub\proj\src\proj_area.c
            [ExcelArgument(Name = "WestLongitude", Description = "West longitude of usage area (blank = none)")] object westLongitude = null,
            [ExcelArgument(Name = "SouthLatitude", Description = "South latitude of usage area (blank = none)")] object southLatitude = null,
            [ExcelArgument(Name = "EastLongitude", Description = "East longitude of usage area (blank = none)")] object eastLongitude = null,
            [ExcelArgument(Name = "NorthLatitude", Description = "North latitude of usage area (blank = none)")] object northLatitude = null,

            [ExcelArgument(Name = "BestOnly", Description = "Use best transform only (FALSE)")] object bestOnly = null,
            [ExcelArgument(Name = "ForceOver", Description = "Force over-range longitude handling (FALSE)")] object forceOver = null,
            [ExcelArgument(Name = "IntermediateCrsUsage", Description = "Auto, Always or Never (Auto)")] object intermediateCrsUsage = null,

            [ExcelArgument(Name = "NoBallparkConversions", Description = "Disallow ballpark conversions (FALSE)")] object noBallparkConversions = null,
            [ExcelArgument(Name = "NoDiscardIfMissing", Description = "Allow transforms even when a grid is missing (FALSE)")] object noDiscardIfMissing = null,

            [ExcelArgument(Name = "StrictContains", Description = "Require transform to strictly contain the area of interest (FALSE)")] object strictContains = null,

            [ExcelArgument(Name = "UsePrimaryGridNames", Description = "Use primary grid names (FALSE)")] object usePrimaryGridNames = null,
            [ExcelArgument(Name = "UseSuperseded", Description = "Allow superseded transforms (FALSE)")] object useSuperseded = null
            )
        {
            try
            {
            var parts = new List<string>
            {
                "CandidateIndex=" + Math.Max(ParseIntOrDefault(candidateIndex, 0), 0)
            };

            if (ParseBooleanOption(allowDeprecatedCrs, "AllowDeprecatedCrs", false))
                parts.Add("AllowDeprecatedCrs=True");

            string authorityValue = Convert.ToString(authority);
            if (!string.IsNullOrWhiteSpace(authorityValue))
                parts.Add("Authority=" + authorityValue);

            double? accuracyValue = ParseNullableDoubleOrDefault(accuracy, null);
            if (accuracyValue.HasValue)
                parts.Add("Accuracy=" + Convert.ToString(accuracyValue.Value));

            string areaValue = BuildAreaToken(westLongitude, southLatitude, eastLongitude, northLatitude);
            if (!string.IsNullOrEmpty(areaValue))
                parts.Add("Area=" + areaValue);

            if (ParseBooleanOption(bestOnly, "BestOnly", false))
                parts.Add("BestOnly=True");

            if (ParseBooleanOption(forceOver, "ForceOver", false))
                parts.Add("ForceOver=True");

            if (!Optional.IsEmpty(intermediateCrsUsage) &&
                !string.IsNullOrWhiteSpace(Convert.ToString(intermediateCrsUsage)))
            {
                string inputUsage = Convert.ToString(intermediateCrsUsage).Trim();
                if (!string.Equals(inputUsage, "Auto", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(inputUsage, "Always", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(inputUsage, "Never", StringComparison.OrdinalIgnoreCase))
                    return "#ERR: IntermediateCrsUsage must be Auto, Always, or Never.";
            }

            string usage = ParseIntermediateCrsUsageOrDefault(intermediateCrsUsage);
            if (usage != "Auto")
                parts.Add("IntermediateCrsUsage=" + usage);

            if (ParseBooleanOption(noBallparkConversions, "NoBallparkConversions", false))
                parts.Add("NoBallparkConversions=True");

            if (ParseBooleanOption(noDiscardIfMissing, "NoDiscardIfMissing", false))
                parts.Add("NoDiscardIfMissing=True");

            if (ParseBooleanOption(strictContains, "StrictContains", false))
                parts.Add("StrictContains=True");

            if (ParseBooleanOption(usePrimaryGridNames, "UsePrimaryGridNames", false))
                parts.Add("UsePrimaryGridNames=True");

            if (ParseBooleanOption(useSuperseded, "UseSuperseded", false))
                parts.Add("UseSuperseded=True");

            return string.Join("; ", parts);
            }
            catch (ArgumentException ex)
            {
                return "#ERR: " + ex.Message;
            }
        }

        [ExcelFunction(Name = "PE.trf.OptionsEx", Description = "Adds PROJ context settings to a transform options token")]
        public static string OptionsEx(
            [ExcelArgument(Name = "transformOptions", Description = "Cell containing output from PE.trf.Options()  (blank = defaults)")] object transformOptions = null,
            [ExcelArgument(Name = "EnableNetworkConnections", Description = "Allow online grid access (default: TRUE)")] object enableNetworkConnections = null,
            [ExcelArgument(Name = "EnableGridCache", Description = "Cache downloaded grids (default: TRUE)")] object enableGridCache = null,
            [ExcelArgument(Name = "GridCacheSizeMb", Description = "Grid cache size in MB (default: 300)")] object gridCacheSizeMb = null,
            [ExcelArgument(Name = "GridCacheTtlSeconds", Description = "Grid cache lifetime in seconds (default: 86400)")] object gridCacheTtlSeconds = null,
            [ExcelArgument(Name = "EndpointUrl", Description = "PROJ network endpoint (default: https://cdn.proj.org)")] object endpointUrl = null,
            [ExcelArgument(Name = "GridCachePath", Description = "Grid cache file (default: %LOCALAPPDATA%\\ProjExcel\\proj\\cache.db)")] object gridCachePath = null,
            [ExcelArgument(Name = "LogLevel", Description = "PROJ log level: None, Error (default), Debug, and Trace)")] object logLevel = null
            )
        {
            try
            {
            object raw = transformOptions;
            if (raw is object[,] matrix)
            {
                if (matrix.Length != 1)
                    return "#ERR: transformOptions must be a single cell.";

                raw = matrix[0, 0];
            }

            string token;
            if (raw == null || raw == DBNull.Value || raw == Type.Missing || raw is ExcelMissing || raw is ExcelEmpty)
                token = "CandidateIndex=0";
            else if (raw is string text)
                token = string.IsNullOrWhiteSpace(text) ? "CandidateIndex=0" : text;
            else
                return "#ERR: transformOptions must contain a token from PE.trf.Options().";

            CoordinateTransformOptionsInput.Parse(token);
            var parts = new List<string> { token };
            ProjService defaults = ProjService.Default;

            if (!Optional.IsMissing(enableNetworkConnections))
            {
                bool enabled = ParseBooleanOption(enableNetworkConnections, "EnableNetworkConnections", defaults.EnableNetworkConnections);
                if (enabled != defaults.EnableNetworkConnections)
                    parts.Add("EnableNetworkConnections=" + enabled);
            }

            if (!Optional.IsMissing(enableGridCache))
            {
                bool enabled = ParseBooleanOption(enableGridCache, "EnableGridCache", defaults.EnableGridCache);
                if (enabled != defaults.EnableGridCache)
                    parts.Add("EnableGridCache=" + enabled);
            }

            if (!Optional.IsEmpty(gridCacheSizeMb))
            {
                int size;
                if (!int.TryParse(Convert.ToString(gridCacheSizeMb), out size) || size <= 0)
                    return "#ERR: GridCacheSizeMb must be a positive integer.";

                if (size != defaults.GridCacheSizeMb)
                    parts.Add("GridCacheSizeMb=" + size);
            }

            if (!Optional.IsEmpty(gridCacheTtlSeconds))
            {
                int ttl;
                if (!int.TryParse(Convert.ToString(gridCacheTtlSeconds), out ttl) || ttl < 0)
                    return "#ERR: GridCacheTtlSeconds must be a non-negative integer.";

                if (ttl != defaults.GridCacheTtlSeconds)
                    parts.Add("GridCacheTtlSeconds=" + ttl);
            }

            if (!Optional.IsEmpty(endpointUrl))
            {
                string endpoint = Convert.ToString(endpointUrl).Trim();
                Uri uri;
                if (endpoint.IndexOfAny(new[] { ';', '|' }) >= 0 ||
                    !Uri.TryCreate(endpoint, UriKind.Absolute, out uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                    return "#ERR: EndpointUrl must be an HTTP(S) URL without semicolons or pipes.";

                if (!string.Equals(endpoint, defaults.EndpointUrl, StringComparison.OrdinalIgnoreCase))
                    parts.Add("EndpointUrl=" + endpoint);
            }

            if (!Optional.IsEmpty(gridCachePath))
            {
                string path = Convert.ToString(gridCachePath).Trim();
                if (path.IndexOfAny(new[] { ';', '|' }) >= 0)
                    return "#ERR: GridCachePath cannot contain semicolons or pipes.";

                if (!string.Equals(path, defaults.GridCachePath, StringComparison.OrdinalIgnoreCase))
                    parts.Add("GridCachePath=" + path);
            }

            if (!Optional.IsEmpty(logLevel))
            {
                ProjLogLevel level;
                if (!Enum.TryParse(Convert.ToString(logLevel), true, out level) ||
                    !Enum.IsDefined(typeof(ProjLogLevel), level))
                    return "#ERR: LogLevel must be a valid PROJ log level.";

                if (level != defaults.LogLevel)
                    parts.Add("LogLevel=" + level);
            }

            return string.Join("; ", parts);
            }
            catch (ArgumentException ex)
            {
                return "#ERR: " + ex.Message;
            }
        }
    }
}
