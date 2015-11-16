using System;
using System.ComponentModel.Composition;
using System.Linq;
using Mono.Cecil;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    void HandleExports()
    {
      foreach (var exportable in ModuleDefinition.Assembly.CustomAttributes.Where(a =>
        a.AttributeType.Name == nameof(ExporterAttribute)))
      {
        var exportType = ModuleDefinition.ImportReference(
          (TypeReference)exportable.ConstructorArguments.Single().Value);

        var export = new CustomAttribute(ModuleDefinition.ImportReference(
          AssemblyResolver.Resolve(typeof(ExportAttribute).Assembly.FullName)
          .MainModule.Types.First(t => t.Name == nameof(ExportAttribute)).Methods
          .First(m => m.IsConstructor && m.Parameters.Count == 1 &&
            m.Parameters[0].Name == "contractType")));
        export.ConstructorArguments.Add(new CustomAttributeArgument(
          ModuleDefinition.ImportReference(AssemblyResolver.Resolve("mscorlib")
          .MainModule.Types.First(t => t.Name == nameof(Type))),
          ModuleDefinition.ImportReference(exportType)));

        foreach (var type in ModuleDefinition.GetTypes().Where(t =>
          t.IsClass() && !t.IsAbstract && !t.ExportsType(exportType) &&
          t.ImplementsInterface(exportType)))
        {
          type.CustomAttributes.Add(export);
        }
      }
    }
  }
}
