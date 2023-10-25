using System;
using FluentAssertions;
using Xunit;
using Sig.App.Backend.Extensions;

namespace Sig.App.BackendTests.Extensions;

public class DateTimeExtensionsTests
{
    [Fact]
    public void UnspecifiedDateAsUtcBecomesUtc()
    {
        var dt = new DateTime(2022, 03, 16, 13, 10, 00, DateTimeKind.Unspecified);
        var result = dt.AsUtc();

        result.Should().BeIn(DateTimeKind.Utc);
        result.Hour.Should().Be(13); // No offset change
    }

    [Fact]
    public void UtcDateAsUtcStaysTheSame()
    {
        var dt = new DateTime(2022, 03, 16, 13, 10, 00, DateTimeKind.Utc);
        var result = dt.AsUtc();
        
        result.Should().Be(dt); // No change
    }

    [Fact]
    public void LocalDateAsUtcThrows()
    {
        var dt = new DateTime(2022, 03, 16, 13, 10, 00, DateTimeKind.Local);
        var action = () => dt.AsUtc();
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CanConvertUtcDateToOffsetDateTime()
    {
        var dt = new DateTime(2022, 03, 16, 13, 10, 00, DateTimeKind.Utc);
        var result = dt.FromUtcToOffsetDateTime();

        // Returns the same date with offset specified as +0
        result.LocalDateTime.ToDateTimeUnspecified().Should().Be(dt);
        result.Offset.ToTimeSpan().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void FromUtcToOffsetDateTimeThrowsForLocalDate()
    {
        var dt = new DateTime(2022, 03, 16, 13, 10, 00, DateTimeKind.Local);
        var action = () => dt.FromUtcToOffsetDateTime();
        action.Should().Throw<ArgumentException>();
    }
}