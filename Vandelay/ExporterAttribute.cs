namespace Vandelay;

/// <summary>
/// Assembly attribute to apply exports to a type
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class ExporterAttribute : Attribute
{
  /// <summary>
  /// Class constructor
  /// </summary>
  /// <param name="exportType"></param>
  public ExporterAttribute(Type exportType)
  {
  }
}
