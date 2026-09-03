// Copied from SignFlow ProtectedValues (Codex_SignFlow) — TagEkyc-owned fork;
// do not add a SignFlow project reference.
using System.Text.RegularExpressions;

namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class ProtectedValueProviderRegistration
{
    internal ProtectedValueProviderRegistration(
        IProtectedValueProvider provider,
        IEnumerable<string>? bootstrapProviderSchemes = null)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        BootstrapProviderSchemes = (bootstrapProviderSchemes ?? [])
            .ToHashSet(StringComparer.Ordinal);
    }

    internal IProtectedValueProvider Provider { get; }

    internal IReadOnlySet<string> BootstrapProviderSchemes { get; }
}

internal sealed class ProtectedValueProviderRegistry
{
    private static readonly Regex SchemeGrammar = new(
        "^[a-z][a-z0-9-]{0,31}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private readonly IReadOnlyDictionary<string, IProtectedValueProvider>
        _providers;

    internal ProtectedValueProviderRegistry(
        IEnumerable<ProtectedValueProviderRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        var registrationList = registrations.ToArray();
        var byScheme =
            new Dictionary<string, ProtectedValueProviderRegistration>(
                StringComparer.Ordinal);

        foreach (var registration in registrationList)
        {
            var scheme = registration.Provider.Scheme;
            if (string.IsNullOrEmpty(scheme)
                || !SchemeGrammar.IsMatch(scheme))
            {
                throw new InvalidOperationException(
                    "Protected value provider scheme is invalid.");
            }

            if (!byScheme.TryAdd(scheme, registration))
            {
                throw new InvalidOperationException(
                    $"Duplicate protected value provider scheme: {scheme}.");
            }
        }

        ValidateDependencyGraph(byScheme);
        _providers = byScheme.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Provider,
            StringComparer.Ordinal);
    }

    internal static ProtectedValueProviderRegistry CreateTerminal(
        IEnumerable<IProtectedValueProvider> providers) =>
        new(
            providers.Select(
                provider =>
                    new ProtectedValueProviderRegistration(provider)));

    internal bool TryGet(
        string scheme,
        out IProtectedValueProvider? provider) =>
        _providers.TryGetValue(scheme, out provider);

    private static void ValidateDependencyGraph(
        IReadOnlyDictionary<string, ProtectedValueProviderRegistration>
            registrations)
    {
        foreach (var registration in registrations.Values)
        {
            foreach (var dependency in registration.BootstrapProviderSchemes)
            {
                if (!registrations.ContainsKey(dependency))
                {
                    throw new InvalidOperationException(
                        "Unknown protected value bootstrap provider scheme: " +
                        $"{dependency}.");
                }
            }
        }

        var state = new Dictionary<string, VisitState>(
            StringComparer.Ordinal);
        var path = new List<string>();
        foreach (var scheme in registrations.Keys.Order(StringComparer.Ordinal))
        {
            Visit(scheme, registrations, state, path);
        }
    }

    private static void Visit(
        string scheme,
        IReadOnlyDictionary<string, ProtectedValueProviderRegistration>
            registrations,
        IDictionary<string, VisitState> state,
        IList<string> path)
    {
        if (state.TryGetValue(scheme, out var existing))
        {
            if (existing == VisitState.Visited)
            {
                return;
            }

            var cycleStart = path.IndexOf(scheme);
            var cycle = path.Skip(cycleStart).Append(scheme);
            throw new InvalidOperationException(
                "Protected value bootstrap provider cycle: " +
                $"{string.Join(" -> ", cycle)}.");
        }

        state[scheme] = VisitState.Visiting;
        path.Add(scheme);
        foreach (var dependency in registrations[scheme]
                     .BootstrapProviderSchemes
                     .Order(StringComparer.Ordinal))
        {
            Visit(dependency, registrations, state, path);
        }

        path.RemoveAt(path.Count - 1);
        state[scheme] = VisitState.Visited;
    }

    private enum VisitState
    {
        Visiting,
        Visited,
    }
}
