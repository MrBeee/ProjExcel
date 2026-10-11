using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExcelDna.Integration;

// Added Bart
using SharpProj;
using SharpProj.Proj;

namespace ProjExcel
{
    public static class Lst
    {
        private static readonly object _authorityListLock = new object();
        private static string[] _authorityListCache;

        private static readonly object _coordinateReferenceSystemListLock = new object();
        private static object[,] _coordinateReferenceSystemListCache;

        [ExcelFunction(
            Name = "PE.lst.AuthorityList",
            Category = "LST - List related",
            Description = "Provides a (vertical) spillover list with all authorities from the PROJ database")]
        public static object AuthorityList(
            [ExcelArgument("Use header row (true)", Name = "Header")] object header
            )
        {
            bool bHeader = Optional.Check(header, true);
            int nOffset = bHeader ? 1 : 0;

            try
            {
                string[] authorities = GetAuthorityList();

                object[,] res = new object[authorities.Length + nOffset, 1];

                if (bHeader)
                    res[0, 0] = "Authority";

                for (int i = 0; i < authorities.Length; i++)
                    res[i + nOffset, 0] = authorities[i];

                return res;
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // AuthorityList

        private static string[] GetAuthorityList()
        {
            if (_authorityListCache != null)
                return _authorityListCache;

            lock (_authorityListLock)
            {
                if (_authorityListCache == null)
                    _authorityListCache = BuildAuthorityList();

                return _authorityListCache;
            }
        } // GetAuthorityList

        private static string[] BuildAuthorityList()
        {
            using (ProjContext pc = new ProjContext() { EnableNetworkConnections = false })
            {
                return pc.GetCoordinateReferenceSystems()
                    .Select(crsInfo => crsInfo.Authority)
                    .Where(authority => !String.IsNullOrWhiteSpace(authority))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(authority => authority, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        } // BuildAuthorityList

        [ExcelFunction(
            Name = "PE.lst.CoordinateReferenceSystemList",
            Category = "LST - List related",
            Description = "Provides a (vertical) spillover list with all coordinate reference systems from the PROJ database")]
        public static object CoordinateReferenceSystemList(
            [ExcelArgument("Use header row (true)", Name = "Header")] object header
            )
        {
            bool bHeader = Optional.Check(header, true);
            int nOffset = bHeader ? 1 : 0;

            try
            {
                object[,] data = GetCoordinateReferenceSystemList();

                object[,] res = new object[data.GetLength(0) + nOffset, data.GetLength(1)];

                if (bHeader)
                {
                    res[0, 0] = "Authority";
                    res[0, 1] = "Code";
                    res[0, 2] = "Type";
                    res[0, 3] = "ProjectionName";
                    res[0, 4] = "Name";
                    res[0, 5] = "AreaName";
                    res[0, 6] = "Nr GeoidModels";
                }

                for (int i = 0; i < data.GetLength(0); i++)
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        res[i + nOffset, j] = data[i, j];
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // CoordinateReferenceSystemList

        private static object[,] GetCoordinateReferenceSystemList()
        {
            if (_coordinateReferenceSystemListCache != null)
                return _coordinateReferenceSystemListCache;

            lock (_coordinateReferenceSystemListLock)
            {
                if (_coordinateReferenceSystemListCache == null)
                    _coordinateReferenceSystemListCache = BuildCoordinateReferenceSystemList();

                return _coordinateReferenceSystemListCache;
            }
        } // GetCoordinateReferenceSystemList

        private static object[,] BuildCoordinateReferenceSystemList()
        {
            using (ProjContext pc = new ProjContext() { EnableNetworkConnections = false })
            {
                var crsItems = pc.GetCoordinateReferenceSystems().ToArray();
                object[,] res = new object[crsItems.Length, 7];

                for (int i = 0; i < crsItems.Length; i++)
                {
                    var crsInfo = crsItems[i];

                    using (var crs = crsInfo.Create())
                    {
                        res[i, 0] = crsInfo.Authority;
                        res[i, 1] = crsInfo.Code;
                        res[i, 2] = crsInfo.Type.ToString();
                        res[i, 3] = crsInfo.ProjectionName ?? "<undefined>";
                        res[i, 4] = crsInfo.Name;
                        res[i, 5] = crsInfo.AreaName;
                        res[i, 6] = crsInfo.GetGeoidModels().Count;
                    }
                }

                return res;
            }
        } // BuildCoordinateReferenceSystemList

        [ExcelFunction(
            Name = "PE.lst.OperationList",
            Category = "LST - List related",
            Description = "Provides a (vertical) spillover list with all operations from the PROJ database")]
        public static object OperationList(
            [ExcelArgument("Use header row (true)", Name = "Header")] object header
            )
        {
            bool bHeader = Optional.Check(header, true);
            int nOffset = bHeader ? 1 : 0;

            try
            {
                ProjOperationDefinition.ProjOperationDefinitionCollection v = ProjOperationDefinition.All;
                int nCount = v.Count;

                object[,] res = new object[nCount + nOffset, 5];
                int i = 0;

                if (bHeader)
                {
                    res[i, 0] = "Name";
                    res[i, 1] = "Type";
                    res[i, 2] = "Title";
                    res[i, 3] = "Details";
                    res[i, 4] = "Nr. Arguments";
                    i++;
                }

                foreach (ProjOperationDefinition m in v)
                {
                    res[i, 0] = m.Name;
                    res[i, 1] = m.Type.ToString();
                    res[i, 2] = m.Title;
                    res[i, 3] = m.Details ?? "<not available>";
                    res[i, 4] = m.RequiredArguments.Count;
                    i++;
                }
                return res;
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // OperationList

        [ExcelFunction(
            Name = "PE.lst.CelestialBodyList",
            Category = "LST - List related",
            Description = "Provides a (vertical) spillover list with all celestial bodies from the PROJ database")]
        public static object CelestialBodyList(
            [ExcelArgument("Use header row (true)", Name = "Header")] object header
            )
        {
            bool bHeader = Optional.Check(header, true);
            int nOffset = bHeader ? 1 : 0;

            try
            {
                using (ProjContext pc = new ProjContext() { EnableNetworkConnections = false })
                {
                    var bodies = pc.GetCelestialBodies();
                    int nCount = bodies.Count;

                    object[,] res = new object[nCount + nOffset, 3];
                    int i = 0;

                    if (bHeader)
                    {
                        res[i, 0] = "Name";
                        res[i, 1] = "Authority";
                        res[i, 2] = "IsEarth";
                        i++;
                    }

                    foreach (var p in bodies)
                    {
                        res[i, 0] = p.Name;
                        res[i, 1] = p.Authority;
                        res[i, 2] = p.IsEarth;
                        i++;
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                return AddIn.ProcessException(ex);
            }
        } // CelestialBodyList
    }
}
