using System.Reflection;

namespace FileMapSharp
{
    /// <summary>
    /// Represents a configurable mapping definition for transforming data rows (e.g., from a CSV or Excel file)
    /// into strongly-typed model instances of <typeparamref name="TModel"/>. 
    /// 
    /// This class supports:
    /// - Custom column-to-property bindings, including nested property paths (e.g., "Address.Street").
    /// - Optional enforcement of nullability rules for required fields.
    /// - Type conversion from raw cell values to property types.
    /// 
    /// Typical usage involves initializing a <see cref="FileMap{TModel}"/> with a dictionary of bindings
    /// and then calling <see cref="TryMapRow"/> to convert raw data into validated model instances.
    /// </summary>
    /// <typeparam name="TModel">
    /// The model type to populate from the file rows. Must be a reference type with a public parameterless constructor.
    /// </typeparam>
    public class FileMap<TModel> where TModel : class, new()
    {
        private readonly Dictionary<string, string> _bindings;

        public FileMap(Dictionary<string, string> bindings, FileMapOptions? options = null)
        {
            _bindings = bindings;
            Options = options ?? new();
        }

        public FileMapOptions Options { get; }

        /// <summary>
        /// Attempts to map the values from a data row into a new model instance by matching column names
        /// to property paths defined in the mapping plan. Supports nested property assignment and type conversion.
        /// </summary>
        /// <param name="row">A dictionary representing the data row, where keys are column names and values are cell values.</param>
        /// <param name="model">The resulting model instance populated with mapped values, or <c>null</c> if mapping fails.</param>
        /// <returns>
        /// <c>true</c> if the model was successfully populated and passed validation; otherwise, <c>false</c>.
        /// </returns>
        public bool TryMapRow(IDictionary<string, object> row, out TModel? model)
        {
            model = new TModel();

            foreach (var column in row.Keys)
            {
                if (!_bindings.TryGetValue(column, out var target))
                    continue;

                if (string.IsNullOrWhiteSpace(target))
                    continue;

                var pathSegments = new Queue<string>(target.Split('.'));

                try
                {
                    var result = GetOrCreateNestedPropertyTarget(model, pathSegments);

                    if (result is not (object targetInstance, PropertyInfo targetProp))
                        continue;

                    var cellValue = row[column];
                    if (cellValue == null)
                        continue;

                    var targetType = Nullable.GetUnderlyingType(targetProp.PropertyType) ?? targetProp.PropertyType;

                    if (!TryConvert(cellValue, targetType, out var convertedValue))
                        continue;

                    targetProp.SetValue(targetInstance, convertedValue);
                }
                catch
                {
                    continue;
                }
            }

            return ValidateModel(model);
        }

        private bool TryConvert(object input, Type targetType, out object? result)
        {
            result = null;

            if (input == null)
                return false;

            try
            {
                if (targetType == typeof(DateTime))
                {
                    if (DateTime.TryParse(input.ToString(), out var dt))
                    {
                        result = dt;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(DateTimeOffset))
                {
                    if (DateTimeOffset.TryParse(input.ToString(), out var dto))
                    {
                        result = dto;
                        return true;
                    }
                    return false;
                }
                if (targetType.IsEnum)
                {
                    if (Enum.TryParse(targetType, input.ToString(), ignoreCase: true, out var enumValue))
                    {
                        result = enumValue;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(Guid))
                {
                    if (Guid.TryParse(input.ToString(), out var guid))
                    {
                        result = guid;
                        return true;
                    }
                    return false;
                }
                if (targetType == typeof(bool))
                {
                    var str = input.ToString()?.ToLowerInvariant();
                    result = str switch
                    {
                        "yes" or "true" or "1" => true,
                        "no" or "false" or "0" => false,
                        _ => null
                    };
                    return result != null;
                }
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var stringValue = input.ToString();
                    var items = stringValue?.Split(',') ?? Array.Empty<string>();

                    // Create an instance of List<T>
                    var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

                    // Get the Add method
                    var addMethod = list.GetType().GetMethod("Add")!;

                    foreach (var item in items)
                    {
                        if (TryConvert(item.Trim(), elementType, out var element))
                        {
                            // Add the item using reflection to support generic element types
                            addMethod.Invoke(list, new[] { element });
                        }
                    }

                    result = list;
                    return true;
                }

                result = Convert.ChangeType(input, targetType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Recursively navigates an object graph using a queue of property names, creating intermediate objects if necessary,
        /// and returns the final target instance and property info corresponding to the last segment in the path.
        /// </summary>
        /// <param name="instance">The root object to start from.</param>
        /// <param name="pathSegments">A queue of property names representing the nested path.</param>
        /// <returns>
        /// A tuple containing the target instance and the PropertyInfo for the final segment,
        /// or <c>null</c> if the path is empty.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a property in the path cannot be found on the current instance.
        /// </exception>
        private (object targetInstance, PropertyInfo targetProperty)? GetOrCreateNestedPropertyTarget(
            object instance, Queue<string> pathSegments)
        {
            if (pathSegments.Count == 0)
                return null;

            var currentSegment = pathSegments.Dequeue();
            var prop = instance.GetType().GetProperty(currentSegment, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                throw new InvalidOperationException($"Property '{currentSegment}' not found on type '{instance.GetType().Name}'");

            if (pathSegments.Count == 0)
            {
                return (instance, prop);
            }

            var currentValue = prop.GetValue(instance);

            if (currentValue == null)
            {
                currentValue = Activator.CreateInstance(prop.PropertyType)!;
                prop.SetValue(instance, currentValue);
            }

            return GetOrCreateNestedPropertyTarget(currentValue, pathSegments);
        }


        /// <summary>
        /// Validates the given model instance against the mapping plan options.
        /// </summary>
        /// <param name="model">The model instance to validate.</param>
        /// <returns>
        /// <c>true</c> if the model passes validation; otherwise, <c>false</c>.
        /// </returns>
        private bool ValidateModel(TModel model)
        {
            bool isValid = true;

            if (Options.EnforceNullability)
            {
                foreach (var prop in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;

                    var type = prop.PropertyType;
                    var value = prop.GetValue(model);

                    // Nullable<T> ignore
                    if (Nullable.GetUnderlyingType(type) != null)
                        continue;

                    // Non nullable reference type — must not be null
                    if (!type.IsValueType)
                    {
                        isValid &= value != null;
                    }
                }
            }

            return isValid;
        }
    }
}
