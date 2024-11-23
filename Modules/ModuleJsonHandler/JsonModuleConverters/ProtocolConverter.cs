using System.Text.Json;
using System.Text.Json.Serialization;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;
using SimpleProtocol;

namespace Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;

public class ProtocolConverter : JsonConverter<IProtocolBase>
{
    public override IProtocolBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

                IProtocolBase protocol = gid switch
                {
                    var g when g == Guid.Parse("dee33606-0965-48a1-a829-020d99b8dd44") => new SimpleOne2OneProtocol(),
                    _ => throw new NotSupportedException($"Protocol with Gid {gid} is not supported")
                };
                
                foreach (var property in properties)
                {
                    var propertyInfo = protocol.GetType().GetProperty(property.Key);
                    if (propertyInfo != null)
                    {
                        var value = property.Value.Deserialize(propertyInfo.PropertyType, options);
                        propertyInfo.SetValue(protocol, value);
                    }
                }
                
                return protocol;
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

    public override void Write(Utf8JsonWriter writer, IProtocolBase value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write the gid property
        writer.WritePropertyName("gid");
        writer.WriteStringValue(value.Specification.Gid.ToString());

        writer.WriteEndObject();
    }
}