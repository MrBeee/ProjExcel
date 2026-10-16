using ExcelDna.Integration;
using MathNet.Numerics.IntegralTransforms;
using SharpProj;
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

            public CoordinateTransformOptionsInput()
            {
                Authority = "EPSG";
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

                if (oOptions == null || oOptions == DBNull.Value || oOptions == Type.Missing)
                    return values;

                object raw = oOptions;
                object[,] matrix = raw as object[,];
                if (matrix != null && matrix.Length == 1)
                    raw = matrix[0, 0];

                string token = Convert.ToString(raw);
                if (string.IsNullOrWhiteSpace(token))
                    return values;

                string[] parts = token.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    string[] keyValue = part.Split(new[] { '=' }, 2);
                    if (keyValue.Length != 2)
                        continue;

                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    if (string.Equals(key, "Authority", StringComparison.OrdinalIgnoreCase))
                        values.Authority = string.IsNullOrWhiteSpace(value) ? "EPSG" : value;
                    else if (string.Equals(key, "Accuracy", StringComparison.OrdinalIgnoreCase))
                        values.Accuracy = ParseNullableDoubleOrDefault(value, null);
                    else if (string.Equals(key, "NoBallparkConversions", StringComparison.OrdinalIgnoreCase))
                        values.NoBallparkConversions = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "NoDiscardIfMissing", StringComparison.OrdinalIgnoreCase))
                        values.NoDiscardIfMissing = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "UsePrimaryGridNames", StringComparison.OrdinalIgnoreCase))
                        values.UsePrimaryGridNames = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "UseSuperseded", StringComparison.OrdinalIgnoreCase))
                        values.UseSuperseded = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "StrictContains", StringComparison.OrdinalIgnoreCase))
                        values.StrictContains = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "BestOnly", StringComparison.OrdinalIgnoreCase))
                        values.BestOnly = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "ForceOver", StringComparison.OrdinalIgnoreCase))
                        values.ForceOver = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "IntermediateCrsUsage", StringComparison.OrdinalIgnoreCase))
                    {
                        string usage = ParseIntermediateCrsUsageOrDefault(value);
                        if (string.Equals(usage, "Always", StringComparison.OrdinalIgnoreCase))
                            values.IntermediateCrsUsage = IntermediateCrsUsage.Always;
                        else if (string.Equals(usage, "Never", StringComparison.OrdinalIgnoreCase))
                            values.IntermediateCrsUsage = IntermediateCrsUsage.Never;
                        else
                            values.IntermediateCrsUsage = IntermediateCrsUsage.Auto;
                    }
                    else if (string.Equals(key, "Area", StringComparison.OrdinalIgnoreCase))
                        values.Area = ParseAreaOrDefault(value, values.Area);

                    else if (string.Equals(key, "AllowDeprecatedCrs", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "AllowDeprecatedCRS", StringComparison.OrdinalIgnoreCase))
                        values.AllowDeprecatedCrs = ParseBooleanOrDefault(value, false);
                    else if (string.Equals(key, "CandidateIndex", StringComparison.OrdinalIgnoreCase))
                    {
                        int candidateIndex = ParseIntOrDefault(value, 0);
                        values.CandidateIndex = Math.Max(candidateIndex, 0);
                    }
                }
                return values;
            }
        }

        // this function returns the SharpProj CoordinateTransformOptions object, generated through an Excel UDF
        // note that AllowDeprecatedCrs and CandidateIndex are not carried across
        internal static CoordinateTransformOptions CreateCoordinateTransformOptions(CoordinateTransformOptionsInput values)
        {
            var options = new CoordinateTransformOptions();

            if (values == null)
            {
                options.Authority = "EPSG";
                return options;
            }

            options.Authority = string.IsNullOrWhiteSpace(values.Authority) ? "EPSG" : values.Authority;

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

        internal static bool ParseBooleanOrDefault(object value, bool defaultValue)
        {
            if (value == null || value == DBNull.Value || value == Type.Missing)
                return defaultValue;

            if (value is bool)
                return (bool)value;

            if (value is byte)
                return (byte)value != 0;

            if (value is sbyte)
                return (sbyte)value != 0;

            if (value is short)
                return (short)value != 0;

            if (value is ushort)
                return (ushort)value != 0;

            if (value is int)
                return (int)value != 0;

            if (value is uint)
                return (uint)value != 0;

            if (value is long)
                return (long)value != 0;

            if (value is ulong)
                return (ulong)value != 0;

            if (value is float)
                return Math.Abs((float)value) > 0f;

            if (value is double)
                return Math.Abs((double)value) > 0d;

            if (value is decimal)
                return ((decimal)value) != 0m;

            string text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            bool parsedBool;
            if (bool.TryParse(text, out parsedBool))
                return parsedBool;

            text = text.Trim();
            if (string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(text, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "n", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            double parsedNumber;
            if (double.TryParse(text, out parsedNumber))
                return parsedNumber != 0d;

            return defaultValue;
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

            string[] parts = text.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
                return defaultValue;

            double westValue;
            double southValue;
            double eastValue;
            double northValue;

            if (double.TryParse(parts[0].Trim(), out westValue) &&
                double.TryParse(parts[1].Trim(), out southValue) &&
                double.TryParse(parts[2].Trim(), out eastValue) &&
                double.TryParse(parts[3].Trim(), out northValue))
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

        [ExcelFunction(Name = "PE.trf.Options", Description = "Creates an options token for CoordinateTransformOptions settings")]
        public static string Options2(
            [ExcelArgument(Name = "Authority", Description = "Authority name (EPSG)")] object authority = null,

            [ExcelArgument(Name = "WestLongitude", Description = "West longitude of area of interest (blank = none)")] object westLongitude = null,
            [ExcelArgument(Name = "SouthLatitude", Description = "South latitude of area of interest (blank = none)")] object southLatitude = null,
            [ExcelArgument(Name = "EastLongitude", Description = "East longitude of area of interest (blank = none)")] object eastLongitude = null,
            [ExcelArgument(Name = "NorthLatitude", Description = "North latitude of area of interest (blank = none)")] object northLatitude = null,

            [ExcelArgument(Name = "Accuracy", Description = "Desired accuracy (blank = none)")] object accuracy = null,
            [ExcelArgument(Name = "NoBallparkConversions", Description = "Disallow ballpark conversions (FALSE)")] object noBallparkConversions = null,
            [ExcelArgument(Name = "NoDiscardIfMissing", Description = "Allow transforms even when a grid is missing (FALSE)")] object noDiscardIfMissing = null,
            [ExcelArgument(Name = "UsePrimaryGridNames", Description = "Use primary grid names (FALSE)")] object usePrimaryGridNames = null,
            [ExcelArgument(Name = "UseSuperseded", Description = "Allow superseded transforms (FALSE)")] object useSuperseded = null,
            [ExcelArgument(Name = "StrictContains", Description = "Require transform to strictly contain the area of interest (FALSE)")] object strictContains = null,
            [ExcelArgument(Name = "IntermediateCrsUsage", Description = "Auto, Always or Never (Auto)")] object intermediateCrsUsage = null,
            [ExcelArgument(Name = "BestOnly", Description = "Use best transform only (FALSE)")] object bestOnly = null,
            [ExcelArgument(Name = "ForceOver", Description = "Force over-range longitude handling (FALSE)")] object forceOver = null,

            [ExcelArgument(Name = "CandidateIndex", Description = "Transform candidate index (0)")] object candidateIndex = null,
            [ExcelArgument(Name = "AllowDeprecatedCrs", Description = "Allow deprecated CRSs (FALSE)")] object allowDeprecatedCrs = null
            )
        {
            string authorityValue = Convert.ToString(authority);
            if (string.IsNullOrWhiteSpace(authorityValue))
                authorityValue = "EPSG";

            double? accuracyValue = ParseNullableDoubleOrDefault(accuracy, null);
            string areaValue = BuildAreaToken(westLongitude, southLatitude, eastLongitude, northLatitude);

            return string.Join("; ",
                "Authority=" + authorityValue,

                "Area=" + areaValue,

                "Accuracy=" + (accuracyValue.HasValue ? Convert.ToString(accuracyValue.Value) : string.Empty),
                "NoBallparkConversions=" + ParseBooleanOrDefault(noBallparkConversions, false),
                "NoDiscardIfMissing=" + ParseBooleanOrDefault(noDiscardIfMissing, false),
                "UsePrimaryGridNames=" + ParseBooleanOrDefault(usePrimaryGridNames, false),
                "UseSuperseded=" + ParseBooleanOrDefault(useSuperseded, false),
                "StrictContains=" + ParseBooleanOrDefault(strictContains, false),
                "IntermediateCrsUsage=" + ParseIntermediateCrsUsageOrDefault(intermediateCrsUsage),
                "BestOnly=" + ParseBooleanOrDefault(bestOnly, false),
                "ForceOver=" + ParseBooleanOrDefault(forceOver, false),

                "CandidateIndex=" + ParseIntOrDefault(candidateIndex, 0),
                "AllowDeprecatedCrs=" + ParseBooleanOrDefault(allowDeprecatedCrs, false)
                );
        }
    }
}
