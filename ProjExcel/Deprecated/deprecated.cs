using System;
using ExcelDna.Integration;
using SharpProj;
using SharpProj.Proj;
using static ProjExcel.Crs;

namespace ProjExcel
{
    public static class Deprecated
    {
        [ExcelFunction(
            Name = "PE.crs.AsJsonString",
            Description = "Describes the Coordinate Reference System as a JSON-string"
            )]
        public static object AsJsonString(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            Name = "PE.crs.GeodeticCRS.Name",
            Description = "Gets name of Geodetic Coordinate Reference System"
            )]
        public static object GeodeticCRS_Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            Description = "Confirms whether a CRS has been deprecated"
            )]
        public static object IsDeprecated(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            Name = "PE.crs.Name",
            Description = "Gets name of Coordinate Reference System"
            )]
        public static object Name(
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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
            [ExcelArgument("Single cell with EPSG:1234, an EPSG code, WKT, JSON or PROJ string", Name = "Crs")] object[,] oCrs,
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

    }
}
