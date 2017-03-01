using JetBrains.Annotations;
using Mono.Cecil;

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
  }
}
