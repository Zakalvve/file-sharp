using FileMapSharp.Readers;
using System.Reflection;

namespace FileMapSharp
{
    /// <summary>
    /// Provides functionality for reading structured data from a file and mapping each row into 
    /// strongly-typed model instances using a configurable mapping definition.
    /// </summary>
    /// <remarks>
    /// This class delegates file reading to an <see cref="IFileReader"/> implementation and uses a <see cref="FileMap{TModel}"/>
    /// to define how each row should be transformed into a model instance.
    /// </remarks>
    public class Mapper : IMapper
    {
        private readonly IFileReader _reader;

        public Mapper(IFileReader reader)
        {
            _reader = reader;
        }

        /// <summary>
        /// Reads structured data rows from the specified file and uses the provided mapping definition
        /// to convert each row into an instance of <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="TModel">
        /// The model type to map each row to. Must be a reference type with a parameterless constructor.
        /// </typeparam>
        /// <param name="filePath">The path to the source data file (e.g., CSV, Excel).</param>
        /// <param name="map">
        /// A <see cref="FileMap{TModel}"/> instance that defines how column names map to model properties,
        /// including support for nested properties and validation.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{TModel}"/> containing all successfully mapped and validated model instances.
        /// </returns>
        public IEnumerable<TModel> Map<TModel>(string filePath, FileMap<TModel> map) where TModel : class, new()
        {
            List<TModel> result = new();

            var rows = _reader.ReadRows(filePath);

            foreach (var row in rows)
            {
                if (map.TryMapRow(row, out TModel? model))
                {
                    // Just in case but if TryMapRow is true then model should always be not null
                    if (model != null)
                        result.Add(model);
                }
            }

            return result;
        }
    }
}
