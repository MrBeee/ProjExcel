using ExcelDna.Integration;
using MathNet.Numerics.IntegralTransforms;
using SharpProj;
using SharpProj.Proj;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Xml.Xsl;


namespace ProjExcel
{
    public static class TransformFunctions
    {
        private static CoordinateTransform CreateTransformWithOptions(
            CoordinateReferenceSystem src,
            CoordinateReferenceSystem dst,
            TransformOptions.CoordinateTransformOptionsInput optionValues,
            CoordinateTransformOptions coordinateTransformOptions,
            ProjContext pc)
        {

            // Early-exit validation check (Saves CPU cycles before pipeline creation)
            if (src.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                throw new ArgumentException("Source CRS is deprecated and AllowDeprecatedCrs option is not set.");

            if (dst.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                throw new ArgumentException("Target CRS is deprecated and AllowDeprecatedCrs option is not set.");

            try
            {
                return CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
            }
            catch (Exception ex) when ((ex is ProjException || ex is ArgumentNullException) && coordinateTransformOptions.BestOnly)
            {
                // try to recover from a failed "best only" transform by retrying with BestOnly = false
                // This is a workaround for cases where the "best only" option fails due to missing grids or other issues,
                // but a valid transform can still be constructed if we allow multiple candidates.

                // For consistencey, it may be better to throw an exception and let the user handle it, but for now we will try to recover automatically.
                coordinateTransformOptions.BestOnly = false;
                try
                {
                    return CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                }
                catch
                {
                    throw;  // rethrow the original exception if the second attempt fails
                }
            }
        }

        internal static bool isWktString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            return (s.IndexOf("PROJCRS") > -1) || (s.IndexOf("GEOGCRS") > -1) || (s.IndexOf("SPHEROID") > -1);
        }

        private static CoordinateTransform CreateTransformFromDescription(
            object transform,
            TransformOptions.CoordinateTransformOptionsInput optionValues,
            CoordinateTransformOptions coordinateTransformOptions,
            ProjContext pc)
        {
            try
            {
                int nTransform;
                string sTransform;

                if (transform is double)
                {
                    // First cast to double, then to int, to deal with Excel datatypes
                    nTransform = (int)(double)transform;

                    // we just have an EPSG number from a single input parameter:
                    return CoordinateTransform.CreateFromEpsg(nTransform, pc);
                }
                else if (transform is string)
                {
                    // cast to string, to deal with Excel datatypes
                    sTransform = (string)transform;
                    sTransform = sTransform.Trim();

                    bool isInt = int.TryParse(sTransform, out nTransform);
                    if (isInt)
                    {
                        // The string is an EPSG number from a single input parameter:
                        return CoordinateTransform.CreateFromEpsg(nTransform, pc);
                    }
                    else
                    {
                        // we have a string of some sorts and a single input parameter:
                        if (isWktString(sTransform))
                        {
                            // it must be WKT (well, we hope)
                            return CoordinateTransform.CreateFromWellKnownText(sTransform, pc);
                        }
                        else
                        {
                            // Does the string start with an Authority ID ?
                            bool hasAuthorityPrefix = Lst.GetAuthorityList()
                                .Any(authority => sTransform.StartsWith(authority + ":", StringComparison.OrdinalIgnoreCase));

                            if (hasAuthorityPrefix)
                            {
                                // we have an authority prefix, so we can try to parse it as an authority code
                                var parts = sTransform.Split(':');
                                if (parts.Length == 2 && int.TryParse(parts[1], out nTransform))
                                {
                                    return CoordinateTransform.CreateFromDatabase(parts[0], nTransform, pc);
                                }
                                else if (parts.Length == 2)
                                {
                                    // If the second part is not an integer, we can try to create a transform from a string code
                                    return CoordinateTransform.CreateFromDatabase(parts[0], parts[1], pc);
                                }
                                else
                                {
                                    throw new ArgumentException($"Invalid authority code format: {sTransform}");
                                }
                            }
                            else
                                // it might be anything
                                return CoordinateTransform.Create(sTransform, pc);
                        }
                    }
                }

                throw new ArgumentException("Unsupported transform specification type.");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to create transform from description: {ex.Message}", ex);
            }
        }

        private static CoordinateTransform CreateTransformForInput(
            object sourceCrs,
            object targetCrs,
            TransformOptions.CoordinateTransformOptionsInput optionValues,
            ProjContext pc)
        {
            CoordinateTransformOptions coordinateTransformOptions = TransformOptions.CreateCoordinateTransformOptions(optionValues);

            if (Optional.IsEmpty(targetCrs))
                return CreateTransformFromDescription(sourceCrs, optionValues, coordinateTransformOptions, pc);

            using (var src = Crs.CreateNormalizedCrs(sourceCrs, pc))
            using (var dst = Crs.CreateNormalizedCrs(targetCrs, pc))
            {
                return CreateTransformWithOptions(src, dst, optionValues, coordinateTransformOptions, pc);
            }
        }

        private static CoordinateTransform SelectCandidateTransform(CoordinateTransform transform, int candidateIndex)
        {
            if (transform == null)
                throw new ArgumentException("No mathematical transformation pipeline could be constructed.");

            if (transform is ChooseCoordinateTransform candidates)
            {
                if (candidateIndex < 0 || candidateIndex >= candidates.Count)
                    throw new ArgumentException("Candidate Index for transform out of range.");

                return candidates[candidateIndex];
            }

            if (candidateIndex != 0)
                throw new ArgumentException("Candidate Index for transform out of range. Only one candidate available.");

            return transform;
        }

        internal static int ParseOutputFormatString(object value)
        {
            string output = Convert.ToString(value);

            if (string.IsNullOrWhiteSpace(output))
                return 0;

            if (string.Equals(output, "None", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (string.Equals(output, "Proj", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(output, "Wkt", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (string.Equals(output, "Json", StringComparison.OrdinalIgnoreCase))
                return 3;

            return 0;
        }

        private static string FormatCoordinateValue(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static readonly string[] TransformPropertyNames = new[]
        {
            "Accuracy",
            "Context",
            "Count",
            "GridUsages",
            "HasBallParkTransformation",
            "HasInverse",
            "Identifier",
            "Identifiers",
            "IsAvailable",
            "MethodName",
            "Name",
            "Parameters",
            "Remarks",
            "Scope",
            "SourceCRS",
            "TargetCRS",
            "Type",
            "UsageArea"
        };

        [ExcelFunction(Name = "PE.trf.ListTransformProperties", Description = "List accessible coordinate transform properties by name and index")]
        public static object ListTransformProperties(
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null)
        {
            bool bTranspose = Optional.Check(transpose, false);
            string[] reflectedNames = typeof(CoordinateTransform)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
                .Select(p => p.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            string[] fixedNames = TransformPropertyNames
                .Where(name => name != "Count")
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (!reflectedNames.SequenceEqual(fixedNames))
            {
                Debug.WriteLine("TransformPropertyNames differs from reflected CoordinateTransform properties.");
            }

            object[,] result = new object[TransformPropertyNames.Length + 1, 2];
            result[0, 0] = "Index";
            result[0, 1] = "Property name";
            for (int i = 0; i < TransformPropertyNames.Length; i++)
            {
                result[i + 1, 0] = i;
                result[i + 1, 1] = TransformPropertyNames[i];
            }

            return bTranspose ? TransposeObjectArray(result) : result;
        }

        [ExcelFunction(Name = "PE.trf.ListContextSettings", Description = "List default or token-overridden PROJ context settings")]
        public static object ListContextSettings(
            [ExcelArgument(Name = "transformOptions", Description = "Optional token from PE.trf.OptionsEx() (blank = defaults)")] object options = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null)
        {
            object raw = options;
            if (raw is object[,] matrix)
            {
                if (matrix.Length != 1)
                    return "#ERR: transformOptions must be a single cell.";

                raw = matrix[0, 0];
            }

            if (raw is ExcelError)
                return "#ERR: Invalid transformOptions value.";

            if (raw is string token && token.StartsWith("#ERR:", StringComparison.OrdinalIgnoreCase))
                return token;

            TransformOptions.CoordinateTransformOptionsInput values = TransformOptions.CoordinateTransformOptionsInput.Parse(raw);
            ProjService defaults = ProjService.Default;

            object[,] result = new object[,]
            {
                { "Setting", "Value" },
                { "EnableNetworkConnections", values.EnableNetworkConnections ?? defaults.EnableNetworkConnections },
                { "EnableGridCache", values.EnableGridCache ?? defaults.EnableGridCache },
                { "GridCacheSizeMb", values.GridCacheSizeMb ?? defaults.GridCacheSizeMb },
                { "GridCacheTtlSeconds", values.GridCacheTtlSeconds ?? defaults.GridCacheTtlSeconds },
                { "EndpointUrl", values.EndpointUrl ?? defaults.EndpointUrl },
                { "GridCachePath", values.GridCachePath ?? defaults.GridCachePath },
                { "LogLevel", (values.LogLevel ?? defaults.LogLevel).ToString() }
            };
            return Optional.Check(transpose, false) ? TransposeObjectArray(result) : result;
        }

        [ExcelFunction(Name = "PE.trf.Properties", Description = "Show transform properties selected by index or name in a row")]
        public static object Properties(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "properties", Description = "String of comma-separated property names/indices. See PE.trf.ListTransformProperties()")] object properties,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");

            try
            {
                bool multipleProperties = properties is string names && names.Contains(",");
                int[] propertyIndices = multipleProperties
                    ? ((string)properties).Split(',').Select(name => ParseTransformPropertyIndex(name.Trim())).ToArray()
                    : new[] { ParseTransformPropertyIndex(properties) };

                if (propertyIndices.Any(index => index < 0 || index >= TransformPropertyNames.Length))
                    return "#ERR: Property index out of range.";

////                using (var pc = CreateContext())
                using (var pc = ProjService.Default.CreateContext(optionValues))

                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);
                    int count = trf is ChooseCoordinateTransform candidates ? candidates.Count : 1;

                    if (!multipleProperties)
                        return TransformPropertyNames[propertyIndices[0]] == "Count"
                            ? (object)count
                            : GetTransformPropertyValue(selectedTransform, propertyIndices[0], nullValueText);

                    object[,] result = new object[1, propertyIndices.Length];
                    for (int i = 0; i < propertyIndices.Length; i++)
                    {
                        result[0, i] = TransformPropertyNames[propertyIndices[i]] == "Count"
                            ? (object)count
                            : GetTransformPropertyValue(selectedTransform, propertyIndices[i], nullValueText);
                    }

                    return result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListUsageAreaProperties", Description = "List the selected transform's usage area properties")]
        public static object ListUsageAreaProperties(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);
                    UsageArea area = selectedTransform.UsageArea;

                    object[,] result = new object[,]
                    {
                        { "Property", "Value" },
                        { "Name", area == null ? nullValueText : SafeCandidateValue(area.Name, nullValueText) },
                        { "WestLongitude", area == null || double.IsNaN(area.WestLongitude) || double.IsInfinity(area.WestLongitude) ? (object)nullValueText : area.WestLongitude },
                        { "SouthLatitude", area == null || double.IsNaN(area.SouthLatitude) || double.IsInfinity(area.SouthLatitude) ? (object)nullValueText : area.SouthLatitude },
                        { "EastLongitude", area == null || double.IsNaN(area.EastLongitude) || double.IsInfinity(area.EastLongitude) ? (object)nullValueText : area.EastLongitude },
                        { "NorthLatitude", area == null || double.IsNaN(area.NorthLatitude) || double.IsInfinity(area.NorthLatitude) ? (object)nullValueText : area.NorthLatitude },
                        { "CenterX", area == null || double.IsNaN(area.CenterX) || double.IsInfinity(area.CenterX) ? (object)nullValueText : area.CenterX },
                        { "CenterY", area == null || double.IsNaN(area.CenterY) || double.IsInfinity(area.CenterY) ? (object)nullValueText : area.CenterY }
                    };
                    return Optional.Check(transpose, false) ? TransposeObjectArray(result) : result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.UsageAreaProperties", Description = "Show selected transform usage area properties by name in a row")]
        public static object UsageAreaProperties(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "properties", Description = "Comma-separated usage area property names: Name, WestLongitude, SouthLatitude, EastLongitude, NorthLatitude, CenterX, CenterY")] object properties,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");

            try
            {
                string[] propertyNames = { "Name", "WestLongitude", "SouthLatitude", "EastLongitude", "NorthLatitude", "CenterX", "CenterY" };
                if (Optional.IsEmpty(properties))
                    return "#ERR: Property is required.";

                bool multipleProperties = properties is string names && names.Contains(",");
                string[] requestedProperties = multipleProperties ? ((string)properties).Split(',') : new[] { Convert.ToString(properties) };
                int[] propertyIndices = new int[requestedProperties.Length];
                for (int i = 0; i < requestedProperties.Length; i++)
                {
                    string name = requestedProperties[i].Trim();
                    propertyIndices[i] = Array.FindIndex(propertyNames, value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
                    if (propertyIndices[i] < 0)
                        return $"#ERR: Unknown usage area property '{name}'. Valid properties: {string.Join(", ", propertyNames)}.";
                }

                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);
                    UsageArea area = selectedTransform.UsageArea;

                    object[] values =
                    {
                        area == null ? nullValueText : SafeCandidateValue(area.Name, nullValueText),
                        area == null || double.IsNaN(area.WestLongitude) || double.IsInfinity(area.WestLongitude) ? (object)nullValueText : area.WestLongitude,
                        area == null || double.IsNaN(area.SouthLatitude) || double.IsInfinity(area.SouthLatitude) ? (object)nullValueText : area.SouthLatitude,
                        area == null || double.IsNaN(area.EastLongitude) || double.IsInfinity(area.EastLongitude) ? (object)nullValueText : area.EastLongitude,
                        area == null || double.IsNaN(area.NorthLatitude) || double.IsInfinity(area.NorthLatitude) ? (object)nullValueText : area.NorthLatitude,
                        area == null || double.IsNaN(area.CenterX) || double.IsInfinity(area.CenterX) ? (object)nullValueText : area.CenterX,
                        area == null || double.IsNaN(area.CenterY) || double.IsInfinity(area.CenterY) ? (object)nullValueText : area.CenterY
                    };

                    if (!multipleProperties)
                        return values[propertyIndices[0]];

                    object[,] result = new object[1, propertyIndices.Length];
                    for (int i = 0; i < propertyIndices.Length; i++)
                        result[0, i] = values[propertyIndices[i]];

                    return result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListTransforms", Description = "Create a list of available transforms between source and target CRS")]
        public static object ListTransforms(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "outputString", Description = "None, Proj, Wkt or Json (\"none\")")] object outputString,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");
            bool bTranspose = Optional.Check(transpose, false);
            int outputFormat = ParseOutputFormatString(outputString);

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))

                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    if (trf == null)
                        return "#ERR: No mathematical transformation pipeline could be constructed.";

                    List<CoordinateTransform> candidates = new List<CoordinateTransform>();

                    if (trf is ChooseCoordinateTransform list)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            candidates.Add(list[i]);
                        }
                    }
                    else
                    {
                        candidates.Add(trf);
                    }

                    return BuildTransformListResult(candidates, nullValueText, outputFormat, bTranspose);
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ToString", Description = "Create a string representation of a coordinate transform")]
        public static object ToString(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "stringFormat", Description = "Proj, Wkt or Json (\"Proj\")")] object stringFormat
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);

            int outputFormat = ParseOutputFormatString(stringFormat);   

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                {
                    using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                    {
                        CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                        // work with the selected transform and return its string representation in the requested format
                        switch (outputFormat)
                        {
                            case 1: // Proj
                                return selectedTransform.AsProjString();
                            case 2: // Wkt
                                return selectedTransform.AsWellKnownText();
                            case 3: // Json
                                return selectedTransform.AsProjJson();
                            default:
                                return selectedTransform.AsProjString(); // Default to Proj string if no valid format is specified
                        }
                    }

                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }




        [ExcelFunction(Name = "PE.trf.ListTransformParameters", Description = "List parameters of the selected transform and its steps")]
        public static object ListTransformParameters(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);
                    object[,] result = BuildTransformParametersResult(selectedTransform, nullValueText, true);
                    return Optional.Check(transpose, false) ? TransposeObjectArray(result) : result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListTransformSteps", Description = "List the steps in the transform (pipeline) from source CRS to target CRS")]
        public static object ListTransformSteps(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "outputString", Description = "None, Proj, Wkt or Json (\"none\")")] object outputString,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description= "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            int outputFormat = ParseOutputFormatString(outputString);
            string nullValueText = Optional.Check(nullValues, "<null>");
            bool bTranspose = Optional.Check(transpose, false);

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                    List<CoordinateTransform> steps = GetTransformSteps(selectedTransform);
                    return BuildTransformStepsResult(steps, outputFormat, nullValueText, bTranspose);
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListTransformOperations", Description = "List the PROJ operations in a selected coordinate transform")]
        public static object ListTransformOperations(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (or null/empty)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");
            bool bTranspose = Optional.Check(transpose, false);

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                    IReadOnlyList<ProjOperation> operations = selectedTransform.ProjOperations();
                    object[,] result = new object[8, operations.Count + 1];
                    result[0, 0] = "Operation Number";
                    result[1, 0] = "Name";
                    result[2, 0] = "Type";
                    result[3, 0] = "IsForwardOnly";
                    result[4, 0] = "IsInverseOnly";
                    result[5, 0] = "IsInverse";
                    result[6, 0] = "Tokens";
                    result[7, 0] = "AllTokens";

                    for (int i = 0; i < operations.Count; i++)
                    {
                        ProjOperation operation = operations[i];
                        result[0, i + 1] = i;
                        result[1, i + 1] = SafeTransformStepValue(operation.Name, nullValueText);
                        result[2, i + 1] = operation.Type.ToString();
                        result[3, i + 1] = operation.IsForwardOnly;
                        result[4, i + 1] = operation.IsInverseOnly;
                        result[5, i + 1] = operation.IsInverse;
                        result[6, i + 1] = SafeTransformStepValue(string.Join(" ", operation.Tokens), nullValueText);
                        result[7, i + 1] = SafeTransformStepValue(string.Join(" ", operation.AllTokens), nullValueText);
                    }

                    return bTranspose ? TransposeObjectArray(result) : result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.StepCount", Description = "Number of steps in the transform (pipeline) from source CRS to target CRS")]
        public static object StepCount(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                    List<CoordinateTransform> steps = GetTransformSteps(selectedTransform);
                    return steps.Count;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.StepProperties", Description = "Properties of step-N in the transform (pipeline) from source CRS to target CRS")]
        public static object StepProperties(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "step number", Description = "Step nr to draw information from. Default = 0")] object oStepNumber,
            [ExcelArgument(Name = "properties", Description = "String of comma-separated property names/indices. See PE.trf.ListTransformProperties()")] object properties,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            int stepNumber = Optional.Check(oStepNumber, 0);
            string nullValueText = Optional.Check(nullValues, "<null>");
            bool multipleProperties = properties is string names && names.Contains(",");

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                    List<CoordinateTransform> transformSteps = GetTransformSteps(selectedTransform);
                    if (stepNumber < 0 || stepNumber >= transformSteps.Count)
                        return "#ERR: Step Index for transform out of range.";

                    CoordinateTransform transformStep = transformSteps[stepNumber];

                    int[] propertyIndices = multipleProperties
                        ? ((string)properties).Split(',').Select(name => ParseTransformPropertyIndex(name.Trim())).ToArray()
                        : new[] { ParseTransformPropertyIndex(properties) };

                    if (propertyIndices.Any(index => index < 0 || index >= TransformPropertyNames.Length))
                        return "#ERR: Property index out of range.";

                    int count = transformStep is ChooseCoordinateTransform candidates ? candidates.Count : 1;

                    if (!multipleProperties)
                        return TransformPropertyNames[propertyIndices[0]] == "Count"
                            ? (object)count
                            : GetTransformPropertyValue(transformStep, propertyIndices[0], nullValueText);

                    object[,] result = new object[1, propertyIndices.Length];
                    for (int i = 0; i < propertyIndices.Length; i++)
                    {
                        result[0, i] = TransformPropertyNames[propertyIndices[i]] == "Count"
                            ? (object)count
                            : GetTransformPropertyValue(transformStep, propertyIndices[i], nullValueText);
                    }

                    return result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListStepParameters", Description = "List parameters of a step in the selected transform")]
        public static object ListStepParameters(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "stepNumber", Description = "Zero-based step number. Default = 0")] object oStepNumber,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");

            try
            {
                int stepNumber = Optional.Check(oStepNumber, 0);
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);
                    List<CoordinateTransform> steps = GetTransformSteps(selectedTransform);
                    if (stepNumber < 0 || stepNumber >= steps.Count)
                        return "#ERR: Step Index for transform out of range.";

                    object[,] result = BuildTransformParametersResult(steps[stepNumber], nullValueText);
                    return Optional.Check(transpose, false) ? TransposeObjectArray(result) : result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.ListTransformGrids", Description = "List grids used by the selected transform. Valid properties: Name, FullName, IsAvailable")]
        public static object ListTransformGrids(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (or null/empty)")] object TargetCrs,
            [ExcelArgument(Name = "properties", Description = "Comma-separated grid properties: \"Name, FullName, IsAvailable\". Empty cell lists all three")] object properties = null,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). CandidateIndex defaults to 0")] object options = null,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null,
            [ExcelArgument(Name = "transpose", Description = "Option to transpose the list. Default = FALSE")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            string nullValueText = Optional.Check(nullValues, "<null>");
            bool bTranspose = Optional.Check(transpose, false);

            try
            {
                string[] selectedProperties = Optional.IsEmpty(properties)
                    ? GridPropertyNames
                    : Convert.ToString(properties).Split(',').Select(name => name.Trim()).ToArray();

                if (selectedProperties.Any(name => !GridPropertyNames.Contains(name, StringComparer.OrdinalIgnoreCase)))
                    return "#ERR: Valid grid properties are Name, FullName, IsAvailable.";

                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                    List<GridUsage> grids = GetTransformGrids(selectedTransform);

                    object[,] result = new object[grids.Count + 1, selectedProperties.Length + 1];
                    result[0, 0] = "Grid Number";
                    for (int propertyIndex = 0; propertyIndex < selectedProperties.Length; propertyIndex++)
                        result[0, propertyIndex + 1] = GridPropertyNames.First(name => string.Equals(name, selectedProperties[propertyIndex], StringComparison.OrdinalIgnoreCase));

                    for (int gridIndex = 0; gridIndex < grids.Count; gridIndex++)
                    {
                        GridUsage grid = grids[gridIndex];
                        result[gridIndex + 1, 0] = gridIndex;
                        for (int propertyIndex = 0; propertyIndex < selectedProperties.Length; propertyIndex++)
                        {
                            int index = Array.FindIndex(GridPropertyNames, name => string.Equals(name, selectedProperties[propertyIndex], StringComparison.OrdinalIgnoreCase));
                            result[gridIndex + 1, propertyIndex + 1] = GetGridPropertyValue(grid, index, nullValueText);
                        }
                    }

                    return bTranspose ? TransposeObjectArray(result) : result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.GridProperties", Description = "Properties of grid-N in the selected transform. Valid properties: Name, FullName, IsAvailable")]
        public static object GridProperties(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (or null/empty)")] object TargetCrs,
            [ExcelArgument(Name = "grid number", Description = "Grid number in the selected transform. Default = 0")] object oGridNumber,
            [ExcelArgument(Name = "properties", Description = "Comma-separated property names/indices: Name (0), FullName (1), IsAvailable (2)")] object properties,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). CandidateIndex defaults to 0")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default value = \"<null>\"")] object nullValues = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            int gridNumber = Optional.Check(oGridNumber, 0);
            string nullValueText = Optional.Check(nullValues, "<null>");
            bool multipleProperties = properties is string names && names.Contains(",");

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                {
                    CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                    List<GridUsage> grids = GetTransformGrids(selectedTransform);
                    if (gridNumber < 0 || gridNumber >= grids.Count)
                        return "#ERR: Grid Index for transform out of range.";

                    int[] propertyIndices = multipleProperties
                        ? ((string)properties).Split(',').Select(name => ParseGridPropertyIndex(name.Trim())).ToArray()
                        : new[] { ParseGridPropertyIndex(properties) };

                    if (propertyIndices.Any(index => index < 0 || index >= GridPropertyNames.Length))
                        return "#ERR: Property index out of range.";

                    GridUsage grid = grids[gridNumber];
                    if (!multipleProperties)
                        return GetGridPropertyValue(grid, propertyIndices[0], nullValueText);

                    object[,] result = new object[1, propertyIndices.Length];
                    for (int i = 0; i < propertyIndices.Length; i++)
                        result[0, i] = GetGridPropertyValue(grid, propertyIndices[i], nullValueText);

                    return result;
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        private static readonly string[] GridPropertyNames = { "Name", "FullName", "IsAvailable" };

        private static List<GridUsage> GetTransformGrids(CoordinateTransform selectedTransform)
        {
            List<GridUsage> grids = selectedTransform.GridUsages.ToList();
            if (grids.Count == 0 && selectedTransform is CoordinateTransformList)
            {
                foreach (CoordinateTransform step in selectedTransform.Steps())
                {
                    foreach (GridUsage grid in step.GridUsages)
                    {
                        if (!grids.Any(existing => string.Equals(existing.Name, grid.Name, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(existing.FullName, grid.FullName, StringComparison.OrdinalIgnoreCase)))
                            grids.Add(grid);
                    }
                }
            }

            return grids;
        }

        private static int ParseGridPropertyIndex(object property)
        {
            if (Optional.IsEmpty(property))
                throw new ArgumentException("Property is required.");

            if (property is int n)
                return n;

            if (property is double d)
                return (int)d;

            string name = Convert.ToString(property)?.Trim();
            if (int.TryParse(name, out int index))
                return index;

            int propertyIndex = Array.FindIndex(GridPropertyNames, value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
            if (propertyIndex >= 0)
                return propertyIndex;

            throw new ArgumentException($"Unknown grid property. Name: '{name}'. Valid properties: Name, FullName, IsAvailable.");
        }

        private static object GetGridPropertyValue(GridUsage grid, int propertyIndex, string nullValueText)
        {
            switch (propertyIndex)
            {
                case 0:
                    return SafeTransformStepValue(grid.Name, nullValueText);
                case 1:
                    return SafeTransformStepValue(grid.FullName, nullValueText);
                case 2:
                    return grid.IsAvailable;
                default:
                    throw new ArgumentOutOfRangeException(nameof(propertyIndex));
            }
        }




        private static readonly string[] TransformStepAspectNames = new[]
        {
            "Step Name",
            "Step MethodName",
            "Step type",
            "Step Identifier",
            "Step accuracy [m]",
            "Step is available",
            "Step has ballpark",
            "Step has inverse",
            "Step scope",
            "Remarks",
            "Nr grids",
            "Name 1st grid",
            "Name 2nd grid",
            "UsageArea.Name",
            "UsageArea.WestLongitude",
            "UsageArea.SouthLatitude",
            "UsageArea.EastLongitude",
            "UsageArea.NorthLatitude",
            "UsageArea.CenterX",
            "UsageArea.CenterY"
        };

        private static object[] GetTransformStepAspectValues(CoordinateTransform transformStep, int outputFormat, string nullValueText)
        {
            string ids;
            string scopes;
            string remarks;

            GetTransformIdentifierScopeRemarks(transformStep, out ids, out scopes, out remarks);

            string grid1 = "n/a";
            string grid2 = "n/a";

            if (transformStep.GridUsages.Count > 0)
            {
                grid1 = transformStep.GridUsages[0].Name;
                grid1 = Optional.CheckUnknown(grid1, "Missing");
            }

            if (transformStep.GridUsages.Count > 1)
            {
                grid2 = transformStep.GridUsages[1].Name;
                grid2 = Optional.CheckUnknown(grid2, "Missing");
            }

            object usageAreaName;
            object westLongitude;
            object southLatitude;
            object eastLongitude;
            object northLatitude;
            object centerX;
            object centerY;

            if (transformStep.UsageArea != null)
            {
                usageAreaName = transformStep.UsageArea.Name;
                westLongitude = FormatCoordinateValue(transformStep.UsageArea.WestLongitude);
                southLatitude = FormatCoordinateValue(transformStep.UsageArea.SouthLatitude);
                eastLongitude = FormatCoordinateValue(transformStep.UsageArea.EastLongitude);
                northLatitude = FormatCoordinateValue(transformStep.UsageArea.NorthLatitude);
                centerX = FormatCoordinateValue(transformStep.UsageArea.CenterX);
                centerY = FormatCoordinateValue(transformStep.UsageArea.CenterY);
            }
            else
            {
                usageAreaName = null;
                westLongitude = null;
                southLatitude = null;
                eastLongitude = null;
                northLatitude = null;
                centerX = null;
                centerY = null;
            }
            
            return new object[]
            {
                transformStep.Name,
                transformStep.MethodName,
                transformStep.Type.ToString(),
                SafeTransformStepValue(transformStep.Identifier is null ? null : (object)transformStep.Identifier.ToString(), nullValueText),
                SafeTransformStepValue(transformStep.Accuracy is null ? null : (object)transformStep.Accuracy.Value, nullValueText),
                transformStep.IsAvailable,
                transformStep.HasBallParkTransformation,
                transformStep.HasInverse,
                SafeTransformStepValue(scopes, nullValueText),
                SafeTransformStepValue(remarks, nullValueText),
                transformStep.GridUsages.Count,
                grid1,
                grid2,
                SafeTransformStepValue(usageAreaName, nullValueText),
                SafeTransformStepValue(westLongitude, nullValueText),
                SafeTransformStepValue(southLatitude, nullValueText),
                SafeTransformStepValue(eastLongitude, nullValueText),
                SafeTransformStepValue(northLatitude, nullValueText),
                SafeTransformStepValue(centerX, nullValueText),
                SafeTransformStepValue(centerY, nullValueText),
                GetTransformStepOutputValue(transformStep, outputFormat)
            };
        }

        private static List<CoordinateTransform> GetTransformSteps(CoordinateTransform selectedTransform)
        {
            CoordinateTransformList stepList = selectedTransform as CoordinateTransformList;
            if (stepList == null)
                return new List<CoordinateTransform> { selectedTransform };

            return stepList.ToList();
        }

        private static object[,] BuildTransformParametersResult(CoordinateTransform transform, string nullValueText, bool includeSteps = false)
        {
            var parameters = new List<KeyValuePair<int?, CoordinateTransformParameter>>();
            if (transform.Parameters != null)
            {
                foreach (CoordinateTransformParameter parameter in transform.Parameters)
                    parameters.Add(new KeyValuePair<int?, CoordinateTransformParameter>(null, parameter));
            }

            if (includeSteps && transform is CoordinateTransformList steps)
            {
                for (int stepNumber = 0; stepNumber < steps.Count; stepNumber++)
                {
                    if (steps[stepNumber].Parameters == null)
                        continue;

                    foreach (CoordinateTransformParameter parameter in steps[stepNumber].Parameters)
                        parameters.Add(new KeyValuePair<int?, CoordinateTransformParameter>(stepNumber, parameter));
                }
            }

            int rowOffset = includeSteps ? 1 : 0;
            object[,] result = new object[9 + rowOffset, parameters.Count + 1];
            result[0, 0] = "Parameter Number";
            if (includeSteps)
                result[1, 0] = "Step Number";
            result[1 + rowOffset, 0] = "Name";
            result[2 + rowOffset, 0] = "Value";
            result[3 + rowOffset, 0] = "ValueString";
            result[4 + rowOffset, 0] = "UnitName";
            result[5 + rowOffset, 0] = "UnitCategory";
            result[6 + rowOffset, 0] = "UnitConversionFactor";
            result[7 + rowOffset, 0] = "Identifier";
            result[8 + rowOffset, 0] = "UnitIdentifier";

            for (int i = 0; i < parameters.Count; i++)
            {
                CoordinateTransformParameter parameter = parameters[i].Value;
                result[0, i + 1] = i;
                if (includeSteps)
                    result[1, i + 1] = parameters[i].Key.HasValue ? (object)parameters[i].Key.Value : nullValueText;
                result[1 + rowOffset, i + 1] = SafeTransformStepValue(parameter.Name, nullValueText);
                result[2 + rowOffset, i + 1] = double.IsNaN(parameter.Value) || double.IsInfinity(parameter.Value)
                    ? (object)nullValueText : parameter.Value;
                result[3 + rowOffset, i + 1] = SafeTransformStepValue(parameter.ValueString, nullValueText);
                result[4 + rowOffset, i + 1] = SafeTransformStepValue(parameter.UnitName, nullValueText);
                result[5 + rowOffset, i + 1] = SafeTransformStepValue(parameter.UnitCategory, nullValueText);
                result[6 + rowOffset, i + 1] = double.IsNaN(parameter.UnitConversionFactor) || double.IsInfinity(parameter.UnitConversionFactor)
                    ? (object)nullValueText : parameter.UnitConversionFactor;
                result[7 + rowOffset, i + 1] = string.IsNullOrWhiteSpace(parameter.AuthName) || string.IsNullOrWhiteSpace(parameter.Code)
                    ? nullValueText : parameter.AuthName + ":" + parameter.Code;
                result[8 + rowOffset, i + 1] = string.IsNullOrWhiteSpace(parameter.UnitAuthName) || string.IsNullOrWhiteSpace(parameter.UnitCode)
                    ? nullValueText : parameter.UnitAuthName + ":" + parameter.UnitCode;
            }

            return result;
        }

        private static object[,] BuildTransformStepsResult(IList<CoordinateTransform> steps, int outputFormat, string nullValueText, bool bTranspose)
        {
            int stepCount = steps == null ? 0 : steps.Count;
            object[,] result = new object[TransformStepAspectNames.Length + 2, stepCount + 1];

            result[0, 0] = "Step Number";
            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                result[0, stepIndex + 1] = stepIndex;
            }

            for (int row = 0; row < TransformStepAspectNames.Length; row++)
            {
                result[row + 1, 0] = TransformStepAspectNames[row];
            }

            result[TransformStepAspectNames.Length + 1, 0] = "Step string rep: " + GetTransformStepOutputHeader(outputFormat);

            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                object[] values = GetTransformStepAspectValues(steps[stepIndex], outputFormat, nullValueText);
                for (int row = 0; row < values.Length; row++)
                {
                    result[row + 1, stepIndex + 1] = values[row];
                }
            }

            return bTranspose ? TransposeObjectArray(result) : result;
        }

        private static object SafeTransformStepValue(object value, string nullValueText)
        {
            if (value == null)
                return nullValueText;

            if (value is string s && string.IsNullOrWhiteSpace(s))
                return nullValueText;

            return value;
        }


        private static string GetTransformStepOutputHeader(int outputFormat)
        {
            switch (outputFormat)
            {
                default:
                case 0:
                    return "N/A";
                case 1:
                    return "Proj";
                case 2:
                    return "Wkt";
                case 3:
                    return "Json";
            }
        }

        private static string GetTransformStepOutputValue(CoordinateTransform transformStep, int outputFormat)
        {
            switch (outputFormat)
            {
                default:
                case 0:
                    return ("-");
                case 1:
                    return transformStep.AsProjString();
                case 2:
                    return transformStep.AsWellKnownText();
                case 3:
                    return transformStep.AsProjJson();
            }
        }

        private static void GetTransformIdentifierScopeRemarks(CoordinateTransform transform, out string ids, out string scopes, out string remarks, string nullValueText = "Unknown")
        {
            var identifier = transform?.Identifier;
            ids = identifier?.Authority != null && identifier.Code != null
                ? identifier.Authority + ":" + identifier.Code
                : null;
            scopes = Optional.CheckUnknown(transform?.Scope);
            remarks = Optional.CheckUnknown(transform?.Remarks, "");

            if (ids == null && transform is CoordinateTransformList ctl)
            {
                ids = string.Join(", ", ctl.Where(x => x.Identifiers != null)
                    .SelectMany(x => x.Identifiers)
                    .Where(x => x != null && x.Authority != null && x.Code != null)
                    .Select(x => x.Authority + ":" + x.Code));
                scopes = string.Join("  ", ctl.Select(x => x.Scope).Where(x => x != null).Distinct());
                remarks = string.Join("  ", ctl.Select(x => x.Remarks).Where(x => x != null).Distinct());

                ids = Optional.CheckUnknown(ids, nullValueText);
                scopes = Optional.CheckUnknown(scopes.TrimStart(' ', ','));
                remarks = Optional.CheckUnknown(remarks.TrimStart(' ', ','), "");
            }
        }

        [ExcelFunction(Name = "PE.trf.Apply", Description = "Forward transform of one or more a point(s) from source CRS to target CRS", IsExceptionSafe = false)]
        public static object Apply(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Description = "Vertical single-column of X-values (longitude or easting).")] object[,] x,
            [ExcelArgument(Description = "Vertical single-column of Y-values (latitude or northing).")] object[,] y,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options
            )
        {
            return ApplyCore(SourceCrs, TargetCrs, x, y, options, false);
        }

        [ExcelFunction(Name = "PE.trf.ApplyReversed", Description = "Reversed transform of one or more a point(s) from source CRS to target CRS", IsExceptionSafe = false)]
        public static object ApplyReversed(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Description = "Vertical single-column of X-values (longitude or easting).")] object[,] x,
            [ExcelArgument(Description = "Vertical single-column of Y-values (latitude or northing).")] object[,] y,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options
            )
        {
            return ApplyCore(SourceCrs, TargetCrs, x, y, options, true);
        }

        private static object ApplyCore(object SourceCrs, object TargetCrs, object[,] x, object[,] y, object options, bool reversed)
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                {
                    using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                    {
                        CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                        int xRows = x.GetLength(0);
                        int xCols = x.GetLength(1);
                        int yRows = y.GetLength(0);
                        int yCols = y.GetLength(1);

                        if (xCols != 1 || yCols != 1)
                            return "#ERR: X and Y must be vertical single-column ranges.";

                        if (xRows != yRows)
                            return "#ERR: X and Y ranges must have the same number of rows.";

                        int valueCount = xRows;
                        object[,] result = new object[valueCount, 2];

                        for (int row = 0; row < xRows; row++)
                        {
                            object xValue = x[row, 0];
                            object yValue = y[row, 0];

                            if (Optional.IsEmpty(xValue) || Optional.IsEmpty(yValue))
                            {
                                result[row, 0] = "";
                                result[row, 1] = "";
                                continue;
                            }

                            double sx = Convert.ToDouble(xValue);
                            double sy = Convert.ToDouble(yValue);

                            var pnt = new PPoint(sx, sy);
                            if (reversed)
                                pnt = selectedTransform.ApplyReversed(pnt);
                            else
                                pnt = selectedTransform.Apply(pnt);
                            if (double.IsNaN(pnt.X) || double.IsInfinity(pnt.X) ||
                                double.IsNaN(pnt.Y) || double.IsInfinity(pnt.Y))
                                return $"#ERR: Transform produced a non-finite coordinate at row {row + 1}.";

                            result[row, 0] = pnt.X;
                            result[row, 1] = pnt.Y;
                        }

                        return result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }


        [ExcelFunction(Name = "PE.trf.ApplyEx", Description = "Forward transform of one or more 2D/3D/4D points from source CRS to target CRS", IsExceptionSafe = false)]
        public static object ApplyEx(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Description = "Vertical table with adjacent columns for X, Y, [Z], [T] input values")] object[,] xyzt,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options
            )
        {
            return ApplyExCore(SourceCrs, TargetCrs, xyzt, options, false);
        }

        [ExcelFunction(Name = "PE.trf.ApplyReversedEx", Description = "Reversed transform of one or more 2D/3D/4D points from source CRS to target CRS", IsExceptionSafe = false)]
        public static object ApplyReversedEx(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Description = "Vertical table with adjacent columns for X, Y, [Z], [T] input values")] object[,] xyzt,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options
            )
        {
            return ApplyExCore(SourceCrs, TargetCrs, xyzt, options, true);
        }

        private static object ApplyExCore(object SourceCrs, object TargetCrs, object[,] xyzt, object options, bool reversed)
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);

            try
            {
                using (
                    
                    
                    var pc = ProjService.Default.CreateContext(optionValues))
                {
                    using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                    {
                        CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                        int rows = xyzt.GetLength(0);
                        int cols = xyzt.GetLength(1);

                        if (rows < 1)
                            return "#ERR: Need one or more (z, y, [z], [t]) coordinates";

                        if (cols < 2)
                            return "#ERR: Need (at least) two adjacent columns for X- and Y-values";

                            // start core of function
                            object[,] result = new object[rows, cols];

                            // work with nr of input columns
                            switch (cols)
                            {
                                default:
                                case 2:
                                    // we have two columns (x, y) in the input data
                                    for (int row = 0; row < rows; row++)
                                    {
                                        object xValue = xyzt[row, 0];
                                        object yValue = xyzt[row, 1];

                                        if (Optional.IsEmpty(xValue) || Optional.IsEmpty(yValue))
                                        {
                                            result[row, 0] = "";
                                            result[row, 1] = "";
                                            continue;
                                        }

                                        double sx = Convert.ToDouble(xValue);
                                        double sy = Convert.ToDouble(yValue);

                                        var pnt = new PPoint(sx, sy);
                                        if (reversed)
                                            pnt = selectedTransform.ApplyReversed(pnt);
                                        else
                                            pnt = selectedTransform.Apply(pnt);
                                        if (double.IsNaN(pnt.X) || double.IsInfinity(pnt.X) ||
                                            double.IsNaN(pnt.Y) || double.IsInfinity(pnt.Y))
                                            return $"#ERR: Transform produced a non-finite coordinate at row {row + 1}.";

                                        result[row, 0] = pnt.X;
                                        result[row, 1] = pnt.Y;
                                    }
                                    break;

                                case 3:
                                    // we have three columns (x, y, z) in the input data
                                    for (int row = 0; row < rows; row++)
                                    {
                                        object xValue = xyzt[row, 0];
                                        object yValue = xyzt[row, 1];
                                        object zValue = xyzt[row, 2];

                                        if (Optional.IsEmpty(xValue) || Optional.IsEmpty(yValue) || Optional.IsEmpty(zValue))
                                        {
                                            result[row, 0] = "";
                                            result[row, 1] = "";
                                            result[row, 2] = "";
                                            continue;
                                        }

                                        double sx = Convert.ToDouble(xValue);
                                        double sy = Convert.ToDouble(yValue);
                                        double sz = Convert.ToDouble(zValue);

                                        var pnt = new PPoint(sx, sy, sz);
                                        if (reversed)
                                            pnt = selectedTransform.ApplyReversed(pnt);
                                        else
                                            pnt = selectedTransform.Apply(pnt);
                                        if (double.IsNaN(pnt.X) || double.IsInfinity(pnt.X) ||
                                            double.IsNaN(pnt.Y) || double.IsInfinity(pnt.Y) ||
                                            double.IsNaN(pnt.Z) || double.IsInfinity(pnt.Z))
                                            return $"#ERR: Transform produced a non-finite coordinate at row {row + 1}.";

                                        result[row, 0] = pnt.X;
                                        result[row, 1] = pnt.Y;
                                        result[row, 2] = pnt.Z;
                                    }
                                    break;

                                case 4:
                                    // we have four columns (x, y, z, t) in the input data
                                    for (int row = 0; row < rows; row++)
                                    {
                                        object xValue = xyzt[row, 0];
                                        object yValue = xyzt[row, 1];
                                        object zValue = xyzt[row, 2];
                                        object tValue = xyzt[row, 3];

                                        if (Optional.IsEmpty(xValue) || Optional.IsEmpty(yValue) || Optional.IsEmpty(zValue) || Optional.IsEmpty(tValue))
                                        {
                                            result[row, 0] = "";
                                            result[row, 1] = "";
                                            result[row, 2] = "";
                                            result[row, 3] = "";
                                            continue;
                                        }

                                        double sx = Convert.ToDouble(xValue);
                                        double sy = Convert.ToDouble(yValue);
                                        double sz = Convert.ToDouble(zValue);
                                        double st = Convert.ToDouble(tValue);

                                        var pnt = new PPoint(sx, sy, sz, st);
                                        if (reversed)
                                            pnt = selectedTransform.ApplyReversed(pnt);
                                        else
                                            pnt = selectedTransform.Apply(pnt);
                                        if (double.IsNaN(pnt.X) || double.IsInfinity(pnt.X) ||
                                            double.IsNaN(pnt.Y) || double.IsInfinity(pnt.Y) ||
                                            double.IsNaN(pnt.Z) || double.IsInfinity(pnt.Z) ||
                                            double.IsNaN(pnt.T) || double.IsInfinity(pnt.T))
                                            return $"#ERR: Transform produced a non-finite coordinate at row {row + 1}.";

                                        result[row, 0] = pnt.X;
                                        result[row, 1] = pnt.Y;
                                        result[row, 2] = pnt.Z;
                                        result[row, 3] = pnt.T;
                                    }
                                    break;
                                }

                        return result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.Roundtrip", Description = "Get the cumulative error of N transform roundtrips, starting forward", IsExceptionSafe = false)]
        public static object Roundtrip(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS (or transform description)")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS (leave empty for transform)")] object TargetCrs,
            [ExcelArgument(Name = "testPoint", Description = "(x, y, [z]) test point with adjacentcoordinates")] object[,] testPoint,
            [ExcelArgument(Name = "nrRoundtrips", Description = "Nr of roundtrips to make, Default = 1")] object oRoundTrips,
            [ExcelArgument(Name = "transformOptions", Description = "Optional token string from PE.trf.Options() or PE.trf.OptionsEx(). Default: leave empty")] object options
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);

            // Check specific input data
            int nTrips = (int)Optional.Check(oRoundTrips, 1.0);
            // max three adjacent [x, y, z] cells on the same row
            int cols = testPoint.GetLength(1);
            if (testPoint.GetLength(0) > 1 || cols > 3) return "ERR: need one test point, containing up to three coordinates";

            try
            {
                using (var pc = ProjService.Default.CreateContext(optionValues))
                {
                    using (CoordinateTransform trf = CreateTransformForInput(SourceCrs, TargetCrs, optionValues, pc))
                    {
                        CoordinateTransform selectedTransform = SelectCandidateTransform(trf, optionValues.CandidateIndex);

                        double x = testPoint.GetLength(1) > 0 ? (double)testPoint[0, 0] : 0.0;
                        double y = testPoint.GetLength(1) > 1 ? (double)testPoint[0, 1] : 0.0;
                        double z = testPoint.GetLength(1) > 2 ? (double)testPoint[0, 2] : 0.0;
                        PPoint pt = new PPoint(x, y, z);

                        double error = selectedTransform.RoundTrip(true, nTrips, pt);

                        if (Double.IsInfinity(error))
                            return "#ERR: Infinite roundtrip error";

                        return error;
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        // From : D:\GitHub\SharpProj\src\SharpProj\CoordinateTransform.cpp line 49.
        // CoordinateTransform^ CoordinateTransform::Create(CoordinateReferenceSystem^ sourceCrs, CoordinateReferenceSystem^ targetCrs, CoordinateTransformOptions^ options, ProjContext^ ctx)
        // Note that ic ctx = NULL, the context is copied from the source CRS: 
        // if (!ctx) // After fromCrs
        //     ctx = sourceCrs->Context;

        // public class CoordinateReferenceSystemFilter can be used to filter a list of CRS entries using a AllowDeprecated flag

        // Instantiate a default set of parameters to be used by proj_get_crs_list():
        // auto ret = new(std::nothrow) PROJ_CRS_LIST_PARAMETERS();
        //if (ret) {
        //    ret->types = nullptr;
        //    ret->typesCount = 0;
        //    ret->crs_area_of_use_contains_bbox = TRUE;
        //    ret->bbox_valid = FALSE;
        //    ret->west_lon_degree = 0.0;
        //    ret->south_lat_degree = 0.0;
        //    ret->east_lon_degree = 0.0;
        //    ret->north_lat_degree = 0.0;
        //    ret->allow_deprecated = FALSE;
        //    ret->celestial_body_name = nullptr;
        //}
        //return ret;



        private static int GetNrCoordinateColumns(int nMode, int nDefault)
        {
            int nOut;

            switch (nMode & 7)
            {
                default:
                case 0:
                    nOut = nDefault;
                    break;
                case 1:
                    nOut = 4;
                    break;
                case 2:
                    nOut = 3;
                    break;
                case 3:
                    nOut = 2;
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    nOut = 1;
                    break;
            }

            return nOut;
        }

        private static int ParseTransformPropertyIndex(object properties)
        {
            if (Optional.IsEmpty(properties))
                throw new ArgumentException("Property is required.");

            if (properties is int n)
                return n;

            if (properties is double d)
                return (int)d;

            string name = Convert.ToString(properties);
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Property is required.");

            name = name.Trim();
            if (int.TryParse(name, out int indexFromText))
                return indexFromText;

            for (int i = 0; i < TransformPropertyNames.Length; i++)
            {
                if (string.Equals(TransformPropertyNames[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            throw new ArgumentException($"Unknown property. Name: '{name}'.");
        }

        private static object GetTransformPropertyValue(CoordinateTransform transform, int propertyIndex, string nullValueText)
        {
            string propertyName = TransformPropertyNames[propertyIndex];

            switch (propertyName)
            {
                case "Accuracy":
                    return transform.Accuracy.HasValue ? (object)transform.Accuracy.Value : nullValueText;
                case "Context":
                    return SafeCandidateContextValue(transform, nullValueText);
                case "GridUsages":
                    return transform.GridUsages?.Count ?? 0;
                case "HasBallParkTransformation":
                    return transform.HasBallParkTransformation;
                case "HasInverse":
                    return transform.HasInverse;
                case "Identifier":
                    GetTransformIdentifierScopeRemarks(transform, out string ids, out _, out _, nullValueText);
                    return ids ?? nullValueText;
                case "Identifiers":
                    if (transform.Identifiers == null || transform.Identifiers.Count == 0)
                        return nullValueText;
                    return string.Join(" | ", transform.Identifiers.Select(identifier =>
                        identifier == null || identifier.Authority == null || identifier.Code == null
                            ? nullValueText
                            : identifier.Authority + ":" + identifier.Code));
                case "IsAvailable":
                    return transform.IsAvailable;
                case "MethodName":
                    return SafeCandidateValue(transform.MethodName, nullValueText);
                case "Name":
                    return SafeCandidateValue(transform.Name, nullValueText);
                case "Parameters":
                    return FormatTransformEnumerable(transform.Parameters, nullValueText);
                case "Remarks":
                    return SafeCandidateValue(Optional.CheckUnknown(transform.Remarks, ""), nullValueText);
                case "Scope":
                    return SafeCandidateValue(Optional.CheckUnknown(transform.Scope), nullValueText);
                case "SourceCRS":
                    return SafeCandidateValue(transform.SourceCRS == null ? null : (object)transform.SourceCRS.Name, nullValueText);
                case "TargetCRS":
                    return SafeCandidateValue(transform.TargetCRS == null ? null : (object)transform.TargetCRS.Name, nullValueText);
                case "Type":
                    return transform.Type.ToString();
                case "UsageArea":
                    return FormatUsageArea(transform.UsageArea, nullValueText);
                default:
                    return nullValueText;
            }
        }

        private static string FormatTransformEnumerable(IEnumerable values, string nullValueText)
        {
            if (values == null)
                return nullValueText;

            var parts = new List<string>();
            foreach (object item in values)
            {
                if (item == null)
                    continue;

                string text = Convert.ToString(item);
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = DescribeObject(item);
                }

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                parts.Add(text);
            }

            return parts.Count == 0 ? nullValueText : string.Join(" | ", parts);
        }

        private static string FormatUsageArea(UsageArea usageArea, string nullValueText)
        {
            if (usageArea == null)
                return nullValueText;

            return string.Join("; ",
                "Name=" + Convert.ToString(usageArea.Name),
                "WestLongitude=" + FormatCoordinateValue(usageArea.WestLongitude),
                "SouthLatitude=" + FormatCoordinateValue(usageArea.SouthLatitude),
                "EastLongitude=" + FormatCoordinateValue(usageArea.EastLongitude),
                "NorthLatitude=" + FormatCoordinateValue(usageArea.NorthLatitude),
                "CenterX=" + FormatCoordinateValue(usageArea.CenterX),
                "CenterY=" + FormatCoordinateValue(usageArea.CenterY));
        }

        private static string DescribeObject(object value)
        {
            var parts = new List<string>();

            foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead)
                    continue;

                object propertyValue;
                try
                {
                    propertyValue = property.GetValue(value, null);
                }
                catch
                {
                    continue;
                }

                if (propertyValue == null)
                    continue;

                if (!IsSimpleValue(propertyValue))
                    continue;

                parts.Add(property.Name + "=" + Convert.ToString(propertyValue));
                if (parts.Count >= 6)
                    break;
            }

            return parts.Count == 0 ? string.Empty : string.Join("; ", parts);
        }

        private static bool IsSimpleValue(object value)
        {
            Type type = value.GetType();

            if (type.IsEnum)
                return true;

            if (type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal) ||
                type == typeof(DateTime))
            {
                return true;
            }

            return false;
        }


        private static readonly string[] TransformListAspectNames = new[]
        {
            "Transform Name",
            "Transform Type",
            "Identifiers",
            "Accuracy",
            "IsAvailable",
            "HasBallParkTransformation",
            "HasInverse",
            "Scope",
            "Remarks",
            "SourceCRS",
            "TargetCRS",
            "Context",
            "UsageArea.Name",
            "UsageArea.WestLongitude",
            "UsageArea.SouthLatitude",
            "UsageArea.EastLongitude",
            "UsageArea.NorthLatitude",
            "UsageArea.CenterX",
            "UsageArea.CenterY"
        };

        private static string[] TransformListAspectValues(CoordinateTransform candidate, string nullValueText)
        {
            var areaBounds = candidate?.UsageArea;
            string ids;
            string scopes;
            string remarks;
            GetTransformIdentifierScopeRemarks(candidate, out ids, out scopes, out remarks);

            return new[]
            {
                SafeCandidateValue(candidate == null ? null : (object)candidate.Name, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Type, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)ids, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Accuracy, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.IsAvailable, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.HasBallParkTransformation, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.HasInverse, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)scopes, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)remarks, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.SourceCRS, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.TargetCRS, nullValueText),
                SafeCandidateContextValue(candidate, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)areaBounds.Name, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)FormatCoordinateValue(areaBounds.WestLongitude), nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)FormatCoordinateValue(areaBounds.SouthLatitude), nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)FormatCoordinateValue(areaBounds.EastLongitude), nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)FormatCoordinateValue(areaBounds.NorthLatitude), nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)FormatCoordinateValue(areaBounds.CenterX), nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)FormatCoordinateValue(areaBounds.CenterY), nullValueText)
            };
        }


        private static object[,] BuildTransformListResult(IList<CoordinateTransform> candidates, string nullValueText, int outputFormat, bool bTranspose)
        {
            int candidateCount = candidates == null ? 0 : candidates.Count;
            object[,] result = new object[TransformListAspectNames.Length + 2, candidateCount + 1];

            result[0, 0] = "Transform Number";
            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                result[0, candidateIndex + 1] = candidateIndex;
            }

            for (int row = 0; row < TransformListAspectNames.Length; row++) // provide header names for each property
            {
                result[row + 1, 0] = TransformListAspectNames[row];
            }

            result[TransformListAspectNames.Length + 1, 0] = "Transform string rep: " + GetTransformStepOutputHeader(outputFormat);

            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                string[] values = TransformListAspectValues(candidates[candidateIndex], nullValueText);
                for (int row = 0; row < TransformListAspectNames.Length; row++)
                {
                    result[row + 1, candidateIndex + 1] = values[row];
                }

                result[TransformListAspectNames.Length + 1, candidateIndex + 1] = GetTransformStepOutputValue(candidates[candidateIndex], outputFormat);
            }

            return bTranspose ? TransposeObjectArray(result) : result;
        }

        private static object[,] TransposeObjectArray(object[,] source)
        {
            if (source == null)
                return null;

            int rows = source.GetLength(0);
            int columns = source.GetLength(1);
            object[,] transposed = new object[columns, rows];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    transposed[column, row] = source[row, column];
                }
            }

            return transposed;
        }

        private static string SafeCandidateContextValue(CoordinateTransform candidate, string nullValueText)
        {
            if (candidate == null)
                return nullValueText;

            try
            {
                ProjContext context = candidate.Context;
                if (context == null)
                    return nullValueText;

                return string.Join("; ",
                    "LogLevel=" + Convert.ToString(context.LogLevel),
                    "EndpointUrl=" + Convert.ToString(context.EndpointUrl),
                    "EnableNetworkConnections=" + Convert.ToString(context.EnableNetworkConnections));
            }
            catch
            {
                return nullValueText;
            }
        }

        private static string SafeCandidateValue(object value, string nullValueText)
        {
            if (value == null)
                return nullValueText;

            if (value is IEnumerable && !(value is string))
                return nullValueText;

            try
            {
                string text = Convert.ToString(value);
                if (string.IsNullOrWhiteSpace(text))
                    return nullValueText;

                string fullName = value.GetType().FullName;
                string name = value.GetType().Name;

                if (string.Equals(text, fullName, StringComparison.Ordinal) ||
                    string.Equals(text, name, StringComparison.Ordinal))
                    return nullValueText;

                return text;
            }
            catch
            {
                return nullValueText;
            }
        }

        // some code to pull from
        private static string BuildCandidatePros(CoordinateTransform candidate, int index, bool hasMultipleCandidates)
        {
            var pros = new List<string>();

            if (candidate == null)
                return string.Empty;

            if (candidate.IsAvailable)
                pros.Add("Available");

            if (!candidate.HasBallParkTransformation)
                pros.Add("Non-ballpark");

            if (candidate.Accuracy.HasValue)
                pros.Add("Accuracy=" + candidate.Accuracy.Value.ToString("0.###"));

            if (candidate.UsageArea != null)
                pros.Add("Has usage area");

            if (!hasMultipleCandidates || index == 0)
                pros.Add("Default selection");

            return string.Join("; ", pros);
        }

        private static string BuildCandidateCons(CoordinateTransform candidate, int index, bool hasMultipleCandidates)
        {
            var cons = new List<string>();

            if (candidate == null)
                return string.Empty;

            if (!candidate.IsAvailable)
                cons.Add("Not available");

            if (candidate.HasBallParkTransformation)
                cons.Add("Ballpark transformation");

            if (!candidate.Accuracy.HasValue)
                cons.Add("No reported accuracy");

            if (hasMultipleCandidates && index > 0)
                cons.Add("Alternate candidate");

            return string.Join("; ", cons);
        }

        private static object[,] ToObjectTable(List<object[]> rows)
        {
            if (rows == null || rows.Count == 0)
                return new object[0, 0];

            int columnCount = rows.Max(r => r == null ? 0 : r.Length);
            object[,] result = new object[rows.Count, columnCount];

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                object[] row = rows[rowIndex] ?? Array.Empty<object>();
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    result[rowIndex, columnIndex] = columnIndex < row.Length ? row[columnIndex] : null;
                }
            }

            return result;
        }
    }
}
