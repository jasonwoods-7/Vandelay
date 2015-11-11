using System.Linq;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    void CleanReferences()
    {
      ModuleDefinition.Assembly.CustomAttributes.RemoveExportable();

      var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(r => r.Name == "Vandelay");
      if (null == referenceToRemove)
      {
        LogInfo("\tNo reference to 'Vandelay' found. References not modified.");
        return;
      }

      ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
      LogInfo("\tRemoving reference to 'Vandelay'.");
    }
  }
}
