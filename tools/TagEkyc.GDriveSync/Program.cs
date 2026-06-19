using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;

namespace TagEkyc.GDriveSync;

internal static class Program
{
    private static readonly string[] DriveScopes = [DriveService.Scope.Drive];

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Usage.Write();
            return 0;
        }

        try
        {
            var command = args[0];
            var options = CliOptions.Parse(args.Skip(1));
            var configPath = options.Get("--config", ".gdrive/config.local.json");
            var config = SyncConfig.Load(configPath ?? ".gdrive/config.local.json");
            var profileName = options.GetRequired("--profile");
            var profile = config.GetProfile(profileName);

            return command switch
            {
                "auth" => await AuthAsync(config, profileName, profile),
                "sync" => await SyncAsync(config, profileName, profile, options, SyncMode.Full),
                "sync-changed" => await SyncAsync(config, profileName, profile, options, SyncMode.CommitChanges),
                "sync-dirty" => await SyncAsync(config, profileName, profile, options, SyncMode.DirtyChanges),
                _ => Fail($"Unknown command '{command}'. Run with --help for usage.")
            };
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> AuthAsync(SyncConfig config, string profileName, SyncProfile profile)
    {
        await CreateDriveServiceAsync(config, profileName, profile);
        Console.WriteLine($"OAuth token is ready for profile '{profileName}'.");
        return 0;
    }

    private static async Task<int> SyncAsync(
        SyncConfig config,
        string profileName,
        SyncProfile profile,
        CliOptions options,
        SyncMode mode)
    {
        var localRoot = PathResolver.Resolve(options.Get("--local", profile.LocalRoot ?? "docs") ?? "docs");
        var driveFolderId = options.Get("--drive-folder-id", profile.DriveFolderId);
        var deleteMissing = options.Has("--delete-missing");
        var dryRun = options.Has("--dry-run");
        var writeIndex = !options.Has("--no-index");
        var indexRelativePath = NormalizeRelativePath(options.Get("--index", profile.IndexPath ?? "00_GDRIVE_FILE_INDEX.md") ?? "00_GDRIVE_FILE_INDEX.md");

        if (string.IsNullOrWhiteSpace(driveFolderId))
        {
            throw new CliException("Drive folder id is required. Set profile.driveFolderId or pass --drive-folder-id.");
        }

        if (!Directory.Exists(localRoot))
        {
            throw new CliException($"Local root does not exist: {localRoot}");
        }

        var service = await CreateDriveServiceAsync(config, profileName, profile);
        var sync = new DriveSynchronizer(service, dryRun);

        IReadOnlyCollection<LocalChange> changes = mode switch
        {
            SyncMode.Full => LocalChangeReader.GetAllFiles(localRoot),
            SyncMode.CommitChanges => await GitChangeReader.GetChangesAsync(localRoot, options.Get("--commit", "HEAD") ?? "HEAD"),
            SyncMode.DirtyChanges => await GitDirtyReader.GetChangesAsync(localRoot),
            _ => throw new InvalidOperationException($"Unsupported sync mode: {mode}")
        };

        if (writeIndex)
        {
            changes = changes
                .Where(change => !string.Equals(change.RelativePath, indexRelativePath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (changes.Count == 0)
        {
            Console.WriteLine("No local files to sync.");
            return 0;
        }

        var result = await sync.SyncAsync(localRoot, driveFolderId, changes, deleteMissing && mode == SyncMode.Full);

        if (writeIndex && !dryRun)
        {
            var commitLabel = mode switch
            {
                SyncMode.DirtyChanges => "dirty",
                SyncMode.CommitChanges => await GitInfo.GetCommitAsync(options.Get("--commit", "HEAD") ?? "HEAD"),
                SyncMode.Full => await GitInfo.GetCommitAsync("HEAD"),
                _ => "unknown"
            };

            var indexResult = await SyncIndexAsync(sync, localRoot, driveFolderId, indexRelativePath, commitLabel, result.Files);
            result.Merge(indexResult);
        }

        Console.WriteLine(
            $"Sync complete. created={result.Created}, updated={result.Updated}, skipped={result.Skipped}, deleted={result.Deleted}, missingLocal={result.MissingLocal}");

        if (result.Files.Count > 0)
        {
            Console.WriteLine("Synced files:");
            foreach (var file in result.Files.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"{file.Action}: path={file.RelativePath} id={file.DriveFileId} sha256={file.Sha256} view={file.ViewLink}");
            }
        }

        if (mode != SyncMode.Full && deleteMissing)
        {
            Console.WriteLine("Note: --delete-missing is ignored outside full sync. Use full sync --delete-missing for remote cleanup.");
        }

        return 0;
    }

    private static async Task<SyncResult> SyncIndexAsync(
        DriveSynchronizer sync,
        string localRoot,
        string driveFolderId,
        string indexRelativePath,
        string commitLabel,
        IReadOnlyCollection<SyncedFile> syncedFiles)
    {
        var indexPath = Path.Combine(localRoot, indexRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(indexPath) ?? localRoot);

        var entries = GDriveFileIndex.Load(indexPath);
        var syncedAtUtc = DateTimeOffset.UtcNow;

        foreach (var file in syncedFiles)
        {
            var entry = GDriveFileIndexEntry.FromSyncedFile(file, commitLabel, syncedAtUtc);
            entries[entry.Path] = entry;
        }

        GDriveFileIndex.Write(indexPath, entries.Values);

        var firstIndexSync = await sync.SyncAsync(localRoot, driveFolderId, [new LocalChange(indexRelativePath, IsDeleted: false)], deleteMissing: false);
        var indexFile = firstIndexSync.Files.LastOrDefault(file => string.Equals(file.RelativePath, indexRelativePath, StringComparison.OrdinalIgnoreCase));
        if (indexFile is not null)
        {
            var indexEntry = GDriveFileIndexEntry.FromSyncedFile(indexFile, commitLabel, DateTimeOffset.UtcNow);
            entries[indexEntry.Path] = indexEntry;
            GDriveFileIndex.Write(indexPath, entries.Values);
        }

        var secondIndexSync = await sync.SyncAsync(localRoot, driveFolderId, [new LocalChange(indexRelativePath, IsDeleted: false)], deleteMissing: false);
        firstIndexSync.Merge(secondIndexSync);
        return firstIndexSync;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static async Task<DriveService> CreateDriveServiceAsync(SyncConfig config, string profileName, SyncProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.ClientId) || string.IsNullOrWhiteSpace(profile.ClientSecret))
        {
            throw new CliException($"Profile '{profileName}' must set clientId and clientSecret.");
        }

        var tokenPath = PathResolver.Resolve(profile.TokenPath ?? $".gdrive/tokens/{profileName}");
        Directory.CreateDirectory(tokenPath);

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = profile.ClientId,
                ClientSecret = profile.ClientSecret,
            },
            DriveScopes,
            profileName,
            CancellationToken.None,
            new FileDataStore(tokenPath, fullPath: true));

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = config.ApplicationName ?? "TagEkyc.GDriveSync",
        });
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        return 2;
    }
}

internal enum SyncMode
{
    Full,
    CommitChanges,
    DirtyChanges,
}

internal sealed class DriveSynchronizer(DriveService service, bool dryRun)
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    public async Task<SyncResult> SyncAsync(
        string localRoot,
        string rootDriveFolderId,
        IReadOnlyCollection<LocalChange> changes,
        bool deleteMissing)
    {
        var result = new SyncResult();
        var folderCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = rootDriveFolderId,
        };

        foreach (var change in changes.OrderBy(change => change.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            if (change.IsDeleted)
            {
                result.MissingLocal++;
                Console.WriteLine($"delete skipped: {change.RelativePath}");
                continue;
            }

            var localPath = Path.Combine(localRoot, change.RelativePath);
            if (!System.IO.File.Exists(localPath))
            {
                result.MissingLocal++;
                Console.WriteLine($"missing local: {change.RelativePath}");
                continue;
            }

            var folderRelativePath = Path.GetDirectoryName(change.RelativePath)?.Replace('\\', '/') ?? "";
            var fileName = Path.GetFileName(change.RelativePath);
            var parentId = await EnsureDriveFolderPathAsync(rootDriveFolderId, folderRelativePath, folderCache);
            var driveFile = await FindChildAsync(parentId, fileName, folderOnly: false);
            var localMd5 = FileHasher.Md5(localPath);
            var localSha256 = FileHasher.Sha256(localPath);

            if (driveFile is not null && string.Equals(driveFile.Md5Checksum, localMd5, StringComparison.OrdinalIgnoreCase))
            {
                result.Skipped++;
                Console.WriteLine($"skip: {change.RelativePath}");
                result.Files.Add(SyncedFile.FromDriveFile(change.RelativePath, SyncAction.Skipped, localSha256, driveFile));
                continue;
            }

            if (driveFile is null)
            {
                var created = await CreateFileAsync(parentId, fileName, localPath, change.RelativePath);
                result.Created++;
                result.Files.Add(SyncedFile.FromDriveFile(change.RelativePath, SyncAction.Created, localSha256, created));
            }
            else
            {
                var updated = await UpdateFileAsync(driveFile.Id, localPath, change.RelativePath);
                result.Updated++;
                result.Files.Add(SyncedFile.FromDriveFile(change.RelativePath, SyncAction.Updated, localSha256, updated));
            }
        }

        if (deleteMissing)
        {
            var localFiles = LocalChangeReader
                .GetAllFiles(localRoot)
                .Select(change => change.RelativePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            await DeleteRemoteFilesMissingLocallyAsync(rootDriveFolderId, "", localFiles, result);
        }

        return result;
    }

    private async Task<string> EnsureDriveFolderPathAsync(
        string rootDriveFolderId,
        string relativePath,
        Dictionary<string, string> folderCache)
    {
        if (folderCache.TryGetValue(relativePath, out var cachedId))
        {
            return cachedId;
        }

        var currentPath = "";
        var currentFolderId = rootDriveFolderId;

        foreach (var segment in relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
            if (folderCache.TryGetValue(currentPath, out cachedId))
            {
                currentFolderId = cachedId;
                continue;
            }

            var folder = dryRun && currentFolderId.StartsWith("dry-run:", StringComparison.Ordinal)
                ? null
                : await FindChildAsync(currentFolderId, segment, folderOnly: true);
            if (folder is null)
            {
                folder = await CreateFolderAsync(currentFolderId, segment, currentPath);
            }

            folderCache[currentPath] = folder.Id;
            currentFolderId = folder.Id;
        }

        return currentFolderId;
    }

    private async Task<Google.Apis.Drive.v3.Data.File?> FindChildAsync(string parentId, string name, bool folderOnly)
    {
        var request = service.Files.List();
        request.Q = folderOnly
            ? $"'{EscapeQueryValue(parentId)}' in parents and name = '{EscapeQueryValue(name)}' and mimeType = '{FolderMimeType}' and trashed = false"
            : $"'{EscapeQueryValue(parentId)}' in parents and name = '{EscapeQueryValue(name)}' and mimeType != '{FolderMimeType}' and trashed = false";
        request.Fields = FileFields.ListFields;
        request.PageSize = 2;
        request.SupportsAllDrives = true;
        request.IncludeItemsFromAllDrives = true;

        var response = await request.ExecuteAsync();
        return response.Files.FirstOrDefault();
    }

    private async Task<Google.Apis.Drive.v3.Data.File> CreateFolderAsync(string parentId, string name, string relativePath)
    {
        Console.WriteLine(dryRun ? $"dry-run create folder: {relativePath}" : $"create folder: {relativePath}");

        if (dryRun)
        {
            return new Google.Apis.Drive.v3.Data.File
            {
                Id = $"dry-run:{relativePath}",
                Name = name,
                MimeType = FolderMimeType,
            };
        }

        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = name,
            MimeType = FolderMimeType,
            Parents = [parentId],
        };

        var request = service.Files.Create(metadata);
        request.Fields = "id,name,mimeType";
        request.SupportsAllDrives = true;
        return await request.ExecuteAsync();
    }

    private async Task<Google.Apis.Drive.v3.Data.File> CreateFileAsync(string parentId, string fileName, string localPath, string relativePath)
    {
        Console.WriteLine(dryRun ? $"dry-run create file: {relativePath}" : $"create file: {relativePath}");
        if (dryRun)
        {
            return DryRunFile(fileName, relativePath);
        }

        await using var stream = System.IO.File.OpenRead(localPath);
        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = [parentId],
        };

        var request = service.Files.Create(metadata, stream, MimeTypes.FromPath(localPath));
        request.Fields = FileFields.ItemFields;
        request.SupportsAllDrives = true;
        var upload = await request.UploadAsync();
        ThrowIfUploadFailed(upload, relativePath);
        return request.ResponseBody ?? throw new InvalidOperationException($"Upload did not return file metadata for {relativePath}.");
    }

    private async Task<Google.Apis.Drive.v3.Data.File> UpdateFileAsync(string fileId, string localPath, string relativePath)
    {
        Console.WriteLine(dryRun ? $"dry-run update file: {relativePath}" : $"update file: {relativePath}");
        if (dryRun)
        {
            return DryRunFile(Path.GetFileName(relativePath), relativePath);
        }

        await using var stream = System.IO.File.OpenRead(localPath);
        var request = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId, stream, MimeTypes.FromPath(localPath));
        request.Fields = FileFields.ItemFields;
        request.SupportsAllDrives = true;
        var upload = await request.UploadAsync();
        ThrowIfUploadFailed(upload, relativePath);
        return request.ResponseBody ?? await GetFileAsync(fileId);
    }

    private async Task<Google.Apis.Drive.v3.Data.File> GetFileAsync(string fileId)
    {
        var request = service.Files.Get(fileId);
        request.Fields = FileFields.ItemFields;
        request.SupportsAllDrives = true;
        return await request.ExecuteAsync();
    }

    private static Google.Apis.Drive.v3.Data.File DryRunFile(string fileName, string relativePath)
    {
        return new Google.Apis.Drive.v3.Data.File
        {
            Id = $"dry-run:{relativePath}",
            Name = fileName,
            MimeType = MimeTypes.FromPath(fileName),
            WebViewLink = $"dry-run:{relativePath}",
            WebContentLink = $"dry-run:{relativePath}",
        };
    }

    private async Task DeleteRemoteFilesMissingLocallyAsync(
        string folderId,
        string relativeFolder,
        HashSet<string> localFiles,
        SyncResult result)
    {
        var children = await ListChildrenAsync(folderId);
        foreach (var child in children)
        {
            var relativePath = string.IsNullOrEmpty(relativeFolder)
                ? child.Name
                : $"{relativeFolder}/{child.Name}";

            if (child.MimeType == FolderMimeType)
            {
                await DeleteRemoteFilesMissingLocallyAsync(child.Id, relativePath, localFiles, result);
                continue;
            }

            if (localFiles.Contains(relativePath))
            {
                continue;
            }

            Console.WriteLine(dryRun ? $"dry-run trash remote: {relativePath}" : $"trash remote: {relativePath}");
            if (!dryRun)
            {
                await service.Files.Update(new Google.Apis.Drive.v3.Data.File { Trashed = true }, child.Id).ExecuteAsync();
            }

            result.Deleted++;
        }
    }

    private async Task<IReadOnlyList<Google.Apis.Drive.v3.Data.File>> ListChildrenAsync(string folderId)
    {
        var files = new List<Google.Apis.Drive.v3.Data.File>();
        string? pageToken = null;

        do
        {
            var request = service.Files.List();
            request.Q = $"'{EscapeQueryValue(folderId)}' in parents and trashed = false";
            request.Fields = $"nextPageToken,{FileFields.ListFields}";
            request.PageSize = 1000;
            request.PageToken = pageToken;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            var response = await request.ExecuteAsync();
            files.AddRange(response.Files);
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return files;
    }

    private static string EscapeQueryValue(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
    }

    private static void ThrowIfUploadFailed(IUploadProgress upload, string relativePath)
    {
        if (upload.Status == UploadStatus.Failed)
        {
            throw new InvalidOperationException($"Upload failed for {relativePath}: {upload.Exception?.Message}");
        }
    }
}

internal static class FileFields
{
    public const string ItemFields = "id,name,mimeType,md5Checksum,size,webViewLink,webContentLink";
    public const string ListFields = "files(id,name,mimeType,md5Checksum,size,webViewLink,webContentLink)";
}

internal static class GitChangeReader
{
    public static async Task<IReadOnlyCollection<LocalChange>> GetChangesAsync(string localRoot, string commit)
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var relativeLocalRoot = Path.GetRelativePath(repoRoot, localRoot).Replace('\\', '/');
        var output = await RunGitAsync(["diff-tree", "--no-commit-id", "--name-status", "-r", commit, "--", relativeLocalRoot]);
        var changes = new List<LocalChange>();

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var status = parts[0];
            var path = parts[^1].Replace('\\', '/');
            if (!path.StartsWith(relativeLocalRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = path[(relativeLocalRoot.Length + 1)..];
            changes.Add(new LocalChange(relativePath, status.StartsWith('D')));
        }

        return changes;
    }

    private static async Task<string> RunGitAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start git.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new CliException($"git command failed: {error.Trim()}");
        }

        return output;
    }
}

internal static class GitDirtyReader
{
    public static async Task<IReadOnlyCollection<LocalChange>> GetChangesAsync(string localRoot)
    {
        var repoRoot = Directory.GetCurrentDirectory();
        var relativeLocalRoot = Path.GetRelativePath(repoRoot, localRoot).Replace('\\', '/');
        var output = await RunGitAsync(["status", "--porcelain=v1", "--untracked-files=all", "--", relativeLocalRoot]);
        var changes = new List<LocalChange>();

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length < 4)
            {
                continue;
            }

            var status = line[..2];
            var path = line[3..].Replace('\\', '/');
            var arrowIndex = path.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrowIndex >= 0)
            {
                path = path[(arrowIndex + 4)..];
            }

            if (!path.StartsWith(relativeLocalRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = path[(relativeLocalRoot.Length + 1)..];
            var localPath = Path.Combine(localRoot, relativePath);
            if (Directory.Exists(localPath))
            {
                foreach (var file in Directory.EnumerateFiles(localPath, "*", SearchOption.AllDirectories))
                {
                    changes.Add(new LocalChange(Path.GetRelativePath(localRoot, file).Replace('\\', '/'), IsDeleted: false));
                }

                continue;
            }

            var isDeleted = status.Contains('D') && !System.IO.File.Exists(Path.Combine(localRoot, relativePath));
            changes.Add(new LocalChange(relativePath, isDeleted));
        }

        return changes
            .GroupBy(change => change.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
    }

    private static async Task<string> RunGitAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start git.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new CliException($"git command failed: {error.Trim()}");
        }

        return output;
    }
}

internal static class GitInfo
{
    public static async Task<string> GetCommitAsync(string commit)
    {
        var output = await RunGitAsync(["rev-parse", "--short", commit]);
        return output.Trim();
    }

    private static async Task<string> RunGitAsync(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start git.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new CliException($"git command failed: {error.Trim()}");
        }

        return output;
    }
}

internal static class LocalChangeReader
{
    public static IReadOnlyCollection<LocalChange> GetAllFiles(string localRoot)
    {
        return Directory
            .EnumerateFiles(localRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredLocalFile(path))
            .Select(path => new LocalChange(Path.GetRelativePath(localRoot, path).Replace('\\', '/'), IsDeleted: false))
            .ToArray();
    }

    private static bool IsIgnoredLocalFile(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => segment is ".git" or "bin" or "obj" or "TestResults");
    }
}

internal sealed record LocalChange(string RelativePath, bool IsDeleted);

internal enum SyncAction
{
    Created,
    Updated,
    Skipped,
}

internal sealed record SyncedFile(
    string RelativePath,
    SyncAction Action,
    string DriveFileId,
    string Sha256,
    string? DriveMd5,
    long? Size,
    string ViewLink,
    string DownloadLink)
{
    public static SyncedFile FromDriveFile(
        string relativePath,
        SyncAction action,
        string sha256,
        Google.Apis.Drive.v3.Data.File file)
    {
        var viewLink = !string.IsNullOrWhiteSpace(file.WebViewLink)
            ? file.WebViewLink
            : $"https://drive.google.com/file/d/{file.Id}/view?usp=sharing";

        var downloadLink = !string.IsNullOrWhiteSpace(file.WebContentLink)
            ? file.WebContentLink
            : $"https://drive.google.com/uc?export=download&id={file.Id}";

        return new SyncedFile(
            relativePath,
            action,
            file.Id,
            sha256,
            file.Md5Checksum,
            file.Size,
            viewLink,
            downloadLink);
    }
}

internal sealed class SyncResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Deleted { get; set; }
    public int MissingLocal { get; set; }
    public List<SyncedFile> Files { get; } = [];

    public void Merge(SyncResult other)
    {
        Created += other.Created;
        Updated += other.Updated;
        Skipped += other.Skipped;
        Deleted += other.Deleted;
        MissingLocal += other.MissingLocal;

        foreach (var file in other.Files)
        {
            var existingIndex = Files.FindIndex(existing => string.Equals(existing.RelativePath, file.RelativePath, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                Files[existingIndex] = file;
            }
            else
            {
                Files.Add(file);
            }
        }
    }
}

internal sealed class SyncConfig
{
    public string? ApplicationName { get; init; }
    public Dictionary<string, SyncProfile> Profiles { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static SyncConfig Load(string configPath)
    {
        var resolvedPath = PathResolver.Resolve(configPath);
        if (!System.IO.File.Exists(resolvedPath))
        {
            throw new CliException($"Config file not found: {resolvedPath}");
        }

        var json = System.IO.File.ReadAllText(resolvedPath);
        var config = JsonSerializer.Deserialize<SyncConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        if (config is null)
        {
            throw new CliException($"Config file is empty or invalid: {resolvedPath}");
        }

        return config;
    }

    public SyncProfile GetProfile(string profileName)
    {
        if (!Profiles.TryGetValue(profileName, out var profile))
        {
            throw new CliException($"Profile '{profileName}' was not found in config.");
        }

        return profile;
    }
}

internal sealed class SyncProfile
{
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? TokenPath { get; init; }
    public string? DriveFolderId { get; init; }
    public string? LocalRoot { get; init; }
    public string? IndexPath { get; init; }
}

internal sealed class CliOptions
{
    private readonly Dictionary<string, string?> values;

    private CliOptions(Dictionary<string, string?> values)
    {
        this.values = values;
    }

    public static CliOptions Parse(IEnumerable<string> args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var tokens = args.ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliException($"Unexpected argument '{token}'. Options must start with --.");
            }

            if (i + 1 >= tokens.Length || tokens[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[token] = null;
                continue;
            }

            values[token] = tokens[++i];
        }

        return new CliOptions(values);
    }

    public bool Has(string key)
    {
        return values.ContainsKey(key);
    }

    public string? Get(string key, string? defaultValue = null)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }

    public string GetRequired(string key)
    {
        var value = Get(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CliException($"Missing required option {key}.");
        }

        return value;
    }
}

internal static class FileHasher
{
    public static string Md5(string path)
    {
        using var stream = System.IO.File.OpenRead(path);
        var hash = MD5.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Sha256(string path)
    {
        using var stream = System.IO.File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal sealed record GDriveFileIndexEntry(
    string Path,
    string DriveFileId,
    string Sha256,
    string? DriveMd5,
    long? Size,
    string Commit,
    DateTimeOffset SyncedAtUtc,
    string ViewLink,
    string DownloadLink)
{
    public static GDriveFileIndexEntry FromSyncedFile(SyncedFile file, string commit, DateTimeOffset syncedAtUtc)
    {
        return new GDriveFileIndexEntry(
            $"docs/{file.RelativePath}",
            file.DriveFileId,
            file.Sha256,
            file.DriveMd5,
            file.Size,
            commit,
            syncedAtUtc,
            file.ViewLink,
            file.DownloadLink);
    }
}

internal static class GDriveFileIndex
{
    private const string Header = "| Path | Drive file ID | SHA256 | Drive MD5 | Size | Commit | Synced UTC | View link | Download link |";
    private const string Separator = "| --- | --- | --- | --- | ---: | --- | --- | --- | --- |";

    public static Dictionary<string, GDriveFileIndexEntry> Load(string indexPath)
    {
        var entries = new Dictionary<string, GDriveFileIndexEntry>(StringComparer.OrdinalIgnoreCase);
        if (!System.IO.File.Exists(indexPath))
        {
            return entries;
        }

        foreach (var line in System.IO.File.ReadLines(indexPath))
        {
            if (!line.StartsWith("| docs/", StringComparison.Ordinal))
            {
                continue;
            }

            var cells = line
                .Trim()
                .Trim('|')
                .Split('|')
                .Select(cell => cell.Trim())
                .ToArray();

            if (cells.Length < 9)
            {
                continue;
            }

            var size = long.TryParse(cells[4], out var parsedSize) ? parsedSize : (long?)null;
            var syncedAt = DateTimeOffset.TryParse(cells[6], out var parsedSyncedAt)
                ? parsedSyncedAt
                : DateTimeOffset.MinValue;

            entries[cells[0]] = new GDriveFileIndexEntry(
                cells[0],
                cells[1],
                cells[2],
                string.Equals(cells[3], "-", StringComparison.Ordinal) ? null : cells[3],
                size,
                cells[5],
                syncedAt,
                cells[7],
                cells[8]);
        }

        return entries;
    }

    public static void Write(string indexPath, IEnumerable<GDriveFileIndexEntry> entries)
    {
        var lines = new List<string>
        {
            "# TagEkyc Google Drive File Index",
            "",
            "This file is generated by `TagEkyc.GDriveSync`. Use it to map repository documentation paths to public Google Drive file IDs, hashes, and fetch links.",
            "",
            Header,
            Separator,
        };

        foreach (var entry in entries.OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add(string.Join(" ",
                "|",
                EscapeCell(entry.Path),
                "|",
                EscapeCell(entry.DriveFileId),
                "|",
                EscapeCell(entry.Sha256),
                "|",
                EscapeCell(entry.DriveMd5 ?? "-"),
                "|",
                entry.Size?.ToString() ?? "-",
                "|",
                EscapeCell(entry.Commit),
                "|",
                entry.SyncedAtUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                "|",
                EscapeCell(entry.ViewLink),
                "|",
                EscapeCell(entry.DownloadLink),
                "|"));
        }

        lines.Add("");
        System.IO.File.WriteAllLines(indexPath, lines);
    }

    private static string EscapeCell(string value)
    {
        return value.Replace("|", "%7C", StringComparison.Ordinal);
    }
}

internal static class MimeTypes
{
    private static readonly Dictionary<string, string> KnownTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".md"] = "text/markdown",
        [".txt"] = "text/plain",
        [".json"] = "application/json",
        [".yml"] = "application/yaml",
        [".yaml"] = "application/yaml",
        [".xml"] = "application/xml",
        [".csv"] = "text/csv",
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
    };

    public static string FromPath(string path)
    {
        return KnownTypes.TryGetValue(Path.GetExtension(path), out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }
}

internal static class PathResolver
{
    public static string Resolve(string path)
    {
        return Path.GetFullPath(path, Directory.GetCurrentDirectory());
    }
}

internal sealed class CliException(string message) : Exception(message);

internal static class Usage
{
    public static void Write()
    {
        Console.WriteLine(
            """
            TagEkyc.GDriveSync

            Commands:
              auth         Run OAuth for a profile and store the local token.
              sync         Sync all files from local root to the configured Drive folder.
              sync-changed Sync files changed in a Git commit, intended for post-commit hooks.
              sync-dirty   Sync uncommitted modified and untracked files from local root.

            Required:
              --profile <name>                 Profile name in .gdrive/config.local.json.

            Common options:
              --config <path>                  Default: .gdrive/config.local.json
              --local <path>                   Default: profile.localRoot or docs
              --drive-folder-id <id>           Overrides profile.driveFolderId
              --index <path>                   Default: profile.indexPath or 00_GDRIVE_FILE_INDEX.md, relative to local root
              --no-index                       Do not update or sync the generated Drive file index
              --dry-run                        Print actions without writing to Drive.

            sync options:
              --delete-missing                 Trash remote files that no longer exist locally.

            sync-changed options:
              --commit <ref>                   Default: HEAD

            Examples:
              dotnet run --project tools/TagEkyc.GDriveSync -- auth --profile main
              dotnet run --project tools/TagEkyc.GDriveSync -- sync --profile main
              dotnet run --project tools/TagEkyc.GDriveSync -- sync-changed --profile main --commit HEAD
              dotnet run --project tools/TagEkyc.GDriveSync -- sync-dirty --profile main
            """);
    }
}
