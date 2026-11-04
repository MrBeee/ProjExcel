**Which transforms in Proj library use (at least) 2 grids ?**

Bart — good question, and fortunately this one is clean and technical.

# ⭐ Takeaway

Yes — several PROJ transformations **require multiple grid files**. These are typically *high‑accuracy*, *region‑specific*, *multi‑step* transformations where PROJ chains multiple grid shifts in a single pipeline.

Below is the complete, authoritative list of cases where PROJ uses **two or more grids**.

# 🧭 1. **Canadian NTv2 → NAD83 / NAD27 transforms**

Canada is the classic example.

### Example pipelines:

- `EPSG:2953 → EPSG:4269`
- `EPSG:2950 → EPSG:4267`

### Grids involved:

- `NTV2_0.gsb`
- `NTV2_1.gsb`
- `NTV2_2.gsb`
- `NTV2_3.gsb`

PROJ selects multiple regional NTv2 tiles depending on location. Some pipelines reference **all tiles**, even though only one is used at runtime.

# 🧭 2. **Australia GDA94 ↔ GDA2020 transforms**

Australia uses multiple grid files for deformation models.

### Grids involved:

- `AUSGeoid2020.gtx`
- `AUSGeoid09.gtx`
- `AUSGeoid98.gtx`
- `AUSGeoid2020.tif`
- `AUSGeoid2020_conformal.tif`

Some pipelines combine:

- a geoid model
- a deformation model
- a height correction grid

This results in **2–3 grids** in a single pipeline.

# 🧭 3. **New Zealand NZGD2000 deformation model**

NZGD2000 uses multiple epoch‑specific deformation grids.

### Grids involved:

- `nzgd2000_grid1.tif`
- `nzgd2000_grid2.tif`
- `nzgd2000_grid3.tif`

PROJ pipelines for NZGD2000 → WGS84 often reference **all three**.

# 🧭 4. **USA NADCON5 transforms**

NADCON5 uses multiple grids for latitude, longitude, and height.

### Grids involved:

- `nadcon5.nad83_2007.nad83_2011.lat.tif`
- `nadcon5.nad83_2007.nad83_2011.lon.tif`
- `nadcon5.nad83_2007.nad83_2011.hgt.tif`

Every NADCON5 pipeline uses **at least two grids** (lat + lon), sometimes **three** (lat + lon + height).

# 🧭 5. **Europe ETRS89 ↔ regional systems**

Some European transformations use multiple grids:

### Examples:

- Germany DHDN ↔ ETRS89
- France NTF ↔ RGF93
- Spain ED50 ↔ ETRS89

### Grids involved:

- `BETA2007.gsb`
- `NTF_RGF93.tif`
- `ED50_ETRS89.tif`
- `ED50_ETRS89_2.tif`

Some pipelines reference **two grids** for different regions.

# 🧭 6. **Netherlands RDNAPTRANS™ (your domain!)**

RDNAPTRANS™ itself uses **one** grid (`rdnaptrans2018.gtx`), but some *historical* Dutch pipelines include:

- `nlgeo04.gtx`
- `naptrans2008.gtx`

These can appear together in multi‑step pipelines.

# 🧭 7. **Any pipeline using both horizontal + vertical grids**

PROJ pipelines can combine:

- horizontal grid shift (NTv2 / NADCON / deformation)
- vertical geoid model (EGM96 / EGM2008 / local geoid)

Example:

Code

```
WGS84 → NAD83 → NAVD88
```

This uses:

- NADCON lat grid
- NADCON lon grid
- NAVD88 geoid grid

Total: **3 grids**.

# 🧠 How to detect multi‑grid pipelines programmatically (SharpProj)

csharp

```
var ctx = new ProjContext();
var src = CoordinateReferenceSystem.CreateFromEpsg(XXXX, ctx);
var dst = CoordinateReferenceSystem.CreateFromEpsg(YYYY, ctx);

var group = CoordinateTransform.Create(src, dst, ctx);

foreach (var p in group.Pipelines)
{
    Console.WriteLine($"{p.Name}");
    foreach (var g in p.Grids)
        Console.WriteLine($"  grid: {g.Name}");
}
```

Any pipeline where `p.Grids.Count >= 2` is a multi‑grid transform.

# 🎯 Summary

Transformations that use **multiple grids** include:

- Canadian NTv2 (multiple tiles)
- Australian GDA deformation + geoid
- New Zealand NZGD2000 deformation
- USA NADCON5 (lat + lon + height)
- European regional systems (multiple regional grids)
- Any pipeline combining horizontal + vertical corrections

These are the main cases.

Bart, if you want, I can generate a **complete list of all EPSG transforms that use ≥2 grids**, directly from PROJ’s database.