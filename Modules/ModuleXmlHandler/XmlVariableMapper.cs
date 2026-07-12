using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValuePresentation;

namespace Qenex.QSuite.ModuleXmlHandler;

/// <summary>
/// Maps variables to/from their XML representation. Shared by the module XML handler
/// and the standalone variables export/import in the project configuration.
/// </summary>
public static class XmlVariableMapper
{
    public static List<XmlVariable> ToXmlVariables(IEnumerable<IVariableBase> variables, ILogger? logger = null)
    {
        var xmlVariables = new List<XmlVariable>();

        foreach (var variable in variables)
        {
            if (variable is ScalarVariable scalarVariable)
            {
                xmlVariables.Add(new XmlScalarVariable
                {
                    Id = scalarVariable.Id,
                    Namespace = scalarVariable.Namespace,
                    Name = scalarVariable.Name,
                    Label = scalarVariable.Label,
                    Description = scalarVariable.Description,
                    Size = scalarVariable.Size,
                    Values = new XmlValues
                    {
                        DataType = Enum.Parse<XmlValuesDataType>(scalarVariable.Values.ValueType.ToString()),
                        Size = scalarVariable.Values.Size,
                        Length = scalarVariable.Values.Length,
                        PresentationReference = new XmlPresentationReference
                        {
                            Ref = scalarVariable.Values.ValPresentation?.Name ?? string.Empty
                        }
                    }
                });
            }
            else if (variable is StringVariable stringVariable)
            {
                xmlVariables.Add(new XmlStringVariable
                {
                    Id = stringVariable.Id,
                    Namespace = stringVariable.Namespace,
                    Name = stringVariable.Name,
                    Label = stringVariable.Label,
                    Description = stringVariable.Description
                });
            }
            else
            {
                logger?.Log(LogLevel.Warn, $"Variable type {variable.GetType()} is not supported.");
            }
        }

        return xmlVariables;
    }

    public static List<IVariableBase> FromXmlVariables(
        IEnumerable<XmlVariable> xmlVariables,
        IEnumerable<IPresentation> presentations,
        ILogger? logger = null)
    {
        var variables = new List<IVariableBase>();
        var presentationList = presentations as IList<IPresentation> ?? presentations.ToList();

        foreach (var xmlVariable in xmlVariables)
        {
            if (!XmlModuleGlobal.TypeOfXmlVariable2VariableEnumDict.TryGetValue(xmlVariable.GetType(), out var variableType))
            {
                logger?.Log(LogLevel.Warn, $"Variable type {xmlVariable.GetType()} is not supported.");
                continue;
            }

            try
            {
                var variable = VariablesGlobal.CreateInstance(variableType);
                variable.Id = xmlVariable.Id;
                variable.Namespace = xmlVariable.Namespace;
                variable.Name = xmlVariable.Name;
                variable.Label = xmlVariable.Label;
                variable.Description = xmlVariable.Description;

                if (variable is ScalarVariable scalarVariable && xmlVariable is XmlScalarVariable xmlScalarVariable)
                {
                    scalarVariable.Size = xmlScalarVariable.Size;
                    scalarVariable.Values = CreateScalarValues(presentationList, xmlScalarVariable.Values, logger);
                }

                variables.Add(variable);
            }
            catch (ArgumentException e)
            {
                logger?.Log(LogLevel.Error, $"Error creating variable {xmlVariable.Name}: {e.Message}.");
            }
        }

        return variables;
    }

    private static IValuesBase CreateScalarValues(
        IEnumerable<IPresentation> presentations,
        XmlValues xmlValues,
        ILogger? logger)
    {
        var values = ValuesGlobal.CreateInstance(Enum.Parse<ValuesGlobal.ValueDataType>(xmlValues.DataType.ToString()));
        values.Size = xmlValues.Size;
        values.Length = xmlValues.Length;

        var presentationName = xmlValues.PresentationReference?.Ref;
        if (string.IsNullOrEmpty(presentationName))
        {
            // A variable without a presentation is valid; nothing to resolve.
            return values;
        }

        values.ValPresentation = presentations.FirstOrDefault(p => p.Name == presentationName);
        if (values.ValPresentation == null)
        {
            logger?.Log(LogLevel.Error, $"Presentation \"{presentationName}\" not found.");
        }

        return values;
    }
}
