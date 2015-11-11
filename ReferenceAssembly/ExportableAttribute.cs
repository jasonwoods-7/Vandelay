using System;

namespace Vandelay
{
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  public class ExportableAttribute : Attribute
  {
    public Type ExportType { get; set; }

    public ExportableAttribute(Type exportType)
    {
      ExportType = exportType;
    }
  }
}
