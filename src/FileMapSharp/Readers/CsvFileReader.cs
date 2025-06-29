using System.Text;

namespace FileMapSharp.Readers
{
    public class CsvFileReader : IFileReader
    {
        private readonly char _delimiter;
        private readonly Encoding _encoding;

        public CsvFileReader(char delimiter = ',', Encoding? encoding = null)
        {
            _delimiter = delimiter;
            _encoding = encoding ?? Encoding.UTF8;
        }

        public IEnumerable<IDictionary<string, object>> ReadRows(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found", filePath);

            using var reader = new StreamReader(filePath, _encoding);
            var headersLine = reader.ReadLine();

            if (headersLine == null)
                yield break;

            var headers = headersLine.Split(_delimiter);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = line.Split(_delimiter);

                var row = new Dictionary<string, object>();
                for (int i = 0; i < headers.Length; i++)
                {
                    var key = headers[i].Trim();
                    var value = i < values.Length ? values[i].Trim() : null;
                    row[key] = value ?? string.Empty;
                }

                yield return row;
            }
        }
    }
}
