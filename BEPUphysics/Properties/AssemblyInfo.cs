using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("BEPUphysics")]
[assembly: AssemblyProduct("BEPUphysics")]
[assembly: AssemblyDescription("Real time physics simulation library")]
[assembly: AssemblyCompany("Bepu Entertainment LLC")]
[assembly: AssemblyCopyright("Copyright © Bepu Entertainment LLC")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type. Only Windows
// assemblies support COM.
[assembly: ComVisible(false)]

// On Windows, the following GUID is for the ID of the typelib if this
// project is exposed to COM. On other platforms, it unique identifies the
// title storage container when deploying this assembly to the device.
[assembly: Guid("ab0c58ea-ef42-46d7-b180-2baedc61ce9b")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.2.0.0")]
#if WINDOWS_PHONE
[assembly: CodeGeneration(CodeGenerationFlags.EnableFPIntrinsicsUsingSIMD)]
#endif