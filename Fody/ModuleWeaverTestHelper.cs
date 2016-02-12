using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Scalpel;

namespace Vandelay.Fody
{
  [Remove]
  class ModuleWeaverTestHelper
  {
    public readonly string BeforeAssemblyPath;
    public readonly string AfterAssemblyPath;
    public readonly Assembly Assembly;
    public readonly List<string> Errors;

    public ModuleWeaverTestHelper(string inputAssembly)
    {
      BeforeAssemblyPath = Path.GetFullPath(inputAssembly);
#if (!DEBUG)
      BeforeAssemblyPath = BeforeAssemblyPath.Replace("Debug", "Release");
#endif
      AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");
      var oldPdb = BeforeAssemblyPath.Replace(".dll", ".pdb");
      var newPdb = BeforeAssemblyPath.Replace(".dll", "2.pdb");
      File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);
      File.Copy(oldPdb, newPdb, true);

      Errors = new List<string>();

      var assemblyResolver = new MockAssemblyResolver
      {
        Directory = Path.GetDirectoryName(BeforeAssemblyPath)
      };

      using (var symbolStream = File.OpenRead(newPdb))
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
        var moduleDefinition = ModuleDefinition.ReadModule(AfterAssemblyPath, readerParameters);

        var weavingTask = new ModuleWeaver
        {
          ModuleDefinition = moduleDefinition,
          AssemblyResolver = assemblyResolver,
          LogError = s => Errors.Add(s)
        };

        weavingTask.Execute();
        moduleDefinition.Write(AfterAssemblyPath);
      }

      Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }

    public Type GetType(string className)
    {
      return Assembly.GetType(className, true);
    }
  }
}
