// XcpProtocol is a hot data path (XCP-on-CAN receive/DAQ). It must carry ZERO
// per-call obfuscation cost, so NO control-flow obfuscation is applied here.
//
// Control-flow obfuscation is enabled only per-method on the cold license
// validator (SessionSealValid) in XcpCore, never assembly-wide. Everything in
// this assembly gets symbol renaming only, which is free at runtime.
//
// Do not add [assembly: Obfuscation(Feature = "code control flow obfuscation")]
// to this file — it would slow down the sample path.
