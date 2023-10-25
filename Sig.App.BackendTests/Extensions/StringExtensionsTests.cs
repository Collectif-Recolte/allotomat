using FluentAssertions;
using Xunit;
using Sig.App.Backend.Extensions;

namespace Sig.App.BackendTests.Extensions;

public class StringExtensionsTests : TestBase
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("    ", "")]
    [InlineData(" abc ", "abc")]
    public void Trims(string initialValue, string expectedOutput)
    {
        initialValue.Slugify().Should().BeEquivalentTo(expectedOutput);
    }

    [Theory]
    [InlineData("a b c", "a-b-c")]
    [InlineData("a  b  c", "a-b-c")]
    public void ReplaceSpacesWithHyphens(string initialValue, string expectedOutput)
    {
        initialValue.Slugify().Should().BeEquivalentTo(expectedOutput);
    }

    [Theory]
    [InlineData("a_b", "a-b")]
    [InlineData("a__b", "a-b")]
    [InlineData("_a_b_", "a-b")]
    public void ReplacesUnderscoresWithHyphens(string initialValue, string expectedOutput)
    {
        initialValue.Slugify().Should().BeEquivalentTo(expectedOutput);
    }

    [Theory]
    [InlineData("A", "a")]
    [InlineData("AbC", "abc")]
    public void ApplyLowerCase(string initialValue, string expectedOutput)
    {
        initialValue.Slugify().Should().BeEquivalentTo(expectedOutput);
    }

    [Theory]
    [InlineData("a", 1, "a")]
    [InlineData("abc", 3, "abc")]
    [InlineData("abcd", 3, "abc")]
    [InlineData("ab-cd", 3, "ab")]
    [InlineData("a-bcd", 3, "a-b")]
    [InlineData("-a-bcd", 3, "a-b")]
    public void TrimBelowMaxLength(string initialValue, int maxLength, string expectedOutput)
    {
        var result = initialValue.Slugify(maxLength);
        result.Should().BeEquivalentTo(expectedOutput);
        result.Length.Should().BeLessOrEqualTo(maxLength);
    }

    [Theory]
    [InlineData("a.b", "ab")]
    [InlineData("a~b", "ab")]
    [InlineData("a:b", "ab")]
    [InlineData("a?b", "ab")]
    [InlineData("a#b", "ab")]
    [InlineData("a[b", "ab")]
    [InlineData("a]b", "ab")]
    [InlineData("a@b", "ab")]
    [InlineData("a!b", "ab")]
    [InlineData("a$b", "ab")]
    [InlineData("a&b", "ab")]
    [InlineData("a'b", "ab")]
    [InlineData("a(b", "ab")]
    [InlineData("a)b", "ab")]
    [InlineData("a*b", "ab")]
    [InlineData("a+b", "ab")]
    [InlineData("a,b", "ab")]
    [InlineData("a;b", "ab")]
    [InlineData("a=b", "ab")]
    [InlineData("a/b", "ab")]
    [InlineData("a\\b", "ab")]
    [InlineData("a|b", "ab")]
    [InlineData("a%b", "ab")]
    [InlineData("a{b", "ab")]
    [InlineData("a}b", "ab")]
    [InlineData("a-b", "a-b")]
    [InlineData("a_b", "a-b")]
    [InlineData("a b", "a-b")]
    public void RemovesUnsuportedCharacters(string initialValue, string expectedOutput)
    {
        initialValue.Slugify().Should().BeEquivalentTo(expectedOutput);
    }

    [Theory]
    [InlineData("áÁàÀâÂäÄãÃåÅæÆ", "aaaaaaaaaaaaaa")]
    [InlineData("çÇ", "cc")]
    [InlineData("éÉèÈêÊëË", "eeeeeeee")]
    [InlineData("íÍìÌîÎïÏ", "iiiiiiii")]
    [InlineData("ñÑ", "nn")]
    [InlineData("óÓòÒôÔöÖõÕøØœŒ", "oooooooooooooo")]
    [InlineData("úÚùÙûÛüÜ", "uuuuuuuu")]
    public void RemovesAccents(string initialValue, string expectedOutput)
    {
        initialValue.Slugify().Should().BeEquivalentTo(expectedOutput);
    }
}

