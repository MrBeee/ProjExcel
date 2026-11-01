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

    public class TestFunctions
    {
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