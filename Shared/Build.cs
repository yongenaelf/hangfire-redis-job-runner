using System.Diagnostics;
using System.IO.Compression;

namespace Shared;

public class Build
{
    public string DotnetBuild(string file)
    {
        // convert the base64 to bytes
        var fileBytes = Convert.FromBase64String(file);

        // extract the zip to a temp folder with guid
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);
        var tempFile = Path.Combine(tempFolder, "contract.zip");
        File.WriteAllBytes(tempFile, fileBytes);

        ZipFile.ExtractToDirectory(tempFile, tempFolder);

        try
        {
            // build the project
            var projectFile = Directory.GetFiles(tempFolder, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();

            if (projectFile == null)
            {
                throw new Exception("No project file found.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build -p:RunAnalyzers=false",
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new Exception("Failed to start the build process.");
            }
            process.WaitForExit();

            // collect the standard output for returning later
            var output = process.StandardOutput.ReadToEnd();

            // get the dll content as base64
            var dllFile = Directory.GetFiles(tempFolder, "*.dll.patched", SearchOption.AllDirectories).FirstOrDefault();

            // check if the dll exists
            if (!File.Exists(dllFile))
            {
                throw new Exception(output);
            }

            var dllContent = Convert.ToBase64String(File.ReadAllBytes(dllFile));

            // log the content
            Console.WriteLine(dllContent);
            return dllContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return ex.Message;
        }
        finally
        {
            // cleanup
            Directory.Delete(tempFolder, true);
        }
    }
}
