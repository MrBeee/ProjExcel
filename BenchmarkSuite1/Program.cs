// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using System;
using System.IO;

namespace BenchmarkSuite1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string artifactsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\_bench"));
            var _ = BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance.WithArtifactsPath(artifactsPath));
        }
    }
}
