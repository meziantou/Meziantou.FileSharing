using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Meziantou.Framework;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Meziantou.FileSharing;

internal sealed class FileService
{
    private static readonly FullPath RootFolder = FullPath.GetTempPath() / "files";

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string[] GetFiles()
    {
        try
        {
            var files = Directory.GetFiles(RootFolder, "*", SearchOption.AllDirectories).Select(FullPath.FromPath);
            return files.Select(f => f.MakePathRelativeTo(RootFolder)).ToArray();
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public async Task AddFileAsync(string name, Stream content)
    {
        var fullPath = GetFullPath(name);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Stream GetByName(string name)
    {
        var path = GetFullPath(name);
        return File.OpenRead(path);
    }

    private static FullPath GetFullPath(string name)
    {
        var result = RootFolder / name;
        if (result.IsChildOf(RootFolder) == false)
            throw new ArgumentException("File name contains invalid characters", nameof(name));

        return result;
    }
}
