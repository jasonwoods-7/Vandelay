using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
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

      var importType = methodReference.GenericArguments[0];
      var searchPatterns = ((instruction.Previous.Operand as string)
        ?? string.Empty).Split('|');

      var importer = InjectHelper(importType, searchPatterns);

      instruction.Operand = importer;
      method.Body.GetILProcessor().Remove(instruction.Previous);
    }

    MethodDefinition InjectHelper(TypeReference importType,
      IReadOnlyCollection<string> searchPatterns)
    {
      const TypeAttributes typeAttributes = TypeAttributes.AnsiClass |
        TypeAttributes.Sealed | TypeAttributes.AutoClass;
      string targetName = $"{importType.Name}Retriever";
      var targetType = new TypeDefinition($"{ModuleDefinition.Name}.Retriever",
        targetName, typeAttributes, ModuleDefinition.TypeSystem.Object);

      var counter = 0;
      while (null != ModuleDefinition.Types.FirstOrDefault(
        t => t.Name == targetType.Name))
      {
        targetType.Name = $"{targetName}_{counter++}";
      }

      ModuleDefinition.Types.Add(targetType);

      var fieldTuple = InjectImportsField(importType, targetType);

      var constructor = InjectConstructor(targetType, searchPatterns);

      return InjectRetrieverProperty(importType, fieldTuple.Item2,
        constructor, fieldTuple.Item1, targetType);
    }

    Tuple<FieldDefinition, GenericInstanceType> InjectImportsField(
      TypeReference importType, TypeDefinition targetType)
    {
      var importerCollectionType = ModuleDefinition.ImportReference(
        typeof(IEnumerable<>)).MakeGenericInstanceType(importType);
      var fieldDefinition = new FieldDefinition("_imports",
        FieldAttributes.Private, importerCollectionType);
      targetType.Fields.Add(fieldDefinition);

      var importAttribute = new CustomAttribute(ModuleDefinition.ImportReference(
        typeof(ImportManyAttribute).GetConstructor(new[] {typeof(Type)})));
      importAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        ModuleDefinition.TypeSystem.TypedReference, importType));

      fieldDefinition.CustomAttributes.Add(importAttribute);

      return Tuple.Create(fieldDefinition, importerCollectionType);
    }

    MethodDefinition InjectConstructor(TypeDefinition targetType,
      IReadOnlyCollection<string> searchPatterns)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig |
        MethodAttributes.Private;

      var constructor = new MethodDefinition(".ctor", methodAttributes,
        ModuleDefinition.TypeSystem.Void);
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

      if (searchPatterns.Count == 0)
      {
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(AggregateCatalog)
          .GetProperty("Catalogs").GetGetMethod())));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
          ModuleDefinition.ImportReference(typeof(Assembly)
          .GetMethod("GetExecutingAssembly"))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(Assembly)
          .GetProperty("EscapedCodeBase").GetGetMethod())));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
          ModuleDefinition.ImportReference(typeof(Uri)
          .GetConstructor(new[] {typeof(string)}))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
          ModuleDefinition.ImportReference(typeof(Uri)
          .GetProperty("LocalPath").GetGetMethod())));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
          ModuleDefinition.ImportReference(typeof(Directory)
          .GetMethod("GetParent"))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(FileSystemInfo)
          .GetProperty("FullName").GetGetMethod())));
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

        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
          ModuleDefinition.ImportReference(typeof(Assembly)
          .GetMethod("GetExecutingAssembly"))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(Assembly)
          .GetProperty("EscapedCodeBase").GetGetMethod())));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
          ModuleDefinition.ImportReference(typeof(Uri)
          .GetConstructor(new[] {typeof(string)}))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
          ModuleDefinition.ImportReference(typeof(Uri)
          .GetProperty("LocalPath").GetGetMethod())));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
          ModuleDefinition.ImportReference(typeof(Directory)
          .GetMethod("GetParent"))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(typeof(FileSystemInfo)
          .GetProperty("FullName").GetGetMethod())));
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

      var compositionContainerStart = Instruction.Create(OpCodes.Ldloc_1);
      constructor.Body.Instructions.Add(compositionContainerStart);
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
      targetType.Methods.Add(constructor);

      return constructor;
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

    static MethodDefinition InjectRetrieverProperty(MemberReference importerType,
      TypeReference importerCollectionType, MethodReference ctor,
      FieldReference fieldDefinition, TypeDefinition targetType)
    {
      var retriever = new MethodDefinition($"{importerType.Name}Retriever",
        MethodAttributes.Public | MethodAttributes.Static |
        MethodAttributes.HideBySig, importerCollectionType);

      retriever.Body.MaxStackSize = 1;
      retriever.Body.Variables.Add(new VariableDefinition(importerCollectionType));
      retriever.Body.InitLocals = true;

      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
      var load = Instruction.Create(OpCodes.Ldloc_0);
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Br_S, load));
      retriever.Body.Instructions.Add(load);
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      targetType.Methods.Add(retriever);

      return retriever;
    }
  }
}
