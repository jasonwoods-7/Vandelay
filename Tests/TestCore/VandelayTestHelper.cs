using System.Diagnostics;
using System.Reflection;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TypeSystem = Fody.TypeSystem;

#pragma warning disable 618

namespace TestCore;

/// <summary>
/// Uses <see cref="Assembly.CodeBase"/> to derive the current directory.
/// </summary>
public static class VandelayTestHelper
{
  public static TestResult ExecuteVandelayTestRun(
    this BaseModuleWeaver weaver,
    string assemblyPath,
    bool runPeVerify = true,
    Action<ModuleDefinition>? afterExecuteCallback = null,
    Action<ModuleDefinition>? beforeExecuteCallback = null,
    string? assemblyName = null,
    IEnumerable<string>? ignoreCodes = null,
    bool purgeTempDir = true,
    byte[]? strongNameKeyBlob = null)
  {
    assemblyPath = Path.Combine(CodeBaseLocation.CurrentDirectory, assemblyPath);
    var fodyTempDir = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "fodytemp");
    Directory.CreateDirectory(fodyTempDir);

    if (purgeTempDir)
    {
      var info = new DirectoryInfo(fodyTempDir);

      foreach (var file in info.GetFiles())
      {
        file.Delete();
      }

      foreach (var directory in info.GetDirectories())
      {
        directory.Delete(true);
      }
    }

    string targetFileName;
    if (assemblyName == null)
    {
      assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
      targetFileName = Path.GetFileName(assemblyPath);
    }
    else
    {
      targetFileName = assemblyName + ".dll";
    }

    var targetAssemblyPath = Path.Combine(fodyTempDir, targetFileName);

    using var assemblyResolver = new TestAssemblyResolver();
    var typeCache = CacheTypes(weaver, assemblyResolver.Resolve);

    var testStatus = new TestResult();

    var addMessageInfo = typeof(TestResult).GetMethod("AddMessage",
      BindingFlags.NonPublic | BindingFlags.Instance);
    Debug.Assert(addMessageInfo != null, $"{nameof(addMessageInfo)} != null");
    var addMessage = addMessageInfo!
      .CreateDelegate<Action<string, MessageImportance>>(testStatus);

    var addWarningInfo = typeof(TestResult).GetMethod("AddWarning",
      BindingFlags.NonPublic | BindingFlags.Instance);
    Debug.Assert(addWarningInfo != null, $"{nameof(addWarningInfo)} != null");
    var addWarning = addWarningInfo!
      .CreateDelegate<Action<string, SequencePoint?>>(testStatus);

    var addErrorInfo = typeof(TestResult).GetMethod("AddError",
      BindingFlags.NonPublic | BindingFlags.Instance);
    Debug.Assert(addErrorInfo != null, $"{nameof(addErrorInfo)} != null");
    var addError = addErrorInfo!
      .CreateDelegate<Action<string, SequencePoint?>>(testStatus);

    weaver.LogDebug = text => addMessage(text, MessageImportanceDefaults.Debug);
    weaver.LogInfo = text => addMessage(text, MessageImportanceDefaults.Info);
    weaver.LogMessage = (text, messageImportance) => addMessage(text, messageImportance);
    weaver.LogWarning = text => addWarning(text, null);
    weaver.LogWarningPoint = (text, sequencePoint) => addWarning(text, sequencePoint);
    weaver.LogError = text => addError(text, null);
    weaver.LogErrorPoint = (text, sequencePoint) => addError(text, sequencePoint);
    weaver.AssemblyFilePath = assemblyPath;
    weaver.FindType = typeCache.FindType;
    weaver.TryFindType = typeCache.TryFindType;
    weaver.ResolveAssembly = assemblyResolver.Resolve;

    var readerProviderType = typeof(BaseModuleWeaver).Assembly
      .GetType("SymbolReaderProvider", true);
    var readerProvider = (ISymbolReaderProvider)Activator.CreateInstance(readerProviderType);

    var readerParameters = new ReaderParameters
    {
      AssemblyResolver = assemblyResolver,
      SymbolReaderProvider = readerProvider,
      ReadWrite = false,
      ReadSymbols = true,
    };

    using (var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters))
    {
      module.Assembly.Name.Name = assemblyName;
      weaver.ModuleDefinition = module;
      weaver.TypeSystem = new TypeSystem(typeCache.FindType, module);
      beforeExecuteCallback?.Invoke(module);

      weaver.Execute();

      var referenceCleanerType = typeof(BaseModuleWeaver).Assembly
        .GetType("ReferenceCleaner", true);
      var cleanReferencesInfo = referenceCleanerType.GetMethod("CleanReferences");
      Debug.Assert(cleanReferencesInfo != null, nameof(cleanReferencesInfo) + " != null");
      var cleaner = cleanReferencesInfo!
        .CreateDelegate<Action<ModuleDefinition, BaseModuleWeaver, List<string>, List<string>, Action<string>>>();
      cleaner(module, weaver, weaver.ReferenceCopyLocalPaths, weaver.ReferenceCopyLocalPaths, weaver.LogDebug);

      afterExecuteCallback?.Invoke(module);

      var writerParameters = new WriterParameters
      {
        StrongNameKeyBlob = IsPrivateKeyFile(strongNameKeyBlob) ? strongNameKeyBlob : null,
        WriteSymbols = true
      };

      module.Write(targetAssemblyPath, writerParameters);
    }

    if (runPeVerify && IsWindows())
    {
      var ignoreList = ignoreCodes?.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();

      PeVerifier.ThrowIfDifferent(assemblyPath, targetAssemblyPath, ignoreList,
        Path.GetDirectoryName(assemblyPath));
    }

#if NETSTANDARD
    AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
    {
      var tempAssemblyPath = Path.Combine(fodyTempDir, new AssemblyName(args.Name).Name + ".dll");
      return !File.Exists(tempAssemblyPath) ? null : Assembly.LoadFrom(tempAssemblyPath);
    };
#endif

    typeof(TestResult).GetMethod("set_Assembly",
        BindingFlags.NonPublic | BindingFlags.Instance)!
      .Invoke(testStatus, new object[]
      {
        Assembly.Load(AssemblyName.GetAssemblyName(targetAssemblyPath))
      });
    typeof(TestResult).GetMethod("set_AssemblyPath",
        BindingFlags.NonPublic | BindingFlags.Instance)!
      .Invoke(testStatus, new object[]
      {
        targetAssemblyPath
      });
    return testStatus;
  }

  static bool IsWindows()
  {
    var platform = Environment.OSVersion.Platform.ToString();
    return platform.StartsWith("win", StringComparison.OrdinalIgnoreCase);
  }

  static TypeCache CacheTypes(BaseModuleWeaver weaver,
    Func<string, AssemblyDefinition?> resolver)
  {
    var typeCache = new TypeCache(resolver);
    typeCache.BuildAssembliesToScan(weaver);
    return typeCache;
  }

  static bool IsPrivateKeyFile(byte[]? blob) =>
    blob is not null &&
    blob.Length >= 12 &&
    blob[0] == 0x07 && // PRIVATEKEYBLOB (0x07)
    blob[1] == 0x02 && // Version (0x02)
    blob[2] == 0x00 && // Reserved (word)
    blob[3] == 0x00 &&
    BitConverter.ToUInt32(blob, 8) == 0x32415352; // DWORD magic = RSA2
}
