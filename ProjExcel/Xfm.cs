// Compile as a Class Library and expose via Excel-DNA.
// References needed:
// - SharpProj (NuGet)
// - Excel-DNA (ExcelDna.Integration)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelDna.Integration;
using SharpProj;


namespace ProjExcel
{

    public static class SharpProjExcelUdfs
    {

        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }



        // Shared PROJ context (you can customize search paths, grids, etc. here)
        private static readonly ProjContext Ctx = CreateContext();

        // ---------------------------
        // Helper types
        // ---------------------------

        private class PipelineInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double? Accuracy { get; set; }
            public string Grids { get; set; }
            public bool IsAvailable { get; set; }
        }

        private static CoordinateReferenceSystem CreateCrs(string crsSpec)
        {
            // Accepts "EPSG:4326", "4326", or full PROJ strings.
            if (string.IsNullOrWhiteSpace(crsSpec))
                throw new ArgumentException("CRS specification is empty.");

            crsSpec = crsSpec.Trim();

            if (crsSpec.StartsWith("EPSG:", StringComparison.OrdinalIgnoreCase))
            {
                var epsg = int.Parse(crsSpec.Substring(5));
                return CoordinateReferenceSystem.CreateFromEpsg(epsg, Ctx);
            }

            // If it's just a number, treat as EPSG
            if (int.TryParse(crsSpec, out var epsgCode))
                return CoordinateReferenceSystem.CreateFromEpsg(epsgCode, Ctx);

            // Otherwise assume full PROJ string
            return CoordinateReferenceSystem.Create(crsSpec, Ctx);
        }

        private static IReadOnlyList<PipelineInfo> GetPipelines(string sourceCrsSpec, string destCrsSpec)
        {
            var src = CreateCrs(sourceCrsSpec);
            var dst = CreateCrs(destCrsSpec);

            // CoordinateTransform.Create returns a group of possible operations
            var group = CoordinateTransform.Create(src, dst, Ctx);

            var list = new List<PipelineInfo>();
            int id = 1;

            foreach (var p in GetPipelineObjects(group))
            {
                // Properties here follow the idea from the previous explanation.
                // Adjust names if your SharpProj version uses slightly different ones.
                list.Add(new PipelineInfo
                {
                    Id = id++,
                    Name = GetStringProperty(p, "Name") ?? string.Empty,
                    Accuracy = GetNullableDoubleProperty(p, "Accuracy"), // metres, nullable
                    Grids = FormatGrids(GetPropertyValue(p, "Grids")),
                    IsAvailable = GetBoolProperty(p, "IsAvailable", true)
                });
            }

            return list;
        }

        private static IEnumerable<object> GetPipelineObjects(CoordinateTransform transform)
        {
            if (transform == null)
                yield break;

            if (transform is ChooseCoordinateTransform list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item != null)
                        yield return item;
                }

                yield break;
            }

            yield return transform;
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null)
                return null;

            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                return null;

            try
            {
                return property.GetValue(target, null);
            }
            catch
            {
                return null;
            }
        }

        private static string GetStringProperty(object target, string propertyName)
        {
            var value = GetPropertyValue(target, propertyName);
            return value == null ? null : Convert.ToString(value);
        }

        private static bool GetBoolProperty(object target, string propertyName, bool defaultValue)
        {
            var value = GetPropertyValue(target, propertyName);
            if (value == null)
                return defaultValue;

            if (value is bool b)
                return b;

            if (bool.TryParse(Convert.ToString(value), out var parsed))
                return parsed;

            return defaultValue;
        }
        private static double? GetNullableDoubleProperty(object target, string propertyName)
        {
            var value = GetPropertyValue(target, propertyName);
            if (value == null)
                return null;

            if (value is double d)
                return d;

            if (value is float f)
                return f;

            if (value is decimal m)
                return (double)m;

            if (value is int i)
                return i;

            if (double.TryParse(Convert.ToString(value), out var parsed))
                return parsed;

            return null;
        }

        private static string FormatGrids(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is string s)
                return s;

            if (value is System.Collections.IEnumerable enumerable)
            {
                var items = new List<string>();
                foreach (object item in enumerable)
                {
                    if (item != null)
                        items.Add(Convert.ToString(item));
                }

                return string.Join(",", items);
            }

            return Convert.ToString(value);
        }

        private static CoordinateTransform CreateTransformFromPipeline(object pipeline)
        {
            if (pipeline == null)
                return null;

            if (pipeline is CoordinateTransform direct)
                return direct;

            var createTransformMethod = pipeline.GetType().GetMethod("CreateTransform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (createTransformMethod != null)
            {
                return createTransformMethod.Invoke(pipeline, null) as CoordinateTransform;
            }

            return null;
        }

        private static PipelineInfo ChooseBestAvailablePipeline(IReadOnlyList<PipelineInfo> pipelines)
        {
            // Filter to available pipelines
            var available = pipelines.Where(p => p.IsAvailable).ToList();
            if (!available.Any())
                throw new InvalidOperationException("No available pipelines (required grids may be missing).");

            // Order by accuracy (ascending, best first). Null accuracy goes last.
            return available
                .OrderBy(p => p.Accuracy ?? double.MaxValue)
                .First();
        }

        private static CoordinateTransform CreatePinnedTransform(string sourceCrsSpec, string destCrsSpec, int pipelineId)
        {
            var src = CreateCrs(sourceCrsSpec);
            var dst = CreateCrs(destCrsSpec);

            var group = CoordinateTransform.Create(src, dst, Ctx);

            object pipeline = null;
            int id = 1;
            foreach (var candidate in GetPipelineObjects(group))
            {
                if (id == pipelineId)
                {
                    pipeline = candidate;
                    break;
                }

                id++;
            }

            if (pipeline == null)
                throw new ArgumentException($"Pipeline with ID {pipelineId} not found for this CRS pair.");

            // Pin this specific pipeline
            var pinned = CreateTransformFromPipeline(pipeline);
            if (pinned == null)
                throw new InvalidOperationException($"Pipeline with ID {pipelineId} cannot be converted into a transform.");

            return pinned;
        }

        private static CoordinateTransform CreatePinnedTransformAuto(string sourceCrsSpec, string destCrsSpec, out PipelineInfo chosenPipeline, out bool multiplePipelines)
        {
            var pipelines = GetPipelines(sourceCrsSpec, destCrsSpec);
            multiplePipelines = pipelines.Count > 1;

            chosenPipeline = ChooseBestAvailablePipeline(pipelines);

            var src = CreateCrs(sourceCrsSpec);
            var dst = CreateCrs(destCrsSpec);

            var group = CoordinateTransform.Create(src, dst, Ctx);

            object pipeline = null;
            int id = 1;
            foreach (var candidate in GetPipelineObjects(group))
            {
                if (id == chosenPipeline.Id)
                {
                    pipeline = candidate;
                    break;
                }

                id++;
            }

            if (pipeline == null)
                throw new ArgumentException($"Pipeline with ID {chosenPipeline.Id} not found for this CRS pair.");

            var pinned = CreateTransformFromPipeline(pipeline);
            if (pinned == null)
                throw new InvalidOperationException($"Pipeline with ID {chosenPipeline.Id} cannot be converted into a transform.");

            return pinned;
        }

        // ---------------------------
        // UDF 1: ListTransformPipelines
        // ---------------------------

        [ExcelFunction(
            Name = "PE.xfm.ListTransformPipelines",
            Description = "List all available coordinate transform pipelines between two CRSs.",
            Category = "SharpProj",
            IsVolatile = false)]
        public static object ListTransformPipelines(
            [ExcelArgument(Description = "Source CRS (e.g. EPSG:4326 or PROJ string).")]
        string sourceCrs,
            [ExcelArgument(Description = "Destination CRS (e.g. EPSG:28992 or PROJ string).")]
        string destCrs)
        {
            try
            {
                var pipelines = GetPipelines(sourceCrs, destCrs);

                if (pipelines.Count == 0)
                    return "No pipelines found.";

                // Return as a 2D array for Excel
                var rows = pipelines.Count + 1;
                var cols = 5;
                var result = new object[rows, cols];

                // Header row
                result[0, 0] = "PipelineID";
                result[0, 1] = "Name";
                result[0, 2] = "Accuracy(m)";
                result[0, 3] = "Grids";
                result[0, 4] = "IsAvailable";

                // Data rows
                for (int i = 0; i < pipelines.Count; i++)
                {
                    var p = pipelines[i];
                    result[i + 1, 0] = p.Id;
                    result[i + 1, 1] = p.Name;
                    result[i + 1, 2] = p.Accuracy.HasValue ? (object)p.Accuracy.Value : "";
                    result[i + 1, 3] = p.Grids;
                    result[i + 1, 4] = p.IsAvailable ? "TRUE" : "FALSE";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // ---------------------------
        // UDF 2: TransformCoordinate (explicit pipeline)
        // ---------------------------

        [ExcelFunction(
            Name = "PE.xfm.TransformCoordinate",
            Description = "Transform a coordinate using a specific pipeline ID.",
            Category = "SharpProj",
            IsVolatile = false)]
        public static object TransformCoordinate([ExcelArgument(Description = "Source CRS (e.g. EPSG:4326).")] string sourceCrs, 
            [ExcelArgument(Description = "Destination CRS (e.g. EPSG:28992).")] string destCrs, 
            [ExcelArgument(Description = "X (longitude or easting).")] double x, 
            [ExcelArgument(Description = "Y (latitude or northing).")] double y, 
            [ExcelArgument(Description = "Pipeline ID (from ListTransformPipelines).")]
        int pipelineId)
        {
            try
            {
                var transform = CreatePinnedTransform(sourceCrs, destCrs, pipelineId);
                var result = transform.Apply(new PPoint(x, y));

                // Return as 1x2 array: [X', Y']
                var arr = new object[1, 2];
                arr[0, 0] = result.X;
                arr[0, 1] = result.Y;
                return arr;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // ---------------------------
        // UDF 3: TransformCoordinateAuto (auto pipeline selection)
        // ---------------------------

        [ExcelFunction(
            Name = "PE.xfm.TransformCoordinateAuto",
            Description = "Transform a coordinate using the best available pipeline (auto-selected). Returns X',Y' and a warning if multiple pipelines exist.",
            Category = "SharpProj",
            IsVolatile = false)]
        public static object TransformCoordinateAuto(
            [ExcelArgument(Description = "Source CRS (e.g. EPSG:4326).")]
        string sourceCrs,
            [ExcelArgument(Description = "Destination CRS (e.g. EPSG:28992).")]
        string destCrs,
            [ExcelArgument(Description = "X (longitude or easting).")]
        double x,
            [ExcelArgument(Description = "Y (latitude or northing).")]
        double y)
        {
            try
            {
                PipelineInfo chosen;
                bool multiple;
                var transform = CreatePinnedTransformAuto(sourceCrs, destCrs, out chosen, out multiple);
                var result = transform.Apply(new PPoint(x, y));

                // Return as 1x4 array: [X', Y', PipelineID, Warning]
                var arr = new object[1, 4];
                arr[0, 0] = result.X;
                arr[0, 1] = result.Y;
                arr[0, 2] = chosen.Id;
                arr[0, 3] = multiple
                    ? $"Multiple pipelines exist; auto-selected ID={chosen.Id}, Name={chosen.Name}, Accuracy={chosen.Accuracy ?? double.NaN}m."
                    : "";

                return arr;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}



