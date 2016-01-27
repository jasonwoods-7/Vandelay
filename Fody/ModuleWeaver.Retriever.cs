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
    MethodDefinition InjectRetriever(TypeReference importType,
      IReadOnlyCollection<string> searchPatterns)
    {
      const TypeAttributes typeAttributes = TypeAttributes.AnsiClass |
        TypeAttributes.Sealed | TypeAttributes.AutoClass;

      // internal sealed class ImportTypeRetriever
      var targetType = new TypeDefinition($"{ModuleDefinition.Name}.Retriever",
        TargetName($"{importType.Name}Retriever", -1), typeAttributes,
        ModuleDefinition.TypeSystem.Object);
      ModuleDefinition.Types.Add(targetType);

      var fieldTuple = InjectImportsField(importType);
      targetType.Fields.Add(fieldTuple.Item1);

      var constructor = InjectConstructor(searchPatterns, CreateCompositionBatch);
      targetType.Methods.Add(constructor);

      var retrieverProperty = InjectRetrieverProperty(importType, fieldTuple.Item2,
        constructor, fieldTuple.Item1);
      targetType.Methods.Add(retrieverProperty);

      return retrieverProperty;
    }

    string TargetName(string targetName, int counter)
    {
      var suggestedName = -1 == counter ? targetName : $"{targetName}_{counter}";
      if (null == ModuleDefinition.Types.FirstOrDefault(t => t.Name == suggestedName))
      {
        return suggestedName;
      }

      return TargetName(targetName, counter + 1);
    }

    Tuple<FieldDefinition, GenericInstanceType>
      InjectImportsField(TypeReference importType)
    {
      // [ImportMany(typeof(ImportType))]
      // private IEnumerable<ImportType> _imports;
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

    MethodDefinition InjectConstructor(
      IReadOnlyCollection<string> searchPatterns,
      MethodReference compositionBatch)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig |
        MethodAttributes.Private;

      // private void .ctor(object[] array)
      var constructor = new MethodDefinition(".ctor", methodAttributes,
        ModuleDefinition.TypeSystem.Void);
      constructor.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));

      constructor.Body.MaxStackSize = 5;
      constructor.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(AggregateCatalog))));
      constructor.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(CompositionContainer))));
      constructor.Body.InitLocals = true;

      // base.ctor();
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference(".ctor", ModuleDefinition.TypeSystem.Void,
        ModuleDefinition.TypeSystem.Object) {HasThis = true}));

      // using (var aggregateCatalog = new AggregateCatalog())
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(typeof(AggregateCatalog)
        .GetConstructor(new Type[] {}))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

      var catalogBodyStart = Instruction.Create(OpCodes.Nop);
      constructor.Body.Instructions.Add(catalogBodyStart);

      InjectSearchPatterns(constructor, searchPatterns);

      // using (var compositionContainer = new CompositionContainer(aggregateCatalog, new ExportProvider[0]))
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

      // compositionContainer.Compose(CompositionBatchHelper.Create(array));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compositionBatch));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(typeof(CompositionContainer)
        .GetMethod("Compose"))));

      // compositionContainer.ComposeParts(this);
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
        // aggregateCatalog.Catalogs.Add(new DirectoryCatalog(catalogPath));
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
          // aggregateCatalog.Catalogs.Add(new DirectoryCatalog(catalogPath, "search.pattern"));
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
      // var catalogPath = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly()
      //   .EscapedCodeBase).LocalPath).FullName;
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
      // public static IEnumerable<ImportType> ImportTypeRetriever(object[] array)
      var retriever = new MethodDefinition($"{importerType.Name}Retriever",
        MethodAttributes.Public | MethodAttributes.Static |
        MethodAttributes.HideBySig, importerCollectionType);
      retriever.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));

      // return new ImportTypeRetriever(array)._imports;
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return retriever;
    }
  }
}
