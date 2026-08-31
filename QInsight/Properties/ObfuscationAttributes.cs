using System.Reflection;

// Eazfuscator.NET: treat this executable as a library so that PUBLIC symbols are NOT renamed.
// QInsight has many name-based serialization/reflection surfaces (XML AppSettings and window
// layouts, DataContract .qproj/.ws project data, XAML/BAML bindings). Renaming public members
// would break all of them. Private members, control flow and string encryption are still
// obfuscated, which is what protects the license validators and hinders patching.
[assembly: ObfuscateAssembly(false)]
