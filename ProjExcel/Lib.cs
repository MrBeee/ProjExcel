using System;
using System.Reflection;

using ExcelDna.Integration;

using SharpProj;

namespace ProjExcel
{
    public static class LibraryFunctions
    {
        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }

        [ExcelFunction(Name = "PE.lib.AddinVersion", Description = "Returns assembly version, file version and informational version of the add-in")]
        public static object AddInVersion(
            [ExcelArgument(Name = "Header", Description = "Use header row (true)")] object header)
        {
            bool bHeader = Optional.Check(header, true);
            int nOffset = bHeader ? 1 : 0;

            Assembly assembly = Assembly.GetExecutingAssembly();
            Version assemblyVersion = assembly.GetName().Version;

            AssemblyFileVersionAttribute fileVersionAttribute =
                (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(
                    assembly,
                    typeof(AssemblyFileVersionAttribute));

            AssemblyInformationalVersionAttribute informationalVersionAttribute =
                (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    assembly,
                    typeof(AssemblyInformationalVersionAttribute));

            string fileVersion = fileVersionAttribute == null ? string.Empty : fileVersionAttribute.Version;
            string informationalVersion = informationalVersionAttribute == null ? string.Empty : informationalVersionAttribute.InformationalVersion;

            object[,] res = new object[4 + nOffset, 2];

            if (bHeader)
            {
                res[0, 0] = "ProjExcel aspect";
                res[0, 1] = "version number";
            }

            res[0 + nOffset, 0] = "Assembly version";
            res[0 + nOffset, 1] = assemblyVersion == null ? string.Empty : assemblyVersion.ToString();

            res[1 + nOffset, 0] = "File version";
            res[1 + nOffset, 1] = fileVersion;

            res[2 + nOffset, 0] = "Info version";
            res[2 + nOffset, 1] = informationalVersion;

            res[3 + nOffset, 0] = "Proj version";

            try
            {
                using (var ctx = CreateContext())
            {
                    res[3 + nOffset, 1] = ctx.Version == null ? string.Empty : ctx.Version.ToString();
                }
            }
            catch (System.Exception ex)
            {
                res[3 + nOffset, 1] = $"#ERR: {ex.Message}";
            }

            return res;
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
        [ExcelFunction(Name = "PE.lib.OperatingSystem", Description = "Returns version of Operating System and Excel")]
        public static string OperatingSystem()
        {
            dynamic xlApp = ExcelDnaUtil.Application;
            string strOS = xlApp.OperatingSystem;
            return strOS;
        }







    }
}

