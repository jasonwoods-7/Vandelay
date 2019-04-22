using System;
using System.Reflection;

namespace TestCore
{
  public static class MethodInfoExtensions
  {
    public static T CreateDelegate<T>(this MethodInfo info)
      where T : Delegate =>
      (T)info.CreateDelegate(typeof(T));

    public static T CreateDelegate<T>(this MethodInfo info, object target)
      where T : Delegate =>
      (T)info.CreateDelegate(typeof(T), target);
  }
}
