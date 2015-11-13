using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Vandelay.Fody
{
  [TestFixture]
  public class ExternalAssemblyTests
  {
    ModuleWeaverTestHelper _unsignedWeaver;
    ModuleWeaverTestHelper _signedWeaver;
    Type _coreExportableType;

    [TestFixtureSetUp]
    public void TestFixtureSetUp()
    {
      _unsignedWeaver = new ModuleWeaverTestHelper(
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.Unsigned.dll");
      Assert.That(_unsignedWeaver.Errors, Is.Null.Or.Empty);

      _signedWeaver = new ModuleWeaverTestHelper(
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.Signed.dll");
      Assert.That(_signedWeaver.Errors, Is.Null.Or.Empty);

      var directoryName = Path.GetDirectoryName(_unsignedWeaver.Assembly.Location);
      Debug.Assert(null != directoryName);

      _coreExportableType = Assembly.LoadFile(Path.GetFullPath(
        Path.Combine(directoryName, "AssemblyToProcess.Core.dll")))
        .GetType("AssemblyToProcess.Core.IExportable", true);

      AppDomain.CurrentDomain.AssemblyResolve += (_, e) => Assembly.LoadFile(
        Path.Combine(directoryName, $"{e.Name.Split(',')[0]}.dll"));
    }

    [TestCase("AssemblyToProcess.Unsigned.AbstractExportable")]
    [TestCase("AssemblyToProcess.Signed.AbstractExportable")]
    public void AbstractTest(string className)
    {
      // Arrange
      var type = GetTestHelper(className).GetType(className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Is.Empty);
    }

    [TestCase("AssemblyToProcess.Unsigned.ExportableInstance")]
    [TestCase("AssemblyToProcess.Unsigned.AlreadyExportedInstance")]
    [TestCase("AssemblyToProcess.Signed.ExportableInstance")]
    [TestCase("AssemblyToProcess.Signed.AlreadyExportedInstance")]
    public void InstanceTest(string className)
    {
      // Arrange
      var type = GetTestHelper(className).GetType(className);

      // Act
      var exports = type.GetCustomAttributes(typeof(ExportAttribute), false);

      // Assert
      Assert.That(exports, Is.Not.Null);
      Assert.That(exports, Has.Length.EqualTo(1));

      var attribute = exports[0] as ExportAttribute;
      Assert.That(attribute, Is.Not.Null);
      Assert.That(attribute, Has.Property("ContractType").EqualTo(_coreExportableType));
    }

    [Test]
    public void PeVerify()
    {
      // Arrange

      // Act
      Verifier.Verify(_unsignedWeaver.BeforeAssemblyPath,
        _unsignedWeaver.AfterAssemblyPath);

      Verifier.Verify(_signedWeaver.BeforeAssemblyPath,
        _signedWeaver.AfterAssemblyPath);

      // Assert
    }

    ModuleWeaverTestHelper GetTestHelper(string className)
    {
      return className.Contains("Unsigned") ? _unsignedWeaver : _signedWeaver;
    }
  }
}
