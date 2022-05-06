using Fody;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody;

partial class ModuleWeaver
{
  void HandleImports()
  {
    foreach (var method in ModuleDefinition.GetTypes()
      .Where(t => t.IsClass())
      .SelectMany(t => t.Methods)
      .Where(method => method.HasBody))
    {
      Process(method);
    }
  }

  void Process(MethodDefinition method)
  {
    var instructions = method.Body.Instructions
      .Where(i => i.OpCode == OpCodes.Call).ToList();

    foreach (var instruction in instructions)
    {
      ProcessInstruction(method, instruction);
    }
  }

  void ProcessInstruction(MethodDefinition method,
    Instruction instruction)
  {
    if (!(instruction.Operand is GenericInstanceMethod methodReference))
    {
      return;
    }

    if (methodReference.DeclaringType.FullName != "Vandelay.Importer")
    {
      return;
    }

    if (methodReference.Name != "ImportMany")
    {
      throw new WeavingException($"Unsupported method '{methodReference.FullName}'.")
      {
        SequencePoint = method.DebugInformation.GetSequencePoint(instruction)
      };
    }

    ProcessImportMany(method, instruction, methodReference);
  }

  void ProcessImportMany(MethodDefinition method,
    Instruction instruction, IGenericInstance methodReference)
  {
    InjectExportValueProvider();
    InjectCompositionBatchHelper();

    var searchPatternInstruction = SearchPatternInstruction(instruction.Previous);
    instruction.Operand = InjectRetriever(methodReference.GenericArguments[0],
      (searchPatternInstruction.Operand as string ?? string.Empty)
      .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));

    method.Body.UpdateInstructions(searchPatternInstruction,
      searchPatternInstruction.Next);

    method.Body.GetILProcessor().Remove(searchPatternInstruction);
  }

  static Instruction SearchPatternInstruction(Instruction instruction)
  {
    if (Code.Ldstr == instruction.OpCode.Code)
    {
      return instruction;
    }

    return SearchPatternInstruction(instruction.Previous);
  }
}
