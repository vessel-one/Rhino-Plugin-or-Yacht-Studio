using System;
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

#if DEV
[assembly: AssemblyTitle("VesselStudioSimplePlugin DEV")]
[assembly: AssemblyDescription("DEV BUILD - Simple Rhino plugin for capturing viewport and uploading to Vessel Studio")]
[assembly: AssemblyProduct("Vessel Studio Rhino Plugin DEV")]
#else
[assembly: AssemblyTitle("VesselStudioSimplePlugin")]
[assembly: AssemblyDescription("Simple Rhino plugin for capturing viewport and uploading to Vessel Studio")]
[assembly: AssemblyProduct("Vessel Studio Rhino Plugin")]
#endif

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Creata Collective Limited")]
[assembly: AssemblyCopyright("Copyright © 2025 Creata Collective Limited (NZ)")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
// DEV builds use a different GUID so they can run side-by-side with RELEASE
#if DEV
[assembly: Guid("D1E2F3A4-B5C6-7D8E-9F0A-1B2C3D4E5F6A")] // DEV GUID
#else
[assembly: Guid("A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D")] // RELEASE GUID
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]