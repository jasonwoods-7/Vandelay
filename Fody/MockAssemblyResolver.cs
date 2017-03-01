using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;
using Scalpel;

namespace Vandelay.Fody
{
  [Remove]
  class MockAssemblyResolver : IAssemblyResolver
  {
    [CanBeNull]
    public string Directory;

    public AssemblyDefinition Resolve(AssemblyNameReference name)
    {
      Debug.Assert(null != Directory);
      var fileName = Path.Combine(Directory, name.Name) + ".dll";
      if (File.Exists(fileName))
      {
        return AssemblyDefinition.ReadAssembly(fileName);
      }

      try
      {
        var assembly = Assembly.Load(name.FullName);
        var codeBase = assembly.CodeBase.Replace("file:///", "");
        return AssemblyDefinition.ReadAssembly(codeBase);
      }
      catch (FileNotFoundException)
      {
        return null;
      }
    }

    public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
      throw new NotSupportedException();
    }

    public AssemblyDefinition Resolve(string fullName)
    {
      try
      {
        var codeBase = Assembly.Load(fullName).CodeBase.Replace("file:///", "");

        return AssemblyDefinition.ReadAssembly(codeBase);
      }
      catch (FileNotFoundException)
      {
        return null;
      }
    }

    public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
    {
      throw new NotSupportedException();
    }
  }
}
