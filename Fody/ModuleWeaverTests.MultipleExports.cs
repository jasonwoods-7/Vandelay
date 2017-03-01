using System;
using System.ComponentModel.Composition;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Vandelay.Fody
{
  [TestFixture]
  public class MultipleExportsTests
  {
    // ReSharper disable NotNullMemberIsNotInitialized
    [NotNull]
    ModuleWeaverTestHelper _multipleWeaver;

    [NotNull]
    Type _fooExporterType;

    [NotNull]
    Type _barExporterType;
    // ReSharper restore NotNullMemberIsNotInitialized

    [TestFixtureSetUp]
    public void TestFixtureSetUp()
    {
      _multipleWeaver = new ModuleWeaverTestHelper(
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.MultipleExports.dll");
      Assert.That(_multipleWeaver.Errors, Is.Null.Or.Empty);

      _fooExporterType = _multipleWeaver.GetType(
        "AssemblyToProcess.MultipleExports.IFooExporter");
      _barExporterType = _multipleWeaver.GetType(
        "AssemblyToProcess.MultipleExports.IBarExporter");
    }

    [TestCase("AssemblyToProcess.MultipleExports.FooExporterA")]
    [TestCase("AssemblyToProcess.MultipleExports.FooExporterB")]
    public void InstanceTest_Foo([NotNull] string className)
    {
      // Arrange
      var type = _multipleWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Has.Length.EqualTo(1));

      var attribute = exports[0] as ExportAttribute;
      Assert.That(attribute, Is.Not.Null);
      Assert.That(attribute, Has.Property("ContractType").EqualTo(_fooExporterType));
    }

    [TestCase("AssemblyToProcess.MultipleExports.BarExporterA")]
    [TestCase("AssemblyToProcess.MultipleExports.BarExporterB")]
    public void InstanceTest_Bar([NotNull] string className)
    {
      // Arrange
      var type = _multipleWeaver.GetType(className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Has.Length.EqualTo(1));

      var attribute = exports[0] as ExportAttribute;
      Assert.That(attribute, Is.Not.Null);
      Assert.That(attribute, Has.Property("ContractType").EqualTo(_barExporterType));
    }

    [Test]
    public void InstanceTest_FooBar()
    {
      // Arrange
      var type = _multipleWeaver.GetType("AssemblyToProcess.MultipleExports.FooBar");

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Has.Length.EqualTo(2));

      Assert.That(exports, Has.Some.Property("ContractType").EqualTo(_barExporterType));
      Assert.That(exports, Has.Some.Property("ContractType").EqualTo(_fooExporterType));
    }

    [TestCase("AssemblyToProcess.MultipleExports.BarImporter")]
    [TestCase("AssemblyToProcess.MultipleExports.FooImporter")]
    public void Importer([NotNull] string className)
    {
      // Arrange
      var type = _multipleWeaver.GetType(className);
      var instance = (dynamic)Activator.CreateInstance(type);

      // Act
      var imports = instance.Imports;

      // Assert
      Assert.That(imports, Is.Not.Null.Or.Empty);
      Assert.That(imports, Has.Length.EqualTo(3));
    }

    [Test]
    public void PeVerify()
    {
      // Arrange

      // Act
      Verifier.Verify(_multipleWeaver.BeforeAssemblyPath,
        _multipleWeaver.AfterAssemblyPath);

      // Assert
    }
  }
}
