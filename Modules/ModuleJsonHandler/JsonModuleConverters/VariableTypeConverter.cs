using System.Text.Json;
using System.Text.Json.Serialization;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;

public class VariableTypeConverter : JsonConverter<IVariableBase>
{
    public override IVariableBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        Dictionary<string, object?> properties = new();
        string? type = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (type == null)
                {
                    throw new JsonException("Missing type property");
                }

                IVariableBase variable = type switch
                {
                    "int" => new Int32Variable(),
                    "double" => new DoubleVariable(),
                    "string" => new StringVariable(),
                    _ => throw new JsonException($"Unknown variable type: {type}")
                };

                foreach (var property in properties)
                {
                    switch (property.Key)
                    {
                        case "id":
                            variable.Id = (int)property.Value!;
                            break;
                        case "gid":
                            variable.Gid = Guid.Parse(property.Value!.ToString()!);
                            break;
                        case "name":
                            variable.Name = (string)property.Value!;
                            break;
                        case "caption":
                            variable.Caption = (string)property.Value!;
                            break;
                        case "description":
                            variable.Description = (string)property.Value!;
                            break;
                        case "address":
                            properties["address"] = (uint)property.Value!;
                            break;
                        case "length":
                            if (variable is not RangeVariable<int> && variable is not RangeVariable<double>)
                            {
                                properties["length"] = (uint)property.Value!;    
                            }
                            break;
                        case "rawRange":
                            if (variable is RangeVariable<int> intVar)
                            {
                                intVar.RawRange = JsonSerializer.Deserialize<ValueRange<int>>(property.Value!.ToString()!, options);
                            }
                            else if (variable is RangeVariable<double> doubleVar)
                            {
                                doubleVar.RawRange = JsonSerializer.Deserialize<ValueRange<double>>(property.Value!.ToString()!, options);
                            }
                            break;
                        case "engineeringRange":
                            if (variable is RangeVariable<int> intVarEng)
                            {
                                intVarEng.EngRange = JsonSerializer.Deserialize<ValueRange<int>>(property.Value!.ToString()!, options);
                            }
                            else if (variable is RangeVariable<double> doubleVarEng)
                            {
                                doubleVarEng.EngRange = JsonSerializer.Deserialize<ValueRange<double>>(property.Value!.ToString()!, options);
                            }
                            break;
                        case "unit":
                            if (variable is RangeVariable<int> intVarUnit)
                            {
                                intVarUnit.Unit = (string)property.Value!;
                            }
                            else if (variable is RangeVariable<double> doubleVarUnit)
                            {
                                doubleVarUnit.Unit = (string)property.Value!;
                            }
                            break;
                    }
                }

                return variable;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString() ?? throw new InvalidOperationException();
                reader.Read();

                switch (propertyName)
                {
                    case "type":
                        type = reader.GetString();
                        break;
                    case "id":
                        properties["id"] = reader.GetInt32();
                        break;
                    case "gid":
                        properties["gid"] = Guid.Parse(reader.GetString()!);
                        break;
                    case "name":
                        properties["name"] = reader.GetString();
                        break;
                    case "caption":
                        properties["caption"] = reader.GetString();
                        break;
                    case "description":
                        properties["description"] = reader.GetString();
                        break;
                    case "address":
                        properties["address"] = reader.GetUInt32();
                        break;
                    case "length":
                        properties["length"] = reader.GetUInt32();
                        break;
                    case "rawRange":
                        properties["rawRange"] = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
                        break;
                    case "engineeringRange":
                        properties["engineeringRange"] = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
                        break;
                    case "unit":
                        properties["unit"] = reader.GetString();
                        break;
                    
                    default:
                        throw new JsonException($"Unknown property: {propertyName}");
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, IVariableBase value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", value.Id);
        writer.WriteString("gid", value.Gid);
        writer.WriteString("name", value.Name);
        writer.WriteString("caption", value.Caption);
        writer.WriteString("description", value.Description);

        string type = value switch
        {
            Int32Variable _ => "int",
            DoubleVariable _ => "double",
            StringVariable _ => "string",
            _ => throw new JsonException($"Unknown variable type: {value.GetType().Name}")
        };
        writer.WriteString("type", type);

        if (value is RangeVariable<int> intVar)
        {
            writer.WritePropertyName("rawRange");
            JsonSerializer.Serialize(writer, intVar.RawRange, options);
            writer.WritePropertyName("engineeringRange");
            JsonSerializer.Serialize(writer, intVar.EngRange, options);
            writer.WriteString("unit", intVar.Unit);
        }
        else if (value is RangeVariable<double> doubleVar)
        {
            writer.WritePropertyName("rawRange");
            JsonSerializer.Serialize(writer, doubleVar.RawRange, options);
            writer.WritePropertyName("engineeringRange");
            JsonSerializer.Serialize(writer, doubleVar.EngRange, options);
            writer.WriteString("unit", doubleVar.Unit);
        }

        writer.WriteEndObject();
    }
}