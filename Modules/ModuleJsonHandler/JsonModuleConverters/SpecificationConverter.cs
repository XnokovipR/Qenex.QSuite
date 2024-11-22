using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.ModuleJsonHandler.JsonModuleConverters;

public class SpecificationConverter : JsonConverter<ISpecification>
{
    public override ISpecification Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize as SpecificationBase
        return JsonSerializer.Deserialize<SpecificationBase>(ref reader, options) ?? throw new InvalidOperationException();
    }

    public override void Write(Utf8JsonWriter writer, ISpecification value, JsonSerializerOptions options)
    {
        // Serialize as SpecificationBase
        JsonSerializer.Serialize(writer, (SpecificationBase)value, options);
    }
}