using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Vandelay.Fody
{
  [TestFixture]
  public class ExternalAssemblyTests
  {
    [NotNull]
    // ReSharper disable NotNullMemberIsNotInitialized
    ModuleWeaverTestHelper _unsignedWeaver;

    [NotNull]
    ModuleWeaverTestHelper _signedWeaver;

    [NotNull]
    Type _coreExportableType;
    // ReSharper restore NotNullMemberIsNotInitialized

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
      _unsignedWeaver = new ModuleWeaverTestHelper(
        Path.Combine(TestContext.CurrentContext.TestDirectory,
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.Unsigned.dll"));
      Assert.That(_unsignedWeaver.Errors, Is.Null.Or.Empty);

      _signedWeaver = new ModuleWeaverTestHelper(
        Path.Combine(TestContext.CurrentContext.TestDirectory,
        @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.Signed.dll"));
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
    public void AbstractTest([NotNull] string className)
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
    public void InstanceTest([NotNull] string className)
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

    [TestCase("AssemblyToProcess.Unsigned.Importer")]
    [TestCase("AssemblyToProcess.Signed.Importer")]
    public void ImportMany([NotNull] string className)
    {
      // Arrange
      var importsType = GetTestHelper(className).GetType(className);
      var importsInstance = (dynamic)Activator.CreateInstance(importsType);

      // Act
      var imports = importsInstance.Imports;

      // Assert
      Assert.That(imports, Is.Not.Null.Or.Empty);
      Assert.That(imports, Has.Length.EqualTo(4));
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

    [NotNull]
    ModuleWeaverTestHelper GetTestHelper([NotNull] string className) =>
      className.Contains("Unsigned") ? _unsignedWeaver : _signedWeaver;
  }
}
