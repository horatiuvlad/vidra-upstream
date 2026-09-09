namespace Vidra.Hosting;

internal static class BridgeFileSystemRoots
{
    public static string Resolve(string root)
        => root switch
        {
            "appData" => Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory,
            "cache" => Microsoft.Maui.Storage.FileSystem.Current.CacheDirectory,
            "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "downloads" => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"),
            _ => throw new InvalidOperationException(
                $"Unknown filesystem root '{root}'."),
        };
}
