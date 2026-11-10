/// <summary>Loads the lazy assemblies an entry needs before it runs or renders.</summary>
public interface IToolLoader
{
    /// <summary>
    /// Loads every name in <paramref name="assemblies"/> not yet loaded (concurrent calls join), then
    /// calls <paramref name="pack"/>'s <see cref="IPack.Loaded"/> if this is its first load.
    /// </summary>
    Task EnsureAsync(IReadOnlyList<string> assemblies, IPack? pack);

    /// <summary>Names currently downloading, for the "Loading tool…" indicator.</summary>
    IReadOnlyCollection<string> Pending { get; }

    /// <summary>Raised when <see cref="Pending"/> changes.</summary>
    event Action? Changed;
}
