using System.Text.Json;
using System.Text.Json.Serialization;
using Qenex.QSuite.Driver;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.ZmqClientDriver;
using SimpleProtocol;

namespace Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;

public class DriverConverter : JsonConverter<IDriverBase>
{
    public override IDriverBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        Dictionary<string, JsonElement> properties = new();
        Guid? gid = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (gid == null)
                {
                    throw new JsonException("Missing gid property");
                }

                IDriverBase driver = gid switch
                {
                    var g when g == Guid.Parse("985a0511-bb95-44a3-889c-95bddbcc7483") => new ZeroMqClientDriver(),
                    _ => throw new NotSupportedException($"Driver with Gid {gid} is not supported")
                };

                foreach (var property in properties)
                {
                    var propKey = char.ToUpper(property.Key[0]) + property.Key.Substring(1);
                    var propertyInfo = driver.GetType().GetProperty(propKey);
                    if (propertyInfo != null)
                    {
                        if (property.Key == "protocols")
                        {
                            var protocols = JsonSerializer.Deserialize<List<IProtocolBase>>(property.Value.GetRawText(), options);
                            foreach (var protocol in protocols)
                            {
                                driver.AddProtocol(protocol);
                            }
                        }
                        else
                        {
                            var value = JsonSerializer.Deserialize(property.Value.GetRawText(), propertyInfo.PropertyType, options);
                            propertyInfo.SetValue(driver, value);
                        }
                    }
                }

                return driver;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString() ?? throw new InvalidOperationException();
                reader.Read();

                if (propertyName == "gid")
                {
                    gid = reader.GetGuid();
                }
                else
                {
                    properties[propertyName] = JsonDocument.ParseValue(ref reader).RootElement;
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, IDriverBase value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write the gid property
        writer.WritePropertyName("gid");
        writer.WriteStringValue(value.Specification.Gid.ToString());

        // Write the isEnabled property
        writer.WritePropertyName("isEnabled");
        writer.WriteBooleanValue(value.IsEnabled);

        // Write the protocols property
        writer.WritePropertyName("protocols");
        JsonSerializer.Serialize(writer, value.Protocols, options);

        writer.WriteEndObject();
    }
}