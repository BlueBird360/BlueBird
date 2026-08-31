using System;
using System.IO;

namespace BlueBird.Aspose.Cells.Tests;

public sealed class ExcelReaderMappingTest
{
    [Fact]
    public void Read_ReorderedNamedColumns_ResolvesColumnsForEachWorkbook()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name);
        reader.Map(person => person.Age);

        using MemoryStream firstWorkbook = ExcelTestWorkbook.Create(
            ["Name", "Age"],
            ["Alice", 30]);
        using MemoryStream secondWorkbook = ExcelTestWorkbook.Create(
            ["Age", "Name"],
            [40, "Bob"]);

        Person first = Assert.Single(reader.Read(firstWorkbook));
        Person second = Assert.Single(reader.Read(secondWorkbook));

        Assert.Equal("Alice", first.Name);
        Assert.Equal(30, first.Age);
        Assert.Equal("Bob", second.Name);
        Assert.Equal(40, second.Age);
    }

    [Fact]
    public void Read_ExplicitColumnIndexAcrossWorkbooks_KeepsConfiguredIndex()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name, 0);

        using MemoryStream firstWorkbook = ExcelTestWorkbook.Create(
            ["First Header"],
            ["Alice"]);
        using MemoryStream secondWorkbook = ExcelTestWorkbook.Create(
            ["Second Header"],
            ["Bob"]);

        Person first = Assert.Single(reader.Read(firstWorkbook));
        Person second = Assert.Single(reader.Read(secondWorkbook));

        Assert.Equal("Alice", first.Name);
        Assert.Equal("Bob", second.Name);
    }

    [Fact]
    public void Map_CustomColumnName_ReadsMatchingColumn()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name, "Display Name");
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Display Name"],
            ["Alice"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void MapCustom_GenericNamedColumn_SetsValueWithAction()
    {
        var reader = new ExcelReader<Person>();
        reader.MapCustom<string>((person, value) => person.Name = value!.ToUpperInvariant(), "Name");
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["Alice"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("ALICE", person.Name);
    }

    [Fact]
    public void MapCustom_GenericIndexedColumn_SetsValueWithAction()
    {
        var reader = new ExcelReader<Person>();
        reader.MapCustom<int>((person, value) => person.Age = value * 2, 1);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Ignored", "Value"],
            ["x", 20]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal(40, person.Age);
    }

    [Fact]
    public void MapCustom_RuntimeValueType_ConvertsAndSetsValue()
    {
        var reader = new ExcelReader<Person>();
        reader.MapCustom(typeof(int), (person, value) => person.Age = (int)value!, "Age");
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Age"],
            [42]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal(42, person.Age);
    }

    [Fact]
    public void AutoMap_PublicReadWriteProperties_ReadsEveryProperty()
    {
        var reader = new ExcelReader<Person>();
        reader.AutoMap();
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name", "Age"],
            ["Alice", 30]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
        Assert.Equal(30, person.Age);
    }

    [Fact]
    public void RemoveMaps_AutoMappedProperty_RemovesItsMapping()
    {
        var reader = new ExcelReader<Person>();
        reader.AutoMap();
        reader.RemoveMaps(person => person.Age);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Name"],
            ["Alice"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Equal("Alice", person.Name);
        Assert.Equal(0, person.Age);
    }

    [Fact]
    public void Read_InitiallyMissingOptionalNamedColumn_LeavesDefaultValue()
    {
        var reader = new ExcelReader<Person>();
        reader.Map(person => person.Name, columnRequired: false);
        using MemoryStream workbook = ExcelTestWorkbook.Create(
            ["Other"],
            ["Value"]);

        Person person = Assert.Single(reader.Read(workbook));

        Assert.Null(person.Name);
    }

    [Fact]
    public void Map_NegativeColumnIndex_Throws()
    {
        var reader = new ExcelReader<Person>();

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Map(person => person.Name, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.MapCustom<string>((person, value) => person.Name = value, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.MapCustom(typeof(string), (person, value) => person.Name = (string)value!, -1));
    }

    [Fact]
    public void Mapping_InvalidArguments_Throw()
    {
        var reader = new ExcelReader<Person>();

        Assert.Throws<ArgumentNullException>(() => reader.Map<string>(null!));
        Assert.Throws<ArgumentNullException>(() => reader.Map<string>(null!, 0));
        Assert.Throws<ArgumentNullException>(() => reader.MapCustom<string>(null!, "Name"));
        Assert.Throws<ArgumentNullException>(() => reader.MapCustom<string>((person, value) => person.Name = value, null!));
        Assert.Throws<ArgumentNullException>(() => reader.MapCustom(null!, (person, value) => person.Name = (string)value!, "Name"));
        Assert.Throws<ArgumentNullException>(() => reader.MapCustom(typeof(string), null!, "Name"));
        Assert.Throws<ArgumentNullException>(() => reader.MapCustom(typeof(string), (person, value) => person.Name = (string)value!, null!));
        Assert.Throws<ArgumentNullException>(() => reader.RemoveMaps<string>(null!));
        Assert.Throws<ArgumentException>(() => reader.Map(person => person.Name, " "));
        Assert.Throws<ArgumentException>(() => reader.MapCustom<string>((person, value) => person.Name = value, " "));
        Assert.Throws<ArgumentException>(() => reader.MapCustom(typeof(string), (person, value) => person.Name = (string)value!, " "));
    }

    private sealed class Person
    {
        public string? Name { get; set; }

        public int Age { get; set; }
    }

}
