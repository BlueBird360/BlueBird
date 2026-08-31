using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace BlueBird.Aspose.Cells.Tests;

public sealed class ExcelReaderValidationTest
{
    [Fact]
    public void Read_MapValidationSucceeds_AssignsValue()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Age).Validate(age => age >= 18);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Age"],
            [20]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal(20, person.Age);
    }

    [Fact]
    public void Read_MapValidationFails_ThrowsAggregatedValidationException()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Age).Validate(age => age >= 18, "Age must be at least 18.");
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Age"],
            [17]);

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        ValidationException validationException = Assert.IsType<ValidationException>(Assert.Single(exception.InnerExceptions));
        Assert.Contains("Age must be at least 18.", validationException.Message);
    }

    [Fact]
    public void Read_MultipleMapValidationFailures_AggregatesAllFailures()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Age)
            .Validate(age => age >= 18, "Minimum")
            .Validate(age => age <= 120, "Maximum");
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Age"],
            [10],
            [130]);

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.All(exception.InnerExceptions, inner => Assert.IsType<ValidationException>(inner));
    }

    [Fact]
    public void Read_MapValidation_ReceivesTrimmedValue()
    {
        string? validatedValue = null;
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name).Validate(value =>
        {
            validatedValue = value;
            return true;
        });
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["  Alice  "]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", validatedValue);
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void Read_DataAnnotationsInvalid_ThrowsAggregatedValidationException()
    {
        var reader = new ExcelReader<AnnotatedPerson>();
        reader.AutoMap();
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name", "Age"],
            [null, 10]);

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        ValidationException validationException = Assert.IsType<ValidationException>(Assert.Single(exception.InnerExceptions));
        Assert.Contains("Name is required. Age is invalid.", validationException.Message);
    }

    [Fact]
    public void Read_ValidateOnReadDisabled_SkipsMapAndModelValidation()
    {
        var reader = new ExcelReader<AnnotatedPerson> { ValidateOnRead = false };
        reader.Map(person => person.Name).Validate(value => false, "Map validation should be skipped.");
        reader.Map(person => person.Age);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name", "Age"],
            [null, 10]);

        AnnotatedPerson person = Assert.Single(reader.Read(workbook));

        Assert.Null(person.Name);
        Assert.Equal(10, person.Age);
    }

    [Fact]
    public void Read_ValidationContextItems_AreAvailableToModelValidation()
    {
        var reader = new ExcelReader<ContextValidatedPerson>();
        reader.ValidationContextItems["MinimumAge"] = 18;
        reader.AutoMap();
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Age"],
            [17]);

        AggregateException exception = Assert.Throws<AggregateException>(() => reader.Read(workbook));

        Assert.Contains("Age must be at least 18.", exception.Message);
    }

    private sealed class Person
    {
        public string? Name { get; set; }

        public int Age { get; set; }
    }

    private sealed class AnnotatedPerson
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }

        [Range(18, 120, ErrorMessage = "Age is invalid.")]
        public int Age { get; set; }
    }

    private sealed class ContextValidatedPerson : IValidatableObject
    {
        public int Age { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (validationContext.Items.TryGetValue("MinimumAge", out object? value)
                && value is int minimumAge
                && Age < minimumAge)
            {
                yield return new ValidationResult($"Age must be at least {minimumAge}.");
            }
        }
    }

}
