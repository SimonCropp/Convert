namespace Convert;

/// <summary>
/// A failure shown to the user. Unexpected exceptions also get a pre-filled GitHub issue link;
/// <see cref="UserInputException"/>s don't, since the input was the problem.
/// </summary>
public partial class ErrorPanel
{
    [Inject]
    BrowserInterop Interop { get; set; } = default!;

    /// <summary>The failure.</summary>
    [Parameter, EditorRequired]
    public Exception Exception { get; set; } = default!;

    /// <summary>What the user was doing; becomes the issue title.</summary>
    [Parameter]
    public string Action { get; set; } = "Convert";

    string? issueUrl;

    string Message => Exception switch
    {
        UserInputException => Exception.Message,
        _ => $"Something went wrong: {Exception.Message}"
    };

    protected override async Task OnParametersSetAsync()
    {
        if (Exception is UserInputException)
        {
            issueUrl = null;
            return;
        }

        string? userAgent = null;
        try
        {
            userAgent = await Interop.UserAgentAsync();
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
            // Prerendering or a test host with no JavaScript.
        }

        issueUrl = IssueLauncher.ForException(Action, Exception, IssueLauncher.Environment(userAgent));
    }
}
