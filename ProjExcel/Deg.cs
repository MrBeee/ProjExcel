using System;
using System.Globalization;
using System.Text;
using ExcelDna.Integration;

namespace ProjExcel
{
    public static class Deg
    {
        internal const double PI = 3.1415926535897932384626433832795;
        internal const string doubleDot = "Only the right most part can be a fractional number";

        private static int Hours(double degrees)
        {
            return (int)Math.Truncate(degrees);
        }

        private static int Minutes(double degrees)
        {
            return (int)Math.Abs(Math.Truncate((degrees - Hours(degrees)) * 60.0));
        }

        private static double DecimalMinutes(double degrees)
        {
            return Math.Abs(degrees - Math.Truncate(degrees)) * 60.0;
        }

        private static double Seconds(double degrees)
        {
            return (Math.Abs(degrees - Hours(degrees)) * 60.0 - Minutes(degrees)) * 60.0;
        }

        private static double ToDecimalDegrees(int hours, int minutes, double seconds)
        {
            return hours < 0
                ? -(-hours + minutes / 60.0 + seconds / 3600.0)
                : (hours + minutes / 60.0 + seconds / 3600.0);
        }

        private static double ToDecimalDegrees(int hours, double decimalMinutes)
        {
            return hours < 0
                ? -(-hours + decimalMinutes / 60.0)
                : (hours + decimalMinutes / 60.0);
        }

        private static bool ContainsDecimalSeparator(string value, CultureInfo culture)
        {
            return value.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) >= 0;
        }

        [ExcelFunction(Name = "PE.deg.AsString", Description = "Writes an angle as a Degree-Minute-Seconds or Decimal-Degrees string")]
        public static object AsString(
            [ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle,
            [ExcelArgument("Optional direction indicator (0). 0 = sign, 1 = E/W appended, 2 = N/S appended, 3 = E/W prepended, 4 = N/S prepended", Name = "direction")] object oDirection,
            [ExcelArgument(Name = "format", Description = "Optional format string")] object oFormat)
        {
            int direction = (int)Optional.Check(oDirection, 0.0);
            direction = Math.Min(Math.Max(direction, 0), 4);

            string format = Optional.Check(oFormat, "dd mm ss");
            bool negative = false;

            if (angle < 0 && direction > 0)
            {
                angle = Math.Abs(angle);
                negative = true;
            }

            CultureInfo culture = CultureInfo.CurrentCulture;
            string subFormat;
            string newFormat;
            bool isDecimalHandled = false;

            try
            {
                format = format.ToUpper(CultureInfo.InvariantCulture).Replace("H", "D");

                if (format.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) !=
                    format.LastIndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal))
                    throw new ArgumentException("Only one fractional number is allowed in the string representation");

                int startChar = format.IndexOf("D", StringComparison.Ordinal);
                int endChar;
                if (startChar > -1)
                {
                    endChar = format.LastIndexOf("D", StringComparison.Ordinal);
                    subFormat = format.Substring(startChar, endChar - startChar + 1);
                    newFormat = subFormat.Replace("D", "0");
                    if (newFormat.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) > -1)
                    {
                        isDecimalHandled = true;
                        format = format.Replace(subFormat, angle.ToString(newFormat, culture));
                    }
                    else
                    {
                        format = format.Replace(subFormat, Hours(angle).ToString(newFormat, culture));
                    }
                }

                startChar = format.IndexOf("M", StringComparison.Ordinal);
                if (startChar > -1)
                {
                    endChar = format.LastIndexOf("M", StringComparison.Ordinal);
                    subFormat = format.Substring(startChar, endChar - startChar + 1);
                    newFormat = subFormat.Replace("M", "0");
                    if (newFormat.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) > -1)
                    {
                        if (isDecimalHandled)
                        {
                            throw new ArgumentException(doubleDot, "format");
                        }
                        isDecimalHandled = true;
                        format = format.Replace(subFormat, DecimalMinutes(angle).ToString(newFormat, culture));
                    }
                    else
                    {
                        format = format.Replace(subFormat, Minutes(angle).ToString(newFormat, culture));
                    }
                }

                startChar = format.IndexOf("S", StringComparison.Ordinal);
                if (startChar > -1)
                {
                    endChar = format.LastIndexOf("S", StringComparison.Ordinal);
                    subFormat = format.Substring(startChar, endChar - startChar + 1);
                    newFormat = subFormat.Replace("S", "0");
                    if (newFormat.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) > -1)
                    {
                        if (isDecimalHandled)
                        {
                            throw new ArgumentException(doubleDot, "format");
                        }
                        format = format.Replace(subFormat, Seconds(angle).ToString(newFormat, culture));
                    }
                    else
                    {
                        format = format.Replace(subFormat, Seconds(angle).ToString(newFormat, culture));
                    }
                }

                if (string.Compare(format, "°", true, culture) == 0)
                {
                    format = "0°";
                }

                switch (direction)
                {
                    default:
                        throw new ArgumentException("Wrong direction value");
                    case 0:
                        return format;
                    case 1:
                        return negative ? format + "W" : format + "E";
                    case 2:
                        return negative ? format + "S" : format + "N";
                    case 3:
                        return negative ? "W" + format : "E" + format;
                    case 4:
                        return negative ? "S" + format : "N" + format;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(Name = "PE.deg.FromString", Description = "Reads an angle from a string")]
        public static object FromString(
            [ExcelArgument(Name = "angleString", Description = "Angle string in DMS, DDM or decimal degrees")] string dmsAngle)
        {
            if (string.IsNullOrEmpty(dmsAngle))
            {
                return 0.0;
            }

            CultureInfo culture = CultureInfo.CurrentCulture;
            double angle = 0;
            double polarity = 1;

            try
            {
                string normalizedAngle = dmsAngle.Trim().ToUpperInvariant();

                if (normalizedAngle.IndexOf('W') >= 0 || normalizedAngle.IndexOf('S') >= 0)
                    polarity = -1;

                StringBuilder newValue = new StringBuilder(normalizedAngle);
                newValue.Replace("E", " ").Replace("W", " ").Replace("S", " ").Replace("N", " ");
                newValue.Replace("°", " ").Replace("'", " ").Replace("\"", " ").Replace("  ", " ");

                string[] values = newValue.ToString().Trim().Split(' ');

                switch (values.Length)
                {
                    case 0:
                        return angle;

                    case 1:
                        if (string.IsNullOrWhiteSpace(values[0]))
                        {
                            return angle;
                        }

                        if (values[0].Length == 7 && !ContainsDecimalSeparator(values[0], culture))
                        {
                            angle = ToDecimalDegrees(
                                int.Parse(values[0].Substring(0, 3), culture),
                                int.Parse(values[0].Substring(3, 2), culture),
                                double.Parse(values[0].Substring(5, 2), culture));
                            return angle * polarity;
                        }

                        if (values[0].Length == 8 && values[0][0] == '-' && !ContainsDecimalSeparator(values[0], culture))
                        {
                            angle = ToDecimalDegrees(
                                int.Parse(values[0].Substring(0, 4), culture),
                                int.Parse(values[0].Substring(4, 2), culture),
                                double.Parse(values[0].Substring(6, 2), culture));
                            return angle * polarity;
                        }

                        angle = double.Parse(values[0], culture);
                        return angle * polarity;

                    case 2:
                        if (ContainsDecimalSeparator(values[0], culture))
                        {
                            throw new ArgumentException(doubleDot, "angleString");
                        }

                        angle = ToDecimalDegrees(
                            int.Parse(values[0], culture),
                            double.Parse(values[1], culture));
                        return angle * polarity;

                    default:
                        if (ContainsDecimalSeparator(values[0], culture) || ContainsDecimalSeparator(values[1], culture))
                        {
                            throw new ArgumentException(doubleDot, "angleString");
                        }

                        angle = ToDecimalDegrees(
                            int.Parse(values[0], culture),
                            int.Parse(values[1], culture),
                            double.Parse(values[2], culture));
                        return angle * polarity;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        }

        [ExcelFunction(Name = "PE.deg.FromDmsValues", Description = "Combines degrees, minutes and seconds into decimal degrees")]
        public static double FromDmsValues(
            [ExcelArgument(Name = "Hours", Description = "Degree value")] object oHours,
            [ExcelArgument(Name = "Minutes", Description = "Minute value")] object oMinutes,
            [ExcelArgument(Name = "Seconds", Description = "Second value")] object oSeconds)
        {
            int hours = (int)Optional.Check(oHours, 0.0);
            int minutes = (int)Optional.Check(oMinutes, 0.0);
            double seconds = Optional.Check(oSeconds, 0.0);

            return ToDecimalDegrees(hours, minutes, seconds);
        }

        [ExcelFunction(Name = "PE.deg.FromDmValues", Description = "Combines degrees and decimal minutes into decimal degrees")]
        public static double FromDmValues(
            [ExcelArgument(Name = "Hours", Description = "Degree value")] object oHours,
            [ExcelArgument(Name = "Minutes", Description = "Decimal minute value")] object oMinutes)
        {
            int hours = (int)Optional.Check(oHours, 0.0);
            double minutes = Optional.Check(oMinutes, 0.0);

            return ToDecimalDegrees(hours, minutes);
        }

        [ExcelFunction(Name = "PE.deg.GetHours", Description = "Returns the degree part of an angle")]
        public static double GetHours([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return Hours(angle);
        }

        [ExcelFunction(Name = "PE.deg.GetDecimalMinutes", Description = "Returns the decimal minutes part of an angle")]
        public static double GetDecimalMinutes([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return DecimalMinutes(angle);
        }

        [ExcelFunction(Name = "PE.deg.GetMinutes", Description = "Returns the integer minutes part of an angle")]
        public static double GetMinutes([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return Minutes(angle);
        }

        [ExcelFunction(Name = "PE.deg.GetSeconds", Description = "Returns the seconds part of an angle")]
        public static double GetSeconds([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return Seconds(angle);
        }

        [ExcelFunction(Name = "PE.deg.Normalize0to360", Description = "Constrains an angle to the 0 to 360 degree range")]
        public static double Normalize0to360([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            double value = angle;
            while (value < 0)
            {
                value += 360.0;
            }
            return (value % 360);
        }

        [ExcelFunction(Name = "PE.deg.Normalize180to180", Description = "Constrains an angle to the -180 to +180 degree range")]
        public static double Normalize180to180([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            double value = angle + 180;
            while (value < 0)
            {
                value += 360.0;
            }
            return (value % 360) - 180;
        }

        [ExcelFunction(Name = "PE.deg.IsWithin0to360", Description = "Checks whether an angle is >= 0 and < 360 degrees")]
        public static bool IsWithin0to360([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return angle >= 0 && angle < 360;
        }

        [ExcelFunction(Name = "PE.deg.IsWithin180to180", Description = "Checks whether an angle is >= -180 and < 180 degrees")]
        public static bool IsWithin180to180([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return angle >= -180 && angle < 180;
        }

        [ExcelFunction(Name = "PE.deg.IsWithin90to90", Description = "Checks whether an angle is >= -90 and < 90 degrees")]
        public static bool IsWithin90to90([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return angle >= -90 && angle < 90;
        }

        [ExcelFunction(Name = "PE.deg.ToRadians", Description = "Converts an angle in decimal degrees to radians")]
        public static double ToRadians([ExcelArgument(Name = "angle", Description = "Angle in decimal degrees")] double angle)
        {
            return PI * angle / 180;
        }

        [ExcelFunction(Name = "PE.deg.FromRadians", Description = "Converts an angle in radians to decimal degrees")]
        public static double FromRadians([ExcelArgument(Name = "angle", Description = "Angle in radians")] double angle)
        {
            return 180 * angle / PI;
        }
    }
}
