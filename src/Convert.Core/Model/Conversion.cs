namespace Convert;

/// <summary>
/// One edge of the conversion graph: a source type, a target type and the tool that gets there.
/// Subclasses hold metadata only and delegate <see cref="RunAsync"/> to a runner class, so loading the
/// descriptor at startup never touches the tool's heavy assemblies.
/// </summary>
public abstract class Conversion
{
    /// <summary>Unique id, <c>{source}-{target}-{tool}</c>.</summary>
    public abstract string Id { get; }

    public abstract FileType Source { get; }

    public abstract FileType Target { get; }

    public abstract Tool Tool { get; }

    /// <summary>One line on the tool picker.</summary>
    public abstract string Summary { get; }

    /// <summary>What carries over and what doesn't.</summary>
    public virtual string? Expect => null;

    /// <summary>The transitive closure of lazy <c>.wasm</c> names this conversion needs.</summary>
    public abstract IReadOnlyList<string> Assemblies { get; }

    /// <summary>An <see cref="OptionsPanel{TOptions}"/> in the pack assembly, or null for no options.</summary>
    public virtual Type? OptionsComponent => null;

    /// <summary>Lower first when several tools serve one pair.</summary>
    public virtual int Priority => 0;

    public abstract object CreateOptions();

    public abstract Task<ConversionOutput> RunAsync(Document source, object options, IProgress<ConversionProgress> progress, Cancel cancel);

    internal IPack? Pack { get; set; }

    public override string ToString() => Id;
}

/// <summary>A <see cref="Conversion"/> with a typed options POCO (Core/BCL types only).</summary>
public abstract class Conversion<TOptions> : Conversion
    where TOptions : class, new()
{
    public override object CreateOptions() => new TOptions();

    public override Task<ConversionOutput> RunAsync(Document source, object options, IProgress<ConversionProgress> progress, Cancel cancel) =>
        RunAsync(source, (TOptions) options, progress, cancel);

    protected abstract Task<ConversionOutput> RunAsync(Document source, TOptions options, IProgress<ConversionProgress> progress, Cancel cancel);
}

/// <summary>Options POCO for conversions that have none.</summary>
public sealed class NoOptions;
