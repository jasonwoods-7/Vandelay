using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Vandelay.Fody
{
  [TestFixture]
  public class ModuleWeaverTests
  {
    ModuleWeaverTestHelper _simpleCaseWeaver;
    Type _simpleCaseExportableType;

    ModuleWeaverTestHelper _unsignedWeaver;
    Type _coreExportableType;

    [TestFixtureSetUp]
    public void TestFixtureSetUp()
    {
      _simpleCaseWeaver = new ModuleWeaverTestHelper(
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.SimpleCase.dll");
      Assert.That(_simpleCaseWeaver.Errors, Is.Null.Or.Empty);

      _simpleCaseExportableType = GetType(_simpleCaseWeaver,
        "AssemblyToProcess.SimpleCase.IExportable");

      _unsignedWeaver = new ModuleWeaverTestHelper(
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.Unsigned.dll");
      Assert.That(_simpleCaseWeaver.Errors, Is.Null.Or.Empty);

      _coreExportableType = Assembly.LoadFile(Path.GetFullPath(
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.Core.dll"))
        .GetType("AssemblyToProcess.Core.IExportable", true);
    }

    [TestCase("AssemblyToProcess.SimpleCase.AbstractExportable")]
    public void AbstractTest_SimpleCase(string className)
    {
      // Arrange
      var type = GetType(_simpleCaseWeaver, className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Is.Empty);
    }

    [TestCase("AssemblyToProcess.Unsigned.AbstractExportable")]
    public void AbstractTest_Unsigned(string className)
    {
      // Arrange
      var type = GetType(_unsignedWeaver, className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Is.Empty);
    }

    [TestCase("AssemblyToProcess.SimpleCase.ExportableInstance")]
    [TestCase("AssemblyToProcess.SimpleCase.AlreadyExportedInstance")]
    public void InstanceTest_SimpleCase(string className)
    {
      // Arrange
      var type = GetType(_simpleCaseWeaver, className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Has.Length.EqualTo(1));

      var attribute = exports[0] as ExportAttribute;
      Assert.That(attribute, Is.Not.Null);
      Assert.That(attribute, Has.Property("ContractType").EqualTo(_simpleCaseExportableType));
    }

    [Test]
    public void PeVerify()
    {
      // Arrange

      // Act
      Verifier.Verify(_simpleCaseWeaver.BeforeAssemblyPath,
        _simpleCaseWeaver.AfterAssemblyPath);

      Verifier.Verify(_unsignedWeaver.BeforeAssemblyPath,
        _unsignedWeaver.AfterAssemblyPath);

      // Assert
    }

    static Type GetType(ModuleWeaverTestHelper weaver, string className)
    {
      return weaver.Assembly.GetType(className, true);
    }
  }
}
