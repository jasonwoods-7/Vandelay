# Vandelay

## What is Vandelay?

Vandelay is an extension to the [Fody](https://github.com/Fody/Fody) aspect weaving project framework.  Vandelay is an Importer\Exporter.

## AppVeyor Build Status [![Build status](https://ci.appveyor.com/api/projects/status/uvm747hjwyus7wba?svg=true)](https://ci.appveyor.com/project/jasonwoods-7/vandelay)

## The NuGet Package [![NuGet Status](http://img.shields.io/nuget/v/Vandelay.Fody.svg?style=flat)](https://www.nuget.org/packages/Vandelay.Fody/)

https://www.nuget.org/packages/Vandelay.Fody/

```powershell
PM> Install-Package Vandelay.Fody
```

## What type of problem does Vandelay solve?

Vandelay aims to solve the Open Closed Principle from the SOLID principles.  Behind the scenes, it uses the Managed Extensibility Framework (MEF) to provide importing\exporting.

However, MEF can be a quite verbose framework, and it may be easy to forget to mark a class as exportable.  Vandelay seeks to simplify using MEF.

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

internal static class CompositionBatchHelper
{
  public static CompositionBatch CreateCompositionBatch(object[] exports)
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
}

internal sealed class IFooRetriever
{
  [ImportMany(typeof(IFoo))]
  private IEnumerable<IFoo> _imports;

  private IFooRetriever(object[] exports)
  {
    using (var aggregateCatalog = new AggregateCatalog)
    {
      aggregateCatalog.Catalogs.Add(new DirectoryCatalog(           // *
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

### What does the `string searchPatterns` argument in ImportMany do?

It allows you to specify file patterns to match when searching the directory for files.  Multiple entries are separated by a vertical pipe (`|`).

For example:

``` c#
Vandelay.Importer.ImportMany<IFoo>("*.exe|*.dll")
```

changes the Retriever code (above at *) to

``` c#
var catalogPath = Directory.GetParent(new Uri(
  Assembly.GetExecutingAssembly()
  .EscapedCodeBase).LocalPath).FullName;
aggregateCatalog.Catalogs.Add(new DirectoryCatalog(
  catalogPath, "*.exe"));
aggregateCatalog.Catalogs.Add(new DirectoryCatalog(
  catalogPath, "*.dll"));
```

### What does the `object[] exports` argument in ImportMany do?

This allows you to specify objects you want your imported classes to use.

For example, given the following

``` c#
public class FooWithImport : IFoo
{
  [Import]
  Bar MyBar { get; set; }
}

public class Importer
{
  public IEnumerable<IFoo> Imports { get; } =
    Vandelay.Importer.ImportMany<IFoo>("*.dll", new Bar());
}
```

the Imports collection will contain a FooWithImport object with the MyBar property filled in.

### Are there limitation to what I can export?

Yes, there are currently a few limitations to mention.

First, objects which contain a string in the constructor aren't currently working when you inline the object array.  The current work-around would be to create the array before the call to ImportMany such as:

``` c#
var exports = new object[]
{
  "string export",
  42,
  new Bar()
};

var imports = Vandelay.Importer.ImportMany<IFoo>("*.dll", exports);
```

Another limitation is that you cannot currently specify the contract name or type, so if you had an import expecting a type of IBar you would have to explicitly specify the contract name and type, such as:

``` c#
[Import("Fully.Qualified.Namespace.Bar", typeof(Bar))]
IBar MyBar { get; set; }
```

## Will Vandelay be my latex salesman?

No.
