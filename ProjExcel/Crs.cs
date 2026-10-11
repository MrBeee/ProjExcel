using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExcelDna.Integration;

// Added Bart
using SharpProj;
using SharpProj.Proj;
// using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable IDE0020 // Use pattern matching
#pragma warning disable IDE0038 // Use pattern matching

namespace ProjExcel
{

    public static class Crs
    {
        internal static ProjContext CreateContext()
        {
            return ProjService.Default.CreateContext();
        }

        internal static CoordinateReferenceSystem CreateCrs(in object[,] oCrs, in ProjContext pjContext, bool ExcelInterop = false)
        {
            int nCrsRows = oCrs.GetLength(0);
            int nCrsCols = oCrs.GetLength(1);
            int nOffset  = ExcelInterop ? 1 : 0;

            if (nCrsRows != 1 || nCrsCols > 2)
                throw new ArgumentException("CRS");

            if (nCrsCols == 1)
                return CreateCrsFromSingleCell(oCrs[0 + nOffset, 0 + nOffset], pjContext);

            return CreateCrsFromAuthorityPair(
                oCrs[0 + nOffset, 0 + nOffset],
                oCrs[0 + nOffset, 1 + nOffset],
                pjContext);
        } // CreateCrs

        private static CoordinateReferenceSystem CreateCrsFromSingleCell(object value, ProjContext pjContext)
        {
            int nCrs;
            string sCrs;

            if (value is double)
            {
                nCrs = (int)(double)value;
                return CoordinateReferenceSystem.CreateFromEpsg(nCrs, pjContext);
            }
            else if (value is int)
            {
                nCrs = (int)value;
                return CoordinateReferenceSystem.CreateFromEpsg(nCrs, pjContext);
            }
            else if (value is string)
            {
                sCrs = (string)value;

                bool success = int.TryParse(sCrs, out nCrs);
                if (success)
                    return CoordinateReferenceSystem.CreateFromEpsg(nCrs, pjContext);

                if (IsWellKnownText(sCrs))
                    return CoordinateReferenceSystem.CreateFromWellKnownText(sCrs, pjContext);

                return CoordinateReferenceSystem.Create(sCrs, pjContext);
            }

            throw new ArgumentException("CRS");
        } // CreateCrsFromSingleCell

        private static CoordinateReferenceSystem CreateCrsFromAuthorityPair(object authorityValue, object codeValue, ProjContext pjContext)
        {
            string sCrs = authorityValue as string;
            if (sCrs == null)
                throw new ArgumentException("CRS");

            int nCrs = ParseCrsCode(codeValue);
            return CoordinateReferenceSystem.CreateFromDatabase(sCrs, nCrs, pjContext);
        } // CreateCrsFromAuthorityPair

        private static int ParseCrsCode(object value)
        {
            int nCrs;

            if (value is double)
                return (int)(double)value;

            if (value is int)
                return (int)value;

            if (value is string)
            {
                string sTmp = (string)value;
                bool success = int.TryParse(sTmp, out nCrs);
                if (success)
                    return nCrs;
            }

            throw new ArgumentException("CRS");
        } // ParseCrsCode

        private static bool IsWellKnownText(string sCrs)
        {
            return (sCrs.IndexOf("PROJCS") > -1) || (sCrs.IndexOf("GEOGCS") > -1) || (sCrs.IndexOf("SPHEROID") > -1);
        } // IsWellKnownText

        [ExcelFunction(
            Name = "PE.crs.AsJsonString",
            Description = "Describes the Coordinate Reference System as a JSON-string"
            )]
        public static object AsJsonString(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string CrsString ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        CrsString = crs.AsProjJson();

                        if (String.IsNullOrWhiteSpace(CrsString))
                            return "#ERR: Invalid input.";

                        return CrsString;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // AsJsonString

        [ExcelFunction(
            Name = "PE.crs.AsProjString",
            Description = "Describes the Coordinate Reference System as a PROJ-string"
            )]
        public static object AsProjString(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string CrsString ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        CrsString = crs.AsProjString();

                        if (String.IsNullOrWhiteSpace(CrsString))
                            return "#ERR: Invalid input.";

                        return CrsString;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // AsProjString

        [ExcelFunction(
            Name = "PE.crs.AsWktString",
            Description = "Describes a Coordinate Reference System as a WKT-string"
            )]
        public static object AsWktString(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string CrsString ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        CrsString = crs.AsWellKnownText();

                        if (String.IsNullOrWhiteSpace(CrsString))
                            return "#ERR: Invalid input.";

                        return CrsString;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // AsWellKnownText

        [ExcelFunction(
            Name = "PE.crs.Axis.Abbreviation",
            Description = "Get the short name of axis-N of a coordinate reference system"
            )]
        public static object Axis_Abbreviation(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].Abbreviation;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_Abbreviation

        [ExcelFunction(
            Name = "PE.crs.Axis.Count",
            Description = "Gets type of Coordinate System"
            )]
        public static object Axis_Count(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nAxes = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            nAxes = crs.Axis.Count;

                        return nAxes;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex); 
            }

        } // Axis_Count

        [ExcelFunction(
            Name = "PE.crs.Axis.Direction",
            Description = "Get the direction of axis-N of a coordinate reference system"
            )]
        public static object Axis_Direction(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].Direction;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_Direction

        [ExcelFunction(
            Name = "PE.crs.Axis.Name",
            Description = "Get the name of axis-N of a coordinate reference system"
            )]
        public static object Axis_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_Name

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitAuthName",
            Description = "Get the authority name of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitAuthName(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].UnitAuthName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitAuthName

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitCode",
            Description = "Get the authority name of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitCode(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].UnitCode;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitCode

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitConversionFactor",
            Description = "Get the Unit Conversion Factor of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitConversionFactor(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            double factor = 0;

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            factor = crs.Axis[nIndex].UnitConversionFactor;

                        return factor;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitConversionFactor

        [ExcelFunction(
            Name = "PE.crs.Axis.UnitName",
            Description = "Get the unit name of axis-N of a coordinate reference system"
            )]
        public static object Axis_UnitName(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Axis[nIndex].UnitName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Axis_UnitName

        [ExcelFunction(
            Name = "PE.crs.CelestialBodyName",
            Description = "Gets the name of the appropriate celestial body for this Coordinate Reference System"
            )]
        public static object CelestialBodyName(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CelestialBodyName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CelestialBodyName

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Abbreviation",
            Description = "Get the short name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_Abbreviation(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].Abbreviation;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_Abbreviation

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Count",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_Axis_Count(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nAxes = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null & crs.CoordinateSystem != null)
                            nAxes = crs.CoordinateSystem.Axis.Count;

                        return nAxes;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex); 
            }

        } // CoordinateSystem_Axis_Count

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Direction",
            Description = "Get the direction of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_Direction(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].Direction;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_Direction

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.Name",
            Description = "Get the name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_Name

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitAuthName",
            Description = "Get the authority name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitAuthName(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].UnitAuthName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitAuthName

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitCode",
            Description = "Get the authority name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitCode(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].UnitCode;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitCode

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitConversionFactor",
            Description = "Get the Unit Conversion Factor of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitConversionFactor(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            double factor = 0;

            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            factor = crs.CoordinateSystem.Axis[nIndex].UnitConversionFactor;

                        return factor;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitConversionFactor

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Axis.UnitName",
            Description = "Get the unit name of axis-N of a coordinate system"
            )]
        public static object CoordinateSystem_Axis_UnitName(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Axis list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return ExcelError.ExcelErrorRef;

            int nIndex  = (int)Optional.Check(index , 0.0);
            bool bNorm  = Optional.Check(normalized, true);
            string name ="";            // do the work
            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Axis[nIndex].UnitName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Axis_UnitName

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.CoordinateSystemType",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_CoordinateSystemType(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.CoordinateSystem.CoordinateSystemType.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type = "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_CoordinateSystemType

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Name",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.CoordinateSystem.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // CoordinateSystem_Name

        [ExcelFunction(
            Name = "PE.crs.CoordinateSystem.Type",
            Description = "Gets type of Coordinate System"
            )]
        public static object CoordinateSystem_Type(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);
            try
            {
                object[,] res = new object [1, 1];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;
                            res[0, 0] = projType.ToString();
                        }

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageAreaCenterX

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center",
            Description = "Gets center point of CRS usage area"
            )]
        public static object UsageArea_Center(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                object[,] res = new object [1, 2];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                        {
                            res[0, 0] = crs.UsageArea.CenterX;
                            res[0, 1] = crs.UsageArea.CenterY;
                            return res;
                        }
                        else 
                            throw new ArgumentException("No Center Point found");  // Will return #VALUE to Excel
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center.HasValues",
            Description = "Confirms whether the center point in the usage area has values"
            )]
        public static object UsageArea_Center_HasValues(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null && crs.UsageArea.Center !=null)
                            return crs.UsageArea.Center.HasValues;

                        return false; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center_HasValues

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center.X",
            Description = "Gets x-value of center point of CRS usage area"
            )]
        public static object UsageArea_Center_X(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.CenterX;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center_X

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Center.Y",
            Description = "Gets y-value of center point of CRS usage area"
            )]
        public static object UsageArea_Center_Y(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.CenterY;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Center_Y

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MaxX",
            Description = "Gets maximum X-value of CRS usage area"
            )]
        public static object UsageArea_MaxX(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MaxX;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MaxX

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MaxY",
            Description = "Gets maximum Y-value of CRS usage area"
            )]
        public static object UsageArea_MaxY(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MaxY;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MaxY

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MinX",
            Description = "Gets minimum X-value of CRS usage area"
            )]
        public static object UsageArea_MinX(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MinX;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MinX

        [ExcelFunction(
            Name = "PE.crs.UsageArea.MinY",
            Description = "Gets minimum Y-value of CRS usage area"
            )]
        public static object UsageArea_MinY(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            return crs.UsageArea.MinY;
                        else 
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // UsageArea_MinY

        [ExcelFunction(
            Name = "PE.crs.UsageArea.Name",
            Description = "Gets name of CRS usage area"
            )]
        public static object UsageArea_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            name = crs.UsageArea.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_Name

        [ExcelFunction(
            Name = "PE.crs.UsageArea.WestLongitude",
            Description = "Gets the west longitude of CRS usage area"
            )]
        public static object UsageArea_WestLongitude(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double west = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            west = crs.UsageArea.WestLongitude;

                        return west;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_WestLongitude

        [ExcelFunction(
            Name = "PE.crs.UsageArea.EastLongitude",
            Description = "Gets east longitude of CRS usage area"
            )]
        public static object UsageArea_EastLongitude(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double east = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            east = crs.UsageArea.EastLongitude;

                        return east;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_EastLongitude

        [ExcelFunction(
            Name = "PE.crs.UsageArea.SouthLatitude",
            Description = "Gets south latitude of CRS usage area"
            )]
        public static object UsageArea_SouthLatitude(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double south = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            south = crs.UsageArea.SouthLatitude;

                        return south;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_SouthLatitude

        [ExcelFunction(
            Name = "PE.crs.UsageArea.NorthLatitude",
            Description = "Gets north latitude of CRS usage area"
            )]
        public static object UsageArea_NorthLatitude(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double north = 0.0;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.UsageArea != null)
                            north = crs.UsageArea.NorthLatitude;

                        return north;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageArea_NorthLatitude

        [ExcelFunction(
            Name = "PE.crs.Datum.Name",
            Description = "Gets name of datum of CRS"
            )]
        public static object Datum_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Datum.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Datum_Name

        [ExcelFunction(
            Name = "PE.crs.Datum.Type",
            Description = "Gets type of datum in CRS"
            )]
        public static object Datum_Type(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.Datum.Type.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type = "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Datum_Type

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.InverseFlattening",
            Description = "Gets inverse flattening of ellipsoid"
            )]
        public static object Ellipsoid_InverseFlattening(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.Ellipsoid.InverseFlattening;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_InverseFlattening

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.IsSemiMinorComputed",
            Description = "Confirms whether when size of semi-minor axis has been calculated"
            )]
        public static object Ellipsoid_IsSemiMinorComputed(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            return crs.Ellipsoid.IsSemiMinorComputed;

                        return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EllipsoidIsSemiMinorComputed

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.Name",
            Description = "Gets name of Ellipsoid in Coordinate Reference System"
            )]
        public static object Ellipsoid_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Ellipsoid.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_Name

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.SemiMajorMetre",
            Description = "Gets size of ellipsoid's semi-major axis in metres"
            )]
        public static object Ellipsoid_SemiMajorMetre(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.Ellipsoid.SemiMajorMetre;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_SemiMajorMetre

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.SemiMinorMetre",
            Description = "Gets size of ellipsoid's semi-minor axis in metres"
            )]
        public static object Ellipsoid_SemiMinorMetre(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.Ellipsoid.SemiMinorMetre;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_SemiMinorMetre

        [ExcelFunction(
            Name = "PE.crs.Ellipsoid.Type",
            Description = "Gets type of ellipsoid in Coordinate Reference System"
            )]
        public static object Ellipsoid_Type(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.Ellipsoid.Type.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type = "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Ellipsoid_Type

        [ExcelFunction(
            Name = "PE.crs.EuclideanArea",
            Description = "Gets Euclidean area defined by 3 or more points in a Projected or Engineering CRS, ignoring elevation differences"
            )]
        public static object EuclideanArea(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Vertical list of points", Name = "PointList")] object[,] Points,
            [ExcelArgument("Normalize area to [m2] (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            int nPointRows = Points.GetLength(0);
            int nPointCols = Points.GetLength(1);

            if (nPointRows < 3 )
                return "#ERR: Invalid input.";

            if (nPointCols < 2 || nPointCols > 4 )
                return "#ERR: Invalid input.";

            // Now determine number of points in (poly)line
            int nPoints = nPointRows + 1;

            double[,] points = new double[nPoints, 2];

            // First, get all the points from Points
            for (int i = 0; i < nPointRows; i++)
            {
                points[i, 0] = (double)Points[i, 0];
                points[i, 1] = (double)Points[i, 1];
            }

            // Make sure we close the polygon
            points[nPointRows, 0] = points[0, 0];
            points[nPointRows, 1] = points[0, 1];

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;

                            if (projType == ProjType.ProjectedCrs || projType == ProjType.EngineeringCrs)
                            {
                                double sum1 = 0;
                                double sum2 = 0;

                                // so far now

                                for (int i = 0; i < nPoints - 1; i++)
                                {
                                    sum1 += points[i, 0] * points[i + 1, 1];
                                    sum2 += points[i, 1] * points[i + 1, 0];
                                }

                                double area = Math.Abs(0.5 * (sum1 - sum2));

                                // See : https://github.com/tracyharton/Proj4/blob/master/pj_units.c
                                double conversion0 = crs.Axis[0].UnitConversionFactor;
                                double conversion1 = crs.Axis[1].UnitConversionFactor;

                                if (bNorm)
                                {
                                    area *= (conversion0 * conversion1);
                                }

                                return area;
                            }
                            else
                                throw new Exception("Euclidean Area requires a Projected or Engineering CRS");
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EuclideanArea

        [ExcelFunction(
            Name = "PE.crs.EuclideanDistance",
            Description = "Gets Euclidean distance between two points (or between multiple points in a poly-line), defined in a Projected or Engineering CRS, ignoring elevation differences"
            )]
        public static object EuclideanDistance(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Normalize distance to [m] (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            int nPoint1Rows = Point1.GetLength(0);
            int nPoint1Cols = Point1.GetLength(1);

            int nPoint2Rows = Point2.GetLength(0);
            int nPoint2Cols = Point2.GetLength(1);

            if (nPoint1Rows < 1 )
                return "#ERR: Invalid input.";

            if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                return "#ERR: Invalid input.";

            // Check use of nPoint2; we need it when nPoint1Rows == 1
            if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                return "#ERR: Invalid input.";

            // Do the check on nPoint2Cols only when we need Point2
            if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                return "#ERR: Invalid input.";

            // Now determine number of points in (poly)line
            int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

            double[,] points = new double[nPoints, 2];

            // First, get all the points from Point1
            for (int i = 0; i < nPoint1Rows; i++)
            {
                points[i, 0] = (double)Point1[i, 0];
                points[i, 1] = (double)Point1[i, 1];
            }

            // Now get the last point from Point2 if needed
            if (nPoints > nPoint1Rows)
            {
                points[nPoints - 1, 0] = (double)Point2[0, 0];
                points[nPoints - 1, 1] = (double)Point2[0, 1];
            }

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;

                            if (projType == ProjType.ProjectedCrs || projType == ProjType.EngineeringCrs)
                            {
                                double length = 0;

                                for (int i = 0; i < nPoints - 1; i++)
                                {
                                    length += Math.Sqrt(Math.Pow(points[i + 1, 0] - points[i, 0], 2) + Math.Pow(points[i + 1, 1] - points[i, 1], 2));
                                }
                                double conversion0 = crs.Axis[0].UnitConversionFactor;

                                if (bNorm)
                                {
                                    length *= conversion0;
                                }

                                return length;
                            }
                            else
                                throw new Exception("Euclidean Distance requires a Projected or Engineering CRS");
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EuclideanDistance

        [ExcelFunction(
            Name = "PE.crs.EuclideanDistanceZ",
            Description = "Gets Euclidean distance between two points (or between multiple points in a poly-line), defined in a Projected or Engineering CRS, ignoring elevation differences"
            )]
        public static object EuclideanDistanceZ(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list) of x,y,z points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Normalize distance to [m] (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                int nPoint2Rows = Point2.GetLength(0);
                int nPoint2Cols = Point2.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                // Check use of nPoint2; we need it when nPoint1Rows == 1
                if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                    return "#ERR: Invalid input.";

                // Do the check on nPoint2Cols only when we need Point2
                if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                    return "#ERR: Invalid input.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                double[,] points = new double[nPoints, 3];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    points[i, 0] = (double)Point1[i, 0];
                    points[i, 1] = (double)Point1[i, 1];
                    points[i, 2] = nPoint1Cols > 2 ? (double)Point1[i, 2]: 0;
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    switch (nPoint2Cols)
                    {
                        default:
                        case 2:
                            points[nPoints - 1, 0] = (double)Point2[0, 0];
                            points[nPoints - 1, 1] = (double)Point2[0, 1];
                            break;
                        case 3:
                            points[nPoints - 1, 0] = (double)Point2[0, 0];
                            points[nPoints - 1, 1] = (double)Point2[0, 1];
                            points[nPoints - 1, 2] = (double)Point2[0, 2];
                            break;
                        case 4:
                            points[nPoints - 1, 0] = (double)Point2[0, 0];
                            points[nPoints - 1, 1] = (double)Point2[0, 1];
                            points[nPoints - 1, 2] = (double)Point2[0, 2];
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;

                            if (projType == ProjType.ProjectedCrs || projType == ProjType.EngineeringCrs)
                            {
                                double length = 0;
                                for (int i = 0; i < nPoints - 1; i++)
                                {
                                    length += Math.Sqrt(
                                        Math.Pow(points[i + 1, 0] - points[i, 0], 2)
                                        + Math.Pow(points[i + 1, 1] - points[i, 1], 2)
                                        + Math.Pow(points[i + 1, 2] - points[i, 2], 2));
                                }

                                double conversion0 = crs.Axis[0].UnitConversionFactor;

                                if (bNorm)
                                {
                                    length *= conversion0;
                                }

                                return length;
                            }
                            else
                            {
                                throw new Exception("Euclidean Distance requires a Projected or Engineering CRS");
                            }
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // EuclideanDistanceZ

        [ExcelFunction(
            Name = "PE.crs.GeodesicArea",
            Description = "Gets surface area defined by multiple (at least 3) points in a polygon defined in a Coordinate Reference System"
            )]
        public static object GeodesicArea(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Vertical list of points in a polygon)", Name = "startPointOrPointList")] object[,] Points,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            int nPointRows = Points.GetLength(0);
            int nPointCols = Points.GetLength(1);

            if (nPointRows < 3 )
                return "#ERR: Invalid input.";

            if (nPointCols < 2 || nPointCols > 4 )
                return "#ERR: Invalid input.";

            int nPoints = nPointRows;

            PPoint[] points = new PPoint[nPoints];

            // First, get all the points from Point1
            for (int i = 0; i < nPoints; i++)
            {
                switch (nPointCols)
                {
                    default:
                    case 2:
                        points[i] = new PPoint((double)Points[i, 0], (double)Points[i, 1]);
                        break;
                    case 3:
                        points[i] = new PPoint((double)Points[i, 0], (double)Points[i, 1], (double)Points[i, 2]);
                        break;
                    case 4:
                        points[i] = new PPoint((double)Points[i, 0], (double)Points[i, 1], (double)Points[i, 2], (double)Points[i,3]);
                        break;
                }
            }

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            double distance = Math.Abs(crs.GeoArea(points));
                            return Optional.CheckNan(distance);
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodesicArea

        [ExcelFunction(
            Name = "PE.crs.GeodesicDistance",
            Description = "Gets geodesic distance between two points (or between multiple points in a poly-line), defined in a Coordinate Reference System, ignoring elevation differences"
            )]
        public static object GeodesicDistance(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list of points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                int nPoint2Rows = Point2.GetLength(0);
                int nPoint2Cols = Point2.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                // Check use of nPoint2; we need it when nPoint1Rows == 1
                if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                    return "#ERR: Invalid input.";

                // Do the check on nPoint2Cols only when we need Point2
                if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                    return "#ERR: Invalid input.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                PPoint[] points = new PPoint[nPoints];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    switch (nPoint1Cols)
                    {
                        default:
                        case 2:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1]);
                            break;
                        case 3:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2]);
                            break;
                        case 4:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2], (double)Point1[i,3]);
                            break;
                    }
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    switch (nPoint2Cols)
                    {
                        default:
                        case 2:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1]);
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2]);
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2], (double)Point2[0, 3]);
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            double distance = crs.GeoDistance(points);
                            return Optional.CheckNan(distance);
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodesicDistance

        [ExcelFunction(
            Name = "PE.crs.GeodesicDistanceZ",
            Description = "Gets geodesic distance between two points (or between multiple points in a poly-line), defined in a Coordinate Reference System, honoring elevation differences"
            )]
        public static object GeodesicDistanceZ(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Start point (or vertical list of points)", Name = "startPointOrPointList")] object[,] Point1,
            [ExcelArgument("End point (ignored when using list of points)", Name = "endPointOrNul")] object[,] Point2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                int nPoint1Rows = Point1.GetLength(0);
                int nPoint1Cols = Point1.GetLength(1);

                int nPoint2Rows = Point2.GetLength(0);
                int nPoint2Cols = Point2.GetLength(1);

                if (nPoint1Rows < 1 )
                    return "#ERR: Invalid input.";

                if (nPoint1Cols < 2 || nPoint1Cols > 4 )
                    return "#ERR: Invalid input.";

                // Check use of nPoint2; we need it when nPoint1Rows == 1
                if (nPoint1Rows == 1 &&  nPoint2Rows != 1 )
                    return "#ERR: Invalid input.";

                // Do the check on nPoint2Cols only when we need Point2
                if (nPoint1Rows == 1 && (nPoint2Cols < 2 || nPoint2Cols > 4  ))
                    return "#ERR: Invalid input.";

                // Now determine number of points in (poly)line
                int nPoints = nPoint1Rows > 1 ? nPoint1Rows : 2;

                PPoint[] points = new PPoint[nPoints];

                // First, get all the points from Point1
                for (int i = 0; i < nPoint1Rows; i++)
                {
                    switch (nPoint1Cols)
                    {
                        default:
                        case 2:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1]);
                            break;
                        case 3:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2]);
                            break;
                        case 4:
                            points[i] = new PPoint((double)Point1[i, 0], (double)Point1[i, 1], (double)Point1[i, 2], (double)Point1[i,3]);
                            break;
                    }
                }

                // Now get the last point from Point2 if needed
                if (nPoints > nPoint1Rows)
                {
                    switch (nPoint2Cols)
                    {
                        default:
                        case 2:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1]);
                            break;
                        case 3:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2]);
                            break;
                        case 4:
                            points[nPoints - 1] = new PPoint((double)Point2[0, 0], (double)Point2[0, 1], (double)Point2[0, 2], (double)Point2[0, 3]);
                            break;
                    }
                }

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            double distance = crs.GeoDistanceZ(points);
                            return Optional.CheckNan(distance);
                        }
                        else
                            return "#ERR: Invalid input.";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeoDistanceZ

        [ExcelFunction(
            Name = "PE.crs.GeodeticCRS.Name",
            Description = "Gets name of Geodetic Coordinate Reference System"
            )]
        public static object GeodeticCRS_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.GeodeticCRS.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodeticCRS_Name

        [ExcelFunction(
            Name = "PE.crs.GeodeticCRS.Type",
            Description = "Gets type of Geodetic Coordinate Reference System"
            )]
        public static object GeodeticCRS_Type(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string type ="";

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            type = crs.GeodeticCRS.Type.ToString();

                        if (String.IsNullOrWhiteSpace(type))
                            type= "<NotFound>";

                        return type;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // GeodeticCRS_Type

        [ExcelFunction(
            Name = "PE.crs.Identifiers.Authority",
            Description = "Gets Authority of Identifier N"
            )]
        public static object Identifiers_Authority(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (false)", Name = "Normalized")] object normalized
             )
        {
            bool bNorm  = Optional.Check(normalized, false);

            try
            {
                int nIndex  = (int)Optional.Check(index , 0.0);

                int Count = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    // for Identifiers we CANNOT normalize the Axes, as this will NULL the Identifiers
                    // using (var crs = CreateCrs(oCrs, pjContext).WithAxisNormalized())
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.Identifiers != null)
                        {
                            Count = crs.Identifiers.Count;

                            if (nIndex > Count - 1 || nIndex < 0)
                                return "<index out of range>";

                            return crs.Identifiers[nIndex].Authority;
                        }
                        else
                            return "Unknown";
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Identifiers_Authority

        [ExcelFunction(
            Name = "PE.crs.Identifiers.Code",
            Description = "Gets Code of Identifier N"
            )]
        public static object Identifiers_Code(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Zero based index of Identifier list (0) ", Name = "Index")] object index,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (false)", Name = "Normalized")] object normalized
             )
        {
            bool bNorm  = Optional.Check(normalized, false);

            try
            {
                int nIndex  = (int)Optional.Check(index , 0.0);

                using (ProjContext pjContext = CreateContext())
                {
                    //for Identifiers we CANNOT normalize the Axes, as this will NULL the Identifiers
                    // using (var crs = CreateCrs(oCrs, pjContext).WithAxisNormalized())
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.Identifiers != null)
                        {
                            int Count = crs.Identifiers.Count;

                            if (nIndex > Count - 1 || nIndex < 0)
                                return "<index out of range>";

                            return crs.Identifiers[nIndex].Code;
                        }
                        else
                            return "Unknown";

                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Identifiers_Code

        [ExcelFunction(
            Name = "PE.crs.Identifiers.Count",
            Description = "Gets number of Identifiers"
            )]
        public static object Identifiers_Count(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (false)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, false);

            try
            {
                int Count = -0;

                using (ProjContext pjContext = CreateContext())
                {
                    //for Identifiers we CANNOT normalize the Axes, as this will NULL the Identifiers
                    // using (var crs = CreateCrs(oCrs, pjContext).WithAxisNormalized())
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null && crs.Identifiers != null)
                            Count = crs.Identifiers.Count;

                        return Count;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Identifiers_Count

        [ExcelFunction(
            Name = "PE.crs.IsDeprecated",
            Description = "Confirms whether when size of semi-minor axis has been calculated"
            )]
        public static object IsDeprecated(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            return crs.IsDeprecated;
                        else
                            return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // IsDeprecated

        [ExcelFunction(
            Name = "PE.crs.IsEquivalentTo",
            Description = "Confirms whether two different CRSs are equivalent"
            )]
        public static object IsEquivalentTo(
            [ExcelArgument("CRS-1, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs1")] object[,] oCrs1,
            [ExcelArgument("CRS-2, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs2")] object[,] oCrs2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs1 = bNorm ? CreateCrs(oCrs1, pjContext).WithAxisNormalized() : CreateCrs(oCrs1, pjContext))
                    using (var crs2 = bNorm ? CreateCrs(oCrs2, pjContext).WithAxisNormalized() : CreateCrs(oCrs2, pjContext))
                    {
                        if (crs1 != null && crs2 != null )
                            return crs1.IsEquivalentTo(crs2, pjContext);
                        else
                            return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // IsEquivalentTo

        [ExcelFunction(
            Name = "PE.crs.IsEquivalentToRelaxed",
            Description = "Confirms whether two different CRSs are equivalent with the axes in any order"
            )]
        public static object IsEquivalentToRelaxed(
            [ExcelArgument("CRS-1, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs1")] object[,] oCrs1,
            [ExcelArgument("CRS-2, consisting of one [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs2")] object[,] oCrs2,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs1 = bNorm ? CreateCrs(oCrs1, pjContext).WithAxisNormalized() : CreateCrs(oCrs1, pjContext))
                    using (var crs2 = bNorm ? CreateCrs(oCrs2, pjContext).WithAxisNormalized() : CreateCrs(oCrs2, pjContext))
                    {
                        if (crs1 != null && crs2 != null )
                            return crs1.IsEquivalentToRelaxed(crs2, pjContext);
                        else
                            return "#ERR: Invalid input."; 
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // IsEquivalentToRelaxed

        [ExcelFunction(
            Name = "PE.crs.Name",
            Description = "Gets name of Coordinate Reference System"
            )]
        public static object Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Name

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.Longitude",
            Description = "Gets longitude of prime miridian in degrees"
            )]
        public static object PrimeMeridian_Longitude(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.PrimeMeridian.Longitude;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_Longitude

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.Name",
            Description = "Gets name of prime meridean of coordinate reference system"
            )]
        public static object PrimeMeridian_Name(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.PrimeMeridian.Name;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_Name

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.UnitConversionFactor",
            Description = "Gets unit conversion factor of prime miridian in degrees"
            )]
        public static object PrimeMeridian_UnitConversionFactor(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                double res = -1;

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            res = crs.PrimeMeridian.UnitConversionFactor;

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_UnitConversionFactor

        [ExcelFunction(
            Name = "PE.crs.PrimeMeridian.UnitName",
            Description = "Gets name of prime meridean of coordinate reference system"
            )]
        public static object PrimeMeridian_UnitName(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string name ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            name = crs.PrimeMeridian.UnitName;

                        if (String.IsNullOrWhiteSpace(name))
                            name = "<NotFound>";

                        return name;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // PrimeMeridian_UnitName

        [ExcelFunction(
            Name = "PE.crs.Remarks",
            Description = "Gets scope of Coordinate Reference System"
            )]
        public static object Remarks(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string remarks ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            remarks = crs.Remarks;

                        if (String.IsNullOrWhiteSpace(remarks))
                            remarks= "<NotFound>";

                        return remarks;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Remarks

        [ExcelFunction(
            Name = "PE.crs.Scope",
            Description = "Gets scope of Coordinate Reference System"
            )]
        public static object Scope(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);

            try
            {
                string scope ="";            // do the work
                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                            scope = crs.Scope;

                        if (String.IsNullOrWhiteSpace(scope))
                            scope = "<NotFound>";

                        return scope;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // Scope

        [ExcelFunction(
            Name = "PE.crs.Type",
            Description = "Gets type of coordinate reference system"
            )]
        public static object Type(
            [ExcelArgument("One [or two adjacent] cell[s] with [Authority and] EPSG code, WKT string, JSON string or PROJ string", Name = "Crs")] object[,] oCrs,
            [ExcelArgument("Use normalized axis ordering: {long, lat} & {x, y} (true)", Name = "Normalized")] object normalized
            )
        {
            bool bNorm  = Optional.Check(normalized, true);
            try
            {
                object[,] res = new object [1, 1];

                using (ProjContext pjContext = CreateContext())
                {
                    using (var crs = bNorm ? CreateCrs(oCrs, pjContext).WithAxisNormalized() : CreateCrs(oCrs, pjContext))
                    {
                        if (crs != null)
                        {
                            var projType = crs.Type;
                            res[0, 0] = projType.ToString();
                        }

                        return res;
                    }
                }        
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }

        } // UsageAreaCenterX

    } // class

} // namespace
