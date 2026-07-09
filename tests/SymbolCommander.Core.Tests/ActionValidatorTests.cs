using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Tests;

public class ActionValidatorTests
{
    private static ActionDefinition Make(ActionType type, params (string k, string v)[] ps) => new()
    {
        Name = "test",
        Type = type,
        Parameters = ps.ToDictionary(p => p.k, p => p.v),
    };

    [Fact]
    public void Valid_keystroke_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.Keystroke, ("keys", "Ctrl+W"))));

    [Fact]
    public void Keystroke_with_bad_spec_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Keystroke, ("keys", "Nope+X"))));

    [Fact]
    public void Keystroke_missing_parameter_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Keystroke)));

    [Fact]
    public void Valid_launch_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.Launch, ("target", "https://example.com"))));

    [Fact]
    public void Launch_with_empty_target_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Launch, ("target", "  "))));

    [Fact]
    public void Valid_window_media_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.WindowMedia, ("command", "VolumeUp"))));

    [Fact]
    public void Unknown_window_media_command_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.WindowMedia, ("command", "Reboot"))));

    [Fact]
    public void Valid_shell_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.Shell, ("commandLine", "echo hi"))));

    [Fact]
    public void Shell_with_empty_command_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Shell, ("commandLine", ""))));

    [Fact]
    public void Blank_name_fails()
    {
        var a = Make(ActionType.Launch, ("target", "x"));
        a.Name = "";
        Assert.NotNull(ActionValidator.Validate(a));
    }
}
