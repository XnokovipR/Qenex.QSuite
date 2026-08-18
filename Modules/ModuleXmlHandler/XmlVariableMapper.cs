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
                    BitShift = scalarVariable.BitShift != 0 ? ScalarVariable.FormatBitShift(scalarVariable.BitShift) : null,
                    BitMask = scalarVariable.BitMask != 0 ? ScalarVariable.FormatBitMask(scalarVariable.BitMask) : null,
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
            else if (variable is MatrixVariable matrixVariable)
            {
                xmlVariables.Add(new XmlMatrixVariable
                {
                    Id = matrixVariable.Id,
                    Namespace = matrixVariable.Namespace,
                    Name = matrixVariable.Name,
                    Label = matrixVariable.Label,
                    Description = matrixVariable.Description,
                    Size = matrixVariable.Size,
                    DataType = Enum.Parse<XmlValuesDataType>(matrixVariable.DefaultDataType.ToString()),
                    Endianness = matrixVariable.Endianness == MatrixEndianness.Big
                        ? XmlMatrixEndianness.Big
                        : XmlMatrixEndianness.Little,
                    XAxis = ToXmlMatrixSection(matrixVariable.XAxis, matrixVariable.XCount),
                    YAxis = ToXmlMatrixSection(matrixVariable.YAxis, matrixVariable.YCount),
                    Data = ToXmlMatrixSection(matrixVariable.Data, matrixVariable.DataCount)!
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

                    if (ScalarVariable.TryParseBitShift(xmlScalarVariable.BitShift, out var bitShift))
                    {
                        scalarVariable.BitShift = bitShift;
                    }
                    else
                    {
                        logger?.Log(LogLevel.Warn, $"Variable {scalarVariable.Name}: invalid bitShift \"{xmlScalarVariable.BitShift}\" ignored.");
                    }

                    if (ScalarVariable.TryParseBitMask(xmlScalarVariable.BitMask, out var bitMask))
                    {
                        scalarVariable.BitMask = bitMask;
                    }
                    else
                    {
                        logger?.Log(LogLevel.Warn, $"Variable {scalarVariable.Name}: invalid bitMask \"{xmlScalarVariable.BitMask}\" ignored.");
                    }
                }
                else if (variable is MatrixVariable matrixVariable && xmlVariable is XmlMatrixVariable xmlMatrixVariable)
                {
                    matrixVariable.DefaultDataType = Enum.Parse<ValuesGlobal.ValueDataType>(xmlMatrixVariable.DataType.ToString());
                    matrixVariable.Endianness = xmlMatrixVariable.Endianness == XmlMatrixEndianness.Big
                        ? MatrixEndianness.Big
                        : MatrixEndianness.Little;
                    matrixVariable.XAxis = FromXmlMatrixSection(xmlMatrixVariable.XAxis, presentationList, logger);
                    matrixVariable.YAxis = FromXmlMatrixSection(xmlMatrixVariable.YAxis, presentationList, logger);
                    matrixVariable.Data = FromXmlMatrixSection(xmlMatrixVariable.Data, presentationList, logger) ?? new MatrixSection();

                    var layoutError = matrixVariable.ValidateLayout();
                    if (layoutError != null)
                    {
                        logger?.Log(LogLevel.Error, $"Variable {matrixVariable.Name} has an invalid matrix layout: {layoutError}");
                    }
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

    private static XmlMatrixSection? ToXmlMatrixSection(MatrixSection? section, int count)
    {
        if (section == null)
        {
            return null;
        }

        return new XmlMatrixSection
        {
            Count = count,
            Label = string.IsNullOrEmpty(section.Label) ? null : section.Label,
            DataType = section.DataType != ValuesGlobal.ValueDataType.Undefined
                ? Enum.Parse<XmlValuesDataType>(section.DataType.ToString())
                : default,
            DataTypeSpecified = section.DataType != ValuesGlobal.ValueDataType.Undefined,
            PresentationReference = section.Presentation != null
                ? new XmlPresentationReference { Ref = section.Presentation.Name }
                : null
        };
    }

    private static MatrixSection? FromXmlMatrixSection(
        XmlMatrixSection? xmlSection,
        IEnumerable<IPresentation> presentations,
        ILogger? logger)
    {
        if (xmlSection == null)
        {
            return null;
        }

        var section = new MatrixSection
        {
            Count = xmlSection.Count,
            Label = xmlSection.Label ?? string.Empty,
            DataType = xmlSection.DataTypeSpecified
                ? Enum.Parse<ValuesGlobal.ValueDataType>(xmlSection.DataType.ToString())
                : ValuesGlobal.ValueDataType.Undefined
        };

        var presentationName = xmlSection.PresentationReference?.Ref;
        if (!string.IsNullOrEmpty(presentationName))
        {
            section.Presentation = presentations.FirstOrDefault(p => p.Name == presentationName);
            if (section.Presentation == null)
            {
                logger?.Log(LogLevel.Error, $"Presentation \"{presentationName}\" not found.");
            }
        }

        return section;
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
