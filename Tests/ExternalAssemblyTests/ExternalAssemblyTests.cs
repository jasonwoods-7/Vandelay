using System.Collections;
using System.ComponentModel.Composition;
using System.Reflection;
using FluentAssertions;
using Fody;
using TestCore;
using Vandelay.Fody;
using Xunit;

#pragma warning disable 618

namespace ExternalAssemblyTests;

public class ExternalAssemblySetup
{
  public TestResult UnsignedWeaver { get; }

  public TestResult SignedWeaver { get; }

  public Type CoreExportableType { get; }

  public ExternalAssemblySetup()
  {
    var currentDirectory = Path.GetDirectoryName(typeof(ExternalAssemblySetup)
      .Assembly.GetAssemblyLocation())!;

    {
      var unsignedWeaver = new ModuleWeaver();
      UnsignedWeaver = unsignedWeaver.ExecuteVandelayTestRun(Path.Combine(
          currentDirectory, "AssemblyToProcess.Unsigned.dll"),
        assemblyName: "AssemblyToProcess.Unsigned2"
#if NETCOREAPP
        , runPeVerify: false
#endif
      );
      UnsignedWeaver.Errors.Should().BeEmpty();
    }

    {
      var signedWeaver = new ModuleWeaver();
      SignedWeaver = signedWeaver.ExecuteVandelayTestRun(Path.Combine(
          currentDirectory, "AssemblyToProcess.Signed.dll"),
        assemblyName: "AssemblyToProcess.Signed2"
#if NETCOREAPP
        , runPeVerify: false
#endif
        , purgeTempDir: false
        , strongNameKeyPair: new Mono.Cecil.StrongNameKeyPair(File.ReadAllBytes(
          @"..\..\..\..\..\AssemblyToProcess\Signed\key.snk"))
      );
      SignedWeaver.Errors.Should().BeEmpty();
    }

    CoreExportableType = Assembly.Load(AssemblyName.GetAssemblyName(
        Path.Combine(currentDirectory, "AssemblyToProcess.Core.dll")))
      .GetType("AssemblyToProcess.Core.IExportable", true)!;

    AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
    {
      var assemblyName = new AssemblyName(e.Name!).Name!;

      var assemblyPath = Path.Combine(GetAssemblyDirectory(), $"{assemblyName}.dll");

      return !File.Exists(assemblyPath) ? null : Assembly.LoadFile(assemblyPath);

      string GetAssemblyDirectory() =>
        assemblyName.Contains("Unsigned")
          ? UnsignedWeaver.AssemblyPath
          : assemblyName.Contains("Signed")
            ? SignedWeaver.AssemblyPath
            : currentDirectory;
    };
  }
}

public class ExternalAssemblyTests : IClassFixture<ExternalAssemblySetup>
{
  readonly ExternalAssemblySetup _setup;

  public ExternalAssemblyTests(ExternalAssemblySetup setup) => _setup = setup;

  [Theory]
  [InlineData("AssemblyToProcess.Unsigned.AbstractExportable")]
  [InlineData("AssemblyToProcess.Signed.AbstractExportable")]
  public void AbstractTest(string className)
  {
    // Arrange
    var type = GetTestHelper(className).Assembly.GetType(className, true)!;

    // Act
    var exports = type.GetCustomAttributes<ExportAttribute>(false);

    // Assert
    exports.Should().BeEmpty();
  }

  [Theory]
  [InlineData("AssemblyToProcess.Unsigned.ExportableInstance")]
  [InlineData("AssemblyToProcess.Unsigned.AlreadyExportedInstance")]
  [InlineData("AssemblyToProcess.Signed.ExportableInstance")]
  [InlineData("AssemblyToProcess.Signed.AlreadyExportedInstance")]
  public void InstanceTest(string className)
  {
    // Arrange
    var type = GetTestHelper(className).Assembly.GetType(className, true)!;

    // Act
    var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

    // Assert
    exports.Should().HaveCount(1);

    var attribute = exports[0];
    attribute.ContractType.Should().Be(_setup.CoreExportableType);
  }

  [Theory]
  [InlineData("AssemblyToProcess.Unsigned.Importer")]
  [InlineData("AssemblyToProcess.Signed.Importer")]
  public void ImportMany(string className)
  {
    // Arrange
    var importsInstance = GetTestHelper(className).GetInstance(className);

    // Act
    var imports = (ICollection)importsInstance.Imports;

    // Assert
    imports.Cast<object>().Should().HaveCount(4);
  }

  TestResult GetTestHelper(string className) =>
    className.Contains("Unsigned") ? _setup.UnsignedWeaver : _setup.SignedWeaver;
}
