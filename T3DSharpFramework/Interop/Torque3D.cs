using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using T3DSharpFramework.Generated.Functions;

namespace T3DSharpFramework.Interop {
   public static class Torque3D {
      public delegate void AddFunctionDelegate(IntPtr pNamespace, IntPtr pFnName, uint pOffset, IntPtr pDocString);
      public static AddFunctionDelegate AddFunctionNative = null;

      public static void AddFunction(string pNamespace, string pFnName, uint pOffset = 1, string pDocString = null) {
         IntPtr namespacePtr = pNamespace == null ? IntPtr.Zero : StringMarshal.Utf8StringToIntPtr(pNamespace);
         IntPtr fnNamePtr = pFnName == null ? IntPtr.Zero : StringMarshal.Utf8StringToIntPtr(pFnName);
         IntPtr docstringPtr = pDocString == null ? IntPtr.Zero : StringMarshal.Utf8StringToIntPtr(pDocString);
         AddFunctionNative(namespacePtr, fnNamePtr, pOffset, docstringPtr);
         Marshal.FreeHGlobal(namespacePtr);
         Marshal.FreeHGlobal(fnNamePtr);
         Marshal.FreeHGlobal(docstringPtr);
      }

      public delegate IntPtr LookupEngineFunctionDelegate(IntPtr pFnName);
      public static LookupEngineFunctionDelegate LookupEngineFunctionNative = null;

      public static T LookupEngineFunction<T>(string pFnName) where T : Delegate {
         IntPtr fnNamePtr = pFnName == null ? IntPtr.Zero : StringMarshal.Utf8StringToIntPtr(pFnName);
         IntPtr result = LookupEngineFunctionNative(fnNamePtr);
         Marshal.FreeHGlobal(fnNamePtr);
         return (T)Marshal.GetDelegateForFunctionPointer( result, typeof(T));
      }

      [UnmanagedCallersOnly]
      public static void InitT3DSharp(IntPtr addFunction, IntPtr lookupEngineFunction) {
         CultureInfo customCulture =
            (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
         customCulture.NumberFormat.NumberDecimalSeparator = ".";

         Thread.CurrentThread.CurrentCulture = customCulture;

         Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
         assemblies.Where(a => a.FullName != null && !a.FullName.StartsWith("System"))
            .Select(a => a.GetTypes())
            .ToList()
            .ForEach(Initializer.InitializeTypeDictionaries);

         AddFunctionNative = Marshal.GetDelegateForFunctionPointer<AddFunctionDelegate>(addFunction);
         LookupEngineFunctionNative = Marshal.GetDelegateForFunctionPointer<LookupEngineFunctionDelegate>(lookupEngineFunction);
         AddFunction(null, "CsharpEntryFunction");

         // --- Normally Torque uses the main.tscript file to set these variables, here we have to do it ourselves.
         string CSDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');
         Global.SetMainDotCsDir(CSDir);
         Global.SetCurrentDirectory(CSDir);
      }


      [UnmanagedCallersOnly]
      public static unsafe IntPtr ExecConsoleFunction(IntPtr nameSpace, IntPtr name, int argc, IntPtr argv, bool* result) {
         string _nameSpace = Marshal.PtrToStringAnsi(nameSpace);
         string _name = Marshal.PtrToStringAnsi(name);
         string[] strings = null;
         if (argv != IntPtr.Zero)
            strings = StringMarshal.IntPtrToAnsiStringArray(argv, argc);

         if (_name == "CsharpEntryFunction") {
            Initializer.GetScriptEntry().Invoke(null, null);
            *result = true;
            return StringMarshal.Utf8StringToIntPtr("");
         }

         return StringMarshal.Utf8StringToIntPtr(EngineCallbacks.CallScriptFunction(_nameSpace, _name, strings, out *result));
      }
   }
}
