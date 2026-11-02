using BenchmarkDotNet.Attributes;
using ProjExcel;
using Microsoft.VSDiagnostics;

namespace ProjExcelBenchmarks
{
    [CPUUsageDiagnoser]
    public class TransformFunctionsBenchmarks
    {
        private object[, ] x;
        private object[, ] y;
        private object[, ] xyzt;
        [GlobalSetup]
        public void Setup()
        {
            x = new object[128, 1];
            y = new object[128, 1];
            xyzt = new object[128, 2];
            for (int i = 0; i < 128; i++)
            {
                x[i, 0] = -73.9857 + i * 0.0001;
                y[i, 0] = 40.7484 + i * 0.0001;
                xyzt[i, 0] = x[i, 0];
                xyzt[i, 1] = y[i, 0];
            }
        }

        [Benchmark]
        public object Transform128() => TransformFunctions.Apply(4326, 3857, x, y, null);
        [Benchmark]
        public object TransformEx128() => TransformFunctions.ApplyEx(4326, 3857, xyzt, null);
    }
}