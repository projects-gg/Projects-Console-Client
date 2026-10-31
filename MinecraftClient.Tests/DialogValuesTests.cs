using MinecraftClient.Dialogs;

namespace MinecraftClient.Tests;

/// <summary>
/// Client 4.2.5: a dialog's values are written as the game writes them (Minecraft's InputControlHandlers,
/// StringTag.escapeWithoutQuotes, StringTemplate and NumberRangeInput.RangeInfo), so a press sends what the game sends.
/// </summary>
public sealed class DialogValuesTests
{
    [Theory]
    [InlineData("Kaan_07", "Kaan_07")]
    [InlineData("Ka\"an\\", "Ka\\\"an\\\\")]
    [InlineData("don't", "don\\'t")]
    [InlineData("a\nb\tc\rd\be\ff", "a\\nb\\tc\\rd\\be\\ff")]
    [InlineData("x\u0001y\u001Fz", "x\\x01y\\x1Fz")]
    [InlineData("Gece Kurtları ✦", "Gece Kurtları ✦")]
    public void ATextIsEscapedAsTheGameEscapesIt(string value, string escaped)
    {
        Assert.Equal(escaped, DialogValues.EscapeText(value));
    }

    [Theory]
    [InlineData(0.5F, "0.5")]
    [InlineData(2.5F, "2.5")]
    [InlineData(100F, "100.0")]
    [InlineData(-2.25F, "-2.25")]
    [InlineData(0.001F, "0.001")]
    [InlineData(0.0001F, "1.0E-4")]
    [InlineData(1.5E-5F, "1.5E-5")]
    [InlineData(1234567.5F, "1234567.5")]
    [InlineData(12345678F, "1.2345678E7")]
    [InlineData(0.1F, "0.1")]
    [InlineData(0.3F, "0.3")]
    public void ANumberIsWrittenAsJavaWritesAFloat(float value, string text)
    {
        Assert.Equal(text, DialogValues.JavaFloat(value));
    }

    [Theory]
    [InlineData(5F, "5")]
    [InlineData(-3F, "-3")]
    [InlineData(2.5F, "2.5")]
    [InlineData(0.0001F, "1.0E-4")]
    public void ASliderSendsAWholeNumberAsAnInteger(float value, string text)
    {
        Assert.Equal(text, DialogValues.SliderText(value));
    }

    [Theory]
    [InlineData(0.00001F, "0.00001")]
    [InlineData(2.5F, "2.5")]
    [InlineData(-0.125F, "-0.125")]
    public void APassedValueIsAPlainDecimal(float value, string text)
    {
        Assert.Equal(text, DialogValues.Plain(value));
    }

    [Fact]
    public void ATemplateIsFilledInOnceAsTheGameFillsIt()
    {
        Dictionary<string, string> values = new() { ["a"] = "$(b)", ["b"] = "x" };
        // A value is never read as a template, an unknown variable is nothing, a $ without ( stays.
        Assert.Equal("/say $(b) x", DialogValues.FillTemplate("/say $(a) $(b)", values));
        Assert.Equal("/say  x", DialogValues.FillTemplate("/say $(nope) $(b)", values));
        Assert.Equal("/pay 5$ x", DialogValues.FillTemplate("/pay 5$ $(b)", values));
        Assert.Equal("x$", DialogValues.FillTemplate("$(b)$", values));
        // The game refuses these templates: no variable, a $( without ), a name of other characters.
        Assert.Null(DialogValues.FillTemplate("/spawn", values));
        Assert.Null(DialogValues.FillTemplate("/say $(a", values));
        Assert.Null(DialogValues.FillTemplate("/say $(a-b)", values));
        // An empty name is the game's too.
        Assert.Equal("/say ", DialogValues.FillTemplate("/say $()", values));
    }

    [Fact]
    public void ASliderTakesOnlyItsStepsCountedFromItsInitialValue()
    {
        foreach (float value in new[] { 1F, 4F, 7F, 10F })
            Assert.True(DialogValues.SliderTakes(value, 0, 10, 3, 1), value.ToString());
        foreach (float value in new[] { 0F, 2F, 11F, -2F })
            Assert.False(DialogValues.SliderTakes(value, 0, 10, 3, 1), value.ToString());
        // Without a step any value of the range; a reversed range too.
        Assert.True(DialogValues.SliderTakes(3.3F, 0, 10, null, 5));
        Assert.True(DialogValues.SliderTakes(3.3F, 10, 0, null, 5));
        Assert.False(DialogValues.SliderTakes(10.5F, 0, 10, null, 5));
        Assert.False(DialogValues.SliderTakes(float.NaN, 0, 10, null, 5));
    }

    [Fact]
    public void AnUntouchedSliderShowsWhatTheGameShows()
    {
        // With a step: the initial value itself. Without: the initial value through the slider's float sums.
        Assert.Equal(1F, DialogValues.UntouchedSlider(0, 10, 3, 1));
        float place = (3.3F - 0F) / (10F - 0F);
        Assert.Equal(0F + place * (10F - 0F), DialogValues.UntouchedSlider(0, 10, null, 3.3F));
        Assert.Equal(4F, DialogValues.UntouchedSlider(4, 4, null, 4));
        // The game's step snapping: the nearest step inside the range.
        Assert.Equal(7F, DialogValues.SliderValue(0.65F, 0, 10, 3, 1));
        Assert.Equal(10F, DialogValues.SliderValue(1F, 0, 10, 3, 1));
        Assert.Equal(1F, DialogValues.SliderValue(0F, 0, 10, 3, 1));
    }

    [Theory]
    [InlineData('a', false, true)]
    [InlineData('\n', true, true)]
    [InlineData('\n', false, false)]
    [InlineData('§', false, false)]
    [InlineData('\u007F', true, false)]
    [InlineData('\t', true, false)]
    public void ATextFieldTakesWhatTheGameSFieldTakes(char c, bool multiline, bool allowed)
    {
        Assert.Equal(allowed, DialogValues.AllowedInText(c, multiline));
    }
}
