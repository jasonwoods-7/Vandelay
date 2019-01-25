using System.Collections.Generic;
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
      public static MethodReference ConstructorStringString;
    }

    public static class DebuggerNonUserCodeAttribute
    {
      public static MethodReference Constructor;
    }
  }

  partial class ModuleWeaver
  {
    public override IEnumerable<string> GetAssembliesForScanning()
    {
      yield return "mscorlib";
      yield return "System";
      yield return "netstandard";
      yield return "System.Core";
      yield return "System.Diagnostics.Tools";
      yield return "System.Diagnostics.Debug";
      yield return "System.Runtime";
      yield return "System.ComponentModel.Composition";
    }

    void FindReferences()
    {
      ReferenceFinder.String.TypeReference = ModuleDefinition.ImportReference(FindType("String"));

      var generatedCodeAttribute = FindType("GeneratedCodeAttribute");
      ModuleDefinition.ImportReference(generatedCodeAttribute);
      ReferenceFinder.GeneratedCodeAttribute.ConstructorStringString = ModuleDefinition.ImportReference(
        generatedCodeAttribute.FindMethod(".ctor", "String", "String"));

      var nonUserCodeAttribute = FindType("DebuggerNonUserCodeAttribute");
      ModuleDefinition.ImportReference(nonUserCodeAttribute);
      ReferenceFinder.DebuggerNonUserCodeAttribute.Constructor = ModuleDefinition.ImportReference(
        nonUserCodeAttribute.FindMethod(".ctor"));
    }
  }
}
