using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlueBird.Json.TypeAlias.Tests;

public sealed class AbstractBaseTypeTest
{
    private readonly TypeAliasSerializationBinder _binder;
    private readonly JsonSerializerSettings _settings;

    public AbstractBaseTypeTest()
    {
        this._binder = new TypeAliasRegistry()
            .Register<Shape>()
            .Register<Circle>()
            .Register<Rectangle>()
            .BuildBinder();
        this._settings = TestHelper.CreateSettings(this._binder);
    }

    [Fact]
    public void CircleThroughAbstractShape_PreservesType()
    {
        Shape shape = new Circle { Color = "Red", Radius = 5.0 };
        string json = JsonConvert.SerializeObject(shape, this._settings);

        Assert.Contains("\"$type\":\"circle\"", json);

        Shape? result = JsonConvert.DeserializeObject<Shape>(json, this._settings);

        Assert.IsType<Circle>(result);
        var circle = (Circle)result!;
        Assert.Equal("Red", circle.Color);
        Assert.Equal(5.0, circle.Radius);
    }

    [Fact]
    public void RectangleThroughAbstractShape_PreservesType()
    {
        Shape shape = new Rectangle { Color = "Blue", Width = 10, Height = 20 };
        string json = JsonConvert.SerializeObject(shape, this._settings);

        Assert.Contains("\"$type\":\"rect\"", json);

        Shape? result = JsonConvert.DeserializeObject<Shape>(json, this._settings);

        Assert.IsType<Rectangle>(result);
        var rect = (Rectangle)result!;
        Assert.Equal("Blue", rect.Color);
        Assert.Equal(10, rect.Width);
        Assert.Equal(20, rect.Height);
    }

    [Fact]
    public void CollectionOfAbstractTypes_PreservesEachType()
    {
        var shapes = new Shape[]
        {
            new Circle { Color = "Red", Radius = 5.0 },
            new Rectangle { Color = "Blue", Width = 10, Height = 20 },
        };

        string json = JsonConvert.SerializeObject(shapes, this._settings);
        Shape[]? result = JsonConvert.DeserializeObject<Shape[]>(json, this._settings);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Length);
        Assert.IsType<Circle>(result[0]);
        Assert.IsType<Rectangle>(result[1]);
    }
}
