using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    MethodReference CreateCompositionBatch { get; set; }

    void InjectCompositionBatchHelper()
    {
      if (null != CreateCompositionBatch)
      {
        return;
      }

      // internal static class CompositionBatchHelper
      var compositionBatch = new TypeDefinition(
        $"{ModuleDefinition.Name}.Retriever", "CompositionBatchHelper",
        TypeAttributes.AnsiClass | TypeAttributes.Sealed |
        TypeAttributes.AutoClass | TypeAttributes.Abstract,
        ModuleDefinition.TypeSystem.Object);

      var createCompositionBatch = InjectCreate();
      compositionBatch.Methods.Add(createCompositionBatch);
      CreateCompositionBatch = createCompositionBatch;

      ModuleDefinition.Types.Add(compositionBatch);
    }

    MethodDefinition InjectCreate()
    {
      // public static CompositionBatch Create(object[] array)
      var compositionBatch = new MethodDefinition("Create",
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
        ModuleDefinition.ImportReference(typeof(CompositionBatch)));
      compositionBatch.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));

      compositionBatch.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(CompositionBatch))));
      compositionBatch.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.TypeSystem.Int32));
      compositionBatch.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.TypeSystem.Object));
      compositionBatch.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(Type))));

      // var compositionBatch = new CompositionBatch();
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(CompositionBatch)
        .GetConstructor(new Type[0]))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

      // var i = 0;
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

      var loopConditionCheck = Instruction.Create(OpCodes.Ldloc_1);

      // goto loopConditionCheck;
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S,
        loopConditionCheck));

      // loopStart:
      // var obj = array[i];
      var loopStart = Instruction.Create(OpCodes.Ldarg_0);
      compositionBatch.Body.Instructions.Add(loopStart);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));

      // var type = obj.GetType();
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(object).GetMethod("GetType"))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_3));

      // var contractName = AttributedModelServices.GetContractName(type);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(AttributedModelServices)
        .GetMethod("GetContractName"))));

      // var metaData = new Dictionary<string, object>
      // {
      //   ["ExportTypeIdentity"] = AttributedModelServices.GetTypeIdentity(type)
      // }
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(Dictionary<string, object>)
        .GetConstructor(new Type[0]))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr,
        "ExportTypeIdentity"));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(AttributedModelServices)
        .GetMethod("GetTypeIdentity", new[] {typeof(Type)}))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(Dictionary<string, object>)
        .GetProperty("Item").GetSetMethod())));

      // var valueProvider = new ExportValueProvider(obj);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ExportValueProvider.GetConstructors().First()));

      // var valueFunc = new Func<object>(valueProvider.GetValue);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn,
        ExportValueProvider.Methods.First(m => !m.IsConstructor)));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(Func<object>)
        .GetConstructor(new[] {typeof(object), typeof(IntPtr)}))));

      // var export = new Export(contractName, metaData, valueFunc);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(Export).GetConstructor(new[]
        {
          typeof(string),
          typeof(Dictionary<string, object>),
          typeof(Func<object>)
        }))));

      // compositionBatch.AddExport(export);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(CompositionBatch)
        .GetMethod("AddExport", new[] {typeof(Export)}))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));

      // i++;
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Add));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

      // loopConditionCheck:
      // if (i < array.Length) goto loopStart;
      compositionBatch.Body.Instructions.Add(loopConditionCheck);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Blt_S, loopStart));

      // return compositionBatch;
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return compositionBatch;
    }
  }
}
