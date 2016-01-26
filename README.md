# Vandelay.Fody

## What is Vandelay.Fody?

Vandelay is an extension to the [Fody](https://github.com/Fody/Fody) aspect weaving project framework.  Vandelay is an Importer\Exporter.

## How do I get Vandelay to handle my importing\exporting tasks?

In Visual Studio, add the Vandelay.Fody package to the project you wish to have types exported from.

## How does using Vandelay simplify my exporting needs?

Vandelay will automatically mark your classes with the appropriate Export attribute.

What you write:

``` c#
[assembly: Vandelay.Exporter(typeof(IFoo))]

public interface IFoo {}

public class Foo : IFoo {}
```

What gets compiled:

``` c#
public interface IFoo {}

[Export(typeof(IFoo))]
public class Foo : IFoo {}
```

### I have abstract classes which implement the interface type I want to have exported.  Will they be exported too?

No, Vandelay only exports instances of classes.  Abstract classes will be skipped.

### What if I want to export the types manually?

Not a problem.  Vandelay first checks whether the type is already exported and only exports unexported classes.

## How does using Vandelay simplify my importing needs?

Vandelay will automatically add the code to import a specified type.

What you write:
``` c#
public class SomeClass
{
  public void SomeMethod()
  {
    var foos = Vandelay.Importer.ImportMany<IFoo>("");
  }
}
```

What gets compiled:

``` c#
public class SomeClass
{
  public void SomeMethod()
  {
    var foos = IFooRetriever.IFooRetriever(new object[0]);
  }
}

internal sealed class ExportValueProvider
{
  private readonly object _value;

  ExportValueProvider(object value)
  {
    this._value = value;
  }

  public object GetValue()
  {
    return this._value;
  }
}

internal sealed class IFooRetriever
{
  [ImportMany(typeof(IFoo))]
  private IEnumerable<IFoo> _imports;

  private static CompositionBatch CreateCompositionBatch(object[] exports)
  {
    var compositionBatch = new CompositionBatch();
    for (var i = 0; i < exports.Length; i++)
    {
      var obj = exports[i];
      var type = obj.GetType();
      compositionBatch.AddExport(new Export(
        AttributedModelService.GetContractName(type),
        new Dictionary<string, object>
        {
          ["ExportTypeIdentity"] = AttributedModelService.GetTypeIdentity(type)
        }, new Func<object>(new ExportValueProvider(obj).GetValue)));
    }
    return compositionBatch;
  }

  private IFooRetriever(object[] exports)
  {
    using (var aggregateCatalog = new AggregateCatalog)
    {
      aggregateCatalog.Catalogs.Add(new DirectoryCatalog(
        Directory.GetParent(new Uri(Assembly.GetExecutingAssembly()
        .EscapedCodeBase).LocalPath).FullName));
      using (var compositionContainer = new CompositionContainer(
        aggregateCatalog, new ExportProvider[0]))
      {
        compositionContainer.Compose(CreateCompositionBatch(exports));
        compositionContainer.ComposeParts(this);
      }
    }
  }

  public static IEnumerable<IFoo> IFooRetriever(object[] exports)
  {
    return new IFooRetriever(exports)._imports;
  }
}
```

## Will Vandelay be my latex salesman?

No.
