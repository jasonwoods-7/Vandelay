using System;

namespace Vandelay
{
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  public class ExporterAttribute : Attribute
  {
    public ExporterAttribute(Type exportType)
    {
    }
  }
}
