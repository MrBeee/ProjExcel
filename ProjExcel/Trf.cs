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
    public static class TransformFunctions
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

        // Whereby IntermediateCrsUsage iis defined as

        //public enum class IntermediateCrsUsage
        //{
        //    Auto,
        //    Always,
        //    Never
        //};

        // AllowDeprecatedCrs is NOT PART OF THE ORIGINAL C++ Class; check validity of this property in the future.
        // For now, I will include it in the output string.
        private sealed class CoordinateTransformOptionsInput
        {
            public CoordinateArea Area { get; set; }
            public string Authority { get; set; }
            public double? Accuracy { get; set; }
            public bool NoBallparkConversions { get; set; }
            public bool NoDiscardIfMissing { get; set; }
            public bool UsePrimaryGridNames { get; set; }
            public bool UseSuperseded { get; set; }
            public bool AllowDeprecatedCrs { get; set; }
            public bool StrictContains { get; set; }
            public IntermediateCrsUsage IntermediateCrsUsage { get; set; }
            public int CandidateIndex { get; set; }
            public bool BestOnly { get; set; }
            public bool ForceOver { get; set; }

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
                    AllowDeprecatedCrs = false,
                    StrictContains = false,
                    BestOnly = false,
                    ForceOver = false,
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
                    else if (string.Equals(key, "AllowDeprecatedCrs", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "AllowDeprecatedCRS", StringComparison.OrdinalIgnoreCase))
                        values.AllowDeprecatedCrs = ParseBooleanOrDefault(value, false);
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
                    else if (string.Equals(key, "CandidateIndex", StringComparison.OrdinalIgnoreCase))
                        values.CandidateIndex = ParseIntOrDefault(value, 0);
                    else if (string.Equals(key, "Area", StringComparison.OrdinalIgnoreCase))
                        values.Area = ParseAreaOrDefault(value, values.Area);
                }

                return values;
            }

            //public CoordinateTransformOptions ToSharpProjOptions()
            //{
            //    var options = new CoordinateTransformOptions();
            //    options.Authority = string.IsNullOrWhiteSpace(Authority) ? "EPSG" : Authority;
            //    options.Accuracy = Accuracy;
            //    options.Area = Area;
            //    options.NoBallparkConversions = NoBallparkConversions;
            //    options.NoDiscardIfMissing = NoDiscardIfMissing;
            //    options.UsePrimaryGridNames = UsePrimaryGridNames;
            //    options.UseSuperseded = UseSuperseded;
            //    options.StrictContains = StrictContains;
            //    options.IntermediateCrsUsage = IntermediateCrsUsage;
            //    options.BestOnly = BestOnly;
            //    options.ForceOver = ForceOver;
            //    return options;
            //}
        }

        private static int GetNrCoordinateColumns(int nMode, int nDefault)
        {
            int nOut;

            switch (nMode & 7)
            {
                default:
                case 0:
                    nOut = nDefault;
                    break;
                case 1:
                    nOut = 4;
                    break;
                case 2:
                    nOut = 3;
                    break;
                case 3:
                    nOut = 2;
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    nOut = 1;
                    break;
            }

            return nOut;
        }

        private static bool ParseBooleanOrDefault(object value, bool defaultValue)
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

        private static int ParseIntOrDefault(object value, int defaultValue)
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

        private static string ParseIntermediateCrsUsageOrDefault(object value)
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

        private static double? ParseNullableDoubleOrDefault(object value, double? defaultValue)
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

        private sealed class TransformOptionValues
        {
            public bool NoBallparkConversions { get; set; }
            public bool NoDiscardIfMissing { get; set; }
            public bool UsePrimaryGridNames { get; set; }
            public bool UseSuperseded { get; set; }
            public bool AllowDeprecatedCrs { get; set; }
            public bool StrictContains { get; set; }
            public IntermediateCrsUsage IntermediateCrsUsage { get; set; }
            public int CandidateIndex { get; set; }
        }

        private static TransformOptionValues ParseTransformOptions(object oOptions)
        {
            var values = new TransformOptionValues
            {
                NoBallparkConversions = false,
                NoDiscardIfMissing = false,
                UsePrimaryGridNames = false,
                UseSuperseded = false,
                AllowDeprecatedCrs = false,
                StrictContains = false,
                IntermediateCrsUsage = IntermediateCrsUsage.Auto,
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

                if (string.Equals(key, "NoBallparkConversions", StringComparison.OrdinalIgnoreCase))
                    values.NoBallparkConversions = ParseBooleanOrDefault(value, false);
                else if (string.Equals(key, "NoDiscardIfMissing", StringComparison.OrdinalIgnoreCase))
                    values.NoDiscardIfMissing = ParseBooleanOrDefault(value, false);
                else if (string.Equals(key, "UsePrimaryGridNames", StringComparison.OrdinalIgnoreCase))
                    values.UsePrimaryGridNames = ParseBooleanOrDefault(value, false);
                else if (string.Equals(key, "UseSuperseded", StringComparison.OrdinalIgnoreCase))
                    values.UseSuperseded = ParseBooleanOrDefault(value, false);
                else if (string.Equals(key, "AllowDeprecatedCrs", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "AllowDeprecatedCRS", StringComparison.OrdinalIgnoreCase))
                    values.AllowDeprecatedCrs = ParseBooleanOrDefault(value, false);
                else if (string.Equals(key, "StrictContains", StringComparison.OrdinalIgnoreCase))
                    values.StrictContains = ParseBooleanOrDefault(value, false);
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
                else if (string.Equals(key, "CandidateIndex", StringComparison.OrdinalIgnoreCase))
                    values.CandidateIndex = ParseIntOrDefault(value, 0);
            }

            return values;
        }

        private static CoordinateTransformOptions CreateCoordinateTransformOptions(TransformOptionValues values, double accuracyArg, double westLongitude, double southLatitude, double eastLongitude, double northLatitude)
        {
            var options = new CoordinateTransformOptions();
            options.Authority = "EPSG";

            if (accuracyArg >= 0)
                options.Accuracy = accuracyArg;

            if (westLongitude >= -180 && westLongitude <= 180 &&
                eastLongitude >= -180 && eastLongitude <= 180 &&
                southLatitude >= -90 && southLatitude <= 90 &&
                northLatitude >= -90 && northLatitude <= 90 &&
                southLatitude <= northLatitude)
            {
                if (westLongitude != -1000 || southLatitude != -1000 || eastLongitude != -1000 || northLatitude != -1000)
                    options.Area = new CoordinateArea(westLongitude, southLatitude, eastLongitude, northLatitude);
            }

            options.NoBallparkConversions = values.NoBallparkConversions;
            options.NoDiscardIfMissing = values.NoDiscardIfMissing;
            options.UsePrimaryGridNames = values.UsePrimaryGridNames;
            options.UseSuperseded = values.UseSuperseded;
            options.StrictContains = values.StrictContains;
            options.IntermediateCrsUsage = values.IntermediateCrsUsage;

            return options;
        }

        private static CoordinateTransform CreateCoordinateTransform(
            CoordinateReferenceSystem crsSource,
            CoordinateReferenceSystem crsTarget,
            ProjContext pjContext)
        {
            return CreateCoordinateTransform(crsSource, crsTarget, new CoordinateTransformOptions(), pjContext, true); // true stands for allowDeprecatedCrs input
        }

        private static CoordinateTransform CreateCoordinateTransform(
            CoordinateReferenceSystem crsSource,
            CoordinateReferenceSystem crsTarget,
            ProjContext pjContext,
            bool allowDeprecatedCrs)    // allowDeprecatedCrs is not part of CoordinateTransformOptions, so we need to pass it separately
        {
            return CreateCoordinateTransform(crsSource, crsTarget, new CoordinateTransformOptions(), pjContext, allowDeprecatedCrs);
        }

        private static CoordinateTransform CreateCoordinateTransform(object[,] oTransform, ProjContext pjContext)
        {
            return CreateCoordinateTransform(oTransform, pjContext, true);  // true stands for allowDeprecatedCrs input
        }


        // the next function contains bugs; allowDeprecatedCrs is passed to CreateCoordinateTransform(crsSource, crsTarget, options, pjContext, allowDeprecatedCrs)
        // but not used in the function body
        private static CoordinateTransform CreateCoordinateTransform(object[,] oTransform, ProjContext pjContext, bool allowDeprecatedCrs)
        {
            if (oTransform == null)
                throw new ArgumentException("Incorrect coordinate transform format");

            int nTransformRows = oTransform.GetLength(0);
            int nTransformCols = oTransform.GetLength(1);

            if (nTransformRows != 1 || nTransformCols > 2)
                throw new ArgumentException("Incorrect coordinate transform format");

            int nTransform;
            string sTransform;

            if (nTransformCols == 1)
            {
                if (oTransform[0, 0] is double)
                {
                    nTransform = (int)(double)oTransform[0, 0];
                    return CoordinateTransform.CreateFromEpsg(nTransform, pjContext);
                }

                if (oTransform[0, 0] is string)
                {
                    sTransform = (string)oTransform[0, 0];

                    bool success = int.TryParse(sTransform, out nTransform);
                    if (success)
                        return CoordinateTransform.CreateFromEpsg(nTransform, pjContext);

                    if ((sTransform.IndexOf("PROJCRS", StringComparison.OrdinalIgnoreCase) > -1) ||
                        (sTransform.IndexOf("GEOGCRS", StringComparison.OrdinalIgnoreCase) > -1) ||
                        (sTransform.IndexOf("SPHEROID", StringComparison.OrdinalIgnoreCase) > -1))
                    {
                        return (CoordinateTransform)CoordinateTransform.CreateFromWellKnownText(sTransform, pjContext);
                    }

                    return CoordinateTransform.Create(sTransform, pjContext);
                }

                throw new ArgumentException("Incorrect coordinate transform format");
            }

            sTransform = Convert.ToString(oTransform[0, 0]);

            if (oTransform[0, 1] is double)
            {
                nTransform = (int)(double)oTransform[0, 1];
            }
            else if (oTransform[0, 1] is string)
            {
                string sTmp = (string)oTransform[0, 1];
                bool success = int.TryParse(sTmp, out nTransform);
                if (!success)
                    throw new ArgumentException("Incorrect coordinate transform format");
            }
            else
            {
                throw new ArgumentException("Incorrect coordinate transform format");
            }

            return CoordinateTransform.CreateFromDatabase(sTransform, nTransform, pjContext);
        }

        /// <summary>
        /// Converts WGS84 Lat/Lon (EPSG:4326) into Web Mercator X/Y meters (EPSG:3857).
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <returns>A 1x2 row array containing { X_Meters, Y_Meters }</returns>
        /// <example>
        /// In cell A1, enter a Latitude: 52.3676
        /// In cell B1, enter a Longitude: 4.9041
        /// In cell C1, type the formula: =PE.tst.TransformToWebMercator(A1, B1)
        /// Press Enter. Excel will automatically spill two values across cells C1 and D1:
        /// C1 (X in Meters):  ~545921.915
        /// D1 (Y in Meters): ~6866867.122
        /// </example>
        [ExcelFunction( Name = "PE.trf.LatLongToWebMercator", 
                        Description = "Converts Lat/Lon degrees (EPSG:4326) to Web Mercator meters (EPSG:3857) using SharpProj")]
        public static object TransformLatLongToWebMercator(
            [ExcelArgument(Name = "latitude", Description = "Latitude in decimal degrees (e.g. 52.3676)")] double latitude,
            [ExcelArgument(Name = "longitude", Description = "Longitude in decimal degrees (e.g. 4.9041)")] double longitude)
        {
            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var trf = CreateCoordinateTransform(src, dst, pc))
                    {
                        var r = trf.Apply(new PPoint(longitude, latitude));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.Options", Description = "Creates an options token for advanced transform settings")]
        public static string Options(
            [ExcelArgument(Name = "NoBallparkConversions", Description = "Disallow ballpark conversions (FALSE)")] object noBallparkConversions = null,
            [ExcelArgument(Name = "NoDiscardIfMissing", Description = "Allow transforms even when a grid is missing (FALSE)")] object noDiscardIfMissing = null,
            [ExcelArgument(Name = "UsePrimaryGridNames", Description = "Use primary grid names (FALSE)")] object usePrimaryGridNames = null,
            [ExcelArgument(Name = "UseSuperseded", Description = "Allow superseded transforms (FALSE)")] object useSuperseded = null,
            [ExcelArgument(Name = "AllowDeprecatedCrs", Description = "Allow deprecated CRSs (FALSE)")] object allowDeprecatedCrs = null,
            [ExcelArgument(Name = "StrictContains", Description = "Require transform to strictly contain the area of interest (FALSE)")] object strictContains = null,
            [ExcelArgument(Name = "IntermediateCrsUsage", Description = "Auto, Always or Never (Auto)")] object intermediateCrsUsage = null,
            [ExcelArgument(Name = "CandidateIndex", Description = "Transform candidate index (0)")] object candidateIndex = null)
        {
            return string.Join("; ",
                "NoBallparkConversions=" + ParseBooleanOrDefault(noBallparkConversions, false),
                "NoDiscardIfMissing=" + ParseBooleanOrDefault(noDiscardIfMissing, false),
                "UsePrimaryGridNames=" + ParseBooleanOrDefault(usePrimaryGridNames, false),
                "UseSuperseded=" + ParseBooleanOrDefault(useSuperseded, false),
                "AllowDeprecatedCrs=" + ParseBooleanOrDefault(allowDeprecatedCrs, false),
                "StrictContains=" + ParseBooleanOrDefault(strictContains, false),
                "IntermediateCrsUsage=" + ParseIntermediateCrsUsageOrDefault(intermediateCrsUsage),
                "CandidateIndex=" + ParseIntOrDefault(candidateIndex, 0));
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

        // Note: this function creates a textual representation in Excel of the C++ class CoordinateTransformOptions
        // This class has been defined here: D:\GitHub\SharpProj\src\SharpProj\CoordinateArea.h
        // CandidateIndex is not included in this class. So I need to check if it is needed in the future. For now, I will include it in the output string.
        [ExcelFunction(Name = "PE.trf.Options2", Description = "Creates an options token for CoordinateTransformOptionsInput settings")]
        public static string Options2(
            [ExcelArgument(Name = "Authority", Description = "Authority name (EPSG)")] object authority = null,
            [ExcelArgument(Name = "Accuracy", Description = "Desired accuracy (blank = none)")] object accuracy = null,
            [ExcelArgument(Name = "WestLongitude", Description = "West longitude of area of interest (blank = none)")] object westLongitude = null,
            [ExcelArgument(Name = "SouthLatitude", Description = "South latitude of area of interest (blank = none)")] object southLatitude = null,
            [ExcelArgument(Name = "EastLongitude", Description = "East longitude of area of interest (blank = none)")] object eastLongitude = null,
            [ExcelArgument(Name = "NorthLatitude", Description = "North latitude of area of interest (blank = none)")] object northLatitude = null,
            [ExcelArgument(Name = "NoBallparkConversions", Description = "Disallow ballpark conversions (FALSE)")] object noBallparkConversions = null,
            [ExcelArgument(Name = "NoDiscardIfMissing", Description = "Allow transforms even when a grid is missing (FALSE)")] object noDiscardIfMissing = null,
            [ExcelArgument(Name = "UsePrimaryGridNames", Description = "Use primary grid names (FALSE)")] object usePrimaryGridNames = null,
            [ExcelArgument(Name = "UseSuperseded", Description = "Allow superseded transforms (FALSE)")] object useSuperseded = null,
            [ExcelArgument(Name = "AllowDeprecatedCrs", Description = "Allow deprecated CRSs (FALSE)")] object allowDeprecatedCrs = null,
            [ExcelArgument(Name = "StrictContains", Description = "Require transform to strictly contain the area of interest (FALSE)")] object strictContains = null,
            [ExcelArgument(Name = "IntermediateCrsUsage", Description = "Auto, Always or Never (Auto)")] object intermediateCrsUsage = null,
            [ExcelArgument(Name = "CandidateIndex", Description = "Transform candidate index (0)")] object candidateIndex = null,
            [ExcelArgument(Name = "BestOnly", Description = "Use best transform only (FALSE)")] object bestOnly = null,
            [ExcelArgument(Name = "ForceOver", Description = "Force over-range longitude handling (FALSE)")] object forceOver = null)
        {
            string authorityValue = Convert.ToString(authority);
            if (string.IsNullOrWhiteSpace(authorityValue))
                authorityValue = "EPSG";

            double? accuracyValue = ParseNullableDoubleOrDefault(accuracy, null);
            string areaValue = BuildAreaToken(westLongitude, southLatitude, eastLongitude, northLatitude);

            return string.Join("; ",
                "Authority=" + authorityValue,
                "Accuracy=" + (accuracyValue.HasValue ? Convert.ToString(accuracyValue.Value) : string.Empty),
                "Area=" + areaValue,
                "NoBallparkConversions=" + ParseBooleanOrDefault(noBallparkConversions, false),
                "NoDiscardIfMissing=" + ParseBooleanOrDefault(noDiscardIfMissing, false),
                "UsePrimaryGridNames=" + ParseBooleanOrDefault(usePrimaryGridNames, false),
                "UseSuperseded=" + ParseBooleanOrDefault(useSuperseded, false),
                "AllowDeprecatedCrs=" + ParseBooleanOrDefault(allowDeprecatedCrs, false),
                "StrictContains=" + ParseBooleanOrDefault(strictContains, false),
                "IntermediateCrsUsage=" + ParseIntermediateCrsUsageOrDefault(intermediateCrsUsage),
                "CandidateIndex=" + ParseIntOrDefault(candidateIndex, 0),
                "BestOnly=" + ParseBooleanOrDefault(bestOnly, false),
                "ForceOver=" + ParseBooleanOrDefault(forceOver, false));
        }



        [ExcelFunction(Name = "PE.trf.ApplyForwardEx", Description = "Coordinate conversion with explicit advanced options")]
        public static object ApplyForwardEx(
            [ExcelArgument(Name = "sourceCrsOrTransform", Description = "Source CRS or transform definition")] object[,] SourceCrs,
            [ExcelArgument(Name = "targetCrsOrNul", Description = "Target CRS or blank when source is a transform")] object[,] TargetCrs,
            [ExcelArgument(Name = "Coordinate(s)", Description = "Input coordinate array")] double[,] Coords,
            [ExcelArgument(Name = "outputMode", Description = "(0) 0 = input width, 1 = xyzt, 2 = xyz, 3 = xy, 4 = x, 5 = y, 6 = z, 7 = t")] object oOutputMode,
            [ExcelArgument(Name = "Accuracy", Description = "Desired accuracy (-1)")] object oAccuracy,
            [ExcelArgument(Name = "options", Description = "Options token created by PE.trf.Options")] object oOptions,
            [ExcelArgument(Name = "WestLongitude", Description = "West longitude of area of interest (-1000)")] object oWestLongitude,
            [ExcelArgument(Name = "SouthLatitude", Description = "South latitude of area of interest (-1000)")] object oSouthLatitude,
            [ExcelArgument(Name = "EastLongitude", Description = "East longitude of area of interest (-1000)")] object oEastLongitude,
            [ExcelArgument(Name = "NorthLatitude", Description = "North latitude of area of interest (-1000)")] object oNorthLatitude
            )
        {
            return ApplyTransformEx(SourceCrs, TargetCrs, Coords, oOutputMode, oAccuracy, oOptions, oWestLongitude, oSouthLatitude, oEastLongitude, oNorthLatitude, false);
        }

        [ExcelFunction(Name = "PE.trf.ApplyInverseEx", Description = "Inverse coordinate conversion with explicit advanced options")]
        public static object ApplyInverseEx(
            [ExcelArgument(Name = "sourceCrsOrTransform", Description = "Source CRS or transform definition")] object[,] SourceCrs,
            [ExcelArgument(Name = "targetCrsOrNul", Description = "Target CRS or blank when source is a transform")] object[,] TargetCrs,
            [ExcelArgument(Name = "Coordinate(s)", Description = "Input coordinate array")] double[,] Coords,
            [ExcelArgument(Name = "outputMode", Description = "0 = input width, 1 = xyzt, 2 = xyz, 3 = xy, 4 = x, 5 = y, 6 = z, 7 = t")] object oOutputMode,
            [ExcelArgument(Name = "Accuracy", Description = "Desired accuracy (-1)")] object oAccuracy,
            [ExcelArgument(Name = "options", Description = "Options token created by PE.trf.Options")] object oOptions,
            [ExcelArgument(Name = "WestLongitude", Description = "West longitude of area of interest (-1000)")] object oWestLongitude,
            [ExcelArgument(Name = "SouthLatitude", Description = "South latitude of area of interest (-1000)")] object oSouthLatitude,
            [ExcelArgument(Name = "EastLongitude", Description = "East longitude of area of interest (-1000)")] object oEastLongitude,
            [ExcelArgument(Name = "NorthLatitude", Description = "North latitude of area of interest (-1000)")] object oNorthLatitude
            )
        {
            return ApplyTransformEx(SourceCrs, TargetCrs, Coords, oOutputMode, oOptions, oAccuracy, oWestLongitude, oSouthLatitude, oEastLongitude, oNorthLatitude, true);
        }


        private static bool HasAnyFiniteValues(double[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!double.IsNaN(values[i]) && !double.IsInfinity(values[i]))
                    return true;
            }

            return false;
        }

        private static object ApplyTransformEx(object[,] SourceCrs, object[,] TargetCrs, double[,] Coords, object oOutputMode, object oAccuracy, object oOptions, object oWestLongitude, object oSouthLatitude, object oEastLongitude, object oNorthLatitude, bool inverse)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int outputMode = Optional.Check(oOutputMode, 0);
            bool usingTransform = Optional.IsNul(TargetCrs);
            double accuracyArg = Optional.Check(oAccuracy, -1.0);
            double westLongitude = Optional.Check(oWestLongitude, -1000.0);
            double southLatitude = Optional.Check(oSouthLatitude, -1000.0);
            double eastLongitude = Optional.Check(oEastLongitude, -1000.0);
            double northLatitude = Optional.Check(oNorthLatitude, -1000.0);

            if (outputMode < 0 || outputMode > 7)
                return "#ERR: Invalid input.";

            int nCoordRows = Coords.GetLength(0);
            int nCoordCols = Coords.GetLength(1);

            if (nCoordRows < 1 || nCoordCols < 2 || nCoordCols > 4)
                return "#ERR: Invalid input.";

            int nOut = GetNrCoordinateColumns(outputMode, nCoordCols);
            TransformOptionValues values = ParseTransformOptions(oOptions);
            CoordinateTransformOptions options = CreateCoordinateTransformOptions(values, accuracyArg, westLongitude, southLatitude, eastLongitude, northLatitude);

            ProjContext pjContext = null;
            CoordinateReferenceSystem crsSource = null;
            CoordinateReferenceSystem crsTarget = null;
            CoordinateTransform transform = null;

            try
            {
                pjContext = CreateContext();

                if (usingTransform)
                    transform = CreateCoordinateTransform(SourceCrs, pjContext);
                else
                {
                    crsSource = Crs.CreateCrs(SourceCrs, pjContext);
                    crsTarget = Crs.CreateCrs(TargetCrs, pjContext);
                    transform = CreateCoordinateTransform(crsSource, crsTarget, options, pjContext, values.AllowDeprecatedCrs);
                }

                List<CoordinateTransform> candidates = GetCandidateTransforms(transform, options);
                if (candidates == null)
                    candidates = new List<CoordinateTransform>();

                CoordinateTransform axisOrderNormalization = null;
                List<CoordinateTransform> coreCandidates = new List<CoordinateTransform>();

                for (int i = 0; i < candidates.Count; i++)
                {
                    CoordinateTransform candidate = candidates[i];
                    if (candidate != null &&
                        !string.IsNullOrEmpty(candidate.Name) &&
                        candidate.Name.IndexOf("axis order change", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (axisOrderNormalization == null)
                            axisOrderNormalization = candidate;
                    }
                    else
                    {
                        coreCandidates.Add(candidate);
                    }
                }

                if (transform != null && coreCandidates.Count == 0)
                    coreCandidates.Add(transform);

                if (values.CandidateIndex > 0 && values.CandidateIndex <= coreCandidates.Count)
                {
                    CoordinateTransform preferred = coreCandidates[values.CandidateIndex - 1];
                    coreCandidates.RemoveAt(values.CandidateIndex - 1);
                    coreCandidates.Insert(0, preferred);
                }

                if (coreCandidates.Count == 0)
                    return "#ERR: No valid coordinate transform.";

                double[] x = new double[nCoordRows];
                double[] y = new double[nCoordRows];
                double[] z = new double[nCoordRows];
                double[] t = new double[nCoordRows];
                object[,] res = new object[nCoordRows, nOut];
                Exception lastException = null;

                foreach (CoordinateTransform candidate in coreCandidates)
                {
                    for (int i = 0; i < nCoordRows; i++)
                    {
                        x[i] = Coords[i, 0];
                        y[i] = Coords[i, 1];

                        if (nCoordCols > 2)
                            z[i] = Coords[i, 2];

                        if (nCoordCols > 3)
                            t[i] = Coords[i, 3];
                    }

                    try
                    {
                        switch (nCoordCols)
                        {
                            default:
                            case 2:
                                if (inverse)
                                    candidate.ApplyReversed(x, y);
                                else
                                    candidate.Apply(x, y);
                                break;

                            case 3:
                                if (inverse)
                                    candidate.ApplyReversed(x, y, z);
                                else
                                    candidate.Apply(x, y, z);
                                break;

                            case 4:
                                if (inverse)
                                    candidate.ApplyReversed(x, y, z, t);
                                else
                                    candidate.Apply(x, y, z, t);
                                break;
                        }

                        if (axisOrderNormalization != null)
                        {
                            if (inverse)
                                axisOrderNormalization.ApplyReversed(x, y);
                            else
                                axisOrderNormalization.Apply(x, y);
                        }

                        switch (outputMode & 7)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                if (nOut == 2)
                                {
                                    for (int i = 0; i < nCoordRows; i++)
                                    {
                                        res[i, 0] = Optional.CheckNan(x[i]);
                                        res[i, 1] = Optional.CheckNan(y[i]);
                                    }
                                }
                                else if (nOut == 3)
                                {
                                    for (int i = 0; i < nCoordRows; i++)
                                    {
                                        res[i, 0] = Optional.CheckNan(x[i]);
                                        res[i, 1] = Optional.CheckNan(y[i]);
                                        res[i, 2] = Optional.CheckNan(z[i]);
                                    }
                                }
                                else if (nOut == 4)
                                {
                                    for (int i = 0; i < nCoordRows; i++)
                                    {
                                        res[i, 0] = Optional.CheckNan(x[i]);
                                        res[i, 1] = Optional.CheckNan(y[i]);
                                        res[i, 2] = Optional.CheckNan(z[i]);
                                        res[i, 3] = Optional.CheckNan(t[i]);
                                    }
                                }
                                else
                                    return "#ERR: Invalid input.";
                                break;

                            case 4:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(x[i]);
                                break;

                            case 5:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(y[i]);
                                break;

                            case 6:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(z[i]);
                                break;

                            case 7:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(t[i]);
                                break;
                        }

                        return res;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }

                return lastException != null
                    ? AddIn.ProcessException(lastException)
                    : "#ERR: No valid coordinate transform.";
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
            finally
            {
                transform?.Dispose();
                crsSource?.Dispose();
                crsTarget?.Dispose();
                pjContext?.Dispose();
            }
        }

        /// <summary>
        /// Converts Web Mercator X/Y meters (EPSG:3857) into WGS84 Lat/Lon (EPSG:4326).
        /// </summary>
        /// </example>
        [ExcelFunction(Name = "PE.trf.WebMercatorToLatLong", Description = "Converts Web Mercator meters (EPSG:3857) to Lat/Lon degrees (EPSG:4326) using SharpProj")]
        public static object TransformWebMercatorToLatLong(
            [ExcelArgument(Name = "Easting", Description = "Easting in meters (e.g. 545,922")] double easting,
            [ExcelArgument(Name = "Northing", Description = "northing in meters (e.g. 6,866,867)")] double northing)
        {
            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var trf = CreateCoordinateTransform(src, dst, pc))
                    {
                        var r = trf.Apply(new PPoint(easting, northing));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        public static PPoint TransformPoint(int sourceEpsg, int targetEpsg, double x, double y, bool normalizeAxes = true)
        {
            using (var context = CreateContext())
            using (var source = CoordinateReferenceSystem.CreateFromEpsg(sourceEpsg, context))
            using (var target = CoordinateReferenceSystem.CreateFromEpsg(targetEpsg, context))
            using (var transform = CreateCoordinateTransform(
                normalizeAxes ? source.WithAxisNormalized() : source,
                normalizeAxes ? target.WithAxisNormalized() : target,
                context,
                normalizeAxes))
            {
                return transform.Apply(new PPoint(x, y));
            }
        }


        /// <summary>
        /// Converts coordinates between two EPSG CRSs.
        /// </summary>
        [ExcelFunction(Name = "PE.trf.FromEpsgNoToEpsgNo", Description = "Transforms X/Y coordinates from one EPSG CRS to another using SharpProj. Optionally selects a specific transform candidate.")]
        public static object FromEpsgNoToEpsgNo(
            [ExcelArgument(Name = "Source CRS", Description = "EPSG number of source CRS. Default = 4326")] object sourceCrs = null,
            [ExcelArgument(Name = "Target CRS", Description = "EPSG number of target CRS. Default = 3857")] object targetCrs = null,
            [ExcelArgument(Name = "X", Description = "Input X coordinate (for EPSG:4326 this is longitude)")] object x = null,
            [ExcelArgument(Name = "Y", Description = "Input Y coordinate (for EPSG:4326 this is latitude)")] object y = null,
            [ExcelArgument(Name = "Candidate Index", Description = "Optional transform candidate index. Leave blank for SharpProj default selection; use 0, 1, ... to force a specific candidate.")] object candidateIndex = null)
        {
            try
            {
                int actualSourceCrs = Optional.Check(sourceCrs, 4326);
                int actualTargetCrs = Optional.Check(targetCrs, 3857);

                double actualX;
                if (!ExcelValueConverters.TryGetDouble(x, out actualX))
                    return "#ERR: X must be supplied and numeric.";

                double actualY;
                if (!ExcelValueConverters.TryGetDouble(y, out actualY))
                    return "#ERR: Y must be supplied and numeric.";

                bool hasCandidateIndex = !Optional.IsMissing(candidateIndex);
                int actualCandidateIndex = hasCandidateIndex ? ExcelValueConverters.ToInt32(candidateIndex) : -1;

                if (hasCandidateIndex && actualCandidateIndex < 0)
                    return "#ERR: Candidate Index must be 0 or greater.";

                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst))
                    {
                        global::SharpProj.CoordinateTransform transformToApply = trf;

                        if (hasCandidateIndex)
                        {
                            string selectionError;
                            transformToApply = TryGetCandidateTransform(trf, actualCandidateIndex, out selectionError);
                            if (transformToApply == null)
                                return selectionError;
                        }

                        var r = transformToApply.Apply(new PPoint(actualX, actualY));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListTransformInfo", Description = "Lists diagnostic information about the SharpProj transform selected between two EPSG CRSs")]
        public static object[,] ListTransformInfo(
            [ExcelArgument(Name = "Source CRS", Description = "Optional EPSG number of source CRS. Default = 4326")] object sourceCrs = null,
            [ExcelArgument(Name = "Destination CRS", Description = "Optional EPSG number of destination CRS. Default = 3857")] object targetCrs = null)
        {
            try
            {
                int actualSourceCrs = Optional.Check(sourceCrs, 4326);
                int actualTargetCrs = Optional.Check(targetCrs, 3857);

                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst))
                    {
                        var rows = new List<KeyValuePair<string, string>>();

                        rows.Add(new KeyValuePair<string, string>("SourceCrs", actualSourceCrs.ToString()));
                        rows.Add(new KeyValuePair<string, string>("DestinationCrs", actualTargetCrs.ToString()));

                        AddTransformInfo(rows, "Transform", trf);
                        AddCandidateInfo(rows, trf);

                        return ToKeyValueResult(rows);
                    }
                }
            }
            catch (Exception ex)
            {
                return new object[,] { { "#ERR: " + ex.Message } };
            }
        }

        private static void AddTransformInfo(List<KeyValuePair<string, string>> rows, string prefix, object target)
        {
            rows.Add(new KeyValuePair<string, string>(prefix + ".RuntimeType", target.GetType().FullName));
            rows.Add(new KeyValuePair<string, string>(prefix + ".PublicProperties", string.Join(", ", GetPublicPropertyNames(target))));

            AddPropertyInfo(rows, target, prefix, "Name");
            AddPropertyInfo(rows, target, prefix, "Type");
            AddPropertyInfo(rows, target, prefix, "Identifier");
            AddPropertyInfo(rows, target, prefix, "Identifiers");
            AddPropertyInfo(rows, target, prefix, "Accuracy");
            AddPropertyInfo(rows, target, prefix, "Count");
            AddPropertyInfo(rows, target, prefix, "IsAvailable");
            AddPropertyInfo(rows, target, prefix, "HasBallParkTransformation");
            AddPropertyInfo(rows, target, prefix, "HasInverse");
            AddPropertyInfo(rows, target, prefix, "MethodName");
            AddPropertyInfo(rows, target, prefix, "Scope");
            AddPropertyInfo(rows, target, prefix, "Remarks");
            AddPropertyInfo(rows, target, prefix, "GridUsages");
            AddPropertyInfo(rows, target, prefix, "Parameters");
            AddPropertyInfo(rows, target, prefix, "SourceCRS");
            AddPropertyInfo(rows, target, prefix, "TargetCRS");
            AddPropertyInfo(rows, target, prefix, "Context");
            AddPropertyInfo(rows, target, prefix, "UsageArea");

            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "Name");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "WestLongitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "SouthLatitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "EastLongitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "NorthLatitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "Center");

            if (!(target is string))
                AddEnumerableInfo(rows, target, prefix);
        }

        private static IEnumerable<string> GetPublicPropertyNames(object value)
        {
            return value.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Select(p => p.Name)
                .OrderBy(p => p);
        }

        private static void AddPropertyInfo(List<KeyValuePair<string, string>> rows, object target, string prefix, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                return;

            object value;
            try
            {
                value = property.GetValue(target, null);
            }
            catch
            {
                return;
            }

            rows.Add(new KeyValuePair<string, string>(prefix + "." + propertyName, FormatValue(value)));
        }

        private static void AddNestedPropertyInfo(List<KeyValuePair<string, string>> rows, object target, string prefix, string propertyName, string nestedPropertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                return;

            object nestedTarget;
            try
            {
                nestedTarget = property.GetValue(target, null);
            }
            catch
            {
                return;
            }

            if (nestedTarget == null)
                return;

            PropertyInfo nestedProperty = nestedTarget.GetType().GetProperty(nestedPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (nestedProperty == null || !nestedProperty.CanRead || nestedProperty.GetIndexParameters().Length != 0)
                return;

            object nestedValue;
            try
            {
                nestedValue = nestedProperty.GetValue(nestedTarget, null);
            }
            catch
            {
                return;
            }

            rows.Add(new KeyValuePair<string, string>(prefix + "." + propertyName + "." + nestedPropertyName, FormatValue(nestedValue)));
        }

        private static void AddEnumerableInfo(List<KeyValuePair<string, string>> rows, object target, string prefix)
        {
            IEnumerable enumerable = target as IEnumerable;
            if (enumerable == null || target is string)
                return;

            var items = new List<string>();
            int count = 0;

            foreach (object item in enumerable)
            {
                count++;
                if (items.Count < 8)
                    items.Add(FormatValue(item));
            }

            rows.Add(new KeyValuePair<string, string>(prefix + ".EnumerableCount", count.ToString()));

            if (items.Count > 0)
                rows.Add(new KeyValuePair<string, string>(prefix + ".EnumerableItems", string.Join(" | ", items)));
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is string s)
                return s;

            if (value is IEnumerable enumerable)
            {
                var items = new List<string>();
                int count = 0;

                foreach (object item in enumerable)
                {
                    count++;
                    if (items.Count < 8)
                        items.Add(FormatSimpleValue(item));
                }

                if (count == 0)
                    return string.Empty;

                if (count > items.Count)
                    items.Add("...");

                return string.Join("; ", items);
            }

            return FormatSimpleValue(value);
        }

        private static string FormatSimpleValue(object value)
        {
            if (value == null)
                return string.Empty;

            string text;
            try
            {
                text = Convert.ToString(value);
            }
            catch
            {
                return value.GetType().FullName;
            }

            if (string.IsNullOrEmpty(text))
                return value.GetType().FullName;

            if (text == value.GetType().FullName || text == value.GetType().Name)
            {
                string described = DescribeObject(value);
                return string.IsNullOrEmpty(described) ? text : described;
            }

            return text;
        }

        private static string DescribeObject(object value)
        {
            var parts = new List<string>();

            foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead)
                    continue;

                object propertyValue;
                try
                {
                    propertyValue = property.GetValue(value, null);
                }
                catch
                {
                    continue;
                }

                if (propertyValue == null)
                    continue;

                if (!IsSimpleValue(propertyValue))
                    continue;

                parts.Add(property.Name + "=" + Convert.ToString(propertyValue));
                if (parts.Count >= 6)
                    break;
            }

            return parts.Count == 0 ? string.Empty : string.Join("; ", parts);
        }

        private static bool IsSimpleValue(object value)
        {
            Type type = value.GetType();

            if (type.IsEnum)
                return true;

            if (type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal) ||
                type == typeof(DateTime))
            {
                return true;
            }

            return false;
        }

        private static object[,] ToKeyValueResult(List<KeyValuePair<string, string>> rows)
        {
            object[,] result = new object[rows.Count, 2];

            for (int i = 0; i < rows.Count; i++)
            {
                result[i, 0] = rows[i].Key;
                result[i, 1] = rows[i].Value;
            }

            return result;
        }

        [ExcelFunction(Name = "PE.trf.ListTransformOptions", Description = "Lists selectable transform candidates between two EPSG CRSs, including heuristic pros and cons")]
        public static object[,] ListTransformOptions(
            [ExcelArgument(Name = "Source CRS", Description = "Optional EPSG number of source CRS. Default = 4326")] object sourceCrs = null,
            [ExcelArgument(Name = "Destination CRS", Description = "Optional EPSG number of destination CRS. Default = 3857")] object targetCrs = null)
        {
            try
            {
                int actualSourceCrs = Optional.Check(sourceCrs, 4326);
                int actualTargetCrs = Optional.Check(targetCrs, 3857);

                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst))
                    {
                        List<global::SharpProj.CoordinateTransform> candidates = GetCandidateTransforms(trf);
                        bool hasMultipleCandidates = trf is global::SharpProj.ChooseCoordinateTransform && candidates.Count > 1;

                        var rows = new List<object[]>()
                        {
                            new object[]
                            {
                                "CandidateIndex",
                                "DefaultSelection",
                                "Name",
                                "Type",
                                "Accuracy",
                                "IsAvailable",
                                "HasBallPark",
                                "UsageArea",
                                "Pros",
                                "Cons"
                            }
                        };

                        for (int i = 0; i < candidates.Count; i++)
                        {
                            global::SharpProj.CoordinateTransform candidate = candidates[i];
                            string usageAreaName = candidate.UsageArea == null ? string.Empty : candidate.UsageArea.Name;

                            rows.Add(new object[]
                            {
                                i,
                                hasMultipleCandidates ? (i == 0 ? "Yes" : "No") : "Only",
                                candidate.Name,
                                Convert.ToString(candidate.Type),
                                candidate.Accuracy,
                                candidate.IsAvailable,
                                candidate.HasBallParkTransformation,
                                usageAreaName,
                                BuildCandidatePros(candidate, i, hasMultipleCandidates),
                                BuildCandidateCons(candidate, i, hasMultipleCandidates)
                            });
                        }

                        return ToObjectTable(rows);
                    }
                }
            }
            catch (Exception ex)
            {
                return new object[,] { { "#ERR: " + ex.Message } };
            }
        }

        private sealed class PipelineCandidate
        {
            public int Index { get; set; }
            public object Pipeline { get; set; }
            public string Name { get; set; }
            public double? Accuracy { get; set; }
            public bool IsAvailable { get; set; }
            public bool HasBallpark { get; set; }
            public bool IsSuperseded { get; set; }
            public CoordinateArea UsageArea { get; set; }
        }

        private static IEnumerable<object> GetPipelineObjects(CoordinateTransform transform)
        {
            if (transform == null)
                yield break;

            PropertyInfo pipelinesProperty = transform.GetType().GetProperty("Pipelines", BindingFlags.Instance | BindingFlags.Public);
            if (pipelinesProperty != null)
            {
                object pipelinesValue = pipelinesProperty.GetValue(transform, null);
                IEnumerable pipelinesEnumerable = pipelinesValue as IEnumerable;
                if (pipelinesEnumerable != null)
                {
                    foreach (object item in pipelinesEnumerable)
                    {
                        if (item != null)
                            yield return item;
                    }
                    yield break;
                }
            }

            IEnumerable enumerable = transform as IEnumerable;
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
                yield break;
            }

            yield return transform;
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null)
                return null;

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                return null;

            try
            {
                return property.GetValue(target, null);
            }
            catch
            {
                return null;
            }
        }

        private static string GetStringProperty(object target, string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            return value == null ? null : Convert.ToString(value);
        }

        private static bool GetBoolProperty(object target, string propertyName, bool defaultValue)
        {
            object value = GetPropertyValue(target, propertyName);
            if (value == null)
                return defaultValue;

            if (value is bool)
                return (bool)value;

            bool parsed;
            if (bool.TryParse(Convert.ToString(value), out parsed))
                return parsed;

            return defaultValue;
        }

        private static double? GetNullableDoubleProperty(object target, string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            if (value == null)
                return null;

            if (value is double)
                return (double)value;

            if (value is float)
                return Convert.ToDouble(value);

            if (value is decimal)
                return Convert.ToDouble(value);

            if (value is int)
                return (int)value;

            double parsed;
            if (double.TryParse(Convert.ToString(value), out parsed))
                return parsed;

            return null;
        }

        private static CoordinateArea GetCoordinateAreaProperty(object target, string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            if (value == null)
                return null;

            CoordinateArea area = value as CoordinateArea;
            if (area != null)
                return area;

            object west = GetPropertyValue(value, "WestLongitude");
            object south = GetPropertyValue(value, "SouthLatitude");
            object east = GetPropertyValue(value, "EastLongitude");
            object north = GetPropertyValue(value, "NorthLatitude");

            double westValue;
            double southValue;
            double eastValue;
            double northValue;

            if (west != null && south != null && east != null && north != null &&
                double.TryParse(Convert.ToString(west), out westValue) &&
                double.TryParse(Convert.ToString(south), out southValue) &&
                double.TryParse(Convert.ToString(east), out eastValue) &&
                double.TryParse(Convert.ToString(north), out northValue))
            {
                return new CoordinateArea(westValue, southValue, eastValue, northValue);
            }

            return null;
        }

        private static List<PipelineCandidate> GetPipelineCandidates(CoordinateTransform transform)
        {
            var candidates = new List<PipelineCandidate>();
            int index = 0;

            foreach (object pipeline in GetPipelineObjects(transform))
            {
                candidates.Add(new PipelineCandidate
                {
                    Index = index++,
                    Pipeline = pipeline,
                    Name = GetStringProperty(pipeline, "Name") ?? "<unnamed>",
                    Accuracy = GetNullableDoubleProperty(pipeline, "Accuracy"),
                    IsAvailable = GetBoolProperty(pipeline, "IsAvailable", true),
                    HasBallpark = GetBoolProperty(pipeline, "HasBallParkTransformation", false) || GetBoolProperty(pipeline, "HasBallparkTransformation", false),
                    IsSuperseded = GetBoolProperty(pipeline, "IsSuperseded", false),
                    UsageArea = GetCoordinateAreaProperty(pipeline, "UsageArea")
                });
            }

            if (candidates.Count == 0 && transform != null)
            {
                candidates.Add(new PipelineCandidate
                {
                    Index = 0,
                    Pipeline = transform,
                    Name = transform.Name,
                    Accuracy = transform.Accuracy,
                    IsAvailable = transform.IsAvailable,
                    HasBallpark = transform.HasBallParkTransformation,
                    IsSuperseded = false,
                    UsageArea = transform.UsageArea
                });
            }

            return candidates;
        }

        private static bool AreaContains(CoordinateArea outer, CoordinateArea inner)
        {
            return outer.WestLongitude <= inner.WestLongitude &&
                   outer.EastLongitude >= inner.EastLongitude &&
                   outer.SouthLatitude <= inner.SouthLatitude &&
                   outer.NorthLatitude >= inner.NorthLatitude;
        }

        private static bool AreaIntersects(CoordinateArea a, CoordinateArea b)
        {
            return a.WestLongitude <= b.EastLongitude &&
                   a.EastLongitude >= b.WestLongitude &&
                   a.SouthLatitude <= b.NorthLatitude &&
                   a.NorthLatitude >= b.SouthLatitude;
        }

        private static int GetAreaScore(CoordinateArea candidateArea, CoordinateArea requestedArea, bool strictContains)
        {
            if (requestedArea == null)
                return 0;

            if (candidateArea == null)
                return strictContains ? -1 : 0;

            if (AreaContains(candidateArea, requestedArea))
                return 2;

            if (strictContains)
                return -1;

            return AreaIntersects(candidateArea, requestedArea) ? 1 : 0;
        }

        private static int GetAccuracyScore(double? candidateAccuracy, double? requestedAccuracy)
        {
            if (!requestedAccuracy.HasValue || requestedAccuracy.Value < 0)
                return 0;

            if (!candidateAccuracy.HasValue)
                return 1;

            return candidateAccuracy.Value <= requestedAccuracy.Value ? 0 : 1;
        }

        private static List<PipelineCandidate> RankPipelineCandidates(IEnumerable<PipelineCandidate> candidates, CoordinateTransformOptions options)
        {
            var list = candidates.Where(c => c != null).ToList();
            if (list.Count == 0)
                return list;

            if (options == null)
                options = new CoordinateTransformOptions();

            if (!options.NoDiscardIfMissing)
            {
                var available = list.Where(c => c.IsAvailable).ToList();
                if (available.Count > 0)
                    list = available;
            }

            if (!options.UseSuperseded)
            {
                var preferred = list.Where(c => !c.IsSuperseded).ToList();
                if (preferred.Count > 0)
                    list = preferred;
            }

            if (options.NoBallparkConversions)
            {
                var nonBallpark = list.Where(c => !c.HasBallpark).ToList();
                if (nonBallpark.Count > 0)
                    list = nonBallpark;
            }

            return list
                .Select(c => new
                {
                    Candidate = c,
                    AreaScore = GetAreaScore(c.UsageArea, options.Area, options.StrictContains),
                    AccuracyScore = GetAccuracyScore(c.Accuracy, options.Accuracy),
                    AccuracyValue = c.Accuracy ?? double.MaxValue
                })
                .Where(x => x.AreaScore >= 0)
                .OrderByDescending(x => x.Candidate.IsAvailable)
                .ThenByDescending(x => x.AreaScore)
                .ThenBy(x => x.AccuracyScore)
                .ThenBy(x => x.AccuracyValue)
                .ThenBy(x => x.Candidate.HasBallpark)
                .ThenBy(x => x.Candidate.Index)
                .Select(x => x.Candidate)
                .ToList();
        }

        private static CoordinateTransform CreateTransformFromPipeline(object pipeline)
        {
            if (pipeline == null)
                return null;

            CoordinateTransform direct = pipeline as CoordinateTransform;
            if (direct != null)
                return direct;

            MethodInfo createTransformMethod = pipeline.GetType().GetMethod(
                "CreateTransform",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (createTransformMethod != null)
            {
                object value = createTransformMethod.Invoke(pipeline, null);
                return value as CoordinateTransform;
            }

            return null;
        }

        private static CoordinateTransform CreateCoordinateTransform(CoordinateReferenceSystem crsSource, CoordinateReferenceSystem crsTarget, CoordinateTransformOptions options, ProjContext pjContext, bool allowDeprecatedCrs)
        {
            bool hasDeprecatedCrs = crsSource.IsDeprecated || crsTarget.IsDeprecated;

            if (hasDeprecatedCrs && !allowDeprecatedCrs)
                throw new ArgumentException("Using deprecated CRS when not allowed");

            CoordinateTransform chooser = CoordinateTransform.Create(crsSource.WithAxisNormalized(), crsTarget.WithAxisNormalized(), pjContext);
            List<CoordinateTransform> candidates = GetCandidateTransforms(chooser, options);

            if (candidates.Count == 0)
                throw new ArgumentException("No transformation available");

            return candidates[0];
        }

        private static CoordinateTransform GetSelectedTransform(CoordinateTransform transform, int candidateIndex)
        {
            List<CoordinateTransform> candidates = GetCandidateTransforms(transform);

            if (candidateIndex < 0 || candidateIndex >= candidates.Count)
                throw new ArgumentException("Transform candidate is not available");

            return candidates[candidateIndex];
        }

        private static CoordinateTransform TryGetCandidateTransform(
            CoordinateTransform transform,
            int candidateIndex,
            out string errorMessage)
        {
            errorMessage = null;

            List<CoordinateTransform> candidates = GetCandidateTransforms(transform);
            if (candidateIndex < 0 || candidateIndex >= candidates.Count)
            {
                errorMessage = "#ERR: Candidate Index out of range.";
                return null;
            }

            return candidates[candidateIndex];
        }

        private static List<CoordinateTransform> GetCandidateTransforms(CoordinateTransform transform)
        {
            return GetCandidateTransforms(transform, new CoordinateTransformOptions());
        }

        private static List<CoordinateTransform> GetCandidateTransforms(CoordinateTransform transform, CoordinateTransformOptions options)
        {
            var ranked = RankPipelineCandidates(GetPipelineCandidates(transform), options);
            var result = new List<CoordinateTransform>();

            foreach (var candidate in ranked)
            {
                CoordinateTransform actual = CreateTransformFromPipeline(candidate.Pipeline);
                if (actual != null)
                    result.Add(actual);
            }

            if (result.Count == 0 && transform != null)
                result.Add(transform);

            return result;
        }

        private static void AddCandidateInfo(List<KeyValuePair<string, string>> rows, object target)
        {
            CoordinateTransform transform = target as CoordinateTransform;
            if (transform == null)
                return;

            List<CoordinateTransform> candidates = GetCandidateTransforms(transform);

            for (int index = 0; index < candidates.Count; index++)
            {
                AddTransformInfo(rows, "Candidate[" + index + "]", candidates[index]);
            }
        }

        private static string BuildCandidatePros(CoordinateTransform candidate, int index, bool hasMultipleCandidates)
        {
            var pros = new List<string>();

            if (candidate == null)
                return string.Empty;

            if (candidate.IsAvailable)
                pros.Add("Available");

            if (!candidate.HasBallParkTransformation)
                pros.Add("Non-ballpark");

            if (candidate.Accuracy.HasValue)
                pros.Add("Accuracy=" + candidate.Accuracy.Value.ToString("0.###"));

            if (candidate.UsageArea != null)
                pros.Add("Has usage area");

            if (!hasMultipleCandidates || index == 0)
                pros.Add("Default selection");

            return string.Join("; ", pros);
        }

        private static string BuildCandidateCons(CoordinateTransform candidate, int index, bool hasMultipleCandidates)
        {
            var cons = new List<string>();

            if (candidate == null)
                return string.Empty;

            if (!candidate.IsAvailable)
                cons.Add("Not available");

            if (candidate.HasBallParkTransformation)
                cons.Add("Ballpark transformation");

            if (!candidate.Accuracy.HasValue)
                cons.Add("No reported accuracy");

            if (hasMultipleCandidates && index > 0)
                cons.Add("Alternate candidate");

            return string.Join("; ", cons);
        }

        private static object[,] ToObjectTable(List<object[]> rows)
        {
            if (rows == null || rows.Count == 0)
                return new object[0, 0];

            int columnCount = rows.Max(r => r == null ? 0 : r.Length);
            object[,] result = new object[rows.Count, columnCount];

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                object[] row = rows[rowIndex] ?? Array.Empty<object>();
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    result[rowIndex, columnIndex] = columnIndex < row.Length ? row[columnIndex] : null;
                }
            }

            return result;
        }
    }
}
