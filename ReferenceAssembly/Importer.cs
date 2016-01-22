using System;
using System.Collections.Generic;

namespace Vandelay
{
  public static class Importer
  {
    public static IEnumerable<T> ImportMany<T>(
      string searchPatterns, params object[] exports)
    {
      throw new NotSupportedException();
    }
  }
}
