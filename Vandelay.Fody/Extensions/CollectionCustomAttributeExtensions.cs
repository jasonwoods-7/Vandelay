using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Vandelay.Fody.Extensions
{
  static class CollectionCustomAttributeExtensions
  {
    public static void MarkAsGeneratedCode(
      [NotNull] this Collection<CustomAttribute> customAttributes,
      [NotNull] ModuleDefinition moduleDefinition,
      [NotNull] Import import)
    {
      AddDebuggerNonUserCodeAttribute(customAttributes, import);
      AddCustomAttributeArgument(customAttributes, moduleDefinition, import);
    }

    static void AddDebuggerNonUserCodeAttribute(
      [NotNull] ICollection<CustomAttribute> customAttributes,
      [NotNull] Import import)
    {
      var debuggerAttribute = new CustomAttribute(
        import.System.Diagnostics.DebuggerNonUserCodeAttribute.Constructor);
      customAttributes.Add(debuggerAttribute);
    }

    static void AddCustomAttributeArgument(
      [NotNull] ICollection<CustomAttribute> customAttributes,
      [NotNull] ModuleDefinition moduleDefinition,
      [NotNull] Import import)
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
      [NotNull] this Collection<CustomAttribute> attributes)
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
