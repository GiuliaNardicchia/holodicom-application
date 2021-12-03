using FellowOakDicom;
using FellowOakDicom.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine.Networking;
using System.Collections;
using System;

public static class DicomFileUtils
{
    public static string DicomDirectoryPath => Path.Combine(Application.streamingAssetsPath, "DICOM");

    private static IEnumerable<FileInfo> OrderedDirectoryListing(string path)
    {
        return OrderedDirectoryListing(() =>
            {

                var directoryInfo = new DirectoryInfo(path);
                return directoryInfo.GetFiles().Where(x => DicomFile.HasValidHeader(x.FullName));
            },
            x => x.Name
        );
    }

    private static IEnumerable<T> OrderedDirectoryListing<T>(Func<IEnumerable<T>> enumerableSupplier, Func<T, string> sortingKey)
    {
        return enumerableSupplier()
            .OrderBy(x =>
            {
                var match = Regex.Match(sortingKey(x), @"(\d+)");
                if (!match.Success) return int.MaxValue;
                var fileNumber = int.Parse(match.Groups[0].Value);
                return fileNumber;
            });
    }

    public static async Task<IEnumerable<DicomFile>> ReadFromDirectoryAsync(string path)
    {
        return await Task.WhenAll(
            OrderedDirectoryListing(path)
                .Select(x => Task.Run(() => DicomFile.OpenAsync(x.FullName)))
        );
    }

    public static IEnumerable<DicomFile> ReadFromDirectory(string path)
    {
        return OrderedDirectoryListing(path)
            .Select(x => DicomFile.Open(x.FullName));
    }

    public async static Task<IEnumerable<DicomFile>> GetZipArchive(string url)
    {
        var response = await Rest.GetAsync(url, readResponseData: true).ConfigureAwait(false);
        var stream = new MemoryStream(response.ResponseData);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var orderedZipEntries = OrderedDirectoryListing(
            () => archive.Entries,
            x => x.Name
        );
        return await Task.WhenAll(
            orderedZipEntries
                .Select(x => x.Open())
                .Select(x=> DicomFile.OpenAsync(x))
        );
    }

    public static Texture2D ExtractTexture(DicomFile file) => ExtractTexture(file.Dataset);

    public static Texture2D ExtractTexture(DicomDataset dataset)
    {
        return new DicomImage(dataset)
            .RenderImage()
            .AsTexture2D();
    }
}
