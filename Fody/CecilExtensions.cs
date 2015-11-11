using System.ComponentModel.Composition;
using System.Linq;
using Mono.Cecil;

namespace Vandelay.Fody
{
  public static class CecilExtensions
  {
    public static bool IsClass(this TypeDefinition typeDefinition)
    {
      return (typeDefinition.BaseType != null) &&
        !typeDefinition.IsEnum && !typeDefinition.IsInterface;
    }

    public static bool ExportsType(this TypeDefinition typeDefinition,
      TypeReference exportedType)
    {
      return typeDefinition.CustomAttributes.Any(a =>
        a.AttributeType.FullName == typeof(ExportAttribute).FullName &&
        a.ConstructorArguments.Any(c => ((TypeReference)c.Value).FullName == exportedType.FullName));
    }
  }
}
