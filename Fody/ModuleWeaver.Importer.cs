using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    TypeDefinition ExportValueProvider { get; set; }

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

    void ProcessInstruction(MethodDefinition method, Instruction instruction)
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
        throw new WeavingException($"Unsupported method '{methodReference.FullName}'.");
      }

      ProcessImportMany(method, instruction, methodReference);
    }

    void ProcessImportMany(MethodDefinition method, Instruction instruction,
      IGenericInstance methodReference)
    {
      var importType = methodReference.GenericArguments[0];

      var searchPatternInstruction = instruction.Previous;
      while (searchPatternInstruction.OpCode.Code != Code.Ldstr)
      {
        searchPatternInstruction = searchPatternInstruction.Previous;
        Debug.Assert(null != searchPatternInstruction);
      }

      if (null == ExportValueProvider)
      {
        ExportValueProvider = InjectExportValueProvider();
        ModuleDefinition.Types.Add(ExportValueProvider);
      }

      var searchPatterns = ((searchPatternInstruction.Operand as string) ??
        string.Empty).Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);

      var importer = InjectHelper(importType, searchPatterns);

      instruction.Operand = importer;

      method.Body.GetILProcessor().Remove(searchPatternInstruction);
    }

    TypeDefinition InjectExportValueProvider()
    {
      var valueProvider = new TypeDefinition($"{ModuleDefinition.Name}.Retriever",
        "ExportValueProvider", TypeAttributes.Sealed | TypeAttributes.AutoClass,
        ModuleDefinition.TypeSystem.Object);

      var valueField = InjectValueField();
      valueProvider.Fields.Add(valueField);

      var valueCtor = InjectValueConstructor(valueField);
      valueProvider.Methods.Add(valueCtor);

      var valueFunc = InjectValueFunc(valueField);
      valueProvider.Methods.Add(valueFunc);

      return valueProvider;
    }

    FieldDefinition InjectValueField()
    {
      var valueField = new FieldDefinition("_value",
        FieldAttributes.Private | FieldAttributes.InitOnly,
        ModuleDefinition.TypeSystem.Object);

      return valueField;
    }

    MethodDefinition InjectValueConstructor(FieldDefinition valueField)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig;

      var constructor = new MethodDefinition(".ctor", methodAttributes,
        ModuleDefinition.TypeSystem.Void);

      constructor.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.TypeSystem.Object));

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference(".ctor", ModuleDefinition.TypeSystem.Void,
          ModuleDefinition.TypeSystem.Object) {HasThis = true}));

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, valueField));

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return constructor;
    }

    MethodDefinition InjectValueFunc(FieldDefinition valueField)
    {
      var func = new MethodDefinition("GetValue", MethodAttributes.Public,
        ModuleDefinition.TypeSystem.Object);

      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, valueField));
      func.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return func;
    }

    MethodDefinition InjectHelper(TypeReference importType,
      IReadOnlyCollection<string> searchPatterns)
    {
      const TypeAttributes typeAttributes = TypeAttributes.AnsiClass |
        TypeAttributes.Sealed | TypeAttributes.AutoClass;

      var targetType = new TypeDefinition($"{ModuleDefinition.Name}.Retriever",
        TargetName($"{importType.Name}Retriever", 0), typeAttributes,
        ModuleDefinition.TypeSystem.Object);

      ModuleDefinition.Types.Add(targetType);

      var fieldTuple = InjectImportsField(importType);
      targetType.Fields.Add(fieldTuple.Item1);

      var compositionBatch = InjectCreateCompositionBatch();
      targetType.Methods.Add(compositionBatch);

      var constructor = InjectConstructor(searchPatterns, compositionBatch);
      targetType.Methods.Add(constructor);

      var retrieverProperty = InjectRetrieverProperty(importType, fieldTuple.Item2,
        constructor, fieldTuple.Item1);
      targetType.Methods.Add(retrieverProperty);

      return retrieverProperty;
    }

    string TargetName(string targetName, int counter)
    {
      var suggestedName = targetName;
      while (null != ModuleDefinition.Types.FirstOrDefault(
        t => t.Name == suggestedName))
      {
        suggestedName = $"{targetName}_{counter++}";
      }

      return suggestedName;
    }

    Tuple<FieldDefinition, GenericInstanceType>
      InjectImportsField(TypeReference importType)
    {
      var importerCollectionType = ModuleDefinition.ImportReference(
        typeof(IEnumerable<>)).MakeGenericInstanceType(importType);
      var fieldDefinition = new FieldDefinition("_imports",
        FieldAttributes.Private, importerCollectionType);

      var importAttribute = new CustomAttribute(ModuleDefinition.ImportReference(
        typeof(ImportManyAttribute).GetConstructor(new[] {typeof(Type)})));
      importAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        ModuleDefinition.TypeSystem.TypedReference, importType));

      fieldDefinition.CustomAttributes.Add(importAttribute);

      return Tuple.Create(fieldDefinition, importerCollectionType);
    }

    MethodDefinition InjectCreateCompositionBatch()
    {
      var compositionBatch = new MethodDefinition("CreateCompositionBatch",
        MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
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

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(CompositionBatch)
        .GetConstructor(new Type[0]))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

      var loopConditionCheck = Instruction.Create(OpCodes.Ldloc_1);

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S,
        loopConditionCheck));

      var loopStart = Instruction.Create(OpCodes.Ldarg_0);
      compositionBatch.Body.Instructions.Add(loopStart);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(object).GetMethod("GetType"))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_3));

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_3));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(AttributedModelServices)
        .GetMethod("GetContractName"))));
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
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ExportValueProvider.Methods.First(m => m.IsConstructor)));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn,
        ExportValueProvider.Methods.First(m => !m.IsConstructor)));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(Func<object>)
        .GetConstructor(new[] {typeof(object), typeof(IntPtr)}))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(Export).GetConstructor(new []
        {
          typeof(string),
          typeof(Dictionary<string, object>),
          typeof(Func<object>)
        }))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(CompositionBatch)
        .GetMethod("AddExport", new[] {typeof(Export)}))));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Add));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

      compositionBatch.Body.Instructions.Add(loopConditionCheck);
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Blt_S, loopStart));

      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
      compositionBatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return compositionBatch;
    }

    MethodDefinition InjectConstructor(
      IReadOnlyCollection<string> searchPatterns,
      MethodReference compositionBatch)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig |
        MethodAttributes.Private;

      var constructor = new MethodDefinition(".ctor", methodAttributes,
        ModuleDefinition.TypeSystem.Void);
      constructor.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference(".ctor", ModuleDefinition.TypeSystem.Void,
        ModuleDefinition.TypeSystem.Object) {HasThis = true}));

      constructor.Body.MaxStackSize = 5;
      constructor.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(AggregateCatalog))));
      constructor.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(CompositionContainer))));
      constructor.Body.InitLocals = true;

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(AggregateCatalog)
        .GetConstructor(new Type[] {}))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

      var catalogBodyStart = Instruction.Create(OpCodes.Nop);
      constructor.Body.Instructions.Add(catalogBodyStart);

      InjectSearchPatterns(constructor, searchPatterns);

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr,
        ModuleDefinition.ImportReference(typeof(ExportProvider))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(CompositionContainer)
        .GetConstructor(new[]
        {
          typeof(ComposablePartCatalog),
          typeof(ExportProvider[])
        }))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

      var compositionContainerStart = Instruction.Create(OpCodes.Nop);
      constructor.Body.Instructions.Add(compositionContainerStart);

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compositionBatch));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(CompositionContainer)
        .GetMethod("Compose"))));

      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr,
        ModuleDefinition.TypeSystem.Object));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(AttributedModelServices)
        .GetMethod("ComposeParts"))));

      var ret = Instruction.Create(OpCodes.Ret);
      var catalogLeave = Instruction.Create(OpCodes.Leave, ret);

      InjectUsingStatement(constructor.Body,
        compositionContainerStart, OpCodes.Ldloc_1, catalogLeave,
        Instruction.Create(OpCodes.Leave, catalogLeave));
      InjectUsingStatement(constructor.Body,
        catalogBodyStart, OpCodes.Ldloc_0, ret, catalogLeave);

      constructor.Body.Instructions.Add(ret);

      return constructor;
    }

    void InjectSearchPatterns(MethodDefinition constructor,
      IReadOnlyCollection<string> searchPatterns)
    {
      if (searchPatterns.Count == 0)
      {
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(AggregateCatalog)
          .GetProperty("Catalogs").GetGetMethod())));
        InjectCatalogPath(constructor);
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
          ModuleDefinition.ImportReference(typeof(DirectoryCatalog)
          .GetConstructor(new[] {typeof(string)}))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(ICollection<ComposablePartCatalog>)
          .GetMethod("Add"))));
      }
      else
      {
        constructor.Body.Variables.Add(new VariableDefinition(ModuleDefinition.TypeSystem.String));

        InjectCatalogPath(constructor);
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));

        foreach (var searchPattern in searchPatterns)
        {
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
            ModuleDefinition.ImportReference(typeof(AggregateCatalog)
            .GetProperty("Catalogs").GetGetMethod())));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, searchPattern));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
            ModuleDefinition.ImportReference(typeof(DirectoryCatalog)
            .GetConstructor(new[] {typeof(string), typeof(string)}))));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
            ModuleDefinition.ImportReference(typeof(ICollection<ComposablePartCatalog>)
            .GetMethod("Add"))));
        }
      }
    }

    void InjectCatalogPath(MethodDefinition constructor)
    {
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(Assembly)
        .GetMethod("GetExecutingAssembly"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(Assembly)
        .GetProperty("EscapedCodeBase").GetGetMethod())));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(Uri)
        .GetConstructor(new[] { typeof(string) }))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(Uri)
        .GetProperty("LocalPath").GetGetMethod())));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(typeof(Directory)
        .GetMethod("GetParent"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(FileSystemInfo)
        .GetProperty("FullName").GetGetMethod())));
    }

    void InjectUsingStatement(MethodBody methodBody, Instruction bodyStart,
      OpCode loadLocation, Instruction handlerEnd, Instruction leave)
    {
      var startFinally = Instruction.Create(loadLocation);
      var endFinally = Instruction.Create(OpCodes.Endfinally);

      methodBody.Instructions.Add(leave);
      methodBody.Instructions.Add(startFinally);
      methodBody.Instructions.Add(Instruction.Create(OpCodes.Brfalse_S, endFinally));

      methodBody.Instructions.Add(Instruction.Create(loadLocation));
      methodBody.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(IDisposable).GetMethod("Dispose"))));

      methodBody.Instructions.Add(endFinally);

      var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
      {
        TryStart = bodyStart,
        TryEnd = startFinally,
        HandlerStart = startFinally,
        HandlerEnd = handlerEnd
      };

      methodBody.ExceptionHandlers.Add(handler);
    }

    MethodDefinition InjectRetrieverProperty(MemberReference importerType,
      TypeReference importerCollectionType, MethodReference ctor,
      FieldReference fieldDefinition)
    {
      var retriever = new MethodDefinition($"{importerType.Name}Retriever",
        MethodAttributes.Public | MethodAttributes.Static |
        MethodAttributes.HideBySig, importerCollectionType);

      retriever.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));

      retriever.Body.MaxStackSize = 1;
      retriever.Body.Variables.Add(new VariableDefinition(importerCollectionType));
      retriever.Body.InitLocals = true;

      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
      var load = Instruction.Create(OpCodes.Ldloc_0);
      //retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, load));
      retriever.Body.Instructions.Add(load);
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return retriever;
    }
  }
}
