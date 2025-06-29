namespace FileMapSharp.Readers
{
    public interface IFileReader
    {
        IEnumerable<IDictionary<string, object>> ReadRows(string filePath);
    }
}
