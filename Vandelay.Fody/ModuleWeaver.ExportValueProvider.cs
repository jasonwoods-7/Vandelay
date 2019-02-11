using Mono.Cecil;
using Mono.Cecil.Cil;
using Vandelay.Fody.Extensions;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    TypeDefinition ExportValueProvider { get; set; }

    void InjectExportValueProvider()
    {
      if (null != ExportValueProvider)
      {
        return;
      }

      // internal sealed class ExportValueProvider
      var valueProvider = new TypeDefinition("Vandelay",
        "ExportValueProvider", TypeAttributes.Sealed | TypeAttributes.AutoClass,
        TypeSystem.ObjectReference);

      var valueField = InjectValueField();
      valueProvider.Fields.Add(valueField);

      var valueCtor = InjectValueConstructor(valueField);
      valueProvider.Methods.Add(valueCtor);

      var valueFunc = InjectValueFunc(valueField);
      valueProvider.Methods.Add(valueFunc);

      ExportValueProvider = valueProvider;
      ModuleDefinition.Types.Add(ExportValueProvider);
    }

    FieldDefinition InjectValueField() =>
      // private readonly object _value;
      new FieldDefinition("_value",
        FieldAttributes.Private | FieldAttributes.InitOnly,
        TypeSystem.ObjectReference);

    MethodDefinition InjectValueConstructor(FieldReference valueField)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig;

      // public void .ctor(object value)
      var constructor = new MethodDefinition(".ctor", methodAttributes,
        TypeSystem.VoidReference);
      constructor.Parameters.Add(new ParameterDefinition(
        TypeSystem.ObjectReference));
      constructor.CustomAttributes.MarkAsGeneratedCode(ModuleDefinition, _import);

      // base.ctor();
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference(".ctor", TypeSystem.VoidReference,
          TypeSystem.ObjectReference)
        { HasThis = true }));

      // this._value = value;
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, valueField));

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return constructor;
    }

    MethodDefinition InjectValueFunc(FieldReference valueField)
    {
      // public object GetValue()
      var func = new MethodDefinition("GetValue", MethodAttributes.Public,
        TypeSystem.ObjectReference);
      func.CustomAttributes.MarkAsGeneratedCode(ModuleDefinition, _import);

      // return this._value;
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, valueField));
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return func;
    }
  }
}
