using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: InternalsVisibleTo("TweakHub.Tests")]

// Every P/Invoke target is a Windows (or AMD driver) DLL in System32: never load a same-named DLL dropped beside a
// portable TweakHub.exe, which would then run with the administrator rights TweakHub restarts with.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]
