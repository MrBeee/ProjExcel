using System;
using ExcelDna.Integration;

namespace ProjExcel
{
    internal static class ExcelValueConverters
    {
        internal static bool TryGetDouble(object value, out double result)
        {
            result = 0.0;

            if (Optional.IsMissing(value))
                return false;

            if (value is double d)
            {
                result = d;
                return true;
            }

            if (value is int i)
            {
                result = i;
                return true;
            }

            if (value is string s)
                return double.TryParse(s, out result);

            try
            {
                result = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static double ToDouble(object value)
        {
            double result;
            return TryGetDouble(value, out result) ? result : 0.0;
        }

        internal static bool ToBoolean(object value)
        {
            if (Optional.IsMissing(value))
                return false;

            if (value is bool b)
                return b;

            if (value is double d)
                return d != 0.0;

            if (value is int i)
                return i != 0;

            if (value is string s)
            {
                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                    return false;

                double d2;
                if (double.TryParse(s, out d2))
                    return d2 != 0.0;
            }

            return false;
        }

        internal static int ToInt32(object value)
        {
            if (Optional.IsMissing(value))
                return 0;

            if (value is int i)
                return i;

            if (value is double d)
                return (int)d;

            if (value is bool b)
                return b ? 1 : 0;

            if (value is string s)
            {
                int i2;
                if (int.TryParse(s, out i2))
                    return i2;

                double d2;
                if (double.TryParse(s, out d2))
                    return (int)d2;

                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                    return 1;

                if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                    return 0;
            }

            return 0;
        }
    }
}