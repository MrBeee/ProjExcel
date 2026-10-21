using ExcelDna.Integration;
using MathNet.Numerics.IntegralTransforms;
using SharpProj;
using SharpProj.Proj;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;


namespace ProjExcel
{
    public static class TransformFunctions
    {
        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }

        // SharpProj supports the following options; try to find the relevant one automatically based on the input type.
        // The following are the relevant Create() methods in SharpProj:

        // public new unsafe static CoordinateReferenceSystem   Create(string[] from, [Optional] ProjContext ctx)
        // public new unsafe static CoordinateReferenceSystem   Create(string from, [Optional] ProjContext ctx)

        // public new unsafe static CoordinateReferenceSystem   CreateFromWellKnownText(string from, CreateFromWKTOptions options, out string[] warnings, [Optional] ProjContext ctx)
        // public new static CoordinateReferenceSystem          CreateFromWellKnownText(string from, out string[] warnings, [Optional] ProjContext ctx)
        // public new static CoordinateReferenceSystem          CreateFromWellKnownText(string from, CreateFromWKTOptions options, [Optional] ProjContext ctx)
        // public new static CoordinateReferenceSystem          CreateFromWellKnownText(string from, [Optional] ProjContext ctx)

        // public static CoordinateReferenceSystem              CreateFromDatabase(Identifier identifier, [Optional] ProjContext ctx)
        // public static CoordinateReferenceSystem              CreateFromDatabase(string authority, int code, [Optional] ProjContext ctx)
        // public unsafe static CoordinateReferenceSystem       CreateFromDatabase(string authority, string code, [Optional] ProjContext ctx)

        // public static CoordinateReferenceSystem              CreateFromEpsg(int epsgCode, [Optional] ProjContext ctx)

        // The next routine allows for wider input than just epsg integer values; It returns a CoordinateReferenceSystem from various input types
        // It is also used to normalize the input CRS or transform specification into a CoordinateReferenceSystem object.
        // It has implemented all above Creation methods, apart from the stringlist variant: (string[] from).

        internal static CoordinateReferenceSystem CreateNormalizedCrs(in object oTransform, ProjContext pc)
        {
            if (Optional.IsEmpty(oTransform))
                throw new ArgumentException("CRS specification is empty.");

            if (oTransform is double)
            {
                // First cast to double, then to int, to deal with Excel datatypes
                int nTransform = (int)(double)oTransform;

                // we have an EPSG number from a single input parameter:
                return CoordinateReferenceSystem.CreateFromEpsg(nTransform, pc).WithNormalizedAxis(pc);
            }
            else if (oTransform is string)
            {
                // cast to string, to deal with Excel datatypes
                string sTransform = (string)oTransform;

                bool success = int.TryParse(sTransform, out int nTransform);

                if (success)
                {
                    // we have a simple EPSG number
                    return CoordinateReferenceSystem.CreateFromEpsg(nTransform, pc).WithNormalizedAxis(pc);
                }
                else
                {
                    // we have a string of some sorts. The first thing to check is if it starts with an Authority ID
                    bool hasAuthorityPrefix = Lst.GetAuthorityList()
                        .Any(authority => sTransform.StartsWith(authority + ":", StringComparison.OrdinalIgnoreCase));

                    if (hasAuthorityPrefix)
                    {
                        // we have an authority prefix, so we can try to parse it as an authority code
                        var parts = sTransform.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out nTransform))
                        {
                            return CoordinateReferenceSystem.CreateFromDatabase(parts[0], nTransform, pc).WithNormalizedAxis(pc);
                        }
                        else if (parts.Length == 2)
                        {
                            // If the second part is not an integer, we can try to create from a string code
                            return CoordinateReferenceSystem.CreateFromDatabase(parts[0], parts[1], pc).WithNormalizedAxis(pc);
                        }
                        else
                        {
                            throw new ArgumentException("Invalid authority code format.");
                        }
                    }
                    else
                    {
                        // We have NO authority prefix, so it must be a different type of string.
                        // It could be a WKT string or a PROJ string. We can try to detect it by looking for certain keywords.
                        if ((sTransform.IndexOf("PROJCRS") > -1) || (sTransform.IndexOf("GEOGCRS") > -1) || (sTransform.IndexOf("SPHEROID") > -1))
                        {
                            // it must be WKT (well, we hope so)
                            return CoordinateReferenceSystem.CreateFromWellKnownText(sTransform, pc).WithNormalizedAxis(pc);

                        }

                        // Otherwise, assume it's a PROJ string
                        return CoordinateReferenceSystem.Create(sTransform, pc).WithNormalizedAxis(pc);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Unsupported CRS input type.");
            }
        }

        internal static int ParseOutputString(object value)
        {
            string output = Convert.ToString(value);

            if (string.IsNullOrWhiteSpace(output))
                return 0;

            if (string.Equals(output, "Proj", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (string.Equals(output, "Wkt", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(output, "Json", StringComparison.OrdinalIgnoreCase))
                return 2;

            return 0;
        }

        [ExcelFunction(Name = "PE.trf.TransformSteps", Description = "List the steps in the transform (pipeline) from source CRS to target CRS")]
        public static object TransformSteps(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS")] object TargetCrs,
            [ExcelArgument(Name = "options", Description = "Optional token string created by PE.trf.Options()")] object options,
            [ExcelArgument(Name = "outputString", Description = "Proj, Wkt or Json (Proj)")] object outputString = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            CoordinateTransformOptions coordinateTransformOptions = TransformOptions.CreateCoordinateTransformOptions(optionValues);
            int actualOutputString = ParseOutputString(outputString);

            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CreateNormalizedCrs(SourceCrs, pc))
                    using (var dst = CreateNormalizedCrs(TargetCrs, pc))
                    {
                        // Early-exit validation check (Saves CPU cycles before pipeline creation)
                        if (src.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Source CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        if (dst.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Destination CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        CoordinateTransform trf = null;

                        try
                        {
                            // Attempt #1: Try the absolute best precision transform path
                            trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                        }
                        catch (Exception ex) when (ex is ProjException || ex is ArgumentNullException)
                        {
                            // Fallback: If BestOnly caused a null pointer crash or missing grid exception, 
                            // drop the strict constraint to allow the next best mathematical equation path.
                            if (coordinateTransformOptions.BestOnly)
                            {
                                coordinateTransformOptions.BestOnly = false;
                                trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                            }
                            else
                            {
                                // If it wasn't a BestOnly issue, rethrow the original error
                                throw;
                            }
                        }


                        using (trf)
                        {
                            CoordinateTransform selectedTransform = null;

                            if (trf == null)
                                return "#ERR: No mathematical transformation pipeline could be constructed.";

                            if (trf is ChooseCoordinateTransform list)
                            {
                                if (optionValues.CandidateIndex >= list.Count)
                                    return "#ERR: Candidate Index for transform out of range.";

                                selectedTransform = list[optionValues.CandidateIndex];
                            }
                            else
                            {
                                if (optionValues.CandidateIndex >= 1)
                                    return "#ERR: Candidate Index for transform out of range.";
                                
                                selectedTransform = trf;    
                            }

                            // cct.cs line 3725 spew out TL.cct.Transforms.ListAll information
                            CoordinateTransformList stepList = selectedTransform as CoordinateTransformList;

                            int stepCount = (stepList != null) ? stepList.Count : 1;

                            object[,] result = new object[15, stepCount + 1];

                            result[0, 0] = "Step Number";
                            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
                            {
                                result[0, stepIndex + 1] = stepIndex;
                            }

                            result[1, 0] = "Step Identifiers";
                            result[2, 0] = "Step Name";
                            result[3, 0] = "Step sccuracy [m]";
                            result[4, 0] = "UsageArea";
                            result[5, 0] = "Scope";
                            result[6, 0] = "Remarks";
                            result[7, 0] = "UsageArea.WestLongitude";
                            result[8, 0] = "UsageArea.SouthLatitude";
                            result[9, 0] = "UsageArea.EastLongitude";
                            result[10, 0] = "UsageArea.NorthLatitude";
                            result[11, 0] = "Nr grids";
                            result[12, 0] = "1st grid";
                            result[13, 0] = "2nd grid";

                            switch (actualOutputString)
                            {
                                default:
                                case 0:
                                    result[14, 0] = "Step PROJ string";
                                    break;
                                case 1:
                                    result[14, 0] = "Step WKT string";
                                    break;
                                case 2:
                                    result[14, 0] = "Step JSON string";
                                    break;
                            }

                            // now give it some info

                            if (stepCount == 1)
                            {
                                string ids = selectedTransform.Identifier?.ToString();
                                string scopes = Optional.CheckUnknown(selectedTransform.Scope);
                                string remarks = Optional.CheckUnknown(selectedTransform.Remarks, "None");
                                if (ids == null && selectedTransform is CoordinateTransformList ctl)
                                {
                                    ids = string.Join(", ", ctl.Where(x => x.Identifiers != null).SelectMany(x => x.Identifiers));
                                    scopes = string.Join("  ", ctl.Select(x => x.Scope).Where(x => x != null).Distinct());
                                    remarks = string.Join("  ", ctl.Select(x => x.Remarks).Where(x => x != null).Distinct());

                                    ids = Optional.CheckUnknown(ids.TrimStart(' ', ','));
                                    scopes = Optional.CheckUnknown(scopes.TrimStart(' ', ','));
                                    remarks = Optional.CheckUnknown(remarks.TrimStart(' ', ','), "None");
                                }

                                double? accuracy = coordinateTransformOptions.Accuracy;

                                string grid1 = "N.A.";
                                string grid2 = "N.A.";
                                if (selectedTransform.GridUsages.Count > 0)
                                {
                                    grid1 = selectedTransform.GridUsages[0].FullName;
                                    Optional.CheckUnknown(grid1, "Missing");
                                }

                                if (selectedTransform.GridUsages.Count > 1)
                                {
                                    grid2 = selectedTransform.GridUsages[1].FullName;
                                    Optional.CheckUnknown(grid2, "Missing");
                                }

                                result[1, 1] = selectedTransform.Identifiers is null ? ids : selectedTransform.Identifier.Authority; ;
                                result[2, 1] = selectedTransform.Name;
                                result[3, 1] = accuracy;
                                result[5, 1] = scopes;
                                result[6, 1] = remarks;

                                if (selectedTransform.UsageArea != null)
                                {
                                    result[4, 1] = selectedTransform.UsageArea.Name;
                                    result[7, 1] = selectedTransform.UsageArea.WestLongitude;
                                    result[8, 1] = selectedTransform.UsageArea.EastLongitude;
                                    result[9, 1] = selectedTransform.UsageArea.SouthLatitude;
                                    result[10, 1] = selectedTransform.UsageArea.NorthLatitude;
                                }
                                else
                                {
                                    result[4, 1] = "Unknown";
                                    result[7, 1] = -1000;
                                    result[8, 1] = -1000;
                                    result[9, 1] = -1000;
                                    result[10, 1] = -1000;
                                }

                                result[11, 1] = selectedTransform.GridUsages.Count;
                                result[12, 1] = grid1;
                                result[13, 1] = grid2;

                                switch (actualOutputString)
                                {
                                    default:
                                    case 0:
                                        result[14, 1] = selectedTransform.AsProjString();
                                        break;
                                    case 1:
                                        result[14, 1] = selectedTransform.AsWellKnownText();
                                        break;
                                    case 2:
                                        result[14, 1] = selectedTransform.AsProjJson();
                                        break;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < stepCount; i++)
                                {
                                    string ids = stepList[i].Identifier?.ToString();
                                    string scopes = Optional.CheckUnknown(stepList[i].Scope);
                                    string remarks = Optional.CheckUnknown(stepList[i].Remarks, "None");
                                    if (ids == null && stepList[i] is CoordinateTransformList ctl)
                                    {
                                        ids = string.Join(", ", ctl.Where(x => x.Identifiers != null).SelectMany(x => x.Identifiers));
                                        scopes = string.Join("  ", ctl.Select(x => x.Scope).Where(x => x != null).Distinct());
                                        remarks = string.Join("  ", ctl.Select(x => x.Remarks).Where(x => x != null).Distinct());

                                        ids = Optional.CheckUnknown(ids.TrimStart(' ', ','));
                                        scopes = Optional.CheckUnknown(scopes.TrimStart(' ', ','));
                                        remarks = Optional.CheckUnknown(remarks.TrimStart(' ', ','), "None");
                                    }

                                    double? accuracy = coordinateTransformOptions.Accuracy;

                                    string grid1 = "N.A.";
                                    string grid2 = "N.A.";

                                    if (stepList[i].GridUsages.Count > 0)
                                    {
                                        grid1 = stepList[i].GridUsages[0].FullName;
                                        Optional.CheckUnknown(grid1, "Missing");
                                    }

                                    if (stepList[i].GridUsages.Count > 1)
                                    {
                                        grid2 = stepList[i].GridUsages[1].FullName;
                                        Optional.CheckUnknown(grid2, "Missing");
                                    }

                                    result[1, i + 1] = stepList[i].Identifiers is null ? ids : stepList[i].Identifier.Authority;
                                    result[2, i + 1] = stepList[i].Name;
                                    result[3, i + 1] = accuracy;
                                    result[5, i + 1] = scopes;
                                    result[6, i + 1] = remarks;
                                    if (stepList[i].UsageArea != null)
                                    {
                                        result[4, i + 1] = stepList[i].UsageArea.Name;
                                        result[7, i + 1] = stepList[i].UsageArea.WestLongitude;
                                        result[8, i + 1] = stepList[i].UsageArea.EastLongitude;
                                        result[9, i + 1] = stepList[i].UsageArea.SouthLatitude;
                                        result[10, i + 1] = stepList[i].UsageArea.NorthLatitude;
                                    }
                                    else
                                    {
                                        result[4, 1] = "Unknown";
                                        result[7, 1] = -1000;
                                        result[8, 1] = -1000;
                                        result[9, 1] = -1000;
                                        result[10, 1] = -1000;
                                    }

                                    result[11, i + 1] = stepList[i].GridUsages.Count;
                                    result[12, i + 1] = grid1;
                                    result[13, i + 1] = grid2;

                                    switch (actualOutputString)
                                    {
                                        default:
                                        case 0:
                                            result[14, i + 1] = stepList[i].AsProjString();
                                            break;
                                        case 1:
                                            result[14, i + 1] = stepList[i].AsWellKnownText();
                                            break;
                                        case 2:
                                            result[14, i + 1] = stepList[i].AsProjJson();
                                            break;
                                    }
                                }
                            }

                            return result;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }


        [ExcelFunction(Name = "PE.trf.Transform", Description = "Transform a point from source CRS to target CRS")]
        public static object Transform(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS")] object TargetCrs,
            [ExcelArgument(Description = "X (longitude or easting).")] double x,
            [ExcelArgument(Description = "Y (latitude or northing).")] double y,
            [ExcelArgument(Name = "options", Description = "Optional token string created by PE.trf.Options()")] object options
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            CoordinateTransformOptions coordinateTransformOptions = TransformOptions.CreateCoordinateTransformOptions(optionValues);

            int actualSourceCrs = Optional.Check(SourceCrs, 4326);
            int actualTargetCrs = Optional.Check(TargetCrs, 4326);

            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    {
                        // Early-exit validation check (Saves CPU cycles before pipeline creation)
                        if (src.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Source CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        if (dst.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Destination CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        CoordinateTransform trf = null;

                        try
                        {
                            // Attempt #1: Try the absolute best precision transform path
                            trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                        }
                        catch (Exception ex) when (ex is ProjException || ex is ArgumentNullException)
                        {
                            // Fallback: If BestOnly caused a null pointer crash or missing grid exception, 
                            // drop the strict constraint to allow the next best mathematical equation path.
                            if (coordinateTransformOptions.BestOnly)
                            {
                                coordinateTransformOptions.BestOnly = false;
                                trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                            }
                            else
                            {
                                // If it wasn't a BestOnly issue, rethrow the original error
                                throw;
                            }
                        }

                        using (trf)
                        {
                            if (trf == null)
                                return "#ERR: No mathematical transformation pipeline could be constructed.";

                            // Handle explicit Multi-Candidate Selection wrappers if applicable
                            if (optionValues.CandidateIndex > 0)
                            {
                                if (trf is ChooseCoordinateTransform list)
                                {
                                    if (optionValues.CandidateIndex >= list.Count)
                                        return "#ERR: Candidate Index for transform out of range.";

                                    var trfFromList = list[optionValues.CandidateIndex];
                                    var pntFromList = trfFromList.Apply(new PPoint(x, y));
                                    return new object[,] { { pntFromList.X, pntFromList.Y } };
                                }
                                else
                                {
                                    return "#ERR: Candidate Index for transform out of range. Only one candidate available.";
                                }
                            }

                            // Standard execution route using the best available operational path
                            var pnt = trf.Apply(new PPoint(x, y));
                            return new object[,] { { pnt.X, pnt.Y } };
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.TransformList", Description = "Create a list of available transforms between source and target CRS")]
        public static object TransformList(
            [ExcelArgument(Name = "sourceCrs", Description = "Source CRS")] object SourceCrs,
            [ExcelArgument(Name = "targetCrs", Description = "Target CRS")] object TargetCrs,
            [ExcelArgument(Name = "options", Description = "Optional token string created by PE.trf.Options()")] object options,
            [ExcelArgument(Name = "nullValues", Description = "Optional replacement string for null values. Default = \"<null>\"")] object nullValues = null,
            [ExcelArgument("Transpose the list", Name = "Transpose")] object transpose = null
            )
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            CoordinateTransformOptions coordinateTransformOptions = TransformOptions.CreateCoordinateTransformOptions(optionValues);
            string nullValueText = Optional.Check(nullValues, "<null>");

            int actualSourceCrs = Optional.Check(SourceCrs, 4326);
            int actualTargetCrs = Optional.Check(TargetCrs, 4326);
            bool bTranspose = Optional.Check(transpose, false);

            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    {
                        if (src.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Source CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        if (dst.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Destination CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        CoordinateTransform trf = null;

                        try
                        {
                            // Attempt #1: Try the absolute best precision transform path
                            trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                        }
                        catch (Exception ex) when (ex is ProjException || ex is ArgumentNullException)
                        {
                            // Fallback: If BestOnly caused a null pointer crash or missing grid exception, 
                            // drop the strict constraint to allow the next best mathematical equation path.
                            if (coordinateTransformOptions.BestOnly)
                            {
                                coordinateTransformOptions.BestOnly = false;
                                trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc);
                            }
                            else
                            {
                                // If it wasn't a BestOnly issue, rethrow the original error
                                throw;
                            }
                        }

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

                        return BuildTransformListResult(candidates, nullValueText, bTranspose);
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }




        [ExcelFunction(Name = "PE.trf.LatLongToWebMercator", Description = "Converts Lat/Lon degrees (EPSG:4326) to Web Mercator meters (EPSG:3857) using SharpProj")]
        public static object TransformLatLongToWebMercator(
            [ExcelArgument(Name = "latitude", Description = "Latitude in decimal degrees (e.g. 52.3676)")] double latitude,
            [ExcelArgument(Name = "longitude", Description = "Longitude in decimal degrees (e.g. 4.9041)")] double longitude)
        {
            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst, pc))
                    {
                        // note CoordinateTransform::Apply() is equal to CoordinateTransform::DoTransform(with bool forward = true)
                        // void CoordinateTransform::ApplyReversed() is equal to CoordinateTransform::DoTransform(with bool forward = false)
                        var r = trf.Apply(new PPoint(longitude, latitude));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        [ExcelFunction(Name = "PE.trf.LatLongToWebMercatorEx", Description = "Converts Lon/Lat degrees (EPSG:4326) normalized input, to Web Mercator meters (EPSG:3857)")]
        public static object TransformLatLongToWebMercatorEx(
            [ExcelArgument(Name = "latitude", Description = "Latitude in decimal degrees (e.g. 52.3676)")] double latitude,
            [ExcelArgument(Name = "longitude", Description = "Longitude in decimal degrees (e.g. 4.9041)")] double longitude, 
            [ExcelArgument(Name = "Options", Description = "Transformation options read from a cell defined by =PE.trf.options()") ] object options)
        {
            TransformOptions.CoordinateTransformOptionsInput optionValues = TransformOptions.CoordinateTransformOptionsInput.Parse(options);
            CoordinateTransformOptions coordinateTransformOptions = TransformOptions.CreateCoordinateTransformOptions(optionValues);

            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst, coordinateTransformOptions, pc))
                    {
                        if (src.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Source CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        if (dst.IsDeprecated && !optionValues.AllowDeprecatedCrs)
                            return "#ERR: Destination CRS is deprecated and AllowDeprecatedCrs option is not set.";

                        if (optionValues.CandidateIndex > 0)
                        {
                            if (trf is ChooseCoordinateTransform list)
                            {
                                if (optionValues.CandidateIndex >= list.Count)
                                    return "#ERR: Candidate Index for transform out of range.";

                                var trfFromList = list[optionValues.CandidateIndex];
                                var pntFromList = trfFromList.Apply(new PPoint(longitude, latitude));
                                return new object[,] { { pntFromList.X, pntFromList.Y } };
                            }
                            else
                            {
                                return "#ERR: Candidate Index for transform out of range. Only one candidate available.";
                            }
                        }

                        var pnt = trf.Apply(new PPoint(longitude, latitude));
                        return new object[,] { { pnt.X, pnt.Y } };
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







        private static CoordinateTransform CreateCoordinateTransform(
            CoordinateReferenceSystem crsSource,
            CoordinateReferenceSystem crsTarget,
            CoordinateTransformOptions coordinateTransformOptions,
            ProjContext pjContext)
        {
            return CreateCoordinateTransform(crsSource, crsTarget, coordinateTransformOptions, pjContext, true); // true stands for allowDeprecatedCrs input
        }

        private static CoordinateTransform CreateCoordinateTransform(
            CoordinateReferenceSystem crsSource,
            CoordinateReferenceSystem crsTarget,
            ProjContext pjContext)
        {
            return CreateCoordinateTransform(crsSource, crsTarget, new CoordinateTransformOptions(), pjContext, true); // true stands for allowDeprecatedCrs input
        }

        private static CoordinateTransform CreateCoordinateTransform(
            CoordinateReferenceSystem crsSource,
            CoordinateReferenceSystem crsTarget,
            ProjContext pjContext,
            bool allowDeprecatedCrs)    // allowDeprecatedCrs is not part of CoordinateTransformOptions, so we need to pass it separately
        {
            return CreateCoordinateTransform(crsSource, crsTarget, new CoordinateTransformOptions(), pjContext, allowDeprecatedCrs);
        }

        private static CoordinateTransform CreateCoordinateTransform(object[,] oTransform, ProjContext pjContext)
        {
            return CreateCoordinateTransform(oTransform, pjContext, true);  // true stands for allowDeprecatedCrs input
        }


        // the next function contains bugs; allowDeprecatedCrs is passed to CreateCoordinateTransform(crsSource, crsTarget, options, pjContext, allowDeprecatedCrs)
        // but not used in the function body
        private static CoordinateTransform CreateCoordinateTransform(object[,] oTransform, ProjContext pjContext, bool allowDeprecatedCrs)
        {
            if (oTransform == null)
                throw new ArgumentException("Incorrect coordinate transform format");

            int nTransformRows = oTransform.GetLength(0);
            int nTransformCols = oTransform.GetLength(1);

            if (nTransformRows != 1 || nTransformCols > 2)
                throw new ArgumentException("Incorrect coordinate transform format");

            int nTransform;
            string sTransform;

            if (nTransformCols == 1)
            {
                if (oTransform[0, 0] is double)
                {
                    nTransform = (int)(double)oTransform[0, 0];
                    return CoordinateTransform.CreateFromEpsg(nTransform, pjContext);
                }

                if (oTransform[0, 0] is string)
                {
                    sTransform = (string)oTransform[0, 0];

                    bool success = int.TryParse(sTransform, out nTransform);
                    if (success)
                        return CoordinateTransform.CreateFromEpsg(nTransform, pjContext);

                    if ((sTransform.IndexOf("PROJCRS", StringComparison.OrdinalIgnoreCase) > -1) ||
                        (sTransform.IndexOf("GEOGCRS", StringComparison.OrdinalIgnoreCase) > -1) ||
                        (sTransform.IndexOf("SPHEROID", StringComparison.OrdinalIgnoreCase) > -1))
                    {
                        return (CoordinateTransform)CoordinateTransform.CreateFromWellKnownText(sTransform, pjContext);
                    }

                    return CoordinateTransform.Create(sTransform, pjContext);
                }

                throw new ArgumentException("Incorrect coordinate transform format");
            }

            sTransform = Convert.ToString(oTransform[0, 0]);

            if (oTransform[0, 1] is double)
            {
                nTransform = (int)(double)oTransform[0, 1];
            }
            else if (oTransform[0, 1] is string)
            {
                string sTmp = (string)oTransform[0, 1];
                bool success = int.TryParse(sTmp, out nTransform);
                if (!success)
                    throw new ArgumentException("Incorrect coordinate transform format");
            }
            else
            {
                throw new ArgumentException("Incorrect coordinate transform format");
            }

            return CoordinateTransform.CreateFromDatabase(sTransform, nTransform, pjContext);
        }


        [ExcelFunction(Name = "PE.trf.ApplyForwardEx", Description = "Coordinate conversion with explicit advanced options")]
        public static object ApplyForwardEx(
            [ExcelArgument(Name = "sourceCrsOrTransform", Description = "Source CRS or transform definition")] object[,] SourceCrs,
            [ExcelArgument(Name = "targetCrsOrNul", Description = "Target CRS or blank when source is a transform")] object[,] TargetCrs,
            [ExcelArgument(Name = "Coordinate(s)", Description = "Input coordinate array")] double[,] Coords,
            [ExcelArgument(Name = "outputMode", Description = "(0) 0 = input width, 1 = xyzt, 2 = xyz, 3 = xy, 4 = x, 5 = y, 6 = z, 7 = t")] object oOutputMode,
            [ExcelArgument(Name = "options", Description = "Options token created by PE.trf.Options2")] object oOptions
            )
        {
            return ApplyTransformEx(SourceCrs, TargetCrs, Coords, oOutputMode, oOptions, false);
        }

        [ExcelFunction(Name = "PE.trf.ApplyInverseEx", Description = "Inverse coordinate conversion with explicit advanced options")]
        public static object ApplyInverseEx(
            [ExcelArgument(Name = "sourceCrsOrTransform", Description = "Source CRS or transform definition")] object[,] SourceCrs,
            [ExcelArgument(Name = "targetCrsOrNul", Description = "Target CRS or blank when source is a transform")] object[,] TargetCrs,
            [ExcelArgument(Name = "Coordinate(s)", Description = "Input coordinate array")] double[,] Coords,
            [ExcelArgument(Name = "outputMode", Description = "0 = input width, 1 = xyzt, 2 = xyz, 3 = xy, 4 = x, 5 = y, 6 = z, 7 = t")] object oOutputMode,
            [ExcelArgument(Name = "options", Description = "Options token created by PE.trf.Options2")] object oOptions
            )
        {
            return ApplyTransformEx(SourceCrs, TargetCrs, Coords, oOutputMode, oOptions, true);
        }

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

        private static object ApplyTransformEx(object[,] SourceCrs, object[,] TargetCrs, double[,] Coords, object oOutputMode, object oOptions, bool inverse)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int outputMode = Optional.Check(oOutputMode, 0);
            bool usingTransform = Optional.IsNul(TargetCrs);

            if (outputMode < 0 || outputMode > 7)
                return "#ERR: Invalid input.";

            int nCoordRows = Coords.GetLength(0);
            int nCoordCols = Coords.GetLength(1);

            if (nCoordRows < 1 || nCoordCols < 2 || nCoordCols > 4)
                return "#ERR: Invalid input.";

            int nOut = GetNrCoordinateColumns(outputMode, nCoordCols);
            TransformOptions.CoordinateTransformOptionsInput values = TransformOptions.CoordinateTransformOptionsInput.Parse(oOptions);
            if (!string.IsNullOrWhiteSpace(values.ErrorMessage))
                return "#ERR: " + values.ErrorMessage;

            CoordinateTransformOptions options = TransformOptions.CreateCoordinateTransformOptions(values);

            ProjContext pjContext = null;
            CoordinateReferenceSystem crsSource = null;
            CoordinateReferenceSystem crsTarget = null;
            CoordinateTransform transform = null;

            try
            {
                pjContext = CreateContext();

                if (usingTransform)
                    transform = CreateCoordinateTransform(SourceCrs, pjContext);
                else
                {
                    crsSource = Crs.CreateCrs(SourceCrs, pjContext).WithNormalizedAxis(pjContext);
                    crsTarget = Crs.CreateCrs(TargetCrs, pjContext).WithNormalizedAxis(pjContext);
                    transform = CreateCoordinateTransform(crsSource, crsTarget, options, pjContext, values.AllowDeprecatedCrs);
                }

                double[] x = new double[nCoordRows];
                double[] y = new double[nCoordRows];
                double[] z = new double[nCoordRows];
                double[] t = new double[nCoordRows];
                object[,] res = new object[nCoordRows, nOut];
                Exception lastException = null;

                for (int i = 0; i < nCoordRows; i++)
                {
                    x[i] = Coords[i, 0];
                    y[i] = Coords[i, 1];

                    if (nCoordCols > 2)
                        z[i] = Coords[i, 2];

                    if (nCoordCols > 3)
                        t[i] = Coords[i, 3];
                }

                try
                {
                    switch (nCoordCols)
                    {
                        default:
                        case 2:
                            if (inverse)
                                transform.ApplyReversed(x, y);
                            else
                                transform.Apply(x, y);
                            break;

                        case 3:
                            if (inverse)
                                transform.ApplyReversed(x, y, z);
                            else
                                transform.Apply(x, y, z);
                            break;

                        case 4:
                            if (inverse)
                                transform.ApplyReversed(x, y, z, t);
                            else
                                transform.Apply(x, y, z, t);
                            break;
                    }


                    switch (outputMode & 7)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            if (nOut == 2)
                            {
                                for (int i = 0; i < nCoordRows; i++)
                                {
                                    res[i, 0] = Optional.CheckNan(x[i]);
                                    res[i, 1] = Optional.CheckNan(y[i]);
                                }
                            }
                            else if (nOut == 3)
                            {
                                for (int i = 0; i < nCoordRows; i++)
                                {
                                    res[i, 0] = Optional.CheckNan(x[i]);
                                    res[i, 1] = Optional.CheckNan(y[i]);
                                    res[i, 2] = Optional.CheckNan(z[i]);
                                }
                            }
                            else if (nOut == 4)
                            {
                                for (int i = 0; i < nCoordRows; i++)
                                {
                                    res[i, 0] = Optional.CheckNan(x[i]);
                                    res[i, 1] = Optional.CheckNan(y[i]);
                                    res[i, 2] = Optional.CheckNan(z[i]);
                                    res[i, 3] = Optional.CheckNan(t[i]);
                                }
                            }
                            else
                                return "#ERR: Invalid input.";
                            break;

                        case 4:
                            for (int i = 0; i < nCoordRows; i++)
                                res[i, 0] = Optional.CheckNan(x[i]);
                            break;

                        case 5:
                            for (int i = 0; i < nCoordRows; i++)
                                res[i, 0] = Optional.CheckNan(y[i]);
                            break;

                        case 6:
                            for (int i = 0; i < nCoordRows; i++)
                                res[i, 0] = Optional.CheckNan(z[i]);
                            break;

                        case 7:
                            for (int i = 0; i < nCoordRows; i++)
                                res[i, 0] = Optional.CheckNan(t[i]);
                            break;
                    }

                        return res;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        return lastException != null
                                ? AddIn.ProcessException(lastException)
                                : "#ERR: No valid coordinate transform.";
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
            finally
            {
                transform?.Dispose();
                crsSource?.Dispose();
                crsTarget?.Dispose();
                pjContext?.Dispose();
            }
        }

        private static object ApplyTransformExOld(object[,] SourceCrs, object[,] TargetCrs, double[,] Coords, object oOutputMode, object oOptions, bool inverse)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int outputMode = Optional.Check(oOutputMode, 0);
            bool usingTransform = Optional.IsNul(TargetCrs);

            if (outputMode < 0 || outputMode > 7)
                return "#ERR: Invalid input.";

            int nCoordRows = Coords.GetLength(0);
            int nCoordCols = Coords.GetLength(1);

            if (nCoordRows < 1 || nCoordCols < 2 || nCoordCols > 4)
                return "#ERR: Invalid input.";

            int nOut = GetNrCoordinateColumns(outputMode, nCoordCols);
            TransformOptions.CoordinateTransformOptionsInput values = TransformOptions.CoordinateTransformOptionsInput.Parse(oOptions);
            if (!string.IsNullOrWhiteSpace(values.ErrorMessage))
                return "#ERR: " + values.ErrorMessage;

            CoordinateTransformOptions options = TransformOptions.CreateCoordinateTransformOptions(values);

            ProjContext pjContext = null;
            CoordinateReferenceSystem crsSource = null;
            CoordinateReferenceSystem crsTarget = null;
            CoordinateTransform transform = null;

            try
            {
                pjContext = CreateContext();

                if (usingTransform)
                    transform = CreateCoordinateTransform(SourceCrs, pjContext);
                else
                {
                    crsSource = Crs.CreateCrs(SourceCrs, pjContext);
                    crsTarget = Crs.CreateCrs(TargetCrs, pjContext);
                    transform = CreateCoordinateTransform(crsSource, crsTarget, options, pjContext, values.AllowDeprecatedCrs);
                }

                List<CoordinateTransform> candidates = GetCandidateTransforms(transform, options);
                if (candidates == null)
                    candidates = new List<CoordinateTransform>();

                CoordinateTransform axisOrderNormalization = null;
                List<CoordinateTransform> coreCandidates = new List<CoordinateTransform>();

                for (int i = 0; i < candidates.Count; i++)
                {
                    CoordinateTransform candidate = candidates[i];
                    if (candidate != null &&
                        !string.IsNullOrEmpty(candidate.Name) &&
                        candidate.Name.IndexOf("axis order change", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (axisOrderNormalization == null)
                            axisOrderNormalization = candidate;
                    }
                    else
                    {
                        coreCandidates.Add(candidate);
                    }
                }

                if (transform != null && coreCandidates.Count == 0)
                    coreCandidates.Add(transform);

                if (values.CandidateIndex > 0 && values.CandidateIndex <= coreCandidates.Count)
                {
                    CoordinateTransform preferred = coreCandidates[values.CandidateIndex - 1];
                    coreCandidates.RemoveAt(values.CandidateIndex - 1);
                    coreCandidates.Insert(0, preferred);
                }

                if (coreCandidates.Count == 0)
                    return "#ERR: No valid coordinate transform.";

                double[] x = new double[nCoordRows];
                double[] y = new double[nCoordRows];
                double[] z = new double[nCoordRows];
                double[] t = new double[nCoordRows];
                object[,] res = new object[nCoordRows, nOut];
                Exception lastException = null;

                foreach (CoordinateTransform candidate in coreCandidates)
                {
                    for (int i = 0; i < nCoordRows; i++)
                    {
                        x[i] = Coords[i, 0];
                        y[i] = Coords[i, 1];

                        if (nCoordCols > 2)
                            z[i] = Coords[i, 2];

                        if (nCoordCols > 3)
                            t[i] = Coords[i, 3];
                    }

                    try
                    {
                        switch (nCoordCols)
                        {
                            default:
                            case 2:
                                if (inverse)
                                    candidate.ApplyReversed(x, y);
                                else
                                    candidate.Apply(x, y);
                                break;

                            case 3:
                                if (inverse)
                                    candidate.ApplyReversed(x, y, z);
                                else
                                    candidate.Apply(x, y, z);
                                break;

                            case 4:
                                if (inverse)
                                    candidate.ApplyReversed(x, y, z, t);
                                else
                                    candidate.Apply(x, y, z, t);
                                break;
                        }

                        if (axisOrderNormalization != null)
                        {
                            if (inverse)
                                axisOrderNormalization.ApplyReversed(x, y);
                            else
                                axisOrderNormalization.Apply(x, y);
                        }

                        switch (outputMode & 7)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                if (nOut == 2)
                                {
                                    for (int i = 0; i < nCoordRows; i++)
                                    {
                                        res[i, 0] = Optional.CheckNan(x[i]);
                                        res[i, 1] = Optional.CheckNan(y[i]);
                                    }
                                }
                                else if (nOut == 3)
                                {
                                    for (int i = 0; i < nCoordRows; i++)
                                    {
                                        res[i, 0] = Optional.CheckNan(x[i]);
                                        res[i, 1] = Optional.CheckNan(y[i]);
                                        res[i, 2] = Optional.CheckNan(z[i]);
                                    }
                                }
                                else if (nOut == 4)
                                {
                                    for (int i = 0; i < nCoordRows; i++)
                                    {
                                        res[i, 0] = Optional.CheckNan(x[i]);
                                        res[i, 1] = Optional.CheckNan(y[i]);
                                        res[i, 2] = Optional.CheckNan(z[i]);
                                        res[i, 3] = Optional.CheckNan(t[i]);
                                    }
                                }
                                else
                                    return "#ERR: Invalid input.";
                            break;

                            case 4:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(x[i]);
                                break;

                            case 5:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(y[i]);
                                break;

                            case 6:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(z[i]);
                                break;

                            case 7:
                                for (int i = 0; i < nCoordRows; i++)
                                    res[i, 0] = Optional.CheckNan(t[i]);
                                break;
                        }

                        return res;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }

                return lastException != null
                    ? AddIn.ProcessException(lastException)
                    : "#ERR: No valid coordinate transform.";
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
            finally
            {
                transform?.Dispose();
                crsSource?.Dispose();
                crsTarget?.Dispose();
                pjContext?.Dispose();
            }
        }

        /// <summary>
        /// Converts Web Mercator X/Y meters (EPSG:3857) into WGS84 Lat/Lon (EPSG:4326).
        /// </summary>
        /// </example>
        [ExcelFunction(Name = "PE.trf.WebMercatorToLatLong", Description = "Converts Web Mercator meters (EPSG:3857) to Lat/Lon degrees (EPSG:4326) using SharpProj")]
        public static object TransformWebMercatorToLatLong(
            [ExcelArgument(Name = "Easting", Description = "Easting in meters (e.g. 545,922")] double easting,
            [ExcelArgument(Name = "Northing", Description = "northing in meters (e.g. 6,866,867)")] double northing)
        {
            try
            {
                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst, pc))
                    {
                        var r = trf.Apply(new PPoint(easting, northing));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        public static PPoint TransformPoint(int sourceEpsg, int targetEpsg, double x, double y, bool normalizeAxes = true)
        {
            using (var pjContext = CreateContext())
            using (var source = CoordinateReferenceSystem.CreateFromEpsg(sourceEpsg, pjContext))
            using (var target = CoordinateReferenceSystem.CreateFromEpsg(targetEpsg, pjContext))
            using (var transform = CreateCoordinateTransform(
                normalizeAxes ? source.WithAxisNormalized() : source,
                normalizeAxes ? target.WithAxisNormalized() : target,
                pjContext,
                normalizeAxes))
            {
                return transform.Apply(new PPoint(x, y));
            }
        }


        /// <summary>
        /// Converts coordinates between two EPSG CRSs.
        /// </summary>
        [ExcelFunction(Name = "PE.trf.FromEpsgNoToEpsgNo", Description = "Transforms X/Y coordinates from one EPSG CRS to another using SharpProj. Optionally selects a specific transform candidate.")]
        public static object FromEpsgNoToEpsgNo(
            [ExcelArgument(Name = "Source CRS", Description = "EPSG number of source CRS. Default = 4326")] object sourceCrs = null,
            [ExcelArgument(Name = "Target CRS", Description = "EPSG number of target CRS. Default = 3857")] object targetCrs = null,
            [ExcelArgument(Name = "X", Description = "Input X coordinate (for EPSG:4326 this is longitude)")] object x = null,
            [ExcelArgument(Name = "Y", Description = "Input Y coordinate (for EPSG:4326 this is latitude)")] object y = null,
            [ExcelArgument(Name = "Candidate Index", Description = "Optional transform candidate index. Leave blank for SharpProj default selection; use 0, 1, ... to force a specific candidate.")] object candidateIndex = null)
        {
            try
            {
                int actualSourceCrs = Optional.Check(sourceCrs, 4326);
                int actualTargetCrs = Optional.Check(targetCrs, 3857);

                double actualX;
                if (!ExcelValueConverters.TryGetDouble(x, out actualX))
                    return "#ERR: X must be supplied and numeric.";

                double actualY;
                if (!ExcelValueConverters.TryGetDouble(y, out actualY))
                    return "#ERR: Y must be supplied and numeric.";

                bool hasCandidateIndex = !Optional.IsMissing(candidateIndex);
                int actualCandidateIndex = hasCandidateIndex ? ExcelValueConverters.ToInt32(candidateIndex) : -1;

                if (hasCandidateIndex && actualCandidateIndex < 0)
                    return "#ERR: Candidate Index must be 0 or greater.";

                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst))
                    {
                        global::SharpProj.CoordinateTransform transformToApply = trf;

                        if (hasCandidateIndex)
                        {
                            string selectionError;
                            transformToApply = TryGetCandidateTransform(trf, actualCandidateIndex, out selectionError);
                            if (transformToApply == null)
                                return selectionError;
                        }

                        var r = transformToApply.Apply(new PPoint(actualX, actualY));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
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


        [ExcelFunction(Name = "PE.trf.ListTransformOptions", Description = "Lists selectable transform candidates between two EPSG CRSs, including heuristic pros and cons")]
        public static object[,] ListTransformOptions(
            [ExcelArgument(Name = "Source CRS", Description = "Optional EPSG number of source CRS. Default = 4326")] object sourceCrs = null,
            [ExcelArgument(Name = "Destination CRS", Description = "Optional EPSG number of destination CRS. Default = 3857")] object targetCrs = null)
        {
            try
            {
                int actualSourceCrs = Optional.Check(sourceCrs, 4326);
                int actualTargetCrs = Optional.Check(targetCrs, 3857);

                using (var pc = CreateContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualSourceCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualTargetCrs, pc).WithNormalizedAxis(pc))
                    using (var trf = CoordinateTransform.Create(src, dst))
                    {
                        List<global::SharpProj.CoordinateTransform> candidates = GetCandidateTransforms(trf);
                        bool hasMultipleCandidates = trf is global::SharpProj.ChooseCoordinateTransform && candidates.Count > 1;

                        var rows = new List<object[]>()
                        {
                            new object[]
                            {
                                "CandidateIndex",
                                "DefaultSelection",
                                "Name",
                                "Type",
                                "Accuracy",
                                "IsAvailable",
                                "HasBallPark",
                                "UsageArea",
                                "Pros",
                                "Cons"
                            }
                        };

                        for (int i = 0; i < candidates.Count; i++)
                        {
                            global::SharpProj.CoordinateTransform candidate = candidates[i];
                            string usageAreaName = candidate.UsageArea == null ? string.Empty : candidate.UsageArea.Name;

                            rows.Add(new object[]
                            {
                                i,
                                hasMultipleCandidates ? (i == 0 ? "Yes" : "No") : "Only",
                                candidate.Name,
                                Convert.ToString(candidate.Type),
                                candidate.Accuracy,
                                candidate.IsAvailable,
                                candidate.HasBallParkTransformation,
                                usageAreaName,
                                BuildCandidatePros(candidate, i, hasMultipleCandidates),
                                BuildCandidateCons(candidate, i, hasMultipleCandidates)
                            });
                        }

                        return ToObjectTable(rows);
                    }
                }
            }
            catch (Exception ex)
            {
                return new object[,] { { "#ERR: " + ex.Message } };
            }
        }

        private sealed class PipelineCandidate
        {
            public int Index { get; set; }
            public object Pipeline { get; set; }
            public string Name { get; set; }
            public double? Accuracy { get; set; }
            public bool IsAvailable { get; set; }
            public bool HasBallpark { get; set; }
            public bool IsSuperseded { get; set; }
            public CoordinateArea UsageArea { get; set; }
        }

        private static IEnumerable<object> GetPipelineObjects(CoordinateTransform transform)
        {
            if (transform == null)
                yield break;

            PropertyInfo pipelinesProperty = transform.GetType().GetProperty("Pipelines", BindingFlags.Instance | BindingFlags.Public);
            if (pipelinesProperty != null)
            {
                object pipelinesValue = pipelinesProperty.GetValue(transform, null);
                IEnumerable pipelinesEnumerable = pipelinesValue as IEnumerable;
                if (pipelinesEnumerable != null)
                {
                    foreach (object item in pipelinesEnumerable)
                    {
                        if (item != null)
                            yield return item;
                    }
                    yield break;
                }
            }

            IEnumerable enumerable = transform as IEnumerable;
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
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

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
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
            object value = GetPropertyValue(target, propertyName);
            return value == null ? null : Convert.ToString(value);
        }

        private static bool GetBoolProperty(object target, string propertyName, bool defaultValue)
        {
            object value = GetPropertyValue(target, propertyName);
            if (value == null)
                return defaultValue;

            if (value is bool)
                return (bool)value;

            bool parsed;
            if (bool.TryParse(Convert.ToString(value), out parsed))
                return parsed;

            return defaultValue;
        }

        private static double? GetNullableDoubleProperty(object target, string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            if (value == null)
                return null;

            if (value is double)
                return (double)value;

            if (value is float)
                return Convert.ToDouble(value);

            if (value is decimal)
                return Convert.ToDouble(value);

            if (value is int)
                return (int)value;

            double parsed;
            if (double.TryParse(Convert.ToString(value), out parsed))
                return parsed;

            return null;
        }

        private static CoordinateArea GetCoordinateAreaProperty(object target, string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            if (value == null)
                return null;

            CoordinateArea area = value as CoordinateArea;
            if (area != null)
                return area;

            object west = GetPropertyValue(value, "WestLongitude");
            object south = GetPropertyValue(value, "SouthLatitude");
            object east = GetPropertyValue(value, "EastLongitude");
            object north = GetPropertyValue(value, "NorthLatitude");

            double westValue;
            double southValue;
            double eastValue;
            double northValue;

            if (west != null && south != null && east != null && north != null &&
                double.TryParse(Convert.ToString(west), out westValue) &&
                double.TryParse(Convert.ToString(south), out southValue) &&
                double.TryParse(Convert.ToString(east), out eastValue) &&
                double.TryParse(Convert.ToString(north), out northValue))
            {
                return new CoordinateArea(westValue, southValue, eastValue, northValue);
            }

            return null;
        }

        private static List<PipelineCandidate> GetPipelineCandidates(CoordinateTransform transform)
        {
            var candidates = new List<PipelineCandidate>();
            int index = 0;

            foreach (object pipeline in GetPipelineObjects(transform))
            {
                candidates.Add(new PipelineCandidate
                {
                    Index = index++,
                    Pipeline = pipeline,
                    Name = GetStringProperty(pipeline, "Name") ?? "<unnamed>",
                    Accuracy = GetNullableDoubleProperty(pipeline, "Accuracy"),
                    IsAvailable = GetBoolProperty(pipeline, "IsAvailable", true),
                    HasBallpark = GetBoolProperty(pipeline, "HasBallParkTransformation", false) || GetBoolProperty(pipeline, "HasBallparkTransformation", false),
                    IsSuperseded = GetBoolProperty(pipeline, "IsSuperseded", false),
                    UsageArea = GetCoordinateAreaProperty(pipeline, "UsageArea")
                });
            }

            if (candidates.Count == 0 && transform != null)
            {
                candidates.Add(new PipelineCandidate
                {
                    Index = 0,
                    Pipeline = transform,
                    Name = transform.Name,
                    Accuracy = transform.Accuracy,
                    IsAvailable = transform.IsAvailable,
                    HasBallpark = transform.HasBallParkTransformation,
                    IsSuperseded = false,
                    UsageArea = transform.UsageArea
                });
            }

            return candidates;
        }

        private static bool AreaContains(CoordinateArea outer, CoordinateArea inner)
        {
            return outer.WestLongitude <= inner.WestLongitude &&
                   outer.EastLongitude >= inner.EastLongitude &&
                   outer.SouthLatitude <= inner.SouthLatitude &&
                   outer.NorthLatitude >= inner.NorthLatitude;
        }

        private static bool AreaIntersects(CoordinateArea a, CoordinateArea b)
        {
            return a.WestLongitude <= b.EastLongitude &&
                   a.EastLongitude >= b.WestLongitude &&
                   a.SouthLatitude <= b.NorthLatitude &&
                   a.NorthLatitude >= b.SouthLatitude;
        }

        private static int GetAreaScore(CoordinateArea candidateArea, CoordinateArea requestedArea, bool strictContains)
        {
            if (requestedArea == null)
                return 0;

            if (candidateArea == null)
                return strictContains ? -1 : 0;

            if (AreaContains(candidateArea, requestedArea))
                return 2;

            if (strictContains)
                return -1;

            return AreaIntersects(candidateArea, requestedArea) ? 1 : 0;
        }

        private static int GetAccuracyScore(double? candidateAccuracy, double? requestedAccuracy)
        {
            if (!requestedAccuracy.HasValue || requestedAccuracy.Value < 0)
                return 0;

            if (!candidateAccuracy.HasValue)
                return 1;

            return candidateAccuracy.Value <= requestedAccuracy.Value ? 0 : 1;
        }

        private static List<PipelineCandidate> RankPipelineCandidates(IEnumerable<PipelineCandidate> candidates, CoordinateTransformOptions options)
        {
            var list = candidates.Where(c => c != null).ToList();
            if (list.Count == 0)
                return list;

            if (options == null)
                options = new CoordinateTransformOptions();

            if (!options.NoDiscardIfMissing)
            {
                var available = list.Where(c => c.IsAvailable).ToList();
                if (available.Count > 0)
                    list = available;
            }

            if (!options.UseSuperseded)
            {
                var preferred = list.Where(c => !c.IsSuperseded).ToList();
                if (preferred.Count > 0)
                    list = preferred;
            }

            if (options.NoBallparkConversions)
            {
                var nonBallpark = list.Where(c => !c.HasBallpark).ToList();
                if (nonBallpark.Count > 0)
                    list = nonBallpark;
            }

            return list
                .Select(c => new
                {
                    Candidate = c,
                    AreaScore = GetAreaScore(c.UsageArea, options.Area, options.StrictContains),
                    AccuracyScore = GetAccuracyScore(c.Accuracy, options.Accuracy),
                    AccuracyValue = c.Accuracy ?? double.MaxValue
                })
                .Where(x => x.AreaScore >= 0)
                .OrderByDescending(x => x.Candidate.IsAvailable)
                .ThenByDescending(x => x.AreaScore)
                .ThenBy(x => x.AccuracyScore)
                .ThenBy(x => x.AccuracyValue)
                .ThenBy(x => x.Candidate.HasBallpark)
                .ThenBy(x => x.Candidate.Index)
                .Select(x => x.Candidate)
                .ToList();
        }

        private static CoordinateTransform CreateTransformFromPipeline(object pipeline)
        {
            if (pipeline == null)
                return null;

            CoordinateTransform direct = pipeline as CoordinateTransform;
            if (direct != null)
                return direct;

            MethodInfo createTransformMethod = pipeline.GetType().GetMethod(
                "CreateTransform",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (createTransformMethod != null)
            {
                object value = createTransformMethod.Invoke(pipeline, null);
                return value as CoordinateTransform;
            }

            return null;
        }

        private static CoordinateTransform CreateCoordinateTransform(CoordinateReferenceSystem crsSource, CoordinateReferenceSystem crsTarget, CoordinateTransformOptions options, ProjContext pjContext, bool allowDeprecatedCrs)
        {
            bool hasDeprecatedCrs = crsSource.IsDeprecated || crsTarget.IsDeprecated;

            if (hasDeprecatedCrs && !allowDeprecatedCrs)
                throw new ArgumentException("Using deprecated CRS when not allowed");

            CoordinateTransform chooser = CoordinateTransform.Create(crsSource.WithAxisNormalized(), crsTarget.WithAxisNormalized(), pjContext);
            List<CoordinateTransform> candidates = GetCandidateTransforms(chooser, options);

            if (candidates.Count == 0)
                throw new ArgumentException("No transformation available");

            return candidates[0];
        }

        private static CoordinateTransform TryGetCandidateTransform(
            CoordinateTransform transform,
            int candidateIndex,
            out string errorMessage)
        {
            errorMessage = null;

            List<CoordinateTransform> candidates = GetCandidateTransforms(transform);
            if (candidateIndex < 0 || candidateIndex >= candidates.Count)
            {
                errorMessage = "#ERR: Candidate Index out of range.";
                return null;
            }

            return candidates[candidateIndex];
        }

        private static List<CoordinateTransform> GetCandidateTransforms(CoordinateTransform transform)
        {
            return GetCandidateTransforms(transform, new CoordinateTransformOptions());
        }

        private static List<CoordinateTransform> GetCandidateTransforms(CoordinateTransform transform, CoordinateTransformOptions options)
        {
            var ranked = RankPipelineCandidates(GetPipelineCandidates(transform), options);
            var result = new List<CoordinateTransform>();

            foreach (var candidate in ranked)
            {
                CoordinateTransform actual = CreateTransformFromPipeline(candidate.Pipeline);
                if (actual != null)
                    result.Add(actual);
            }

            if (result.Count == 0 && transform != null)
                result.Add(transform);

            return result;
        }


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

        private static bool HasAnyFiniteValues(double[] values, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!double.IsNaN(values[i]) && !double.IsInfinity(values[i]))
                    return true;
            }

            return false;
        }

        private static readonly string[] CandidateAspectNames = new[]
        {
            "Transform Name",
            "Transform Type",
            "Identifier",
            "Identifiers",
            "Accuracy",
            "IsAvailable",
            "HasBallParkTransformation",
            "HasInverse",
            "MethodName",
            "Scope",
            "Remarks",
            "GridUsages",
            "Parameters",
            "SourceCRS",
            "TargetCRS",
            "Context",
            "UsageArea.Name",
            "UsageArea.WestLongitude",
            "UsageArea.SouthLatitude",
            "UsageArea.EastLongitude",
            "UsageArea.NorthLatitude",
            "UsageArea.Center"
        };

        private static object[,] BuildTransformListResult(IList<CoordinateTransform> candidates, string nullValueText, bool bTranspose)
        {
            int candidateCount = candidates == null ? 0 : candidates.Count;
            object[,] result = new object[CandidateAspectNames.Length + 1, candidateCount + 1];

            result[0, 0] = "Transform Number";
            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                result[0, candidateIndex + 1] = candidateIndex;
            }

            for (int row = 0; row < CandidateAspectNames.Length; row++)
            {
                result[row + 1, 0] = CandidateAspectNames[row];
            }

            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                string[] values = GetCandidateAspectValues(candidates[candidateIndex], nullValueText);
                for (int row = 0; row < CandidateAspectNames.Length; row++)
                {
                    result[row + 1, candidateIndex + 1] = values[row];
                }
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

        private static string[] GetCandidateAspectValues(CoordinateTransform candidate, string nullValueText)
        {
            var areaBounds = candidate?.UsageArea;

            return new[]
            {
                SafeCandidateValue(candidate == null ? null : (object)candidate.Name, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Type, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Identifier, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Identifiers, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Accuracy, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.IsAvailable, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.HasBallParkTransformation, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.HasInverse, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.MethodName, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Scope, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.Remarks, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.GridUsages, nullValueText),
                SafeCandidateParameterValue(candidate == null ? null : (object)candidate.Parameters, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.SourceCRS, nullValueText),
                SafeCandidateValue(candidate == null ? null : (object)candidate.TargetCRS, nullValueText),
                SafeCandidateContextValue(candidate, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)areaBounds.Name, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)areaBounds.WestLongitude, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)areaBounds.SouthLatitude, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)areaBounds.EastLongitude, nullValueText),
                SafeCandidateValue(areaBounds == null ? null : (object)areaBounds.NorthLatitude, nullValueText),
                SafeCandidateValue(areaBounds == null ? null :
                    (object)$"{(areaBounds.WestLongitude + areaBounds.EastLongitude) / 2.0}, {(areaBounds.SouthLatitude + areaBounds.NorthLatitude) / 2.0}",
                    nullValueText)
            };
        }

        private static string SafeCandidateParameterValue(object value, string nullValueText)
        {
            if (value == null)
                return nullValueText;

            if (value is string text)
                return string.IsNullOrWhiteSpace(text) ? nullValueText : text;

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null)
                return SafeCandidateValue(value, nullValueText);

            try
            {
                List<string> items = new List<string>();

                foreach (object item in enumerable)
                {
                    if (item == null)
                        continue;

                    string described = DescribeObject(item);
                    if (!string.IsNullOrWhiteSpace(described))
                        items.Add(described);
                }

                return items.Count == 0 ? nullValueText : string.Join("; ", items);
            }
            catch
            {
                return nullValueText;
            }
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

        private static object GetCandidatePropertyValue(object target, string propertyName)
        {
            if (target == null)
                return null;

            try
            {
                PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                    return null;

                return property.GetValue(target, null);
            }
            catch
            {
                return null;
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
    }
}
