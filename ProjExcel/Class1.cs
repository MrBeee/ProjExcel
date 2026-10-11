using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using SharpProj;
using System;
using System.Reflection;

namespace ProjExcel
{
    internal static class AddIn
    {
        internal static object ProcessException(Exception ex)
        {
            return $"#ERR: {ex.Message}";
        }
    }

    public class IntelliSenseAddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            IntelliSenseServer.Install();
        }

        public void AutoClose()
        {
            IntelliSenseServer.Uninstall();
        }
    }

    public class Class1
    {
        [ExcelFunction(Name = "PE.lib.Version", Description = "Returns assembly version, file version and informational version of the add-in")]
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
                res[0, 0] = "ProjExcel element";
                res[0, 1] = "version number";
            }

            res[0 + nOffset, 0] = "Assembly version";
            res[0 + nOffset, 1] = assemblyVersion == null ? string.Empty : assemblyVersion.ToString();

            res[1 + nOffset, 0] = "File version";
            res[1 + nOffset, 1] = fileVersion;

            res[2 + nOffset, 0] = "Info version";
            res[2 + nOffset, 1] = informationalVersion;

            res[3 + nOffset, 0] = "Proj version";

            using (var ctx = ProjService.Default.CreateContext())
            {
                res[3 + nOffset, 1] = ctx.Version == null ? string.Empty : ctx.Version.ToString();
            }

            return res;
        }

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

        /// <summary>
        /// A simple Excel-DNA function that returns a greeting message.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [ExcelFunction(Name = "PE.tst.SayHello", Description = "Test Excel-DNA library")]
        public static string SayHello(
            [ExcelArgument(Name = "name", Description = "The name to greet")] string name)
        {
            return $"Hello {name}!";
        }

        /// <summary>
        /// Verification that SharpProj is initialized correctly and can be used for coordinate transformations.
        /// </summary>
        [ExcelFunction(Name = "PE.tst.TestProj", Description = "Test SharpProj initialization")]
        public static string TestProj()
        {
            using (var ctx = ProjService.Default.CreateContext())
            {
                return $"SharpProj v{ctx.Version} loaded correctly";
            }
        }

        [ExcelFunction(Name = "PE.tst.TestDefaultValues", Description = "Example with Excel-friendly optional arguments")]
        public static object TestDefaultValues(
            [ExcelArgument(Name = "x", Description = "Required value")] double x,
            [ExcelArgument(Name = "scale", Description = "Optional scale")] object scale,
            [ExcelArgument(Name = "label", Description = "Optional label")] object label)
        {
            double actualScale = Optional.Check(scale, 1.0);
            string actualLabel = Optional.Check(label, "Default");

            return $"{actualLabel}: {x * actualScale}";
        }

    }
}