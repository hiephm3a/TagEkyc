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
                "sync" => await SyncAsync(config, profileName, profile, options, changedOnly: false),
                "sync-changed" => await SyncAsync(config, profileName, profile, options, changedOnly: true),
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
        bool changedOnly)
    {
        var localRoot = PathResolver.Resolve(options.Get("--local", profile.LocalRoot ?? "docs") ?? "docs");
        var driveFolderId = options.Get("--drive-folder-id", profile.DriveFolderId);
        var deleteMissing = options.Has("--delete-missing");
        var dryRun = options.Has("--dry-run");

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

        IReadOnlyCollection<LocalChange> changes = changedOnly
            ? await GitChangeReader.GetChangesAsync(localRoot, options.Get("--commit", "HEAD") ?? "HEAD")
            : LocalChangeReader.GetAllFiles(localRoot);

        if (changes.Count == 0)
        {
            Console.WriteLine("No local files to sync.");
            return 0;
        }

        var result = await sync.SyncAsync(localRoot, driveFolderId, changes, deleteMissing && !changedOnly);

        Console.WriteLine(
            $"Sync complete. created={result.Created}, updated={result.Updated}, skipped={result.Skipped}, deleted={result.Deleted}, missingLocal={result.MissingLocal}");

        if (changedOnly && deleteMissing)
        {
            Console.WriteLine("Note: --delete-missing is ignored by sync-changed. Use full sync --delete-missing for remote cleanup.");
        }

        return 0;
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

            if (driveFile is not null && string.Equals(driveFile.Md5Checksum, localMd5, StringComparison.OrdinalIgnoreCase))
            {
                result.Skipped++;
                Console.WriteLine($"skip: {change.RelativePath}");
                continue;
            }

            if (driveFile is null)
            {
                await CreateFileAsync(parentId, fileName, localPath, change.RelativePath);
                result.Created++;
            }
            else
            {
                await UpdateFileAsync(driveFile.Id, localPath, change.RelativePath);
                result.Updated++;
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
        request.Fields = "files(id,name,mimeType,md5Checksum)";
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

    private async Task CreateFileAsync(string parentId, string fileName, string localPath, string relativePath)
    {
        Console.WriteLine(dryRun ? $"dry-run create file: {relativePath}" : $"create file: {relativePath}");
        if (dryRun)
        {
            return;
        }

        await using var stream = System.IO.File.OpenRead(localPath);
        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = [parentId],
        };

        var request = service.Files.Create(metadata, stream, MimeTypes.FromPath(localPath));
        request.Fields = "id,name,md5Checksum";
        request.SupportsAllDrives = true;
        var upload = await request.UploadAsync();
        ThrowIfUploadFailed(upload, relativePath);
    }

    private async Task UpdateFileAsync(string fileId, string localPath, string relativePath)
    {
        Console.WriteLine(dryRun ? $"dry-run update file: {relativePath}" : $"update file: {relativePath}");
        if (dryRun)
        {
            return;
        }

        await using var stream = System.IO.File.OpenRead(localPath);
        var request = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId, stream, MimeTypes.FromPath(localPath));
        request.Fields = "id,name,md5Checksum";
        request.SupportsAllDrives = true;
        var upload = await request.UploadAsync();
        ThrowIfUploadFailed(upload, relativePath);
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
            request.Fields = "nextPageToken,files(id,name,mimeType,md5Checksum)";
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

internal sealed class SyncResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Deleted { get; set; }
    public int MissingLocal { get; set; }
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

            Required:
              --profile <name>                 Profile name in .gdrive/config.local.json.

            Common options:
              --config <path>                  Default: .gdrive/config.local.json
              --local <path>                   Default: profile.localRoot or docs
              --drive-folder-id <id>           Overrides profile.driveFolderId
              --dry-run                        Print actions without writing to Drive.

            sync options:
              --delete-missing                 Trash remote files that no longer exist locally.

            sync-changed options:
              --commit <ref>                   Default: HEAD

            Examples:
              dotnet run --project tools/TagEkyc.GDriveSync -- auth --profile main
              dotnet run --project tools/TagEkyc.GDriveSync -- sync --profile main
              dotnet run --project tools/TagEkyc.GDriveSync -- sync-changed --profile main --commit HEAD
            """);
    }
}
