using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Fody;
using JetBrains.Annotations;
using Xunit;

namespace Tests
{
  [UsedImplicitly]
  public class ExternalAssemblySetup
  {
    [NotNull]
    public ModuleWeaverTestHelper UnsignedWeaver { get; }

    [NotNull]
    public ModuleWeaverTestHelper SignedWeaver { get; }

    [NotNull]
    public Type CoreExportableType { get; }

    public ExternalAssemblySetup()
    {
      var directoryName = Path.GetFullPath(Path.Combine(
        Environment.CurrentDirectory,
        @"..\..\..\..\AssemblyToProcess\bin",
#if DEBUG
        "Debug",
#else
        "Release",
#endif
#if NETCOREAPP
        "netstandard2.0"
#else
        "net46"
#endif
      ));

      UnsignedWeaver = new ModuleWeaverTestHelper(
        Path.Combine(directoryName, "AssemblyToProcess.Unsigned.dll"));

      Assert.NotNull(UnsignedWeaver.Errors);
      Assert.Empty(UnsignedWeaver.Errors);

      SignedWeaver = new ModuleWeaverTestHelper(
        Path.Combine(directoryName, "AssemblyToProcess.Signed.dll"));

      Assert.NotNull(SignedWeaver.Errors);
      Assert.Empty(SignedWeaver.Errors);

      CoreExportableType = Assembly.LoadFile(Path.GetFullPath(
        Path.Combine(directoryName, "AssemblyToProcess.Core.dll")))
        .GetType("AssemblyToProcess.Core.IExportable", true);

      AppDomain.CurrentDomain.AssemblyResolve += (_, e) => Assembly.LoadFile(
        Path.Combine(directoryName, $"{e.Name.Split(',')[0]}.dll"));
    }
  }

  public class ExternalAssemblyTests : IClassFixture<ExternalAssemblySetup>
  {
    readonly ExternalAssemblySetup _setup;

    public ExternalAssemblyTests(ExternalAssemblySetup setup) => _setup = setup;

    [Theory]
    [InlineData("AssemblyToProcess.Unsigned.AbstractExportable")]
    [InlineData("AssemblyToProcess.Signed.AbstractExportable")]
    public void AbstractTest([NotNull] string className)
    {
      // Arrange
      var type = GetTestHelper(className).GetType(className);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false);

      // Assert
      Assert.NotNull(exports);
      Assert.Empty(exports);
    }

    [Theory]
    [InlineData("AssemblyToProcess.Unsigned.ExportableInstance")]
    [InlineData("AssemblyToProcess.Unsigned.AlreadyExportedInstance")]
    [InlineData("AssemblyToProcess.Signed.ExportableInstance")]
    [InlineData("AssemblyToProcess.Signed.AlreadyExportedInstance")]
    public void InstanceTest([NotNull] string className)
    {
      // Arrange
      var type = GetTestHelper(className).GetType(className);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      Assert.NotNull(exports);
      Assert.Single(exports);

      var attribute = exports[0];
      Assert.NotNull(attribute);
      Assert.Equal(_setup.CoreExportableType, attribute.ContractType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.Unsigned.Importer")]
    [InlineData("AssemblyToProcess.Signed.Importer")]
    public void ImportMany([NotNull] string className)
    {
      // Arrange
      var importsType = GetTestHelper(className).GetType(className);
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = importsInstance.Imports;

      // Assert
      Assert.NotNull(imports);
      Assert.NotEmpty(imports);
      Assert.Equal(4, imports.Length);
    }

#pragma warning disable 618
    [Fact]
    public void PeVerify()
    {
      // Arrange

      // Act
      PeVerifier.ThrowIfDifferent(_setup.UnsignedWeaver.BeforeAssemblyPath,
        _setup.UnsignedWeaver.AfterAssemblyPath);

      PeVerifier.ThrowIfDifferent(_setup.SignedWeaver.BeforeAssemblyPath,
        _setup.SignedWeaver.AfterAssemblyPath);

      // Assert
    }
#pragma warning restore 618

    [NotNull]
    ModuleWeaverTestHelper GetTestHelper([NotNull] string className) =>
      className.Contains("Unsigned") ? _setup.UnsignedWeaver : _setup.SignedWeaver;
  }
}
