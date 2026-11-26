using System;
using System.Linq;
using System.Reflection;

using ExcelDna.Integration;

using SharpProj;

namespace ProjExcel
{
    public static class LibraryFunctions
    {
        private static readonly string[] LibPropertyNames =
        {
            "AddIn.AssemblyVersion", "AddIn.FileVersion", "AddIn.InformationalVersion", "AddIn.LastWriteTime",
            "Excel.OperatingSystem", "ExcelDna.Version",
            "Proj.DataVersion", "Proj.EnableNetworkConnections", "Proj.EndpointUrl", "Proj.EpsgVersion",
            "Proj.EsriVersion", "Proj.IgnfVersion", "Proj.LogLevel", "Proj.Version"
        };

        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }

        [ExcelFunction(Name = "PE.lib.ListLibProperties", Description = "List accessible library properties by name and index")]
        public static object ListLibProperties(
            [ExcelArgument(Name = "transpose", Description = "Transpose the list (FALSE)")] object transpose = null)
        {
            try
            {
                object[,] result = new object[LibPropertyNames.Length + 1, 2];
                result[0, 0] = "Index";
                result[0, 1] = "Property name";
                for (int i = 0; i < LibPropertyNames.Length; i++)
                {
                    result[i + 1, 0] = i;
                    result[i + 1, 1] = LibPropertyNames[i];
                }

                if (!Optional.Check(transpose, false))
                    return result;

                object[,] transposed = new object[2, LibPropertyNames.Length + 1];
                for (int row = 0; row < result.GetLength(0); row++)
                    for (int col = 0; col < result.GetLength(1); col++)
                        transposed[col, row] = result[row, col];
                return transposed;
            }
            catch (Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        private static int ParseLibPropertyIndex(object property)
        {
            if (Optional.IsEmpty(property))
                throw new ArgumentException("Property is required.");

            if (property is int number)
                return number;

            if (property is double value)
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value != Math.Truncate(value) ||
                    value < int.MinValue || value > int.MaxValue)
                    throw new ArgumentException("Property index must be an integer within the Int32 range.");
                return (int)value;
            }

            if (!(property is string text) || string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Property must be a name or index.");

            string name = text.Trim();
            if (int.TryParse(name, out int index))
                return index;

            for (int i = 0; i < LibPropertyNames.Length; i++)
                if (string.Equals(name, LibPropertyNames[i], StringComparison.OrdinalIgnoreCase))
                    return i;

            throw new ArgumentException($"Unknown property. Name: '{name}'.");
        }

        private static int[] ParseLibPropertyIndices(object properties)
        {
            int[] indices = properties is string names && names.Contains(",")
                ? names.Split(',').Select(name => ParseLibPropertyIndex(name.Trim())).ToArray()
                : new[] { ParseLibPropertyIndex(properties) };

            if (indices.Any(index => index < 0 || index >= LibPropertyNames.Length))
                throw new ArgumentException("Property index out of range.");

            return indices;
        }

        [ExcelFunction(Name = "PE.lib.Options", Description = "Create a comma-separated selection of library property tokens")]
        public static string Options(
            [ExcelArgument(Name = "AddIn.AssemblyVersion", Description = "Include add-in assembly version")] object assemblyVersion = null,
            [ExcelArgument(Name = "AddIn.FileVersion", Description = "Include add-in file version")] object fileVersion = null,
            [ExcelArgument(Name = "AddIn.InformationalVersion", Description = "Include add-in informational version")] object informationalVersion = null,
            [ExcelArgument(Name = "AddIn.LastWriteTime", Description = "Include add-in last write time")] object lastWriteTime = null,
            [ExcelArgument(Name = "Excel.OperatingSystem", Description = "Include Excel operating system")] object operatingSystem = null,
            [ExcelArgument(Name = "ExcelDna.Version", Description = "Include Excel-DNA version")] object excelDnaVersion = null,
            [ExcelArgument(Name = "Proj.DataVersion", Description = "Include PROJ data version")] object dataVersion = null,
            [ExcelArgument(Name = "Proj.EnableNetworkConnections", Description = "Include PROJ network setting")] object enableNetworkConnections = null,
            [ExcelArgument(Name = "Proj.EndpointUrl", Description = "Include PROJ endpoint URL")] object endpointUrl = null,
            [ExcelArgument(Name = "Proj.EpsgVersion", Description = "Include EPSG database version")] object epsgVersion = null,
            [ExcelArgument(Name = "Proj.EsriVersion", Description = "Include ESRI database version")] object esriVersion = null,
            [ExcelArgument(Name = "Proj.IgnfVersion", Description = "Include IGNF database version")] object ignfVersion = null,
            [ExcelArgument(Name = "Proj.LogLevel", Description = "Include PROJ log level")] object logLevel = null,
            [ExcelArgument(Name = "Proj.Version", Description = "Include PROJ version")] object projVersion = null)
        {
            try
            {
                object[] inputs =
                {
                    assemblyVersion, fileVersion, informationalVersion, lastWriteTime,
                    operatingSystem, excelDnaVersion, dataVersion, enableNetworkConnections,
                    endpointUrl, epsgVersion, esriVersion, ignfVersion, logLevel, projVersion
                };

                if (inputs.All(Optional.IsMissing))
                    return string.Join(", ", LibPropertyNames);

                return string.Join(", ", Enumerable.Range(0, inputs.Length)
                    .Where(index => Optional.Check(inputs[index], false))
                    .Select(index => LibPropertyNames[index]));
            }
            catch (Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        private static object GetLibPropertyValue(string name, Assembly assembly, ProjContext context, string nullValueText)
        {
            object value;
            switch (name)
            {
                case "AddIn.AssemblyVersion": value = assembly.GetName().Version?.ToString(); break;
                case "AddIn.FileVersion": value = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version; break;
                case "AddIn.InformationalVersion": value = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion; break;
                case "AddIn.LastWriteTime": value = System.IO.File.GetLastWriteTime(ExcelDnaUtil.XllPath).ToString(); break;
                case "Excel.OperatingSystem": value = OperatingSystem(); break;
                case "ExcelDna.Version": value = typeof(ExcelDnaUtil).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version; break;
                case "Proj.DataVersion": value = context.ProjDataVersion?.ToString(); break;
                case "Proj.EnableNetworkConnections": value = context.EnableNetworkConnections; break;
                case "Proj.EndpointUrl": value = context.EndpointUrl; break;
                case "Proj.EpsgVersion": value = context.EpsgVersion?.ToString(); break;
                case "Proj.EsriVersion": value = context.EsriVersion?.ToString(); break;
                case "Proj.IgnfVersion": value = context.IgnfVersion?.ToString(); break;
                case "Proj.LogLevel": value = context.LogLevel.ToString(); break;
                case "Proj.Version": value = context.Version?.ToString(); break;
                default: throw new ArgumentException($"Unknown property. Name: '{name}'.");
            }

            return value == null || value is string text && string.IsNullOrWhiteSpace(text)
                ? (object)nullValueText
                : value;
        }

        [ExcelFunction(Name = "PE.lib.Properties", Description = "Show library properties selected by name or index in adjacent cells")]
        public static object Properties(
            [ExcelArgument(Name = "properties", Description = "Comma-separated property names or indices; see PE.lib.ListLibProperties()")] object properties,
            [ExcelArgument(Name = "nullValues", Description = "Replacement for missing values (<null>)")] object nullValues = null)
        {
            try
            {
                if (nullValues is ExcelError)
                    throw new ArgumentException("Invalid nullValues value.");

                bool multiple = properties is string names && names.Contains(",");
                int[] indices = ParseLibPropertyIndices(properties);

                string nullValueText = Optional.Check(nullValues, "<null>");
                Assembly assembly = Assembly.GetExecutingAssembly();
                bool needsContext = indices.Any(index => LibPropertyNames[index].StartsWith("Proj.", StringComparison.Ordinal));
                using (ProjContext context = needsContext ? CreateContext() : null)
                {
                    if (!multiple)
                        return GetLibPropertyValue(LibPropertyNames[indices[0]], assembly, context, nullValueText);

                    object[,] result = new object[1, indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                        result[0, i] = GetLibPropertyValue(LibPropertyNames[indices[i]], assembly, context, nullValueText);
                    return result;
                }
            }
            catch (Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.lib.VersionInfo", Description = "Returns a selected information aspect of the add-in")]
        public static string VersionInfo(
             [ExcelArgument("Info type (0); 0 = PROJ version, 1 = PROJ DB version, 2 = EPSG DB version, 3 = ESRI DB version, 4 = IGNF DB version ,  5 = Network info, 6 = Network Url, 7 = LogLevelAddIn 8 = Assembly version, 9 = Compile time", Name = "Mode")] object oMode)
        {
            int nMode = (int)Optional.Check(oMode, 0.0);

            string info;

            try
            {
                using (var pc = CreateContext())
                {
                    switch (nMode)
                    {
                        case 0:
                        default:
                            info = pc.Version.ToString();
                            break;
                        case 1:
                            info = pc.ProjDataVersion.ToString();
                            break;
                        case 2:
                            info = pc.EpsgVersion.ToString();
                            break;
                        case 3:
                            info = pc.EsriVersion.ToString();
                            break;
                        case 4:
                            info = pc.IgnfVersion.ToString();
                            break;
                        case 5:
                            info = pc.EnableNetworkConnections.ToString();
                            break;
                        case 6:
                            info = pc.EndpointUrl;
                            break;
                        case 7:
                            info = pc.LogLevel.ToString();
                            break;
                        case 8:
                            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                            info = v.ToString();
                            break;
                        case 9:
                            System.DateTime date_time = System.IO.File.GetLastWriteTime(ExcelDnaUtil.XllPath);
                            info = date_time.ToString();
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }

            return info;

        } // VersionInfo

        /// <summary>
        /// Returns version of Operating System and Excel
        /// </summary>
        /// <returns>Name and version number of the current operating system</returns>
        /// <example>
        /// "Windows (64-bit) NT :.00" with Win10(64-bit) and Excel 2016(16.0.6326.1010, 64-bit)
        /// </example>
        internal static string OperatingSystem()
        {
            dynamic xlApp = ExcelDnaUtil.Application;
            string strOS = xlApp.OperatingSystem;
            return strOS;
        }
    }
}

