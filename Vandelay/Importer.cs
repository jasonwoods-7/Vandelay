using System;
using System.Collections.Generic;

namespace Vandelay
{
  /// <summary>
  ///
  /// </summary>
  public static class Importer
  {
    /// <summary>
    ///
    /// </summary>
    /// <param name="searchPatterns"></param>
    /// <param name="exports"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> ImportMany<T>(
      string searchPatterns, params object[] exports)
    {
      throw new NotSupportedException();
    }
  }
}
