namespace Qenex.QSuite.Variables.ValueConversion;

public class ConversionsGlobal
{
    public enum ConversionType { Undefined = 0, Linear, Enum }
    
    /// <summary>
    /// Dictionary of conversion types and their corresponding string representation.
    /// </summary>
    public static readonly Dictionary<ConversionType, string> ConversionTypeDict = new Dictionary<ConversionType, string>()
    {
        { ConversionType.Linear, "Qenex.QSuite.Variables.ValueConversion.LinearValConversion" },
        { ConversionType.Enum, "Qenex.QSuite.Variables.ValueConversion.EnumValConversion" }
    };    
    
    /// <summary>
    /// Create instance of IVariableBase based on VariableType.
    /// </summary>
    /// <param name="conversionType">Enumerable variable type.</param>
    /// <returns>Created instance of variable.</returns>
    /// /// <exception cref="ArgumentException">Exception is thrown when either a type is not found or instance cannot be created.</exception>
    public static IValConversion CreateInstance(ConversionType conversionType)
    {
        if (!ConversionTypeDict.TryGetValue(conversionType, out var stringType))
        {
            throw new ArgumentException($"Type {conversionType} not found in dictionary.");
        }
        
        var type = Type.GetType(stringType);
        if (type == null)
        {
            throw new ArgumentException($"Type {stringType} not found.");
        }

        if (Activator.CreateInstance(type) is not IValConversion createdInstance)
        {
            throw new ArgumentException($"Instance of {type} not created.");
        }
        
        return createdInstance;
    }
}