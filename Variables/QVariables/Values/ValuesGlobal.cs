using System.Runtime.InteropServices;

namespace Qenex.QSuite.QVariables.Values;

/// <summary>
/// Global definitions and methods for Values.
/// </summary>
public class ValuesGlobal
{
    /// <summary>
    /// Basic data types which of values can be.
    /// </summary>
    public enum ValueDataType { Undefined = 0, UInt8, UInt16, UInt32, UInt64, Int8, Int16, Int32, Int64, Float, Double, String }

    /// <summary>
    /// Dictionary of ValueDataType and string representation of it.
    /// </summary>
    public static readonly Dictionary<ValueDataType, string> ValueDataTypeDict = new Dictionary<ValueDataType, string>()
    {
        { ValueDataType.UInt8, "System.Byte" },
        { ValueDataType.Int8, "System.SByte" },
        { ValueDataType.UInt16, "System.UInt16" },
        { ValueDataType.Int16, "System.Int16" },
        { ValueDataType.UInt32, "System.UInt32" },
        { ValueDataType.Int32, "System.Int32" },
        { ValueDataType.UInt64, "System.UInt64" },
        { ValueDataType.Int64, "System.Int64" },
        { ValueDataType.Float, "System.Single" },
        { ValueDataType.Double, "System.Double" },
        { ValueDataType.String, "System.String" }
    };
    
    /// <summary>
    /// Create instance of IValuesBase based on ValueDataType.
    /// </summary>
    /// <param name="valueType">Enumerable type.</param>
    /// <returns>Created generic instance of Values.</returns>
    /// /// <exception cref="ArgumentException">Exception is thrown when either a type is not found or instance cannot be created.</exception>
    public static IValuesBase CreateInstance(ValuesGlobal.ValueDataType valueType)
    {
        if (!ValueDataTypeDict.TryGetValue(valueType, out var stringType))
        {
            throw new ArgumentException($"Type {valueType} not found in dictionary.");
        }
        
        var type = Type.GetType(stringType);
        if (type == null)
        {
            throw new ArgumentException($"Type {stringType} not found.");
        }
        
        var genericType = typeof(Values<>);
        var constructedType = genericType.MakeGenericType(type);

        if (Activator.CreateInstance(constructedType) is not IValuesBase createdInstance)
        {
            throw new ArgumentException($"Instance of {constructedType} not created.");
        }
        
        createdInstance.ValueType = valueType;
        createdInstance.Size = valueType != ValueDataType.String ? Marshal.SizeOf(type) : 0;
        createdInstance.Length = 1;
        
        return createdInstance;
    }
}