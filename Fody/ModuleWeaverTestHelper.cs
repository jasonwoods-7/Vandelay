using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fody;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Scalpel;

namespace Vandelay.Fody
{
  [Remove]
  class ModuleWeaverTestHelper
  {
    [NotNull]
    public readonly string BeforeAssemblyPath;

    [NotNull]
    public readonly string AfterAssemblyPath;

    [NotNull]
    public readonly Assembly Assembly;

    [NotNull]
    public readonly List<string> Errors;

    public ModuleWeaverTestHelper([NotNull] string inputAssembly)
    {
      BeforeAssemblyPath = Path.GetFullPath(inputAssembly);
#if (!DEBUG)
      BeforeAssemblyPath = BeforeAssemblyPath.Replace("Debug", "Release");
#endif
      AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");
      var oldPdb = BeforeAssemblyPath.Replace(".dll", ".pdb");

      Errors = new List<string>();

      using (var symbolStream = File.OpenRead(oldPdb))
      {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Directory.GetParent(BeforeAssemblyPath).FullName);

        var readerParameters = new ReaderParameters
        {
          ReadSymbols = true,
          SymbolStream = symbolStream,
          SymbolReaderProvider = new PdbReaderProvider(),
          AssemblyResolver = resolver
        };
        using (var moduleDefinition = ModuleDefinition.ReadModule(
          BeforeAssemblyPath, readerParameters))
        {
          var weavingTask = new ModuleWeaver
          {
            ModuleDefinition = moduleDefinition,
            LogError = Errors.Add
          };

          var typeCache = CacheTypes(weavingTask);
          var findType = Info.OfMethod("FodyHelpers", "TypeCache", "FindType", "String");
          weavingTask.FindType = s => (TypeDefinition)findType.Invoke(typeCache, new object[] { s });

          weavingTask.Execute();
          moduleDefinition.Write(AfterAssemblyPath);
        }
      }

      Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }

    [NotNull]
    public Type GetType([NotNull] string className) =>
      Assembly.GetType(className, true);

    [NotNull]
    static object CacheTypes([NotNull] BaseModuleWeaver weavingTask)
    {
      var typeCache = Info.OfConstructor("FodyHelpers", "TypeCache").Invoke(null);

      var assemblyResolver = Info.OfConstructor("FodyHelpers", "Fody.MockAssemblyResolver").Invoke(null);
      var resolve = Info.OfMethod("FodyHelpers", "Fody.MockAssemblyResolver", "Resolve", "String");
      Info.OfMethod("FodyHelpers", "TypeCache", "Initialise", "IEnumerable`1")
        .Invoke(typeCache, new object[]
        {
          weavingTask.GetAssembliesForScanning()
            .Select(a => (AssemblyDefinition)resolve.Invoke(assemblyResolver, new object[] {a}))
            .Where(d => d != null)
        });
      return typeCache;
    }
  }
}
