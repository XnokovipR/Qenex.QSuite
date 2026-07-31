namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// Reflection helpers reading the transport payload type(s) a plugin declares:
/// drivers via <see cref="ITransportSource{T}"/> implementations, protocols via the
/// <see cref="ProtocolBase{T}"/> generic argument. Used at plugin discovery and by the
/// Project Configurator; never on the communication hot path.
/// </summary>
public static class TransportTypes
{
    /// <summary>Transports for either plugin kind — suitable as a discovery probe.</summary>
    public static IReadOnlyList<Type> Probe(Type pluginType)
    {
        return [.. GetDriverTransports(pluginType).Concat(GetProtocolTransports(pluginType)).Distinct()];
    }

    /// <summary>Payload types the driver declares through <see cref="ITransportSource{T}"/>; empty = undeclared.</summary>
    public static IReadOnlyList<Type> GetDriverTransports(Type driverType)
    {
        return [.. driverType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITransportSource<>))
            .Select(i => i.GenericTypeArguments[0])];
    }

    /// <summary>The frame type of a protocol, taken from its <see cref="ProtocolBase{T}"/> base; empty for non-protocols.</summary>
    public static IReadOnlyList<Type> GetProtocolTransports(Type protocolType)
    {
        for (var type = protocolType.BaseType; type != null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ProtocolBase<>))
            {
                return [type.GenericTypeArguments[0]];
            }
        }

        return [];
    }

    /// <summary>
    /// Driver Names a protocol instance narrows itself to; empty = any type-compatible
    /// driver. Takes the instance (not the Type) because the declaration is a virtual
    /// property — suitable as a discovery probe where the plugin is instantiated anyway.
    /// </summary>
    public static IReadOnlyList<string> ProbeCompatibleDrivers(object pluginInstance)
    {
        return (pluginInstance as IProtocolBase)?.CompatibleDrivers ?? [];
    }

    /// <summary>
    /// True when the pair can exchange data. Either side without a declaration
    /// (empty list) is treated as unrestricted so undeclared plugins keep today's behavior.
    /// </summary>
    public static bool AreCompatible(IReadOnlyList<Type> driverTransports, IReadOnlyList<Type> protocolTransports)
    {
        return driverTransports.Count == 0
               || protocolTransports.Count == 0
               || protocolTransports.Any(driverTransports.Contains);
    }
}
