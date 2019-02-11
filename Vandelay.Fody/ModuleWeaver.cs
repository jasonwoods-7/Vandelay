using Fody;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody
{
  public partial class ModuleWeaver : BaseModuleWeaver
  {
    public override void Execute()
    {
      FindReferences();

      HandleExports();
      HandleImports();

      ModuleDefinition.Assembly.CustomAttributes.RemoveExporter();
    }

    public override bool ShouldCleanReference => true;
  }
}
