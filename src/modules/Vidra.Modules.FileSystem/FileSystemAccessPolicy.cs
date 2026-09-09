using Vidra.Bridge;

namespace Vidra.Modules.FileSystem;

internal sealed class FileSystemAccessPolicy
{
    private readonly IReadOnlyList<ResolvedGrant> _grants;
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public FileSystemAccessPolicy(
        IReadOnlyList<BridgeFileSystemGrant> grants,
        Func<string, string> resolveRoot)
    {
        _grants = grants.Select(grant => ResolveGrant(grant, resolveRoot)).ToArray();
    }

    public string RequireAllowed(string method, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new FileSystemPathDeniedException(path);

        var candidate = Path.GetFullPath(path);
        foreach (var grant in _grants)
        {
            if (!grant.Methods.Contains(method, StringComparer.OrdinalIgnoreCase)
                || !IsWithin(candidate, grant.InputRoot))
            {
                continue;
            }

            var resolved = ResolveExistingSegments(
                grant.InputRoot,
                grant.Root,
                candidate);
            if (IsWithin(resolved, grant.Root))
                return candidate;
        }

        throw new FileSystemPathDeniedException(path);
    }

    private static ResolvedGrant ResolveGrant(
        BridgeFileSystemGrant grant,
        Func<string, string> resolveRoot)
    {
        var basePath = resolveRoot(grant.Root);

        var root = Path.GetFullPath(basePath);
        if (!string.IsNullOrEmpty(grant.RelativePath))
        {
            root = Path.GetFullPath(Path.Combine(
                root,
                grant.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsWithin(root, Path.GetFullPath(basePath)))
                throw new InvalidOperationException("Filesystem scope escaped its symbolic root.");
        }

        var inputRoot = TrimEndingSeparator(root);
        return new ResolvedGrant(
            inputRoot,
            TrimEndingSeparator(ResolveAbsolutePath(inputRoot)),
            grant.Methods.Select(method => method.Member).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    private static string ResolveExistingSegments(
        string inputRoot,
        string resolvedRoot,
        string candidate,
        bool enforceContainment = true)
    {
        var relative = Path.GetRelativePath(inputRoot, candidate);
        if (relative == ".")
            return resolvedRoot;

        var current = resolvedRoot;
        var segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < segments.Length; index++)
        {
            current = Path.Combine(current, segments[index]);
            FileSystemInfo? info = Directory.Exists(current)
                ? new DirectoryInfo(current)
                : File.Exists(current)
                    ? new FileInfo(current)
                    : null;

            if (info is null)
            {
                for (var rest = index + 1; rest < segments.Length; rest++)
                    current = Path.Combine(current, segments[rest]);
                return Path.GetFullPath(current);
            }

            if (info.LinkTarget is not null)
            {
                var target = info.ResolveLinkTarget(returnFinalTarget: true)
                    ?? throw new FileSystemPathDeniedException(candidate);
                current = Path.GetFullPath(target.FullName);
                if (enforceContainment && !IsWithin(current, resolvedRoot))
                    throw new FileSystemPathDeniedException(candidate);
            }
        }

        return Path.GetFullPath(current);
    }

    private static string ResolveAbsolutePath(string path)
    {
        var full = Path.GetFullPath(path);
        var volumeRoot = Path.GetPathRoot(full)
            ?? throw new FileSystemPathDeniedException(path);
        if (string.Equals(full, volumeRoot, PathComparison))
            return full;

        var relative = Path.GetRelativePath(volumeRoot, full);
        return ResolveExistingSegments(
            volumeRoot,
            volumeRoot,
            Path.Combine(volumeRoot, relative),
            enforceContainment: false);
    }

    private static bool IsWithin(string candidate, string root)
    {
        var normalizedRoot = TrimEndingSeparator(Path.GetFullPath(root));
        var normalizedCandidate = Path.GetFullPath(candidate);
        return string.Equals(normalizedCandidate, normalizedRoot, PathComparison)
            || normalizedCandidate.StartsWith(
                normalizedRoot + Path.DirectorySeparatorChar,
                PathComparison);
    }

    private static string TrimEndingSeparator(string value)
        => Path.TrimEndingDirectorySeparator(value);

    private sealed record ResolvedGrant(
        string InputRoot,
        string Root,
        HashSet<string> Methods);
}

internal sealed class FileSystemPathDeniedException(string path)
    : BridgeInvocationException(
        "FILESYSTEM_PATH_DENIED",
        $"Filesystem access to '{path}' is outside the configured scope.");
