# ProjExcel Add-in

### An Excel extension library (*.xll) for Coordinate Transformation functions

#### 1	Introduction

`ProjExcel` is an Excel-DNA add-in that exposes a number of user-defined functions (UDFs) for *Coordinate Transformation* as an Excel extension. The code is based on a number of open source libraries, that are available for download on [GitHub](https://github.com/).  

ProjExcel is made available under the [ZLib license](License.md).   It depends on the following libraries ([nuget](https://www.nuget.org/) packages):

| Library                                                      | Version         | License type |
| ------------------------------------------------------------ | --------------- | :----------: |
| [ExcelDna](https://github.com/Excel-DNA/ExcelDna).Integration | 1.10.0-preview5 |     ZLIB     |
| [ExcelDna](https://github.com/Excel-DNA/ExcelDna).IntelliSense | 1.10.0-preview5 |     ZLIB     |
| [SharpProj](https://github.com/AmpScm/SharpProj)             | 9.5001.295.0    |  Apache 2.0  |
| [MathNet](https://numerics.mathdotnet.com/).Numeric          | 6.0.0.0         |     MIT      |

Note that **SharpProj** is the **core package** for the Add-in as it makes the [proj library](https://proj.org/index.html) (*written in C/C++*) accessible from C-Sharp. This is crucial as the Excel DNA code (*that creates the UDFs*) has been written in C-Sharp. 

> [!NOTE]
>
> **[PROJ](https://proj.org/en/stable/index.html)** is a generic coordinate transformation software that transforms geospatial coordinates from one coordinate reference system (CRS) to another. This includes cartographic projections as well as geodetic transformations PROJ is released under the [X/MIT open source license](https://proj.org/en/stable/about.html#license).
>
> PROJ started purely as a cartography application letting users convert geodetic coordinates into projected coordinates using a number of different cartographic projections. Over the years, as the need has become apparent, support for datum shifts has worked its way into PROJ as well. Today PROJ supports more than a hundred different map projections and can transform coordinates between datums using all but the most obscure geodetic techniques. 
>
> PROJ is widely used in the industry, for instance [QGIS](https://www.qgis.org/en/site/), depends on it. QGIS is a very well established Open Source Geographic Information System in the geospatial industry. 

> [!NOTE]
>
> [**SharpProj**](https://github.com/ampscm/SharpProj) v9.5001.295.0 makes use of proj v9.5.1 released on 1st Dec 2024.   As of 4 October 2026, the latest version of proj is v9.8.1. Instructions to update SharpProj to a later version of proj can be found [here](https://github.com/ampscm/SharpProj).



#### 2	Excel UDFs

> The UDFs are categorized using a naming convention consisting of three characters; **crs** stands for coordinate reference system functions,  **deg** stands for angle conversion functions, **trf** stands for coordinate transform functions

##### crs - coordinate reference system functions



##### deg - angle conversion  functions



##### trf - coordinate transform functions







### 

#### 3	Installation

To install the `ProjExcel` add-in, follow these steps:

1. Download the latest release from the [Releases](https://github.com/yourusername/SeismicExcel/releases) page.
2. Extract the contents of the ZIP file to a directory of your choice.
3. Open Excel and navigate to `File` > `Options` > `Add-ins`.
4. At the bottom of the window, select `Excel Add-ins` from the `Manage` dropdown and click `Go...`.
5. Click `Browse...` and locate the `SeismicExcel.xll` file you extracted earlier.
6. Select the file and click `OK` to add it to your Excel environment.
7. Use of the various UDF's is demonstrated in the file SeismicExcelTests.xlsx



#### 4	Usage

Once installed, you can start using the functions directly in your Excel worksheets. Simply type the function name in a cell, followed by any required arguments. For example:

`=SE.lib.Version()`


This will return the version information of the `SeismicExcel` add-in.



#### 5	Contributing

We welcome contributions to the `SeismicExcel` project! If you would like to contribute, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes and commit them with clear messages.
4. Push your changes to your forked repository.
5. Submit a pull request detailing your changes.



#### 6	License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for more details.



#### 7	Contact

For any inquiries or support, please reach out to the project maintainer at [Bart.Duijndam@ziggo.nl](mailto:Bart.Duijndam@ziggo.nl).