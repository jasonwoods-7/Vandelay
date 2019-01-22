using System;

namespace Vandelay
{
  /// <summary>
  ///
  /// </summary>
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  public class ExporterAttribute : Attribute
  {
    /// <summary>
    ///
    /// </summary>
    /// <param name="exportType"></param>
    public ExporterAttribute(Type exportType)
    {
    }
  }
}
