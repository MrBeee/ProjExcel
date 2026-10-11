//
// Copyright (c) 2020 - 2061 by Bart Duijndam. See: https://www.duijndam.dev 
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License.
// You may obtain a License copy at http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//
// See the License for the specific language governing permissions and limitations under the License.
//
using System;
using System.Globalization;
using ExcelDna.Integration;

namespace ProjExcel
{
    internal static class Optional
    {
        internal static bool IsMissing(object arg)
        {
            return arg == null || arg is ExcelMissing || arg is ExcelEmpty;
        }

        internal static string Check(object arg, string defaultValue)
        {
            if (IsMissing(arg))
                return defaultValue;

            if (arg is string value)
                return value;

            return Convert.ToString(arg, CultureInfo.CurrentCulture);
        }

        internal static double Check(object arg, double defaultValue)
        {
            if (IsMissing(arg))
                return defaultValue;

            if (arg is double value)
                return value;

            return Convert.ToDouble(arg, CultureInfo.CurrentCulture);
        }

        internal static int Check(object arg, int defaultValue)
        {
            if (IsMissing(arg))
                return defaultValue;

            if (arg is int value)
                return value;

            if (arg is double doubleValue)
                return Convert.ToInt32(doubleValue);

            if (arg is bool boolValue)
                return boolValue ? 1 : 0;

            return Convert.ToInt32(arg, CultureInfo.CurrentCulture);
        }

        internal static bool Check(object arg, bool defaultValue)
        {
            if (IsMissing(arg))
                return defaultValue;

            if (arg is bool value)
                return value;

            if (arg is double doubleValue)
                return doubleValue != 0.0;

            if (arg is int intValue)
                return intValue != 0;

            if (arg is string text)
            {
                bool boolValue;
                if (bool.TryParse(text, out boolValue))
                    return boolValue;

                double numericValue;
                if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out numericValue))
                    return numericValue != 0.0;
            }

            return Convert.ToBoolean(arg, CultureInfo.CurrentCulture);
        }

        internal static DateTime Check(object arg, DateTime defaultValue)
        {
            if (IsMissing(arg))
                return defaultValue;

            if (arg is DateTime value)
                return value;

            if (arg is double serialDate)
                return DateTime.FromOADate(serialDate);

            if (arg is string text)
                return DateTime.Parse(text, CultureInfo.CurrentCulture);

            return Convert.ToDateTime(arg, CultureInfo.CurrentCulture);
        }

        internal static bool IsNul(object[,] arg)
        {
            if (arg == null || arg.Length == 0)
                return true;

            object value = arg[0, 0];

            if (value == null || value is ExcelMissing || value is ExcelEmpty || value is ExcelError)
                return true;

            if (value is double doubleValue && doubleValue == 0.0)
                return true;

            if (value is string text)
            {
                text = text.TrimStart('\'');
                return string.IsNullOrEmpty(text) || text == "0";
            }

            return false;
        }

        internal static bool IsEmpty(object arg)
        {
            if (arg == null || arg is ExcelMissing || arg is ExcelEmpty || arg is ExcelError)
                return true;

            if (arg is string text)
            {
                text = text.TrimStart('\'');
                return string.IsNullOrEmpty(text);
            }

            return false;
        }

        internal static string GetString(object arg)
        {
            if (arg is string text)
            {
                text = text.TrimStart(' ');
                return string.IsNullOrEmpty(text) ? "" : text;
            }

            return "";
        }

        internal static double GetDouble(object arg)
        {
            if (arg == null || arg is string || arg is bool || arg is ExcelMissing || arg is ExcelEmpty || arg is ExcelError)
                return double.NaN;

            if (arg is double value)
                return value;

            return double.PositiveInfinity;
        }

        internal static object CheckNan(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? (object)ExcelError.ExcelErrorNum
                : value;
        }

        internal static object CheckUnknown(double? value, string message = "Unknown")
        {
            return value == null || value < 0
                ? (object)message
                : value.Value;
        }

        internal static string CheckUnknown(string value, string message = "Unknown")
        {
            return string.IsNullOrWhiteSpace(value) ? message : value;
        }
    }
}

