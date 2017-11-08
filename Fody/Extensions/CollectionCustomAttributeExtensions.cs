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
      [NotNull] this Collection<CustomAttribute> customAttributes)
    {
      AddCustomAttributeArgument(customAttributes);
      AddDebuggerNonUserCodeAttribute(customAttributes);
    }

    static void AddDebuggerNonUserCodeAttribute(
      [NotNull] ICollection<CustomAttribute> customAttributes)
    {
      var debuggerAttribute = new CustomAttribute(ReferenceFinder
        .DebuggerNonUserCodeAttribute.Constructor);
      customAttributes.Add(debuggerAttribute);
    }

    static void AddCustomAttributeArgument(
      [NotNull] ICollection<CustomAttribute> customAttributes)
    {
      var version = typeof(ModuleWeaver).Assembly.GetName().Version.ToString();
      var name = typeof(ModuleWeaver).Assembly.GetName().Name;

      var generatedAttribute = new CustomAttribute(
        ReferenceFinder.GeneratedCodeAttribute.ConstructorStringString);
      generatedAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        ReferenceFinder.String.TypeReference, name));
      generatedAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        ReferenceFinder.String.TypeReference, version));
      customAttributes.Add(generatedAttribute);
    }

    public static void RemoveExporter(
      [NotNull] this Collection<CustomAttribute> attributes)
    {
      foreach (var attribute in attributes.Where(a =>
        a.AttributeType.FullName == typeof(ExporterAttribute).FullName).ToList())
      {
        attributes.Remove(attribute);
      }
    }
  }
}
