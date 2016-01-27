using Mono.Cecil;
using Mono.Cecil.Cil;

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
      var valueProvider = new TypeDefinition($"{ModuleDefinition.Name}.Retriever",
        "ExportValueProvider", TypeAttributes.Sealed | TypeAttributes.AutoClass,
        ModuleDefinition.TypeSystem.Object);

      var valueField = InjectValueField();
      valueProvider.Fields.Add(valueField);

      var valueCtor = InjectValueConstructor(valueField);
      valueProvider.Methods.Add(valueCtor);

      var valueFunc = InjectValueFunc(valueField);
      valueProvider.Methods.Add(valueFunc);

      ExportValueProvider = valueProvider;
      ModuleDefinition.Types.Add(ExportValueProvider);
    }

    FieldDefinition InjectValueField()
    {
      // private readonly object _value;
      var valueField = new FieldDefinition("_value",
        FieldAttributes.Private | FieldAttributes.InitOnly,
        ModuleDefinition.TypeSystem.Object);

      return valueField;
    }

    MethodDefinition InjectValueConstructor(FieldReference valueField)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig;

      // public void .ctor(object value)
      var constructor = new MethodDefinition(".ctor", methodAttributes,
        ModuleDefinition.TypeSystem.Void);
      constructor.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.TypeSystem.Object));

      // base.ctor();
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference(".ctor", ModuleDefinition.TypeSystem.Void,
          ModuleDefinition.TypeSystem.Object) {HasThis = true}));

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
        ModuleDefinition.TypeSystem.Object);

      // return this._value;
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, valueField));
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return func;
    }
  }
}
