﻿using System.Collections;
using System.ComponentModel.Composition;
using System.Reflection;
using FluentAssertions;
using Fody;
using TestCore;
using Vandelay.Fody;
using Xunit;

#pragma warning disable 618

namespace SimpleCaseTests;

public class SimpleCaseSetup
{
  public TestResult SimpleCaseWeaver { get; }

  public Type SimpleCaseExportableType { get; }

  public SimpleCaseSetup()
  {
    var weaver = new ModuleWeaver();
    SimpleCaseWeaver = weaver.ExecuteVandelayTestRun(Path.Combine(
        Path.GetDirectoryName(typeof(SimpleCaseSetup).Assembly.GetAssemblyLocation())!,
        "AssemblyToProcess.SimpleCase.dll"),
      assemblyName: "AssemblyToProcess.SimpleCase2"
#if NETCOREAPP
      , runPeVerify: false
#endif
    );

    SimpleCaseWeaver.Errors.Should().BeEmpty();

    SimpleCaseExportableType = SimpleCaseWeaver.Assembly.GetType(
      "AssemblyToProcess.SimpleCase.IExportable", true)!;
  }
}

public class SimpleCaseTests : IClassFixture<SimpleCaseSetup>
{
  readonly SimpleCaseSetup _setup;

  public SimpleCaseTests(SimpleCaseSetup setup) => _setup = setup;

  [Theory]
  [InlineData("AssemblyToProcess.SimpleCase.AbstractExportable")]
  public void AbstractTest(string className)
  {
    // Arrange
    var type = _setup.SimpleCaseWeaver.Assembly.GetType(className, true)!;

    // Act
    var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

    // Assert
    exports.Should().BeEmpty();
  }

  [Theory]
  [InlineData("AssemblyToProcess.SimpleCase.ExportableInstance")]
  [InlineData("AssemblyToProcess.SimpleCase.AlreadyExportedInstance")]
  [InlineData("AssemblyToProcess.SimpleCase.NonPublicExported")]
  [InlineData("AssemblyToProcess.SimpleCase.ImplementsExtended")]
  public void InstanceTest(string className)
  {
    // Arrange
    var type = _setup.SimpleCaseWeaver.Assembly.GetType(className, true)!;

    // Act
    var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

    // Assert
    exports.Should().HaveCount(1);

    var attribute = exports[0];
    attribute.ContractType.Should().Be(_setup.SimpleCaseExportableType);
  }

  [Theory]
  [InlineData("AssemblyToProcess.SimpleCase.ImporterSingleSearchPattern")]
  [InlineData("AssemblyToProcess.SimpleCase.ImporterMultipleSearchPatterns")]
  public void ImportMany(string searchPattern)
  {
    // Arrange
    var importsInstance = _setup.SimpleCaseWeaver.GetInstance(searchPattern);

    // Act
    var imports = (ICollection)importsInstance.Imports;

    // Assert
    imports.Cast<object>().Should().HaveCount(4);
  }

  [Fact]
  public void ImportManyWithImport()
  {
    // Arrange
    var importsInstance = _setup.SimpleCaseWeaver.GetInstance(
      "AssemblyToProcess.SimpleCase.ImporterSingleSearchPatternWithImport");

    // Act
    var imports = (ICollection)importsInstance.Imports;

    // Assert
    imports.Cast<object>().Should().HaveCount(5);

    var greeting = (string)((dynamic)imports.Cast<object>().First(i =>
      i.GetType().Name == "ExportableWithImport")).Greeting;
    greeting.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void ImportInheritsBase()
  {
    // Arrange
    var importsInstance = _setup.SimpleCaseWeaver.GetInstance(
      "AssemblyToProcess.SimpleCase.ImporterInheritsBase");

    // Act
    var imports = (ICollection)importsInstance.Imports;

    // Assert
    imports.Cast<object>().Should().HaveCount(1);
  }

  [Fact]
  public void ImportInheritedExport()
  {
    // Arrange
    var importsInstance = _setup.SimpleCaseWeaver.GetInstance(
      "AssemblyToProcess.SimpleCase.ImporterInheritedExport");

    // Act
    var imports = (ICollection)importsInstance.Imports;

    // Assert
    imports.Cast<object>().Should().HaveCount(1);
  }
}
