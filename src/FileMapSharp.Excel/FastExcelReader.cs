using FileMapSharp.Readers;

namespace FileMapSharp.Excel
{
    /// <summary>
    /// An Excel reader implementation based on the FastExcel library, used to read rows of data 
    /// from Excel worksheets into dictionaries keyed by column headers.
    /// 
    /// This reader supports reading from both `.xlsx` and `.xls` files, and allows selecting a specific sheet to read.
    /// Each row is represented as a dictionary of column header to cell value.
    /// </summary>
    /// <remarks>
    /// This class implements <see cref="IExcelReader"/> and can be substituted with any other implementation 
    /// that supports reading Excel files in a similar manner.
    /// </remarks>
    public class FastExcelReader : IExcelReader
    {
        public string SheetName { get; private set; }

        public FastExcelReader() : this(String.Empty) { }
        public FastExcelReader(string sheetName)
        {
            SheetName = sheetName;
        }

        public void SetSheet(string sheetName)
        {
            SheetName = sheetName;
        }

        public IEnumerable<IDictionary<string, object>> ReadRows(string filePath)
        {
            return ReadSheet(filePath, SheetName);
        }

        public IEnumerable<IDictionary<string, object>> ReadSheet(string filePath, string sheetName)
        {
            if (String.IsNullOrEmpty(sheetName))
                throw new ArgumentNullException(nameof(sheetName));

            var fileInfo = new FileInfo(filePath);
            var extension = fileInfo.Extension.ToLowerInvariant();

            if (!new[] { ".xlsx", ".xls" }.Contains(extension))
            {
                throw new NotSupportedException($"Unsupported file extension: {fileInfo.Extension}");
            }

            using var fastExcel = new FastExcel.FastExcel(fileInfo, true);
            var worksheet = fastExcel.Read(sheetName);

            var rows = new List<IDictionary<string, object>>();
            List<string> headers = worksheet.Rows.First().Cells.Select(c => c.Value.ToString() ?? String.Empty).ToList();

            if (headers.Any(h => String.IsNullOrEmpty(h))) {
                throw new InvalidOperationException("Headers not found. All columns must have a header.");
            }

            foreach (var worksheetRow in worksheet.Rows.Skip(1))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = worksheetRow.Cells.ElementAtOrDefault(i);
                    var value = cell?.Value;

                    // Detect dates values which have a style attribute of 1
                    var styleAttr = cell.XElement?.Attribute("s");
                    int? styleIndex = styleAttr != null ? int.Parse(styleAttr.Value) : null;

                    var knownDateStyleIndices = new HashSet<int> { 1 };

                    if (styleIndex.HasValue && knownDateStyleIndices.Contains(styleIndex.Value))
                    {
                        value = DateTime.FromOADate(Convert.ToDouble(value));
                    }

                    row[headers[i]] = value ?? string.Empty;
                }
                rows.Add(row);
            }

            return rows;
        }

        public IEnumerable<string> GetSheetNames(string filePath)
        {
            using var fastExcel = new FastExcel.FastExcel(new FileInfo(filePath), true);

            return fastExcel.Worksheets.Select(s => s.Name);
        }
    }
}
