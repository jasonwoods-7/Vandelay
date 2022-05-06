using System.ComponentModel.Composition;
using AssemblyToProcess.SimpleCase;
using Vandelay;

[assembly: Exporter(typeof(IInheritedExport))]

namespace AssemblyToProcess.SimpleCase;

[InheritedExport]
public interface IInheritedExport
{
}
