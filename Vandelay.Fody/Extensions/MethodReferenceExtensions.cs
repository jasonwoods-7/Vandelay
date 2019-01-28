using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Vandelay.Fody.Extensions
{
  static class MethodReferenceExtensions
  {
    public static bool IsMatch([NotNull] this MethodReference methodReference,
      [NotNull] params string[] paramTypes)
    {
      if (methodReference.Parameters.Count != paramTypes.Length)
      {
        return false;
      }

      for (var index = 0; index < methodReference.Parameters.Count; index++)
      {
        var parameterDefinition = methodReference.Parameters[index];
        var paramType = paramTypes[index];
        if (parameterDefinition.ParameterType.Name != paramType)
        {
          return false;
        }
      }

      return true;
    }

    public static MethodReference MakeGeneric([NotNull] this MethodReference methodReference,
      [NotNull] params TypeReference[] paramTypes)
    {
      var reference = new MethodReference(
        methodReference.Name,
        methodReference.ReturnType,
        methodReference.DeclaringType.MakeGenericInstanceType(paramTypes));

      foreach (var parameter in methodReference.Parameters)
      {
        reference.Parameters.Add(parameter);
      }

      foreach (var genericParam in methodReference.GenericParameters)
      {
        reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
      }

      return reference;
    }
  }
}
