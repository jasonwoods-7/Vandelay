using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Fody;
using TestCore;
using Vandelay.Fody;
using Xunit;

#pragma warning disable 618

namespace MultipleExportsTests
{
  public class MultipleExportsSetup
  {
    public TestResult MultipleWeaver { get; }

    public Type FooExporterType { get; }

    public Type BarExporterType { get; }

    public MultipleExportsSetup()
    {
      var weaver = new ModuleWeaver();
      MultipleWeaver = weaver.ExecuteVandelayTestRun(Path.Combine(
          Path.GetDirectoryName(typeof(MultipleExportsSetup).Assembly.GetAssemblyLocation()),
          "AssemblyToProcess.MultipleExports.dll"),
        assemblyName: "AssemblyToProcess.MultipleExports2"
#if NETCOREAPP
        , runPeVerify: false
#endif
      );

      MultipleWeaver.Errors.Should().BeEmpty();

      FooExporterType = MultipleWeaver.Assembly.GetType(
        "AssemblyToProcess.MultipleExports.IFooExporter", true);
      BarExporterType = MultipleWeaver.Assembly.GetType(
        "AssemblyToProcess.MultipleExports.IBarExporter", true);
    }
  }

  public class MultipleExportsTests : IClassFixture<MultipleExportsSetup>
  {
    readonly MultipleExportsSetup _setup;

    public MultipleExportsTests(MultipleExportsSetup setup) => _setup = setup;

    [Theory]
    [InlineData("AssemblyToProcess.MultipleExports.FooExporterA")]
    [InlineData("AssemblyToProcess.MultipleExports.FooExporterB")]
    public void InstanceTest_Foo(string className)
    {
      // Arrange
      var type = _setup.MultipleWeaver.Assembly.GetType(className, true);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      exports.Should().HaveCount(1);

      var attribute = exports[0];
      attribute.ContractType.Should().NotBeNull()
        .And.Be(_setup.FooExporterType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.MultipleExports.BarExporterA")]
    [InlineData("AssemblyToProcess.MultipleExports.BarExporterB")]
    public void InstanceTest_Bar(string className)
    {
      // Arrange
      var type = _setup.MultipleWeaver.Assembly.GetType(className, true);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      exports.Should().HaveCount(1);

      var attribute = exports[0];
      attribute.ContractType.Should().NotBeNull()
        .And.Be(_setup.BarExporterType);
    }

    [Fact]
    public void InstanceTest_FooBar()
    {
      // Arrange
      var type = _setup.MultipleWeaver.Assembly.GetType("AssemblyToProcess.MultipleExports.FooBar", true);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      exports.Should().HaveCount(2);

      exports.Should().Contain(a => a.ContractType == _setup.BarExporterType)
        .And.Contain(a => a.ContractType == _setup.FooExporterType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.MultipleExports.BarImporter")]
    [InlineData("AssemblyToProcess.MultipleExports.FooImporter")]
    public void Importer(string className)
    {
      // Arrange
      var instance = _setup.MultipleWeaver.GetInstance(className);

      // Act
      var imports = (IEnumerable)instance.Imports;

      // Assert
      imports.Should().HaveCount(3);
    }

    [Fact]
    public void IterateFooBars()
    {
      // Arrange
      var instance = _setup.MultipleWeaver.GetInstance(
        "AssemblyToProcess.MultipleExports.FooBarImporter");

      // Act
      var result = (int)instance.IterateFooBars();

      // Assert
      result.Should().Be(6);
    }
  }
}
