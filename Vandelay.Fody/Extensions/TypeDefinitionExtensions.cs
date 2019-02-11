using System.Linq;
using Mono.Cecil;

namespace Vandelay.Fody.Extensions
{
  static class TypeDefinitionExtensions
  {
    public static bool IsClass(this TypeDefinition typeDefinition) =>
      typeDefinition.BaseType != null &&
        !typeDefinition.IsEnum &&
        !typeDefinition.IsInterface;

    public static bool ExportsType(this TypeDefinition typeDefinition,
      TypeReference exportedType) =>
      typeDefinition.CustomAttributes.Any(a =>
        a.AttributeType.FullName == "System.ComponentModel.Composition.ExportAttribute" &&
        a.ConstructorArguments.Any(c => ((TypeReference)c.Value).FullName == exportedType.FullName));

    public static bool ImplementsInterface(this TypeDefinition typeDefinition,
      TypeReference interfaceType)
    {
      if (!interfaceType.Resolve().IsInterface)
      {
        return false;
      }

      if (typeDefinition?.BaseType == null)
      {
        return false;
      }

      if (typeDefinition.Interfaces.Any(i =>
        i.InterfaceType.FullName == interfaceType.FullName))
      {
        return true;
      }

      return typeDefinition.BaseType.Resolve().ImplementsInterface(interfaceType);
    }

    public static bool InheritsBase(this TypeDefinition typeDefinition,
      TypeReference baseType)
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

    public static MethodDefinition FindMethod(
      this TypeDefinition typeDefinition,
      string method,
      params string[] paramTypes) =>
      typeDefinition.Methods.First(x =>
        x.Name == method &&
        x.IsMatch(paramTypes));
  }
}
