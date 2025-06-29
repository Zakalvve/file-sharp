
namespace FileMapSharp
{
    /// <summary>
    /// Provides functionality for reading structured data from a file and mapping each row into 
    /// strongly-typed model instances using a configurable mapping definition.
    /// </summary>
    public interface IMapper
    {
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
        IEnumerable<TModel> Map<TModel>(string filePath, FileMap<TModel> map) where TModel : class, new();
    }
}