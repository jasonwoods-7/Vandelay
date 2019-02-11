using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Vandelay.Fody.Extensions
{
  static class CollectionCustomAttributeExtensions
  {
    public static void MarkAsGeneratedCode(
      this Collection<CustomAttribute> customAttributes,
      ModuleDefinition moduleDefinition,
      Import import)
    {
      AddDebuggerNonUserCodeAttribute(customAttributes, import);
      AddCustomAttributeArgument(customAttributes, moduleDefinition, import);
    }

    static void AddDebuggerNonUserCodeAttribute(
      ICollection<CustomAttribute> customAttributes,
      Import import)
    {
      var debuggerAttribute = new CustomAttribute(
        import.System.Diagnostics.DebuggerNonUserCodeAttribute.Constructor);
      customAttributes.Add(debuggerAttribute);
    }

    static void AddCustomAttributeArgument(
      ICollection<CustomAttribute> customAttributes,
      ModuleDefinition moduleDefinition,
      Import import)
    {
      var version = typeof(ModuleWeaver).Assembly.GetName().Version.ToString();
      var name = typeof(ModuleWeaver).Assembly.GetName().Name;

      var generatedAttribute = new CustomAttribute(
        import.System.CodeDom.Compiler.GeneratedCodeAttribute.Constructor);
      generatedAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        moduleDefinition.TypeSystem.String, name));
      generatedAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        moduleDefinition.TypeSystem.String, version));
      customAttributes.Add(generatedAttribute);
    }

    public static void RemoveExporter(
      this Collection<CustomAttribute> attributes)
    {
      const string exporterName = "Vandelay.ExporterAttribute";
      foreach (var attribute in attributes.Where(a =>
        a.AttributeType.FullName == exporterName).ToList())
      {
        attributes.Remove(attribute);
      }
    }
  }
}
