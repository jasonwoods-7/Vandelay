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
  public class MultipleExportsSetup
  {
    [NotNull]
    public ModuleWeaverTestHelper MultipleWeaver { get; }

    [NotNull]
    public Type FooExporterType { get; }

    [NotNull]
    public Type BarExporterType { get; }

    public MultipleExportsSetup()
    {
      MultipleWeaver = new ModuleWeaverTestHelper(
        Path.Combine(Environment.CurrentDirectory,
        @"..\..\..\..\AssemblyToProcess\bin" +
#if DEBUG
          @"\Debug" +
#else
          @"\Release" +
#endif
#if NET46
          @"\net46" +
#else
          @"\netstandard2.0" +
#endif
          @"\AssemblyToProcess.MultipleExports.dll"));

      Assert.NotNull(MultipleWeaver.Errors);
      Assert.Empty(MultipleWeaver.Errors);

      FooExporterType = MultipleWeaver.GetType(
        "AssemblyToProcess.MultipleExports.IFooExporter");
      BarExporterType = MultipleWeaver.GetType(
        "AssemblyToProcess.MultipleExports.IBarExporter");
    }

  }

  public class MultipleExportsTests : IClassFixture<MultipleExportsSetup>
  {
    readonly MultipleExportsSetup _setup;

    public MultipleExportsTests(MultipleExportsSetup setup) => _setup = setup;

    [Theory]
    [InlineData("AssemblyToProcess.MultipleExports.FooExporterA")]
    [InlineData("AssemblyToProcess.MultipleExports.FooExporterB")]
    public void InstanceTest_Foo([NotNull] string className)
    {
      // Arrange
      var type = _setup.MultipleWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      Assert.NotNull(exports);
      Assert.Single(exports);

      var attribute = exports[0];
      Assert.NotNull(attribute);
      Assert.Equal(_setup.FooExporterType, attribute.ContractType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.MultipleExports.BarExporterA")]
    [InlineData("AssemblyToProcess.MultipleExports.BarExporterB")]
    public void InstanceTest_Bar([NotNull] string className)
    {
      // Arrange
      var type = _setup.MultipleWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      Assert.NotNull(exports);
      Assert.Single(exports);

      var attribute = exports[0];
      Assert.NotNull(attribute);
      Assert.Equal(_setup.BarExporterType, attribute.ContractType);
    }

    [Fact]
    public void InstanceTest_FooBar()
    {
      // Arrange
      var type = _setup.MultipleWeaver.GetType("AssemblyToProcess.MultipleExports.FooBar");

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      Assert.NotNull(exports);
      Assert.Equal(2, exports.Length);

      Assert.Contains(exports, a => a.ContractType == _setup.BarExporterType);
      Assert.Contains(exports, a => a.ContractType == _setup.FooExporterType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.MultipleExports.BarImporter")]
    [InlineData("AssemblyToProcess.MultipleExports.FooImporter")]
    public void Importer([NotNull] string className)
    {
      // Arrange
      var type = _setup.MultipleWeaver.GetType(className);
      var instance = (dynamic)Activator.CreateInstance(type);

      // Act
      var imports = instance.Imports;

      // Assert
      Assert.NotNull(imports);
      Assert.NotEmpty(imports);
      Assert.Equal(3, imports.Length);
    }

    [Fact]
    public void IterateFooBars()
    {
      // Arrange
      var type = _setup.MultipleWeaver.GetType(
        "AssemblyToProcess.MultipleExports.FooBarImporter");
      var instance = (dynamic)Activator.CreateInstance(type);

      // Act
      instance.IterateFooBars();

      // Assert
    }

#pragma warning disable 618
    [Fact]
    public void PeVerify()
    {
      // Arrange

      // Act
      PeVerifier.ThrowIfDifferent(_setup.MultipleWeaver.BeforeAssemblyPath,
        _setup.MultipleWeaver.AfterAssemblyPath);

      // Assert
    }
#pragma warning restore 618
  }
}
