using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Collections.Generic;
using Vandelay.Fody.Extensions;

// ReSharper disable MissingAnnotation
namespace Vandelay.Fody
{
  static class ReferenceFinder
  {
    public static class String
    {
      public static TypeReference TypeReference;
    }

    public static class GeneratedCodeAttribute
    {
      public static TypeReference TypeReference;
      public static MethodReference ConstructorStringString;
    }

    public static class DebuggerNonUserCodeAttribute
    {
      public static TypeReference TypeReference;
      public static MethodReference Constructor;
    }

    public static void FindReferences([NotNull] IAssemblyResolver assemblyResolver,
      [NotNull] ModuleDefinition moduleDefinition)
    {
      var baseLibTypes = GetBaseLibTypes(assemblyResolver);

      String.TypeReference = moduleDefinition.ImportReference(
        baseLibTypes.First(t => t.Name == "String"));

      GeneratedCodeAttribute.TypeReference = moduleDefinition.ImportReference(
        GetGeneratedCodeType(assemblyResolver));
      GeneratedCodeAttribute.ConstructorStringString = moduleDefinition.ImportReference(
        GeneratedCodeAttribute.TypeReference.Resolve()
        .FindMethod(".ctor", "String", "String"));

      DebuggerNonUserCodeAttribute.TypeReference = moduleDefinition
        .ImportReference(GetDebuggerNonUserCodeType(assemblyResolver, baseLibTypes));
      DebuggerNonUserCodeAttribute.Constructor = moduleDefinition.ImportReference(
        DebuggerNonUserCodeAttribute.TypeReference.Resolve().FindMethod(".ctor"));
    }

    [NotNull]
    static Collection<TypeDefinition> GetBaseLibTypes(
      [NotNull] IAssemblyResolver assemblyResolver)
    {
      using (var baseLib = assemblyResolver.Resolve(
        new AssemblyNameReference("mscorlib", null)))
      {
        var baseLibTypes = baseLib.MainModule.Types;

        var winrt = baseLibTypes.All(type => type.Name != "Object");
        if (winrt)
        {
          using (var runtimeLib = assemblyResolver.Resolve(
            new AssemblyNameReference("System.Runtime", null)))
          {
            baseLibTypes = runtimeLib.MainModule.Types;
          }
        }

        return baseLibTypes;
      }
    }

    [NotNull]
    static IEnumerable<TypeDefinition> GetSystemLibTypes(
      [NotNull] IAssemblyResolver assemblyResolver)
    {
      using (var systemLib = assemblyResolver.Resolve(
        new AssemblyNameReference("System", new Version("4.0.0.0"))
        {
          PublicKeyToken = new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 }
        }))
      {
        return systemLib.MainModule.Types;
      }
    }

    [NotNull]
    static TypeDefinition GetGeneratedCodeType([NotNull] IAssemblyResolver assemblyResolver)
    {
      var generatedCodeType = GetSystemLibTypes(assemblyResolver)
        .FirstOrDefault(t => t.Name == "GeneratedCodeAttribute");

      if (generatedCodeType == null)
      {
        using (var systemDiagnosticsTools = assemblyResolver.Resolve(
          new AssemblyNameReference("System.Diagnostics.Tools", null)))
        {
          generatedCodeType = systemDiagnosticsTools.MainModule.Types.First(t =>
            t.Name == "GeneratedCodeAttribute");
        }
      }

      return generatedCodeType;
    }

    [NotNull]
    static TypeDefinition GetDebuggerNonUserCodeType(
      [NotNull] IAssemblyResolver assemblyResolver,
      [NotNull] IEnumerable<TypeDefinition> baseLibTypes)
    {
      var debuggerNonUserCodeType = baseLibTypes.FirstOrDefault(t =>
        t.Name == "DebuggerNonUserCodeAttribute");

      if (debuggerNonUserCodeType == null)
      {
        using (var systemDiagnosticsDebug = assemblyResolver.Resolve(
          new AssemblyNameReference("System.Diagnostics.Debug", null)))
        {
          debuggerNonUserCodeType = systemDiagnosticsDebug.MainModule.Types
            .First(t => t.Name == "DebuggerNonUserCodeAttribute");
        }
      }

      return debuggerNonUserCodeType;
    }
  }
}
