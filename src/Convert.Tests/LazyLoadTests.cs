/// <summary>
/// The part that must be right (plan §4): every entry declares the transitive closure of what it loads,
/// the csproj lazy list is exactly the union, and nothing reached at startup names a lazy assembly.
/// </summary>
public class LazyLoadTests
{
    static ConversionRegistry Registry => Fixtures.Registry;

    static readonly string runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();

    // Loaded at boot in the browser: the framework, Core and the pack glue, the icon runtime.
    static bool IsEager(AssemblyName name)
    {
        var simple = name.Name!;
        if (simple.StartsWith("Convert.", StringComparison.Ordinal) ||
            simple.StartsWith("IconifyBundle", StringComparison.Ordinal) ||
            simple.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) ||
            simple.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) ||
            simple == "Microsoft.JSInterop")
        {
            return true;
        }

        try
        {
            var location = System.Reflection.Assembly.Load(name).Location;
            return location.StartsWith(Path.GetDirectoryName(runtimeDirectory.TrimEnd(Path.DirectorySeparatorChar))!, StringComparison.OrdinalIgnoreCase) &&
                   !location.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase);
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    static IEnumerable<(string Entry, IReadOnlyList<string> Assemblies)> Entries =>
    [
        .. Registry.Conversions.Select(_ => ($"conversion {_.Id}", _.Assemblies)),
        .. Registry.Previews.Select(_ => ($"preview {_.Type.Id} {_.Component.Name}", _.Assemblies)),
        .. Registry.Editors.Select(_ => ($"editor {_.Type.Id} {_.Component.Name}", _.Assemblies))
    ];

    [Test]
    public async Task EveryEntryDeclaresItsClosure()
    {
        var problems = new List<string>();
        foreach (var (entry, assemblies) in Entries)
        {
            var declared = assemblies
                .Select(_ => _.Replace(".wasm", ""))
                .ToHashSet(StringComparer.Ordinal);
            var queue = new Queue<string>(declared);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (queue.TryDequeue(out var name))
            {
                if (!seen.Add(name))
                {
                    continue;
                }

                Assembly assembly;
                try
                {
                    assembly = System.Reflection.Assembly.Load(name);
                }
                catch (FileNotFoundException)
                {
                    problems.Add($"{entry}: declares {name}, which is not shipped");
                    continue;
                }

                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (IsEager(reference))
                    {
                        continue;
                    }

                    if (!declared.Contains(reference.Name!))
                    {
                        problems.Add($"{entry}: {name} references {reference.Name}, which is not declared");
                    }

                    queue.Enqueue(reference.Name!);
                }
            }
        }

        await Assert.That(problems.Distinct()).IsEmpty();
    }

    [Test]
    public async Task CsprojListIsTheUnionOfEveryEntry()
    {
        var csproj = XDocument.Load(Path.Combine(Fixtures.SrcDirectory, "Convert.Web", "Convert.Web.csproj"));
        var listed = csproj.Descendants("BlazorWebAssemblyLazyLoad")
            .Select(_ => (string) _.Attribute("Include")!)
            .Order(StringComparer.Ordinal)
            .ToList();
        await Assert.That(listed).IsEquivalentTo(Registry.AllAssemblies);
        await Assert.That(listed.Distinct().Count()).IsEqualTo(listed.Count);
    }

    // Mono lays a class out (and loads every assembly its fields name) on first touch, so the types touched
    // at startup may name only eager assemblies, in their bases, interfaces, fields, properties,
    // constructors and generic arguments, nested compiler-generated types included.
    [Test]
    public async Task StartupTypesTouchNoLazyAssembly()
    {
        var startup = new HashSet<Type>();
        foreach (var pack in Registry.Packs)
        {
            startup.Add(pack.GetType());
        }

        foreach (var conversion in Registry.Conversions)
        {
            startup.Add(conversion.GetType());
            startup.Add(conversion.CreateOptions().GetType());
            if (conversion.OptionsComponent is { } options)
            {
                startup.Add(options);
            }
        }

        foreach (var preview in Registry.Previews)
        {
            startup.Add(preview.Component);
        }

        foreach (var editor in Registry.Editors)
        {
            startup.Add(editor.Component);
        }

        var problems = new List<string>();
        foreach (var type in startup.SelectMany(WithNested))
        {
            foreach (var (what, named) in Named(type))
            {
                foreach (var part in Expand(named))
                {
                    if (!IsEager(part.Assembly.GetName()))
                    {
                        problems.Add($"{type.FullName} {what} names {part.FullName} from {part.Assembly.GetName().Name}");
                    }
                }
            }
        }

        await Assert.That(problems.Distinct()).IsEmpty();
    }

    static IEnumerable<Type> WithNested(Type type) =>
        [type, .. type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(WithNested)];

    const BindingFlags all = BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.Instance |
                             BindingFlags.Static |
                             BindingFlags.DeclaredOnly;

    static IEnumerable<(string What, Type Named)> Named(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            yield return ("base type", baseType);
        }

        foreach (var contract in type.GetInterfaces())
        {
            yield return ("interface", contract);
        }

        foreach (var field in type.GetFields(all))
        {
            yield return ($"field {field.Name}", field.FieldType);
        }

        foreach (var property in type.GetProperties(all))
        {
            yield return ($"property {property.Name}", property.PropertyType);
        }

        foreach (var constructor in type.GetConstructors(all))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return ($"constructor parameter {parameter.Name}", parameter.ParameterType);
            }
        }
    }

    static IEnumerable<Type> Expand(Type type)
    {
        while (true)
        {
            if (type.HasElementType)
            {
                type = type.GetElementType()!;
                continue;
            }

            if (type.IsGenericParameter)
            {
                yield break;
            }

            yield return type;
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var expanded in Expand(argument))
                {
                    yield return expanded;
                }
            }

            break;
        }
    }

    // Only runners, previews and editors may name a tool's namespace.
    [Test]
    public async Task StartupSourceHasNoHeavyUsings()
    {
        string[] heavy = ["Morph", "Naiad", "GeoConvert", "Markdig", "OpenXmlHtml", "OfficeIMO", "SixLabors", "DocumentFormat", "AngleSharp", "SkiaSharp", "Svg", "YamlDotNet", "Tomlyn"];
        var problems = new List<string>();
        foreach (var project in Directory.GetDirectories(Fixtures.SrcDirectory, "Convert.*"))
        {
            var files = Directory.GetFiles(project, "*Pack.cs")
                .Concat(Folder(project, "Conversions"))
                .Concat(Folder(project, "Options"))
                .Concat(Folder(project, "Sniffers"));
            foreach (var file in files)
            {
                foreach (var line in await File.ReadAllLinesAsync(file))
                {
                    var trimmed = line.Trim();
                    if (heavy.Any(_ => trimmed.StartsWith($"using {_};", StringComparison.Ordinal) ||
                                       trimmed.StartsWith($"using {_}.", StringComparison.Ordinal) ||
                                       trimmed.StartsWith($"@using {_}", StringComparison.Ordinal) ||
                                       trimmed.StartsWith($"@using global::{_}", StringComparison.Ordinal)))
                    {
                        problems.Add($"{Path.GetRelativePath(Fixtures.SrcDirectory, file)}: {trimmed}");
                    }
                }
            }
        }

        await Assert.That(problems).IsEmpty();
    }

    static IEnumerable<string> Folder(string project, string name)
    {
        var path = Path.Combine(project, name);
        if (Directory.Exists(path))
        {
            return Directory
                .GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(_ => _.EndsWith(".cs") || _.EndsWith(".razor"));
        }

        return [];
    }
}
