using Vidra.Bridge;

namespace Vidra.Bridge.Tests;

public sealed class BridgeAccessPolicyTests
{
    [Fact]
    public void DenyAll_Uses_The_Standard_Empty_Policy_Bytes()
    {
        BridgeAccessPolicy.DenyAll.Fingerprint.Should().Be(
            "3ac090de6154b840ef518755f7cf4e16b5860c26805d60cb878574b086947387");
    }

    [Fact]
    public void Parse_Fingerprints_Exact_Utf8_Bytes_With_Unicode()
    {
        var bytes = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "contract",
            "fixtures",
            "bridge-policy.unicode.json"));

        var policy = BridgeAccessPolicy.Parse(bytes);

        policy.Fingerprint.Should().Be(
            "1f592f6d8340ab89ca6e0c3e35193c87a017410a840df2fb333aaf05bbd61c36");
        policy.Document.FileSystem.Single().RelativePath.Should().Be("crème/notes&data");
    }

    [Fact]
    public void Native_And_Event_Grants_Are_Independent()
    {
        var policy = new BridgeAccessPolicy(new BridgePolicyDocument
        {
            NativeMethods = [new("clipboard", "getText")],
            Events = [new("appWindow", "resized")],
        });

        policy.AllowsNativeMethod("CLIPBOARD", "getText").Should().BeTrue();
        policy.AllowsNativeMethod("clipboard", "setText").Should().BeFalse();
        policy.AllowsEvent("appWindow", "resized").Should().BeTrue();
        policy.AllowsEvent("appWindow", "stateChanged").Should().BeFalse();
    }

    [Fact]
    public void Filesystem_Scopes_Also_Grant_Their_Selected_Methods()
    {
        var policy = new BridgeAccessPolicy(new BridgePolicyDocument
        {
            FileSystem =
            [
                new BridgeFileSystemGrant
                {
                    Root = "appData",
                    Methods = [new("filesystem", "readText")],
                },
            ],
        });

        policy.AllowsNativeMethod("filesystem", "readText").Should().BeTrue();
        policy.AllowsNativeMethod("filesystem", "writeText").Should().BeFalse();
    }

    [Fact]
    public void Validation_Rejects_Stale_Grants()
    {
        var dispatcher = new BridgeDispatcher(new BridgeAccessPolicy(new BridgePolicyDocument
        {
            NativeMethods = [new("missing", "method")],
        }));

        var validate = dispatcher.ValidateAccessPolicy;

        validate.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing.method*");
    }
}
