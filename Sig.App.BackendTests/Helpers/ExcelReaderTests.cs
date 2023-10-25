using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Xunit;
using Sig.App.Backend.Helpers;

namespace Sig.App.BackendTests.Helpers;

public class ExcelReaderTests
{
    private readonly Stream sampleStream;
    
    public ExcelReaderTests()
    {
        sampleStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Sig.App.BackendTests.Helpers.sample.xlsx");
    }

    [Fact]
    public void CanOpenValidExcelFile()
    {
        ExcelReader.TryOpenStream(sampleStream, out _).Should().BeTrue();
    }

    [Fact]
    public void CantOpenInvalidExcelFile()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("INVALID EXCEL FILE"));
        ExcelReader.TryOpenStream(stream, out _).Should().BeFalse();
    }

    [Fact]
    public void CallsOnErrorCallbackOnInvalidExcelFile()
    {
        Exception actualException = null;
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("INVALID EXCEL FILE"));
        ExcelReader.TryOpenStream(stream, out _, onError: ex => actualException = ex);

        actualException.Should().NotBeNull();
    }

    [Fact]
    public void CanReadDataFromExcelFile()
    {
        ExcelReader.TryOpenStream(sampleStream, out var reader);

        var results = reader.Worksheet<Person>()
            .String(x => x.FirstName)
            .String(x => x.LastName)
            .GetData()
            .ToList();

        results.Should().BeEquivalentTo(new Person[]
        {
            new("John", "Doe"),
            new("Jean", "Bon"),
            new("Baptiste", "Morch")
        });
    }

    [Fact]
    public void CanReadSecondWorksheet()
    {
        ExcelReader.TryOpenStream(sampleStream, out var reader);

        reader.Worksheet<Person>(); // Skip first worksheet
        
        var results = reader.Worksheet<Person>()
            .String(x => x.FirstName)
            .String(x => x.LastName)
            .GetData()
            .ToList();

        results.Should().BeEquivalentTo(new Person[]
        {
            new("Jane", "Doe"),
            new("Jeanne", "Bonne"),
            new("Baptista", "Morche")
        });
    }

    [Fact]
    public void CanReadRowNumber()
    {
        ExcelReader.TryOpenStream(sampleStream, out var reader);
        
        var results = reader.Worksheet<Person>()
            .String(x => x.FirstName)
            .String(x => x.LastName)
            .RowNumber(x => x.RowNumber)
            .GetData()
            .ToList();

        results[0].RowNumber.Should().Be(2);
        results[1].RowNumber.Should().Be(3);
        results[2].RowNumber.Should().Be(4);
    }

    [Fact]
    public void CanRunCustomColumnMappingFunction()
    {
        ExcelReader.TryOpenStream(sampleStream, out var reader);
        
        var results = reader.Worksheet<Person>()
            .Column((cell, person) => person.FirstName = cell.GetValue<string>().ToUpper())
            .String(x => x.LastName)
            .GetData()
            .ToList();

        results.Should().BeEquivalentTo(new Person[]
        {
            new("JOHN", "Doe"),
            new("JEAN", "Bon"),
            new("BAPTISTE", "Morch")
        });
    }

    private class Person
    {
        public Person() { }

        public Person(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        // Not part of equality check
        public int RowNumber { get; set; }

        protected bool Equals(Person other)
        {
            return FirstName == other.FirstName && LastName == other.LastName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Person)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FirstName, LastName);
        }
    }
}