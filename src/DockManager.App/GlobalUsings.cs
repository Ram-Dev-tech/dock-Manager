// Explicit replacement for <ImplicitUsings>, kept identical to the SDK default set for a class
// library. System.Windows.Forms is deliberately absent: the tray icon is implemented with Win32
// directly so the dock does not load WinForms, and so WPF types are never ambiguous.
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;
