using System.ComponentModel.Composition;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Vandelay.Fody.Extensions
{
  static class TypeDefinitionExtensions
  {
    public static bool IsClass([NotNull] this TypeDefinition typeDefinition)
    {
      return (typeDefinition.BaseType != null) &&
        !typeDefinition.IsEnum && !typeDefinition.IsInterface;
    }

    public static bool ExportsType([NotNull] this TypeDefinition typeDefinition,
      [NotNull] TypeReference exportedType)
    {
      return typeDefinition.CustomAttributes.Any(a =>
        a.AttributeType.FullName == typeof(ExportAttribute).FullName &&
        a.ConstructorArguments.Any(c => ((TypeReference)c.Value).FullName == exportedType.FullName));
    }

    public static bool ImplementsInterface([CanBeNull] this TypeDefinition typeDefinition,
      [NotNull] TypeReference interfaceType)
    {
      if (!interfaceType.Resolve().IsInterface)
      {
        return false;
      }

      if (typeDefinition?.BaseType == null)
      {
        return false;
      }

      if (typeDefinition.Interfaces.Any(i => i.FullName == interfaceType.FullName))
      {
        return true;
      }

      return typeDefinition.BaseType.Resolve().ImplementsInterface(interfaceType);
    }

    public static bool InheritsBase([CanBeNull] this TypeDefinition typeDefinition,
      [NotNull] TypeReference baseType)
    {
      if (baseType.Resolve().IsInterface)
      {
        return false;
      }

      if (typeDefinition?.BaseType == null)
      {
        return false;
      }

      if (typeDefinition.BaseType.FullName == baseType.FullName)
      {
        return true;
      }

      return typeDefinition.BaseType.Resolve().InheritsBase(baseType);
    }

    [NotNull]
    public static MethodDefinition FindMethod(
      [NotNull] this TypeDefinition typeDefinition,
      [NotNull] string method,
      [NotNull] params string[] paramTypes)
    {
      return typeDefinition.Methods.First(x =>
        x.Name == method &&
        x.IsMatch(paramTypes));
    }
  }
}
