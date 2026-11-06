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

        /// <summary>
        /// Verification that SharpProj can access offline resources
        /// </summary>
        /// 

        [ExcelFunction(Name = "PE.tst.TestProjCdnAccess", Description = "Test PROJ CDN access")]
        public static string TestProjCdnAccess()
        {
            try
            {
                ProjService.ConfigureNetworkSecurityProtocol();
                using (var client = new System.Net.WebClient())
                {
                    string response = client.DownloadString("https://cdn.proj.org/");
                    return "SUCCESS: " + response.Substring(0, Math.Min(77, response.Length));
                }
            }
            catch (Exception ex)
            {
                return "FAILURE: " + ex;
            }
        }

        [ExcelFunction(Name = "PE.tst.TestProjCdnAccess2", Description = "Test PROJ CDN access via HttpWebRequest")]
        public static string TestProjCdnAccess2()
        {
            try
            {
                ProjService.ConfigureNetworkSecurityProtocol();
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("https://cdn.proj.org/");
                using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                {
                    return $"SUCCESS: {(int)response.StatusCode} {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return "FAILURE: " + ex;
            }
        }

        [ExcelFunction(Name = "PE.tst.TestProjCdnRaw", Description = "Test raw access to Proj cdn online")]
        public static string TestProjCdnRaw()
        {
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("https://cdn.proj.org/");
                req.Method = "GET";
                req.AllowAutoRedirect = false;

                using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                using (var reader = new System.IO.StreamReader(resp.GetResponseStream()))
                {
                    return $"Status: {(int)resp.StatusCode} {resp.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return "FAILURE: " + ex.ToString();
            }
        }

        [ExcelFunction(Name = "PE.tst.TestTls12", Description = "Test Tls12 for online grid access")]
        public static string TestTls12()
        {
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("https://www.howsmyssl.com/a/check");
                req.Method = "GET";

                using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                using (var reader = new System.IO.StreamReader(resp.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    return json; // .Substring(0, 200);
                }
            }
            catch (Exception ex)
            {
                return "FAILURE: " + ex.ToString();
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