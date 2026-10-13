using FluentAssertions;
using HMS.Modules.Documents.Application.Indexing;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

public class KeywordSearchTermsTests
{
    [Fact]
    public void KeepsTheMeaningfulWordsOfAQuestion_LowerCased_InOrder()
    {
        KeywordSearchTerms.From("Which Antibiotics were given during the last admission?")
            .Should().Equal("antibiotics", "during", "admission");
    }

    [Fact]
    public void KeepsWordsThatMixLettersAndDigits()
    {
        KeywordSearchTerms.From("Latest HbA1c and blood sugar results").Should().Equal("hba1c", "blood", "sugar");
    }

    [Fact]
    public void DropsDuplicatesAndWordsShorterThanThreeCharacters()
    {
        KeywordSearchTerms.From("BP bp reading of BP: reading 120").Should().Equal("reading", "120");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?? !! ..")]
    [InlineData("is it ok")]
    [InlineData("what was the last")]
    public void IsEmptyWhenNothingIsWorthMatching(string query)
    {
        KeywordSearchTerms.From(query).Should().BeEmpty();
    }

    [Fact]
    public void ContainsOnlyLettersMarksAndDigits_SoATermCanNeverCarryAQueryOperator()
    {
        var terms = KeywordSearchTerms.From("fever & (cough | cold) !rash <-> 'pain':* a:b\\c");

        terms.Should().Equal("fever", "cough", "cold", "rash", "pain");
        terms.Should().OnlyContain(term => term.All(c => char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void KeepsAWordWrittenWithCombiningMarksWhole()
    {
        // "காய்ச்சல்" (fever) is letters plus combining vowel signs; it must stay one term.
        KeywordSearchTerms.From("காய்ச்சல் history").Should().Equal("காய்ச்சல்", "history");
    }

    [Fact]
    public void StopsAtTheTermLimit()
    {
        var question = string.Join(' ', Enumerable.Range(1, 40).Select(i => $"term{i}"));

        KeywordSearchTerms.From(question).Should().HaveCount(KeywordSearchTerms.MaxTerms)
            .And.StartWith("term1");
    }
}
