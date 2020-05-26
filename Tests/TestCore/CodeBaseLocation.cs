using System;
using System.IO;
using System.Reflection;

#pragma warning disable 618

namespace TestCore
{
  public static class CodeBaseLocation
  {
    static CodeBaseLocation()
    {
      var assembly = typeof(CodeBaseLocation).Assembly;

      var currentAssemblyPath = assembly.GetAssemblyLocation();
      CurrentDirectory = Path.GetDirectoryName(currentAssemblyPath);
    }

    public static string GetAssemblyLocation(this Assembly assembly)
    {
      Fody.Guard.AgainstNull(nameof(assembly), assembly);
      var uri = new UriBuilder(assembly.CodeBase);
      return Uri.UnescapeDataString(uri.Path);
    }

    public static readonly string CurrentDirectory;
  }
}
