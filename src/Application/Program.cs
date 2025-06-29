using Application;
using FileMapSharp;
using FileMapSharp.Readers;
using FileMapSharp.Excel;

var map = new FileMap<Car>(new Dictionary<string, string>
{
    // Top-level properties
    { "Make", "Make" },
    { "Model", "Model" },
    { "Year", "Year" },
    { "Mileage", "YearlyMilage" },

    // EngineInfo properties
    { "Engine Type", "Engine.Type" },
    { "Displacement (L)", "Engine.Displacement" },
    { "Horsepower", "Engine.Horsepower" },
    { "Fuel Type", "Engine.FuelType" },

    // Manual properties
    { "Manual Author", "Manual.Author" },
    { "Pages", "Manual.Pages" },
    { "Language", "Manual.Language" },
    { "Last Updated", "Manual.LastUpdated" }
},
new FileMapOptions
{
    EnforceNullability = true
});

IMapper excelMapper = new Mapper(new FastExcelReader("Sheet1"));
IMapper csvMapper = new Mapper(new CsvFileReader());

var basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Files");
var fullPath = Path.GetFullPath(Path.Combine(basePath, "Cars"));

var cars = excelMapper.Map(Path.ChangeExtension(fullPath, ".xlsx"), map).ToList();
var cars2 = csvMapper.Map(Path.ChangeExtension(fullPath, ".csv"), map).ToList();

Console.ReadLine();