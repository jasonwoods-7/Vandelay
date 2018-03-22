using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Fody;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Vandelay.Fody
{
  [TestFixture]
  public class SimpleCaseTests
  {
    // ReSharper disable NotNullMemberIsNotInitialized
    [NotNull]
    ModuleWeaverTestHelper _simpleCaseWeaver;

    [NotNull]
    Type _simpleCaseExportableType;
    // ReSharper restore NotNullMemberIsNotInitialized

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      _simpleCaseWeaver = new ModuleWeaverTestHelper(
        Path.Combine(TestContext.CurrentContext.TestDirectory,
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.SimpleCase.dll"));
      Assert.That(_simpleCaseWeaver.Errors, Is.Null.Or.Empty);

      _simpleCaseExportableType = _simpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.IExportable");
    }

    [TestCase("AssemblyToProcess.SimpleCase.AbstractExportable")]
    public void AbstractTest([NotNull] string className)
    {
      // Arrange
      var type = _simpleCaseWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Is.Empty);
    }

    [TestCase("AssemblyToProcess.SimpleCase.ExportableInstance")]
    [TestCase("AssemblyToProcess.SimpleCase.AlreadyExportedInstance")]
    [TestCase("AssemblyToProcess.SimpleCase.NonPublicExported")]
    [TestCase("AssemblyToProcess.SimpleCase.ImplementsExtended")]
    public void InstanceTest([NotNull] string className)
    {
      // Arrange
      var type = _simpleCaseWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Has.Length.EqualTo(1));

      var attribute = exports[0] as ExportAttribute;
      Assert.That(attribute, Is.Not.Null);
      Assert.That(attribute, Has.Property("ContractType").EqualTo(_simpleCaseExportableType));
    }

    [TestCase("AssemblyToProcess.SimpleCase.ImporterSingleSearchPattern")]
    [TestCase("AssemblyToProcess.SimpleCase.ImporterMultipleSearchPatterns")]
    public void ImportMany([NotNull] string searchPattern)
    {
      // Arrange
      var importsType = _simpleCaseWeaver.GetType(searchPattern);
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = importsInstance.Imports;

      // Assert
      Assert.That(imports, Is.Not.Null.Or.Empty);
      Assert.That(imports, Has.Length.EqualTo(4));
    }

    [Test]
    public void ImportManyWithImport()
    {
      // Arrange
      var importsType = _simpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.ImporterSingleSearchPatternWithImport");
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      Assert.That(imports, Is.Not.Null.Or.Empty);
      Assert.That(imports, Has.Length.EqualTo(5));

      var greeting = (dynamic)imports.Cast<object>().First(i => i.GetType().Name == "ExportableWithImport");
      Assert.That(greeting.Greeting, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void ImportInheritsBase()
    {
      // Arrange
      var importsType = _simpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.ImporterInheritsBase");
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      Assert.That(imports, Is.Not.Null.Or.Empty);
      Assert.That(imports, Has.Length.EqualTo(1));
    }

    [Test]
    public void ImportInheritedExport()
    {
      // Arrange
      var importsType = _simpleCaseWeaver.GetType(
        "AssemblyToProcess.SimpleCase.ImporterInheritedExport");
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      Assert.That(imports, Is.Not.Null.Or.Empty);
      Assert.That(imports, Has.Length.EqualTo(1));
    }

    [Test]
    public void PeVerify()
    {
      // Arrange

      // Act
      PeVerifier.ThrowIfDifferent(_simpleCaseWeaver.BeforeAssemblyPath,
        _simpleCaseWeaver.AfterAssemblyPath);

      // Assert
    }
  }
}
