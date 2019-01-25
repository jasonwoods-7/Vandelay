using System;
using System.Collections;
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
  public class SimpleCaseSetup
  {
    [NotNull]
    public ModuleWeaverTestHelper SimpleCaseWeaver { get; }

    [NotNull]
    public Type SimpleCaseExportableType { get; }

    public SimpleCaseSetup()
    {
      SimpleCaseWeaver = new ModuleWeaverTestHelper(
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
          @"\AssemblyToProcess.SimpleCase.dll"));

      Assert.NotNull(SimpleCaseWeaver.Errors);
      Assert.Empty(SimpleCaseWeaver.Errors);

      SimpleCaseExportableType = SimpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.IExportable");
    }
  }

  public class SimpleCaseTests : IClassFixture<SimpleCaseSetup>
  {
    readonly SimpleCaseSetup _setup;

    public SimpleCaseTests(SimpleCaseSetup setup) => _setup = setup;

    [Theory]
    [InlineData("AssemblyToProcess.SimpleCase.AbstractExportable")]
    public void AbstractTest([NotNull] string className)
    {
      // Arrange
      var type = _setup.SimpleCaseWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      Assert.NotNull(exports);
      Assert.Empty(exports);
    }

    [Theory]
    [InlineData("AssemblyToProcess.SimpleCase.ExportableInstance")]
    [InlineData("AssemblyToProcess.SimpleCase.AlreadyExportedInstance")]
    [InlineData("AssemblyToProcess.SimpleCase.NonPublicExported")]
    [InlineData("AssemblyToProcess.SimpleCase.ImplementsExtended")]
    public void InstanceTest([NotNull] string className)
    {
      // Arrange
      var type = _setup.SimpleCaseWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      Assert.NotNull(exports);
      Assert.Single(exports);

      var attribute = exports[0];
      Assert.NotNull(attribute);
      Assert.Equal(_setup.SimpleCaseExportableType, attribute.ContractType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.SimpleCase.ImporterSingleSearchPattern")]
    [InlineData("AssemblyToProcess.SimpleCase.ImporterMultipleSearchPatterns")]
    public void ImportMany([NotNull] string searchPattern)
    {
      // Arrange
      var importsType = _setup.SimpleCaseWeaver.GetType(searchPattern);
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = importsInstance.Imports;

      // Assert
      Assert.NotNull(imports);
      Assert.Equal(4, imports.Length);
    }

    [Fact]
    public void ImportManyWithImport()
    {
      // Arrange
      var importsType = _setup.SimpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.ImporterSingleSearchPatternWithImport");
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      Assert.NotNull(imports);
      Assert.Equal(5, imports.Count);

      var greeting = (dynamic)imports.Cast<object>().First(i => i.GetType().Name == "ExportableWithImport");
      Assert.NotNull(greeting.Greeting);
      Assert.NotEmpty(greeting.Greeting);
    }

    [Fact]
    public void ImportInheritsBase()
    {
      // Arrange
      var importsType = _setup.SimpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.ImporterInheritsBase");
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      Assert.NotNull(imports);
      Assert.Single(imports);
    }

    [Fact]
    public void ImportInheritedExport()
    {
      // Arrange
      var importsType = _setup.SimpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.ImporterInheritedExport");
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      Assert.NotNull(imports);
      Assert.Single(imports);
    }

#pragma warning disable 618
    [Fact]
    public void PeVerify()
    {
      // Arrange

      // Act
      PeVerifier.ThrowIfDifferent(_setup.SimpleCaseWeaver.BeforeAssemblyPath,
        _setup.SimpleCaseWeaver.AfterAssemblyPath);

      // Assert
    }
#pragma warning restore 618
  }
}
