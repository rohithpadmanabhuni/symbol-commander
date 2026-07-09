using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Tests;

public class KeystrokeParserTests
{
    [Fact]
    public void Parses_single_combo()
    {
        var combos = KeystrokeParser.Parse("Ctrl+W");
        var c = Assert.Single(combos);
        Assert.Equal(new[] { "Ctrl" }, c.Modifiers);
        Assert.Equal("W", c.Key);
    }

    [Fact]
    public void Parses_sequence_of_combos()
    {
        var combos = KeystrokeParser.Parse("Ctrl+K, Ctrl+S");
        Assert.Equal(2, combos.Count);
        Assert.Equal("K", combos[0].Key);
        Assert.Equal("S", combos[1].Key);
    }

    [Fact]
    public void Is_case_insensitive_and_trims_whitespace()
    {
        var c = Assert.Single(KeystrokeParser.Parse("  ctrl + shift + t "));
        Assert.Equal(new[] { "Ctrl", "Shift" }, c.Modifiers);
        Assert.Equal("T", c.Key);
    }

    [Fact]
    public void Bare_key_without_modifiers_is_valid()
    {
        var c = Assert.Single(KeystrokeParser.Parse("F5"));
        Assert.Empty(c.Modifiers);
        Assert.Equal("F5", c.Key);
    }

    [Fact]
    public void Named_keys_normalize_casing()
    {
        Assert.Equal("PageDown", KeystrokeParser.Parse("ctrl+pagedown")[0].Key);
        Assert.Equal("Enter", KeystrokeParser.Parse("ENTER")[0].Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Foo+X")]
    [InlineData("Ctrl+NotAKey")]
    [InlineData("Ctrl+Shift")]  // modifier alone is not a key
    public void Invalid_specs_throw_FormatException(string spec)
    {
        Assert.Throws<FormatException>(() => KeystrokeParser.Parse(spec));
    }

    [Fact]
    public void Win_modifier_is_supported()
    {
        var c = Assert.Single(KeystrokeParser.Parse("Win+D"));
        Assert.Equal(new[] { "Win" }, c.Modifiers);
        Assert.Equal("D", c.Key);
    }
}
