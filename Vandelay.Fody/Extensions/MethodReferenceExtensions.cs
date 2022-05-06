namespace Vandelay.Fody.Extensions;

static class MethodReferenceExtensions
{
  public static bool IsMatch(this MethodReference methodReference,
    params string[] paramTypes)
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

  public static MethodReference MakeGeneric(this MethodReference methodReference,
    params TypeReference[] paramTypes)
  {
    var reference = new MethodReference(
      methodReference.Name,
      methodReference.ReturnType,
      methodReference.DeclaringType.MakeGenericInstanceType(paramTypes))
    {
      HasThis = methodReference.HasThis
    };

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
