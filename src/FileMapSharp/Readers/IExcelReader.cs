namespace FileMapSharp.Readers
{
    public interface IExcelReader : IFileReader
    {
        string SheetName { get; }
        IEnumerable<IDictionary<string, object>> ReadSheet(string filePath, string sheetName);
        IEnumerable<string> GetSheetNames(string filePath);
    }
}
