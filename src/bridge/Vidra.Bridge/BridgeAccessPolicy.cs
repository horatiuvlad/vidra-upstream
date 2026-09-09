using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Vidra.Bridge;

public sealed record BridgeMemberGrant(
    [property: JsonPropertyName("contract")] string Contract,
    [property: JsonPropertyName("member")] string Member);

public sealed class BridgeFileSystemGrant
{
    [JsonPropertyName("root")]
    public required string Root { get; init; }

    [JsonPropertyName("relativePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RelativePath { get; init; }

    [JsonPropertyName("methods")]
    public IReadOnlyList<BridgeMemberGrant> Methods { get; init; } = [];
}

public sealed class BridgePolicyDocument
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    [JsonPropertyName("nativeMethods")]
    public IReadOnlyList<BridgeMemberGrant> NativeMethods { get; init; } = [];

    [JsonPropertyName("events")]
    public IReadOnlyList<BridgeMemberGrant> Events { get; init; } = [];

    [JsonPropertyName("filesystem")]
    public IReadOnlyList<BridgeFileSystemGrant> FileSystem { get; init; } = [];
}

public interface IBridgeAccessPolicy
{
    BridgePolicyDocument Document { get; }
    string Fingerprint { get; }
    bool AllowsNativeMethod(string contract, string member);
    bool AllowsEvent(string contract, string member);
}

/// <summary>
/// Immutable access policy compiled from vidra.config.ts by the Vidra CLI.
/// </summary>
public sealed class BridgeAccessPolicy : IBridgeAccessPolicy
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private readonly HashSet<string> _nativeMethods;
    private readonly HashSet<string> _events;

    private const string EmptyPolicyJson =
        "{\n"
        + "  \"version\": 1,\n"
        + "  \"nativeMethods\": [],\n"
        + "  \"events\": [],\n"
        + "  \"filesystem\": []\n"
        + "}\n";

    public static BridgeAccessPolicy DenyAll { get; } = Parse(EmptyPolicyJson);

    public BridgeAccessPolicy(BridgePolicyDocument document)
        : this(document, fingerprint: null)
    {
    }

    private BridgeAccessPolicy(
        BridgePolicyDocument document,
        string? fingerprint)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Version != 1)
            throw new InvalidOperationException($"Unsupported bridge policy version {document.Version}.");

        Document = new BridgePolicyDocument
        {
            Version = document.Version,
            NativeMethods = document.NativeMethods
                .Select(CopyMember)
                .ToArray(),
            Events = document.Events
                .Select(CopyMember)
                .ToArray(),
            FileSystem = document.FileSystem
                .Select(grant => new BridgeFileSystemGrant
                {
                    Root = grant.Root,
                    RelativePath = grant.RelativePath,
                    Methods = grant.Methods.Select(CopyMember).ToArray(),
                })
                .ToArray(),
        };
        _nativeMethods = Document.NativeMethods
            .Concat(Document.FileSystem.SelectMany(grant => grant.Methods))
            .Select(Key)
            .ToHashSet(Comparer);
        _events = Document.Events.Select(Key).ToHashSet(Comparer);

        Fingerprint = fingerprint ?? Hash(Encoding.UTF8.GetBytes(
            BridgeSerializer.Serialize(Document)));
    }

    public BridgePolicyDocument Document { get; }
    public string Fingerprint { get; }

    public bool AllowsNativeMethod(string contract, string member)
        => _nativeMethods.Contains(Key(contract, member));

    public bool AllowsEvent(string contract, string member)
        => _events.Contains(Key(contract, member));

    public static BridgeAccessPolicy Parse(string json)
    {
        var document = BridgeSerializer.Deserialize<BridgePolicyDocument>(json)
            ?? throw new InvalidOperationException("Bridge policy was empty.");
        return new BridgeAccessPolicy(
            document,
            Hash(Encoding.UTF8.GetBytes(json)));
    }

    public static BridgeAccessPolicy Parse(ReadOnlySpan<byte> utf8Json)
    {
        var document = BridgeSerializer.Deserialize<BridgePolicyDocument>(
            Encoding.UTF8.GetString(utf8Json))
            ?? throw new InvalidOperationException("Bridge policy was empty.");
        return new BridgeAccessPolicy(document, Hash(utf8Json));
    }

    private static string Key(BridgeMemberGrant grant)
        => Key(grant.Contract, grant.Member);

    private static BridgeMemberGrant CopyMember(BridgeMemberGrant grant)
        => new(grant.Contract, grant.Member);

    private static string Key(string contract, string member)
        => $"{contract}\0{member}";

    private static string Hash(ReadOnlySpan<byte> value)
        => Convert.ToHexStringLower(SHA256.HashData(value));
}
