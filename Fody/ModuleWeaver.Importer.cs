using System;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody
{
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

    void Process([NotNull] MethodDefinition method)
    {
      var instructions = method.Body.Instructions
        .Where(i => i.OpCode == OpCodes.Call).ToList();

      foreach (var instruction in instructions)
      {
        ProcessInstruction(method, instruction);
      }
    }

    void ProcessInstruction([NotNull] MethodDefinition method,
      [NotNull] Instruction instruction)
    {
      var methodReference = instruction.Operand as GenericInstanceMethod;
      if (null == methodReference)
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
          SequencePoint = instruction.SequencePoint
        };
      }

      ProcessImportMany(method, instruction, methodReference);
    }

    void ProcessImportMany([NotNull] MethodDefinition method,
      [NotNull] Instruction instruction, [NotNull] IGenericInstance methodReference)
    {
      InjectExportValueProvider();
      InjectCompositionBatchHelper();

      var searchPatternInstruction = SearchPatternInstruction(instruction.Previous);
      instruction.Operand = InjectRetriever(methodReference.GenericArguments[0],
        ((searchPatternInstruction.Operand as string) ??
         string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));

      method.Body.GetILProcessor().Remove(searchPatternInstruction);
    }

    [NotNull]
    static Instruction SearchPatternInstruction([NotNull] Instruction instruction)
    {
      if (Code.Ldstr == instruction.OpCode.Code)
      {
        return instruction;
      }

      return SearchPatternInstruction(instruction.Previous);
    }
  }
}
