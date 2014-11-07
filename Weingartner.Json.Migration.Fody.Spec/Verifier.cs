using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Weingartner.Json.Migration.Fody.Spec
{
    public static class Verifier
    {
        public static void Verify(string beforeAssemblyPath, string afterAssemblyPath)
        {
            var before = Validate(beforeAssemblyPath);
            var after = Validate(afterAssemblyPath);
            TrimLineNumbers(after).Should().Be(TrimLineNumbers(before));
        }

        static string Validate(string assemblyPath)
        {
            var exePath = GetPathToPEVerify();
            if (!File.Exists(exePath))
            {
                return string.Empty;
            }

            var startInfo = new ProcessStartInfo(exePath, "/NoLogo \"" + assemblyPath + "\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);

            if (process == null)
            {
                return string.Empty;
            }

            process.WaitForExit(10000);
            return process.StandardOutput.ReadToEnd().Trim().Replace(assemblyPath, "");
        }

        static string GetPathToPEVerify()
        {
            var exePath = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\PEVerify.exe");

            if (!File.Exists(exePath))
            {
                exePath = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\PEVerify.exe");
            }
            return exePath;
        }

        static string TrimLineNumbers(string foo)
        {
            return Regex.Replace(foo, @"0x.*]", "");
        }
    }
}