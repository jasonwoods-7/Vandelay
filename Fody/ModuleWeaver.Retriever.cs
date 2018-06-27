using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Vandelay.Fody.Extensions;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Vandelay.Fody
{
  partial class ModuleWeaver
  {
    [NotNull]
    MethodDefinition InjectRetriever([NotNull] TypeReference importType,
      [NotNull] IReadOnlyCollection<string> searchPatterns)
    {
      const TypeAttributes typeAttributes = TypeAttributes.AnsiClass |
        TypeAttributes.Sealed | TypeAttributes.AutoClass;

      // internal sealed class ImportTypeRetriever
      var targetType = new TypeDefinition("Vandelay",
        TargetName($"{importType.Name}Retriever", -1), typeAttributes,
        TypeSystem.ObjectReference);
      ModuleDefinition.Types.Add(targetType);

      var fieldTuple = InjectImportsField(importType);
      targetType.Fields.Add(fieldTuple.Item1);

      var constructor = InjectConstructor(searchPatterns);
      targetType.Methods.Add(constructor);

      var retrieverProperty = InjectRetrieverProperty(importType, fieldTuple.Item2,
        constructor, fieldTuple.Item1);
      targetType.Methods.Add(retrieverProperty);

      return retrieverProperty;
    }

    [NotNull]
    string TargetName([NotNull] string targetName, int counter)
    {
      var suggestedName = -1 == counter ? targetName : $"{targetName}_{counter}";
      if (null == ModuleDefinition.Types.FirstOrDefault(t => t.Name == suggestedName))
      {
        return suggestedName;
      }

      return TargetName(targetName, counter + 1);
    }

    [NotNull]
    Tuple<FieldDefinition, GenericInstanceType>
      InjectImportsField([NotNull] TypeReference importType)
    {
      // [ImportMany(typeof(ImportType))]
      // private IEnumerable<ImportType> _imports;
      var importerCollectionType = ModuleDefinition.ImportReference(
        typeof(IEnumerable<>)).MakeGenericInstanceType(importType);
      var fieldDefinition = new FieldDefinition("_imports",
        FieldAttributes.Private, importerCollectionType);

      var importAttribute = new CustomAttribute(ModuleDefinition.ImportReference(
        Info.OfConstructor("System.ComponentModel.Composition",
        "System.ComponentModel.Composition.ImportManyAttribute", "Type")));
      importAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
        FindType("System.Type"), importType));

      fieldDefinition.CustomAttributes.Add(importAttribute);

      return Tuple.Create(fieldDefinition, importerCollectionType);
    }

    [NotNull]
    MethodDefinition InjectConstructor(
      [NotNull] IReadOnlyCollection<string> searchPatterns)
    {
      const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
        MethodAttributes.RTSpecialName | MethodAttributes.HideBySig |
        MethodAttributes.Private;

      // private void .ctor(object[] array)
      var constructor = new MethodDefinition(".ctor", methodAttributes,
        TypeSystem.VoidReference);
      constructor.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));
      constructor.CustomAttributes.MarkAsGeneratedCode();

      constructor.Body.MaxStackSize = 5;
      constructor.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(AggregateCatalog))));
      constructor.Body.Variables.Add(new VariableDefinition(
        ModuleDefinition.ImportReference(typeof(CompositionContainer))));
      constructor.Body.InitLocals = true;

      // base.ctor();
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference(".ctor", TypeSystem.VoidReference,
        TypeSystem.ObjectReference)
        { HasThis = true }));

      // using (var aggregateCatalog = new AggregateCatalog())
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(Info.OfConstructor(
          "System.ComponentModel.Composition",
          "System.ComponentModel.Composition.Hosting.AggregateCatalog"))));
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
        ModuleDefinition.ImportReference(Info.OfConstructor(
          "System.ComponentModel.Composition",
          "System.ComponentModel.Composition.Hosting.CompositionContainer",
          "ComposablePartCatalog,ExportProvider[]"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

      var compositionContainerStart = Instruction.Create(OpCodes.Nop);
      constructor.Body.Instructions.Add(compositionContainerStart);

      // compositionContainer.Compose(CompositionBatchHelper.Create(array));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, CreateCompositionBatch));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(Info.OfMethod(
          "System.ComponentModel.Composition",
          "System.ComponentModel.Composition.Hosting.CompositionContainer",
          "Compose", "CompositionBatch"))));

      // compositionContainer.ComposeParts(this);
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr,
        TypeSystem.ObjectReference));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(Info.OfMethod(
          "System.ComponentModel.Composition",
          "System.ComponentModel.Composition.AttributedModelServices",
          "ComposeParts", "CompositionContainer,Object[]"))));

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

    void InjectSearchPatterns([NotNull] MethodDefinition constructor,
      [NotNull] IReadOnlyCollection<string> searchPatterns)
    {
      if (searchPatterns.Count == 0)
      {
        // aggregateCatalog.Catalogs.Add(new DirectoryCatalog(catalogPath));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(Info.OfPropertyGet(
            "System.ComponentModel.Composition",
            "System.ComponentModel.Composition.Hosting.AggregateCatalog",
            "Catalogs"))));
        InjectCatalogPath(constructor);
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
          ModuleDefinition.ImportReference(Info.OfConstructor(
            "System.ComponentModel.Composition",
            "System.ComponentModel.Composition.Hosting.DirectoryCatalog",
            "String"))));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(Info.OfMethod("mscorlib",
            "System.Collections.Generic.ICollection`1<System.ComponentModel.Composition|" +
            "System.ComponentModel.Composition.Primitives.ComposablePartCatalog>",
            "Add", "T"))));
      }
      else
      {
        constructor.Body.Variables.Add(new VariableDefinition(TypeSystem.StringReference));

        InjectCatalogPath(constructor);
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_2));

        foreach (var searchPattern in searchPatterns)
        {
          // aggregateCatalog.Catalogs.Add(new DirectoryCatalog(catalogPath, "search.pattern"));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
            ModuleDefinition.ImportReference(Info.OfPropertyGet(
              "System.ComponentModel.Composition",
              "System.ComponentModel.Composition.Hosting.AggregateCatalog",
              "Catalogs"))));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, searchPattern));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
            ModuleDefinition.ImportReference(Info.OfConstructor(
              "System.ComponentModel.Composition",
              "System.ComponentModel.Composition.Hosting.DirectoryCatalog",
              "String,String"))));
          constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
            ModuleDefinition.ImportReference(Info.OfMethod("mscorlib",
              "System.Collections.Generic.ICollection`1<System.ComponentModel.Composition|" +
              "System.ComponentModel.Composition.Primitives.ComposablePartCatalog>",
              "Add", "T"))));
        }
      }
    }

    void InjectCatalogPath([NotNull] MethodDefinition constructor)
    {
      // var catalogPath = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly()
      //   .EscapedCodeBase).LocalPath).FullName;
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(Info.OfMethod("mscorlib",
        "System.Reflection.Assembly", "GetExecutingAssembly"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(Info.OfPropertyGet("mscorlib",
        "System.Reflection.Assembly", "EscapedCodeBase"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        ModuleDefinition.ImportReference(Info.OfConstructor(
          "System", "System.Uri", "String"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(Info.OfPropertyGet(
          "System", "System.Uri", "LocalPath"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        ModuleDefinition.ImportReference(Info.OfMethod("mscorlib",
        "System.IO.Directory", "GetParent", "String"))));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(Info.OfPropertyGet("mscorlib",
        "System.IO.FileSystemInfo", "FullName"))));
    }

    void InjectUsingStatement([NotNull] MethodBody methodBody, [NotNull] Instruction bodyStart,
      OpCode loadLocation, [NotNull] Instruction handlerEnd, [NotNull] Instruction leave)
    {
      var startFinally = Instruction.Create(loadLocation);
      var endFinally = Instruction.Create(OpCodes.Endfinally);

      methodBody.Instructions.Add(leave);
      methodBody.Instructions.Add(startFinally);
      methodBody.Instructions.Add(Instruction.Create(OpCodes.Brfalse_S, endFinally));

      methodBody.Instructions.Add(Instruction.Create(loadLocation));
      methodBody.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(Info.OfMethod("mscorlib",
        "System.IDisposable", "Dispose"))));

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

    [NotNull]
    MethodDefinition InjectRetrieverProperty([NotNull] MemberReference importerType,
      [NotNull] TypeReference importerCollectionType, [NotNull] MethodReference ctor,
      [NotNull] FieldReference fieldDefinition)
    {
      // public static IEnumerable<ImportType> ImportTypeRetriever(object[] array)
      var retriever = new MethodDefinition($"{importerType.Name}Retriever",
        MethodAttributes.Public | MethodAttributes.Static |
        MethodAttributes.HideBySig, importerCollectionType);
      retriever.Parameters.Add(new ParameterDefinition(
        ModuleDefinition.ImportReference(typeof(object[]))));
      retriever.CustomAttributes.MarkAsGeneratedCode();

      // return new ImportTypeRetriever(array)._imports;
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
      retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

      return retriever;
    }
  }
}
