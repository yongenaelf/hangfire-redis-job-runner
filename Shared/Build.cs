using System.Diagnostics;
using System.IO.Compression;

namespace Shared;

public class Build
{
    public void DotnetBuild(string file)
    {
        // convert the base64 to bytes
        var fileBytes = Convert.FromBase64String(file);

        // extract the zip to a temp folder with guid
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);
        var tempFile = Path.Combine(tempFolder, "contract.zip");
        File.WriteAllBytes(tempFile, fileBytes);

        ZipFile.ExtractToDirectory(tempFile, tempFolder);

        // build the project
        var projectFolder = Directory.GetDirectories(tempFolder).First();
        var projectFile = Directory.GetFiles(projectFolder, "*.csproj").First();

        try
        {
            var process = Process.Start("dotnet", $"build {projectFile}");
            process.WaitForExit();

            // get the dll content as base64
            var dllFile = Directory.GetFiles(projectFolder, "*.dll.patched").First();
            var dllContent = Convert.ToBase64String(File.ReadAllBytes(dllFile));

            // log the content
            Console.WriteLine(dllContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        // cleanup
        Directory.Delete(tempFolder, true);
    }
}
