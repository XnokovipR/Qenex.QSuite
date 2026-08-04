namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>Parsed CONNECT positive response (ASAM XCP 1.1 Part 2, section 1.6.1.1.1).</summary>
public sealed record XcpConnectResponse(
    byte Resource,
    bool IsBigEndian,
    int AddressGranularity,
    byte MaxCto,
    ushort MaxDto,
    byte ProtocolLayerVersion,
    byte TransportLayerVersion)
{
    public bool SupportsCalibration => (Resource & XcpResource.Calibration) != 0;
    public bool SupportsDaq => (Resource & XcpResource.Daq) != 0;
}

/// <summary>Parsed GET_STATUS positive response (ASAM XCP 1.1 Part 2, section 1.6.1.1.3).</summary>
public sealed record XcpStatusResponse(
    byte SessionStatus,
    byte ResourceProtection,
    ushort SessionConfigurationId)
{
    /// <summary>Calibration commands are seed &amp; key protected and would return ERR_ACCESS_LOCKED.</summary>
    public bool IsCalibrationProtected => (ResourceProtection & XcpResource.Calibration) != 0;
}
