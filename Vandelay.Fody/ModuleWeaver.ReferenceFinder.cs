using Vandelay.Fody.Extensions;

namespace Vandelay.Fody;

class Import
{
  public Import(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
  {
    System = new SystemImports(findType, moduleDefinition);
  }

  public SystemImports System { get; }

  internal class SystemImports
  {
    public SystemImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
    {
      Collections = new CollectionsImport(findType, moduleDefinition);
      ComponentModel = new ComponentModelImports(findType, moduleDefinition);
      Reflection = new ReflectionImports(findType, moduleDefinition);
      Uri = new UriImports(findType, moduleDefinition);
      IO = new IOImports(findType, moduleDefinition);
      IDisposable = new IDisposableImports(findType, moduleDefinition);
      Diagnostics = new DiagnosticsImports(findType, moduleDefinition);
      CodeDom = new CodeDomImports(findType, moduleDefinition);
      Object = new ObjectImports(findType, moduleDefinition);
      Func = new FuncImports(findType, moduleDefinition);
      Type = new TypeImports(findType, moduleDefinition);
    }

    public CollectionsImport Collections { get; }
    public ComponentModelImports ComponentModel { get; }
    public ReflectionImports Reflection { get; }
    public UriImports Uri { get; }
    public IOImports IO { get; }
    public IDisposableImports IDisposable { get; }
    public DiagnosticsImports Diagnostics { get; }
    public CodeDomImports CodeDom { get; }
    public ObjectImports Object { get; }
    public FuncImports Func { get; }
    public TypeImports Type { get; }

    internal class CollectionsImport
    {
      public CollectionsImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        Generic = new GenericImports(findType, moduleDefinition);
      }

      public GenericImports Generic { get; }

      internal class GenericImports
      {
        public GenericImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          Dictionary = new DictionaryImports(findType, moduleDefinition);
          ICollection = new ICollectionImports(findType, moduleDefinition);
        }

        public DictionaryImports Dictionary { get; }
        public ICollectionImports ICollection { get; }

        internal class DictionaryImports
        {
          public DictionaryImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            var dictionaryType = findType("System.Collections.Generic.Dictionary`2");
            moduleDefinition.ImportReference(dictionaryType);
            Constructor = dictionaryType.FindMethod(".ctor");
            SetItem = dictionaryType.FindMethod("set_Item", "TKey", "TValue");
          }

          public MethodReference Constructor { get; }
          public MethodReference SetItem { get; }
        }

        internal class ICollectionImports
        {
          public ICollectionImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            var collectionType = findType("System.Collections.Generic.ICollection`1");
            moduleDefinition.ImportReference(collectionType);
            Add = collectionType.FindMethod("Add", "T");
          }

          public MethodReference Add { get; }
        }
      }
    }

    internal class ComponentModelImports
    {
      public ComponentModelImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        Composition = new CompositionImports(findType, moduleDefinition);
      }

      public CompositionImports Composition { get; }

      internal class CompositionImports
      {
        public CompositionImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          ImportManyAttribute = new ImportManyAttributeImports(findType, moduleDefinition);
          Hosting = new HostingImports(findType, moduleDefinition);
          AttributedModelServices = new AttributedModelServicesImports(findType, moduleDefinition);
          Primitives = new PrimitivesImports(findType, moduleDefinition);
          ExportAttribute = new ExportAttributeImports(findType, moduleDefinition);
        }

        public ImportManyAttributeImports ImportManyAttribute { get; }
        public HostingImports Hosting { get; }
        public AttributedModelServicesImports AttributedModelServices { get; }
        public PrimitivesImports Primitives { get; }
        public ExportAttributeImports ExportAttribute { get; }

        internal class ImportManyAttributeImports
        {
          public ImportManyAttributeImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            var importManyType = findType("System.ComponentModel.Composition.ImportManyAttribute");
            moduleDefinition.ImportReference(importManyType);
            Constructor = moduleDefinition.ImportReference(importManyType.FindMethod(".ctor", "Type"));
          }

          public MethodReference Constructor { get; }
        }

        internal class HostingImports
        {
          public HostingImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            AggregateCatalog = new AggregateCatalogImports(findType, moduleDefinition);
            CompositionContainer =new CompositionContainerImports(findType, moduleDefinition);
            DirectoryCatalog = new DirectoryCatalogImports(findType, moduleDefinition);
            CompositionBatch = new CompositionBatchImports(findType, moduleDefinition);
            ExportProvider = new ExportProviderImports(findType, moduleDefinition);
          }

          public AggregateCatalogImports AggregateCatalog { get; }
          public CompositionContainerImports CompositionContainer { get; }
          public DirectoryCatalogImports DirectoryCatalog { get; }
          public CompositionBatchImports CompositionBatch { get; }
          public ExportProviderImports ExportProvider { get; }

          internal class AggregateCatalogImports
          {
            public AggregateCatalogImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
              var aggregateCatalogType = findType("System.ComponentModel.Composition.Hosting.AggregateCatalog");
              Type = moduleDefinition.ImportReference(aggregateCatalogType);
              Constructor = moduleDefinition.ImportReference(aggregateCatalogType.FindMethod(".ctor"));
              GetCatalogs = moduleDefinition.ImportReference(aggregateCatalogType.FindMethod("get_Catalogs"));
            }

            public TypeReference Type { get; }
            public MethodReference Constructor { get; }
            public MethodReference GetCatalogs { get; }
          }

          internal class CompositionContainerImports
          {
            public CompositionContainerImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
              var compositionContainerType = findType("System.ComponentModel.Composition.Hosting.CompositionContainer");
              Type = moduleDefinition.ImportReference(compositionContainerType);
              Constructor = moduleDefinition.ImportReference(compositionContainerType.FindMethod(".ctor", "ComposablePartCatalog", "ExportProvider[]"));
              Compose = moduleDefinition.ImportReference(compositionContainerType.FindMethod("Compose", "CompositionBatch"));
            }

            public TypeReference Type { get; }
            public MethodReference Constructor { get; }
            public MethodReference Compose { get; }
          }

          internal class DirectoryCatalogImports
          {
            public DirectoryCatalogImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
              var catalogType = findType("System.ComponentModel.Composition.Hosting.DirectoryCatalog");
              moduleDefinition.ImportReference(catalogType);
              ConstructorString = moduleDefinition.ImportReference(catalogType.FindMethod(".ctor",
                "String"));
              ConstructorStringString = moduleDefinition.ImportReference(catalogType.FindMethod(".ctor",
                "String", "String"));
            }

            public MethodReference ConstructorString { get; }
            public MethodReference ConstructorStringString { get; }
          }

          internal class CompositionBatchImports
          {
            public CompositionBatchImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
              var batchType = findType("System.ComponentModel.Composition.Hosting.CompositionBatch");
              Type = moduleDefinition.ImportReference(batchType);
              Constructor = moduleDefinition.ImportReference(batchType.FindMethod(".ctor"));
              AddExport = moduleDefinition.ImportReference(batchType.FindMethod("AddExport", "Export"));
            }

            public TypeReference Type { get; }
            public MethodReference Constructor { get; }
            public MethodReference AddExport { get; }
          }

          internal class ExportProviderImports
          {
            public ExportProviderImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
              var exportType = findType("System.ComponentModel.Composition.Hosting.ExportProvider");
              Type = moduleDefinition.ImportReference(exportType);
              moduleDefinition.ImportReference(exportType.MakeArrayType());
            }

            public TypeReference Type { get; }
          }
        }

        internal class AttributedModelServicesImports
        {
          public AttributedModelServicesImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            var servicesType = findType("System.ComponentModel.Composition.AttributedModelServices");
            moduleDefinition.ImportReference(servicesType);
            ComposeParts = moduleDefinition.ImportReference(servicesType.FindMethod("ComposeParts", "CompositionContainer", "Object[]"));
            GetContractName = moduleDefinition.ImportReference(servicesType.FindMethod("GetContractName", "Type"));
            GetTypeIdentity = moduleDefinition.ImportReference(servicesType.FindMethod("GetTypeIdentity", "Type"));
          }

          public MethodReference ComposeParts { get; }
          public MethodReference GetContractName { get; }
          public MethodReference GetTypeIdentity { get; }
        }

        internal class PrimitivesImports
        {
          public PrimitivesImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            ComposablePartCatalog = new ComposablePartCatalogImports(findType, moduleDefinition);
            Export = new ExportImports(findType, moduleDefinition);
          }

          public ComposablePartCatalogImports ComposablePartCatalog { get; }
          public ExportImports Export { get; }

          internal class ComposablePartCatalogImports
          {
            public ComposablePartCatalogImports(Func<string, TypeReference> findType, ModuleDefinition moduleDefinition)
            {
              var partCatalogType = findType("System.ComponentModel.Composition.Primitives.ComposablePartCatalog");
              Type = moduleDefinition.ImportReference(partCatalogType);
            }

            public TypeReference Type { get; }
          }

          internal class ExportImports
          {
            public ExportImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
              var exportType = findType("System.ComponentModel.Composition.Primitives.Export");
              moduleDefinition.ImportReference(exportType);
              Constructor = moduleDefinition.ImportReference(exportType.FindMethod(".ctor", "String", "IDictionary`2", "Func`1"));
            }

            public MethodReference Constructor { get; }
          }
        }

        internal class ExportAttributeImports
        {
          public ExportAttributeImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            var attributeType = findType("System.ComponentModel.Composition.ExportAttribute");
            moduleDefinition.ImportReference(attributeType);
            Constructor = moduleDefinition.ImportReference(attributeType.FindMethod(".ctor", "Type"));
          }

          public MethodReference Constructor { get; }
        }
      }
    }

    internal class ReflectionImports
    {
      public ReflectionImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        Assembly = new AssemblyImports(findType, moduleDefinition);
      }

      public AssemblyImports Assembly { get; }

      internal class AssemblyImports
      {
        public AssemblyImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          var assemblyType = findType("System.Reflection.Assembly");
          moduleDefinition.ImportReference(assemblyType);
          GetExecutingAssembly = moduleDefinition.ImportReference(assemblyType.FindMethod("GetExecutingAssembly"));
          GetEscapedCodeBase = moduleDefinition.ImportReference(assemblyType.FindMethod("get_EscapedCodeBase"));
        }

        public MethodReference GetExecutingAssembly { get; }
        public MethodReference GetEscapedCodeBase { get; }
      }
    }

    internal class UriImports
    {
      public UriImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        var uriType = findType("System.Uri");
        moduleDefinition.ImportReference(uriType);
        Constructor = moduleDefinition.ImportReference(uriType.FindMethod(".ctor", "String"));
        GetLocalPath = moduleDefinition.ImportReference(uriType.FindMethod("get_LocalPath"));
      }

      public MethodReference Constructor { get; }
      public MethodReference GetLocalPath { get; }
    }

    internal class IOImports
    {
      public IOImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        Directory = new DirectoryImports(findType, moduleDefinition);
        FileSystemInfo = new FileSystemInfoImports(findType, moduleDefinition);
      }

      public DirectoryImports Directory { get; }
      public FileSystemInfoImports FileSystemInfo { get; }

      internal class DirectoryImports
      {
        public DirectoryImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          var directoryType = findType("System.IO.Directory");
          moduleDefinition.ImportReference(directoryType);
          GetParent = moduleDefinition.ImportReference(directoryType.FindMethod("GetParent", "String"));
        }

        public MethodReference GetParent { get; }
      }

      internal class FileSystemInfoImports
      {
        public FileSystemInfoImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          var infoType = findType("System.IO.FileSystemInfo");
          moduleDefinition.ImportReference(infoType);
          GetFullName = moduleDefinition.ImportReference(infoType.FindMethod("get_FullName"));
        }

        public MethodReference GetFullName { get; }
      }
    }

    internal class IDisposableImports
    {
      public IDisposableImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        var disposableType = findType("System.IDisposable");
        moduleDefinition.ImportReference(disposableType);
        Dispose = moduleDefinition.ImportReference(disposableType.FindMethod("Dispose"));
      }

      public MethodReference Dispose { get; }
    }

    internal class DiagnosticsImports
    {
      public DiagnosticsImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        DebuggerNonUserCodeAttribute = new DebuggerNonUserCodeAttributeImports(findType, moduleDefinition);
      }

      public DebuggerNonUserCodeAttributeImports DebuggerNonUserCodeAttribute { get; }

      internal class DebuggerNonUserCodeAttributeImports
      {
        public DebuggerNonUserCodeAttributeImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          var attributeType = findType("System.Diagnostics.DebuggerNonUserCodeAttribute");
          moduleDefinition.ImportReference(attributeType);
          Constructor = moduleDefinition.ImportReference(attributeType.FindMethod(".ctor"));
        }

        public MethodReference Constructor { get; }
      }
    }

    internal class CodeDomImports
    {
      public CodeDomImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        Compiler = new CompilerImports(findType, moduleDefinition);
      }

      public CompilerImports Compiler { get; }

      internal class CompilerImports
      {
        public CompilerImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
          GeneratedCodeAttribute = new GeneratedCodeAttributeImports(findType, moduleDefinition);
        }

        public GeneratedCodeAttributeImports GeneratedCodeAttribute { get; }

        internal class GeneratedCodeAttributeImports
        {
          public GeneratedCodeAttributeImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
          {
            var attributeType = findType("System.CodeDom.Compiler.GeneratedCodeAttribute");
            moduleDefinition.ImportReference(attributeType);
            Constructor = moduleDefinition.ImportReference(attributeType.FindMethod(".ctor", "String", "String"));
          }

          public MethodReference Constructor { get; }
        }
      }
    }

    internal class ObjectImports
    {
      public ObjectImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        var objectType = findType("System.Object");
        moduleDefinition.ImportReference(objectType);
        GetType = moduleDefinition.ImportReference(objectType.FindMethod("GetType"));

        ArrayType = moduleDefinition.ImportReference(objectType.MakeArrayType());
      }

      public new MethodReference GetType { get; }
      public TypeReference ArrayType { get; }
    }

    internal class FuncImports
    {
      public FuncImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        var funcType = findType("System.Func`1");
        moduleDefinition.ImportReference(funcType);
        Constructor = funcType.FindMethod(".ctor", "Object", "IntPtr");
      }

      public MethodReference Constructor { get; }
    }

    internal class TypeImports
    {
      public TypeImports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
      {
        var type = findType("System.Type");
        Type = moduleDefinition.ImportReference(type);
      }

      public TypeReference Type { get; }
    }
  }
}

partial class ModuleWeaver
{
  Import? _import;

  public override IEnumerable<string> GetAssembliesForScanning()
  {
    yield return "mscorlib";
    yield return "System";
    yield return "netstandard";
    yield return "System.Core";
    yield return "System.Diagnostics.Tools";
    yield return "System.Diagnostics.Debug";
    yield return "System.Runtime";
    yield return "System.ComponentModel.Composition";
  }

  void FindReferences()
  {
    _import ??= new Import(FindTypeDefinition, ModuleDefinition);
  }
}
