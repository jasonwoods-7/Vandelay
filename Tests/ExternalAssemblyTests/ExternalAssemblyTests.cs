using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Fody;
using JetBrains.Annotations;
using Vandelay.Fody;
using Xunit;

#pragma warning disable 618

namespace ExternalAssemblyTests
{
  [UsedImplicitly]
  public class ExternalAssemblySetup
  {
    [NotNull]
    public TestResult UnsignedWeaver { get; }

    [NotNull]
    public TestResult SignedWeaver { get; }

    [NotNull]
    public Type CoreExportableType { get; }

    public ExternalAssemblySetup()
    {
      var weaver = new ModuleWeaver();

      UnsignedWeaver = weaver.ExecuteTestRun(
        "AssemblyToProcess.Unsigned.dll");
      UnsignedWeaver.Errors.Should().BeEmpty();

      var directoryName = UnsignedWeaver.AssemblyPath;

      SignedWeaver = weaver.ExecuteTestRun(
        "AssemblyToProcess.Signed.dll");
      SignedWeaver.Errors.Should().BeEmpty();

      File.Copy("AssemblyToProcessCore.dll",
        Path.Combine(directoryName, "AssemblyToProcessCore.dll"));

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
      var type = GetTestHelper(className).Assembly.GetType(className, true);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false);

      // Assert
      exports.Should().BeEmpty();
    }

    [Theory]
    [InlineData("AssemblyToProcess.Unsigned.ExportableInstance")]
    [InlineData("AssemblyToProcess.Unsigned.AlreadyExportedInstance")]
    [InlineData("AssemblyToProcess.Signed.ExportableInstance")]
    [InlineData("AssemblyToProcess.Signed.AlreadyExportedInstance")]
    public void InstanceTest([NotNull] string className)
    {
      // Arrange
      var type = GetTestHelper(className).Assembly.GetType(className, true);

      // Act
      var exports = type.GetCustomAttributes<ExportAttribute>(false).ToArray();

      // Assert
      exports.Should().HaveCount(1);

      var attribute = exports[0];
      attribute.ContractType.Should().Be(_setup.CoreExportableType);
    }

    [Theory]
    [InlineData("AssemblyToProcess.Unsigned.Importer")]
    [InlineData("AssemblyToProcess.Signed.Importer")]
    public void ImportMany([NotNull] string className)
    {
      // Arrange
      var importsInstance = GetTestHelper(className).GetInstance(className);

      // Act
      var imports = (ICollection)importsInstance.Imports;

      // Assert
      imports.Should().HaveCount(4);
    }

    [NotNull]
    TestResult GetTestHelper([NotNull] string className) =>
      className.Contains("Unsigned") ? _setup.UnsignedWeaver : _setup.SignedWeaver;
  }
}
