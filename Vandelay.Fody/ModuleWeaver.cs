using Fody;
using JetBrains.Annotations;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody
{
  [UsedImplicitly]
  public partial class ModuleWeaver : BaseModuleWeaver
  {
    [UsedImplicitly]
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
