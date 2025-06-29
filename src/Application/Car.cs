namespace Application
{
    public class Car
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public List<double> YearlyMilage { get; set; } = new();

        public EngineInfo Engine { get; set; } = new EngineInfo();
        public Manual Manual { get; set; } = new Manual();
    }

    public class EngineInfo
    {
        public string Type { get; set; } = string.Empty;
        public double Displacement { get; set; }
        public int Horsepower { get; set; }
        public string FuelType { get; set; } = string.Empty;
    }

    public class Manual
    {
        public string Author { get; set; } = string.Empty;
        public int Pages { get; set; }
        public string Language { get; set; } = "English";
        public DateTime LastUpdated { get; set; }
    }
}
