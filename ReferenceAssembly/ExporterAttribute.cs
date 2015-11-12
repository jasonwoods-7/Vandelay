using System;

namespace Vandelay
{
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  public class ExporterAttribute : Attribute
  {
    public Type ExportType { get; set; }

    public ExporterAttribute(Type exportType)
    {
      ExportType = exportType;
    }
  }
}
