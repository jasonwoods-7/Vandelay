using System.Linq;
using Mono.Cecil;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    void HandleExports()
    {
      const string exporterName = "Vandelay.ExporterAttribute";
      foreach (var exportable in ModuleDefinition.Assembly.CustomAttributes.Where(a =>
        a.AttributeType.FullName == exporterName))
      {
        var exportType = (TypeReference)exportable.ConstructorArguments[0].Value;

        if (exportType.Resolve().CustomAttributes.Any(a =>
          a.AttributeType.FullName == "System.ComponentModel.Composition.InheritedExportAttribute" &&
          a.ConstructorArguments.Count == 0))
        {
          continue;
        }

        var export = new CustomAttribute(
          _import!.System.ComponentModel.Composition.ExportAttribute.Constructor);
        export.ConstructorArguments.Add(new CustomAttributeArgument(
          _import.System.Type.Type, exportType));

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
