using System.ComponentModel.Composition;
using System.Linq;
using Mono.Cecil;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    void HandleExports()
    {
      var exporterName = $"Vandelay.{nameof(ExporterAttribute)}";
      foreach (var exportable in ModuleDefinition.Assembly.CustomAttributes.Where(a =>
        a.AttributeType.FullName == exporterName))
      {
        var exportType = ModuleDefinition.ImportReference(
          (TypeReference)exportable.ConstructorArguments[0].Value);

        if (exportType.Resolve().CustomAttributes.Any(a =>
          a.AttributeType.FullName == typeof(InheritedExportAttribute).FullName &&
          a.ConstructorArguments.Count == 0))
        {
          continue;
        }

        var export = new CustomAttribute(ModuleDefinition.ImportReference(
          Info.OfConstructor("System.ComponentModel.Composition",
          "System.ComponentModel.Composition.ExportAttribute", "Type")));
        export.ConstructorArguments.Add(new CustomAttributeArgument(
          FindType("System.Type"), exportType));

        foreach (var type in ModuleDefinition.GetTypes().Where(t =>
          t.IsClass() && !t.IsAbstract && !t.ExportsType(exportType) &&
          (t.ImplementsInterface(exportType) || t.InheritsBase(exportType))))
        {
          type.CustomAttributes.Add(export);
        }
      }
    }
  }
}
