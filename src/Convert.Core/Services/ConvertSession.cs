namespace Convert;

/// <summary>
/// The wizard's state: the source file, the chosen target and tool, its options and the results. The
/// page and the Core components bind to it and re-render on <see cref="Changed"/>.
/// </summary>
public sealed class ConvertSession(ConversionRegistry registry, IToolLoader loader) : IDisposable
{
    Dictionary<string, ConversionOutput> results = new(StringComparer.Ordinal);
    Dictionary<string, object> options = new(StringComparer.Ordinal);
    CancelSource? work;

    public ConversionRegistry Registry => registry;

    public Document? Source { get; private set; }

    public FileType? Target { get; private set; }

    public Conversion? Conversion { get; private set; }

    /// <summary>The selected conversion's options, kept per conversion while the source stays.</summary>
    public object? Options =>
        Conversion is null ? null : OptionsFor(Conversion);

    /// <summary>The selected conversion's result, if it has run for this source and these options.</summary>
    public ConversionOutput? Result =>
        Conversion is not null && results.TryGetValue(Conversion.Id, out var output) ? output : null;

    public IReadOnlyDictionary<string, ConversionOutput> ResultCache => results;

    public Exception? Error { get; private set; }

    public bool IsConverting { get; private set; }

    public ConversionProgress? Progress { get; private set; }

    public event Action? Changed;

    /// <summary>Opens a file by name and bytes (an upload, a drop or a sample).</summary>
    public Task LoadAsync(string fileName, byte[] bytes)
    {
        var type = registry.Detect(fileName, bytes);
        if (type is null)
        {
            Clear();
            Error = new UserInputException($"'{fileName}' is not a format Convert reads. Supported: {registry.Accept}");
            Raise();
            return Task.CompletedTask;
        }

        return SetSourceAsync(new(fileName, bytes, type));
    }

    /// <summary>Opens pasted text as a document of <paramref name="type"/>.</summary>
    public Task LoadTextAsync(FileType type, string text) =>
        SetSourceAsync(Document.FromText("pasted" + type.Extension, text, type));

    /// <summary>Replaces the source with an edited version, dropping every cached result.</summary>
    public Task ReplaceSourceAsync(Document edited)
    {
        CancelWork();
        Source = edited;
        results.Clear();
        Error = null;
        Raise();
        return Task.CompletedTask;
    }

    Task SetSourceAsync(Document document)
    {
        Clear();
        Source = document;
        Raise();
        return Task.CompletedTask;
    }

    public void SelectTarget(FileType target)
    {
        if (Source is null)
        {
            return;
        }

        Target = target;
        Conversion = registry.TargetsFor(Source.Type)
            .SelectMany(_ => _.Targets)
            .FirstOrDefault(_ => _.Target == target)?
            .Conversions[0];
        Error = null;
        Raise();
    }

    public void SelectConversion(Conversion conversion)
    {
        Target = conversion.Target;
        Conversion = conversion;
        Error = null;
        Raise();
    }

    /// <summary>Called by an options panel: the result for the old options no longer applies.</summary>
    public void OptionsChanged()
    {
        if (Conversion is not null)
        {
            results.Remove(Conversion.Id);
        }

        Raise();
    }

    public object OptionsFor(Conversion conversion)
    {
        if (!options.TryGetValue(conversion.Id, out var value))
        {
            value = conversion.CreateOptions();
            options[conversion.Id] = value;
        }

        return value;
    }

    public async Task ConvertAsync()
    {
        if (Source is not { } source ||
            Conversion is not { } conversion)
        {
            return;
        }

        var conversionOptions = OptionsFor(conversion);
        CancelWork();
        var current = new CancelSource();
        work = current;
        var cancel = current.Token;

        IsConverting = true;
        Error = null;
        Progress = new("Loading tool");
        Raise();

        try
        {
            await loader.EnsureAsync(conversion.Assemblies, conversion.Pack);
            Report(new("Converting", conversion.Tool.DisplayName));
            // Hands the thread back first so the progress state paints before the compute starts.
            var output = await Task.Run(() => conversion.RunAsync(source, conversionOptions, new ProgressSink(this), cancel), cancel);
            if (!cancel.IsCancellationRequested && ReferenceEquals(Source, source))
            {
                results[conversion.Id] = output;
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(work, current))
            {
                Error = exception;
            }
        }
        finally
        {
            if (ReferenceEquals(work, current))
            {
                IsConverting = false;
                Progress = null;
                work = null;
            }

            current.Dispose();
            Raise();
        }
    }

    public void Cancel()
    {
        CancelWork();
        IsConverting = false;
        Progress = null;
        Raise();
    }

    public void Reset()
    {
        Clear();
        Raise();
    }

    void Clear()
    {
        CancelWork();
        Source = null;
        Target = null;
        Conversion = null;
        Error = null;
        IsConverting = false;
        Progress = null;
        results.Clear();
        options.Clear();
    }

    void CancelWork()
    {
        if (work is null)
        {
            return;
        }

        work.Cancel();
        work = null;
    }

    void Report(ConversionProgress progress)
    {
        Progress = progress;
        Raise();
    }

    void Raise() => Changed?.Invoke();

    public void Dispose() => CancelWork();

    sealed class ProgressSink(ConvertSession session) : IProgress<ConversionProgress>
    {
        public void Report(ConversionProgress value) => session.Report(value);
    }
}

/// <summary>
/// A failure caused by the input (a malformed file, an unsupported construct) rather than a bug: shown
/// to the user without an invitation to file an issue.
/// </summary>
public sealed class UserInputException(string message, Exception? inner = null) : Exception(message, inner);
