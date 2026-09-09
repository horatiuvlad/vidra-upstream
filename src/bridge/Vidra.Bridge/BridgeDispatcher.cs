using System.Text.Json;

namespace Vidra.Bridge;

/// <summary>
/// Routes incoming JS messages to the correct <see cref="IBridgeModule"/>
/// and serializes the response back.
/// </summary>
public sealed class BridgeDispatcher
{
    private readonly Dictionary<string, IBridgeModule> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _events = new(StringComparer.OrdinalIgnoreCase);
    private readonly IBridgeAccessPolicy _accessPolicy;

    public BridgeDispatcher(IBridgeAccessPolicy? accessPolicy = null)
    {
        _accessPolicy = accessPolicy ?? BridgeAccessPolicy.DenyAll;
    }

    public IBridgeAccessPolicy AccessPolicy => _accessPolicy;

    public void Register(IBridgeModule module)
    {
        _modules[module.ModuleName] = module;
    }

    /// <summary>
    /// The registered modules, exposed so the host can wire cross-cutting
    /// concerns such as attaching the JS callback channel to every
    /// <see cref="IBridgeEventSource"/>.
    /// </summary>
    public IReadOnlyCollection<IBridgeModule> Modules => _modules.Values.ToList();

    public void RegisterEvents(string contract, params string[] members)
    {
        if (!_events.TryGetValue(contract, out var registered))
        {
            registered = new HashSet<string>(StringComparer.Ordinal);
            _events[contract] = registered;
        }

        foreach (var member in members)
            registered.Add(member);
    }

    public BridgeCapabilities GetCapabilities()
    {
        var contractNames = _modules.Keys
            .Concat(_events.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var contracts = new Dictionary<string, NativeContractCapabilities>(StringComparer.Ordinal);

        foreach (var contract in contractNames.OrderBy(name => name, StringComparer.Ordinal))
        {
            var methods = _modules.TryGetValue(contract, out var module)
                ? module.SupportedMethods
                    .Where(member => _accessPolicy.AllowsNativeMethod(contract, member))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()
                : [];
            var contractEvents = _events.TryGetValue(contract, out var events)
                ? events
                    .Where(member => _accessPolicy.AllowsEvent(contract, member))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()
                : [];
            if (methods.Length > 0 || contractEvents.Length > 0)
            {
                contracts[contract] = new NativeContractCapabilities
                {
                    Methods = methods,
                    Events = contractEvents,
                };
            }
        }

        return new BridgeCapabilities
        {
            ProtocolVersion = BridgeProtocol.Version,
            AccessFingerprint = _accessPolicy.Fingerprint,
            NativeContracts = contracts,
        };
    }

    /// <summary>
    /// Fails host startup when a compiled grant no longer names a registered
    /// module member. A stale permission must not look like a working policy.
    /// </summary>
    public void ValidateAccessPolicy()
    {
        var native = _accessPolicy.Document.NativeMethods
            .Concat(_accessPolicy.Document.FileSystem.SelectMany(grant => grant.Methods));
        foreach (var grant in native)
        {
            if (!_modules.TryGetValue(grant.Contract, out var module)
                || !module.SupportedMethods.Contains(grant.Member, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Bridge access policy grants unknown native method '{grant.Contract}.{grant.Member}'.");
            }
        }

        foreach (var grant in _accessPolicy.Document.Events)
        {
            if (!_events.TryGetValue(grant.Contract, out var members)
                || !members.Contains(grant.Member))
            {
                throw new InvalidOperationException(
                    $"Bridge access policy grants unknown event '{grant.Contract}.{grant.Member}'.");
            }
        }
    }

    /// <summary>
    /// Receive a raw JSON string from the WebView, dispatch it, and return a JSON response string.
    /// </summary>
    public async Task<string> DispatchAsync(string rawJson, CancellationToken ct = default)
    {
        BridgeRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(rawJson, BridgeSerializer.Default);
        }
        catch (JsonException ex)
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = "unknown",
                Success = false,
                Error = new BridgeError { Code = "PARSE_ERROR", Message = ex.Message },
            });
        }

        if (request is null)
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = "unknown",
                Success = false,
                Error = new BridgeError { Code = "PARSE_ERROR", Message = "Request was null." },
            });
        }

        if (request.Contract == "__bridge" && request.Member == "capabilities")
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = true,
                Data = GetCapabilities(),
            });
        }

        if (request.Contract == "__bridge")
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = false,
                Error = new BridgeError
                {
                    Code = "BRIDGE_META_NOT_FOUND",
                    Message = $"No bridge meta member '{request.Member}' exists.",
                },
            });
        }

        if (!_accessPolicy.AllowsNativeMethod(request.Contract, request.Member))
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = false,
                Error = new BridgeError
                {
                    Code = "NATIVE_ACCESS_DENIED",
                    Message = $"Native access to '{request.Contract}.{request.Member}' is not granted.",
                },
            });
        }

        if (!_modules.TryGetValue(request.Contract, out var module))
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = false,
                Error = new BridgeError
                {
                    Code = "NATIVE_CONTRACT_NOT_FOUND",
                    Message = $"No native contract '{request.Contract}' is registered.",
                },
            });
        }

        try
        {
            var payload = request.Payload.HasValue ? new JsonPayload(request.Payload.Value) : null;
            var result = await module.HandleAsync(request.Member, payload, ct);

            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = true,
                Data = result,
            });
        }
        catch (BridgeInvocationException ex)
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = false,
                Error = new BridgeError { Code = ex.Code, Message = ex.Message },
            });
        }
        catch (Exception ex)
        {
            return BridgeSerializer.Serialize(new BridgeResponse
            {
                Id = request.Id,
                Success = false,
                Error = new BridgeError { Code = "NATIVE_MEMBER_ERROR", Message = ex.Message },
            });
        }
    }
}
