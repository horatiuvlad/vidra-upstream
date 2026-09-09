using Vidra.Bridge;

namespace Vidra.Modules.FileSystem;

public record ReadTextArgs(string Path);
public record ReadTextResult(string Content);

public record WriteTextArgs(string Path, string Content);
public record WriteTextResult(bool Success);

public record ExistsArgs(string Path);
public record ExistsResult(bool Exists);

public record DeleteArgs(string Path);
public record DeleteResult(bool Success);

public record ListDirectoryArgs(string Path);
public record DirectoryEntry(string Name, bool IsDirectory);
public record ListDirectoryResult(List<DirectoryEntry> Entries);

[BridgeModule("filesystem")]
public sealed class FileSystemModule : BridgeModuleBase
{
    private readonly FileSystemAccessPolicy _access;

    public FileSystemModule()
        : this([], _ => throw new InvalidOperationException("No filesystem root resolver is configured."))
    {
    }

    public FileSystemModule(
        IReadOnlyList<BridgeFileSystemGrant> grants,
        Func<string, string> resolveRoot)
    {
        _access = new FileSystemAccessPolicy(grants, resolveRoot);
    }

    [BridgeMethod("readText")]
    public async Task<ReadTextResult> ReadTextAsync(ReadTextArgs args, CancellationToken ct)
    {
        var path = _access.RequireAllowed("readText", args.Path);
        var content = await File.ReadAllTextAsync(path, ct);
        return new ReadTextResult(content);
    }

    [BridgeMethod("writeText")]
    public async Task<WriteTextResult> WriteTextAsync(WriteTextArgs args, CancellationToken ct)
    {
        var path = _access.RequireAllowed("writeText", args.Path);
        await File.WriteAllTextAsync(path, args.Content, ct);
        return new WriteTextResult(true);
    }

    [BridgeMethod("exists")]
    public Task<ExistsResult> ExistsAsync(ExistsArgs args, CancellationToken ct)
    {
        var path = _access.RequireAllowed("exists", args.Path);
        return Task.FromResult(new ExistsResult(File.Exists(path)));
    }

    [BridgeMethod("delete")]
    public Task<DeleteResult> DeleteAsync(DeleteArgs args, CancellationToken ct)
    {
        var path = _access.RequireAllowed("delete", args.Path);
        File.Delete(path);
        return Task.FromResult(new DeleteResult(true));
    }

    [BridgeMethod("listDirectory")]
    public Task<ListDirectoryResult> ListDirectoryAsync(ListDirectoryArgs args, CancellationToken ct)
    {
        var path = _access.RequireAllowed("listDirectory", args.Path);
        var entries = Directory.GetFileSystemEntries(path)
            .Select(e => new DirectoryEntry(Path.GetFileName(e), Directory.Exists(e)))
            .ToList();
        return Task.FromResult(new ListDirectoryResult(entries));
    }
}
