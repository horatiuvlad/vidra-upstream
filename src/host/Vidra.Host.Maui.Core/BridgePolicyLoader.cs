using Vidra.Bridge;

namespace Vidra.Hosting;

internal static class BridgePolicyLoader
{
    private const string ResourceName = "Vidra.Bridge.Policy";

    public static BridgeAccessPolicy Load()
    {
        var matches = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic
                && assembly.GetManifestResourceNames().Contains(ResourceName, StringComparer.Ordinal))
            .ToArray();

        if (matches.Length == 0)
            return BridgeAccessPolicy.DenyAll;
        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple assemblies contain the '{ResourceName}' bridge policy resource.");
        }

        using var stream = matches[0].GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Assembly '{matches[0].GetName().Name}' advertised an unreadable bridge policy.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return BridgeAccessPolicy.Parse(buffer.ToArray());
    }
}
