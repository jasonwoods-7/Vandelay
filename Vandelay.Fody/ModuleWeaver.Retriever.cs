using Vandelay.Fody.Extensions;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Vandelay.Fody;

partial class ModuleWeaver
{
  MethodDefinition InjectRetriever(TypeReference importType,
    IReadOnlyCollection<string> searchPatterns)
  {
    const TypeAttributes typeAttributes = TypeAttributes.AnsiClass |
      TypeAttributes.Sealed | TypeAttributes.AutoClass;

    // internal sealed class ImportTypeRetriever
    var targetType = new TypeDefinition("Vandelay",
      TargetName($"{importType.Name}Retriever", -1), typeAttributes,
      TypeSystem.ObjectReference);
    ModuleDefinition.Types.Add(targetType);

    var importFields = InjectImportsField(importType);
    targetType.Fields.Add(importFields.Definition);

    var constructor = InjectConstructor(searchPatterns);
    targetType.Methods.Add(constructor);

    var retrieverProperty = InjectRetrieverProperty(importType,
      importFields.CollectionType, constructor, importFields.Definition);
    targetType.Methods.Add(retrieverProperty);

    return retrieverProperty;
  }

  string TargetName(string targetName, int counter)
  {
    var suggestedName = -1 == counter ? targetName : $"{targetName}_{counter}";
    if (ModuleDefinition.Types.All(t => t.Name != suggestedName))
    {
      return suggestedName;
    }

    return TargetName(targetName, counter + 1);
  }

  ImportFields InjectImportsField(TypeReference importType)
  {
    // [ImportMany(typeof(ImportType))]
    // private ImportType[] _imports;
    var importerCollectionType = importType.MakeArrayType();
    ModuleDefinition.ImportReference(importerCollectionType);

    var fieldDefinition = new FieldDefinition("_imports",
      FieldAttributes.Private, importerCollectionType);

    var importAttribute = new CustomAttribute(
      _import!.System.ComponentModel.Composition.ImportManyAttribute.Constructor);
    importAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
      _import.System.Type.Type, importType));

    fieldDefinition.CustomAttributes.Add(importAttribute);

    return new ImportFields(
      fieldDefinition, importerCollectionType);
  }

  class ImportFields
  {
    public ImportFields(FieldDefinition definition, ArrayType collectionType)
    {
      Definition = definition;
      CollectionType = collectionType;
    }

    public FieldDefinition Definition { get; }
    public ArrayType CollectionType { get; }
  }

  MethodDefinition InjectConstructor(
    IReadOnlyCollection<string> searchPatterns)
  {
    const MethodAttributes methodAttributes = MethodAttributes.SpecialName |
      MethodAttributes.RTSpecialName | MethodAttributes.HideBySig |
      MethodAttributes.Private;

    // private void .ctor(object[] array)
    var constructor = new MethodDefinition(".ctor", methodAttributes,
      TypeSystem.VoidReference);
    constructor.Parameters.Add(new ParameterDefinition(
      _import!.System.Object.ArrayType));
    constructor.CustomAttributes.MarkAsGeneratedCode(ModuleDefinition, _import);

    constructor.Body.MaxStackSize = 5;
    constructor.Body.Variables.Add(new VariableDefinition(
      _import.System.ComponentModel.Composition.Hosting.AggregateCatalog.Type));
    constructor.Body.Variables.Add(new VariableDefinition(
      _import.System.ComponentModel.Composition.Hosting.CompositionContainer.Type));
    constructor.Body.InitLocals = true;

    // base.ctor();
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
      new MethodReference(".ctor", TypeSystem.VoidReference,
        TypeSystem.ObjectReference)
        { HasThis = true }));

    // using (var aggregateCatalog = new AggregateCatalog())
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
      _import.System.ComponentModel.Composition.Hosting.AggregateCatalog.Constructor));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

    var catalogBodyStart = Instruction.Create(OpCodes.Nop);
    constructor.Body.Instructions.Add(catalogBodyStart);

    InjectSearchPatterns(constructor, searchPatterns);

    // using (var compositionContainer = new CompositionContainer(aggregateCatalog, new ExportProvider[0]))
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr,
      _import.System.ComponentModel.Composition.Hosting.ExportProvider.Type));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
      _import.System.ComponentModel.Composition.Hosting.CompositionContainer.Constructor));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));

    var compositionContainerStart = Instruction.Create(OpCodes.Nop);
    constructor.Body.Instructions.Add(compositionContainerStart);

    // compositionContainer.Compose(CompositionBatchHelper.Create(array));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, CreateCompositionBatch));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
      _import.System.ComponentModel.Composition.Hosting.CompositionContainer.Compose));

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
      _import.System.ComponentModel.Composition.AttributedModelServices.ComposeParts));

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
        _import!.System.ComponentModel.Composition.Hosting.AggregateCatalog.GetCatalogs));
      InjectCatalogPath(constructor);
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
        _import.System.ComponentModel.Composition.Hosting.DirectoryCatalog.ConstructorString));
      constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
        ModuleDefinition.ImportReference(_import.System.Collections.Generic.ICollection.Add.MakeGeneric(
          _import.System.ComponentModel.Composition.Primitives.ComposablePartCatalog.Type))));
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
          _import!.System.ComponentModel.Composition.Hosting.AggregateCatalog.GetCatalogs));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_2));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, searchPattern));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
          _import.System.ComponentModel.Composition.Hosting.DirectoryCatalog.ConstructorStringString));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
          ModuleDefinition.ImportReference(_import.System.Collections.Generic.ICollection.Add.MakeGeneric(
            _import.System.ComponentModel.Composition.Primitives.ComposablePartCatalog.Type))));
      }
    }
  }

  void InjectCatalogPath(MethodDefinition constructor)
  {
    // var catalogPath = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly()
    //   .EscapedCodeBase).LocalPath).FullName;
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
      _import!.System.Reflection.Assembly.GetExecutingAssembly));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
      _import.System.Reflection.Assembly.GetEscapedCodeBase));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj,
      _import.System.Uri.Constructor));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
      _import.System.Uri.GetLocalPath));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
      _import.System.IO.Directory.GetParent));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt,
      _import.System.IO.FileSystemInfo.GetFullName));
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
      _import!.System.IDisposable.Dispose));

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
    // public static ImportType[] ImportTypeRetriever(object[] array)
    var retriever = new MethodDefinition($"{importerType.Name}Retriever",
      MethodAttributes.Public | MethodAttributes.Static |
      MethodAttributes.HideBySig, importerCollectionType);
    retriever.Parameters.Add(new ParameterDefinition(
      _import!.System.Object.ArrayType));
    retriever.CustomAttributes.MarkAsGeneratedCode(ModuleDefinition, _import);

    // return new ImportTypeRetriever(array)._imports;
    retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
    retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
    retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
    retriever.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

    return retriever;
  }
}
