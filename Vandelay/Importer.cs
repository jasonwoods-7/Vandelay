using System;
using System.Collections.Generic;

namespace Vandelay
{
  /// <summary>
  /// Static class to retrieve imports
  /// </summary>
  public static class Importer
  {
    /// <summary>
    /// Import all exported instances of a type.
    /// </summary>
    /// <param name="searchPatterns"></param>
    /// <param name="exports"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IReadOnlyList<T> ImportMany<T>(
      string searchPatterns, params object[] exports)
    {
      throw new NotSupportedException();
    }
  }
}
