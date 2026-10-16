using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using SharpProj;
using SharpProj.Proj;

namespace ProjExcel
{
    /// <summary>
    /// Centralizes PROJ context creation and runtime settings for the add-in.
    /// </summary>
    public sealed class ProjService : IDisposable
    {
        private static readonly Lazy<ProjService> _default = new Lazy<ProjService>(() => new ProjService());
        private bool _disposed;

        private ProjService()
        {
            EnableNetworkConnections = true;
            EnableGridCache = true;
            GridCacheSizeMb = 300;
            GridCacheTtlSeconds = 86400;
            EndpointUrl = "https://cdn.proj.org";
            LogLevel = ProjLogLevel.Error;
            GridCachePath = GetDefaultGridCachePath();
        }

        public static ProjService Default => _default.Value;

        public bool EnableNetworkConnections { get; set; }

        public bool EnableGridCache { get; set; }

        public int GridCacheSizeMb { get; set; }

        public int GridCacheTtlSeconds { get; set; }

        public string EndpointUrl { get; set; }

        public string GridCachePath { get; set; }

        public ProjLogLevel LogLevel { get; set; }

        public ProjContext CreateContext()
        {
            ThrowIfDisposed();

            var pjContext = new ProjContext
            {
                EnableNetworkConnections = EnableNetworkConnections,
                LogLevel = LogLevel
            };

            if (!string.IsNullOrWhiteSpace(EndpointUrl))
                pjContext.EndpointUrl = EndpointUrl;

            if (!string.IsNullOrWhiteSpace(GridCachePath))
            {
                string cacheDirectory = Path.GetDirectoryName(GridCachePath);
                if (!string.IsNullOrWhiteSpace(cacheDirectory))
                    Directory.CreateDirectory(cacheDirectory);

                pjContext.SetGridCache(EnableGridCache, GridCachePath, GridCacheSizeMb, GridCacheTtlSeconds);
            }

            if (LogLevel != ProjLogLevel.None)
                pjContext.Log += OnProjLog;

            return pjContext;
        }

        public string GetProxyInfo()
        {
            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            Uri testUri = new Uri(EndpointUrl ?? "https://cdn.proj.org/");

            if (proxy == null || proxy.IsBypassed(testUri))
                return "No proxy / direct connection";

            Uri proxyUri = proxy.GetProxy(testUri);
            return proxyUri == null ? "Proxy not determined" : proxyUri.ToString();
        }

        private static string GetDefaultGridCachePath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "ProjExcel", "proj", "cache.db");
        }

        private static void OnProjLog(ProjLogLevel level, string message)
        {
            Trace.WriteLine(string.Format("[PROJ] {0}: {1}", level, message));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ProjService));
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
