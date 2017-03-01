// Copyright (c) 2017 Applied Systems, Inc.

using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
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

    [CanBeNull]
    static ModuleDefinition _moduleDefinition;

    public static void SetModule([NotNull] ModuleDefinition module)
    {
      _moduleDefinition = module;
    }

    public static void FindReferences([NotNull] IAssemblyResolver assemblyResolver)
    {
      var baseLib = assemblyResolver.Resolve("mscorlib");
      var baseLibTypes = baseLib.MainModule.Types;

      var systemLib = assemblyResolver.Resolve(
        "system, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
      var systemLibTypes = systemLib.MainModule.Types;

      var winrt = baseLibTypes.All(type => type.Name != "Object");
      if (winrt)
      {
        baseLib = assemblyResolver.Resolve("System.Runtime");
        baseLibTypes = baseLib.MainModule.Types;
      }

      Debug.Assert(null != _moduleDefinition);
      String.TypeReference = _moduleDefinition.ImportReference(
        baseLibTypes.First(t => t.Name == "String"));

      var generatedCodeType = systemLibTypes.FirstOrDefault(t =>
        t.Name == "GeneratedCodeAttribute");
      if (generatedCodeType == null)
      {
        var systemDiagnosticsTools = assemblyResolver.Resolve("System.Diagnostics.Tools");
        generatedCodeType = systemDiagnosticsTools.MainModule.Types.First(t =>
          t.Name == "GeneratedCodeAttribute");
      }
      GeneratedCodeAttribute.TypeReference = _moduleDefinition.ImportReference(generatedCodeType);
      GeneratedCodeAttribute.ConstructorStringString = _moduleDefinition.ImportReference(
        GeneratedCodeAttribute.TypeReference.Resolve()
        .FindMethod(".ctor", "String", "String"));

      var debuggerNonUserCodeType = baseLibTypes.FirstOrDefault(t =>
        t.Name == "DebuggerNonUserCodeAttribute");
      if (debuggerNonUserCodeType == null)
      {
        var systemDiagnosticsDebug = assemblyResolver.Resolve("System.Diagnostics.Debug");
        debuggerNonUserCodeType = systemDiagnosticsDebug.MainModule.Types
          .First(t => t.Name == "DebuggerNonUserCodeAttribute");
      }
      DebuggerNonUserCodeAttribute.TypeReference = _moduleDefinition
        .ImportReference(debuggerNonUserCodeType);
      DebuggerNonUserCodeAttribute.Constructor = _moduleDefinition.ImportReference(
        DebuggerNonUserCodeAttribute.TypeReference.Resolve().FindMethod(".ctor"));
    }
  }
}
