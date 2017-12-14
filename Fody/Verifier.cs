using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using NUnit.Framework;
using Scalpel;

namespace Vandelay.Fody
{
  [Remove]
  static class Verifier
  {
    [NotNull] static readonly string ExePath;

    static Verifier()
    {
      var windowsSdk = Environment.ExpandEnvironmentVariables(
        @"%programfiles(x86)%\Microsoft SDKs\Windows\");

      var exePath = Directory.EnumerateFiles(windowsSdk, "PEVerify.exe",
          SearchOption.AllDirectories)
        .OrderByDescending(x =>
        {
          var fileVersionInfo = FileVersionInfo.GetVersionInfo(x);
          return new Version(fileVersionInfo.FileMajorPart,
            fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart);
        }).FirstOrDefault();

      ExePath = exePath ?? throw new Exception("Could not find path to PEVerify");
    }

    public static void Verify([NotNull] string beforeAssemblyPath,
      [NotNull] string afterAssemblyPath)
    {
      var before = Validate(beforeAssemblyPath);
      var after = Validate(afterAssemblyPath);
      var message = $"Failed processing {Path.GetFileName(afterAssemblyPath)}\r\n{after}";
      Assert.AreEqual(TrimLineNumbers(before), TrimLineNumbers(after), message);
    }

    [NotNull]
    static string Validate([NotNull] string assemblyPath2)
    {
      using (var process = Process.Start(new ProcessStartInfo(ExePath, $"\"{assemblyPath2}\"")
      {
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      }))
      {
        Debug.Assert(process != null);
        process.WaitForExit(10000);

        return process.StandardOutput.ReadToEnd().Trim().Replace(assemblyPath2, "");
      }
    }

    [NotNull]
    static string TrimLineNumbers([NotNull] string foo) =>
      Regex.Replace(foo, "0x.*]", "");
  }
}
