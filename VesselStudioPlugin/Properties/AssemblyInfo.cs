using System.Reflection;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Vessel Studio Plugin")]
[assembly: AssemblyDescription("Rhino plugin for capturing viewport screenshots and syncing with Vessel Studio projects")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Vessel One")]
[assembly: AssemblyProduct("Vessel Studio Rhino Plugin")]
[assembly: AssemblyCopyright("Copyright © 2025 Vessel One")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components. If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Removed duplicate GUID - using the Rhino plugin GUID below

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Rhino plugin assembly attributes
[assembly: PlugInDescription(DescriptionType.Address, "contact@vesselone.com")]
[assembly: PlugInDescription(DescriptionType.Country, "United States")]
[assembly: PlugInDescription(DescriptionType.Email, "support@vesselone.com")]
[assembly: PlugInDescription(DescriptionType.Phone, "")]
[assembly: PlugInDescription(DescriptionType.Fax, "")]
[assembly: PlugInDescription(DescriptionType.Organization, "Vessel One")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://vesselone.com/plugins/rhino")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://vesselone.com")]

// Plugin ID - This GUID must be unique for each plugin
[assembly: System.Runtime.InteropServices.Guid("A1B2C3D4-E5F6-7890-ABCD-123456789012")]