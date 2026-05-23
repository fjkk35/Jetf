## Z.Expressions.Eval: C# Eval Expression

Z.Expressions.Eval is a robust NuGet package that enables .NET developers to evaluate, compile, and execute dynamic C# expressions. By interpreting and processing strings as executable code, this package empowers developers to craft highly adaptable and customizable applications.

It has the capability to handle an extensive range of mathematical operations, string manipulations, boolean logic, and it can even interact with .NET objects and classes.

The principal advantage of Z.Expressions.Eval lies in its ability to provide enhanced flexibility by facilitating runtime execution and manipulation of C# expressions. This feature is particularly beneficial in scenarios where business rules are subject to frequent changes or need to be configurable.

Having earned the trust and endorsement of over **5000 customers worldwide**, C# Eval Expression is a highly regarded library that has become an essential tool for a wide variety of projects.

Resources:

- [Official Website](https://eval-expression.net/): Visit for comprehensive information, updates, tutorials, and more. Learn how Z.Expressions.Eval can significantly enhance your application's capabilities.
- [Contact Us](https://eval-expression.net/contact-us): Have a question or need assistance? Don't hesitate to reach out. We're here to help you get the most out of C# Eval Expression.
- [GitHub Issues](https://github.com/zzzprojects/Eval-Expression.NET/issues): Encountered an issue or have a feature request? Let us know on GitHub. Your feedback helps us make C# Eval Expression even better.

## Supported .NET Versions

Z.Expressions.Eval exhibits broad compatibility with a variety of .NET versions. Starting from **.NET Framework 4.5** and **.NET Core 2.0**, it extends support to the most recent versions of .NET.

To take full advantage of the enhancements in newer .NET versions, we highly recommend upgrading to the latest package version of Z.Expressions.Eval.

## Main Features

Z.Expressions.Eval provides a set of powerful features designed to simplify dynamic C# expression evaluation and execution. Here are some of the key features:

- [Eval.Execute](https://eval-expression.net/eval-execute): Allows you to evaluate and execute a string as a C# expression at runtime. This feature can handle a wide variety of mathematical operations, string manipulations, boolean logic, and even work with .NET objects and classes.
- [Eval.Compile](https://eval-expression.net/eval-compile): Offers the ability to compile a string as a C# expression at runtime, reducing the overhead of repeated evaluations. This feature is especially useful when working with expressions that need to be executed multiple times.
- [LINQ Dynamic](https://eval-expression.net/linq-dynamic): Provides the power to execute LINQ expressions dynamically. This feature opens up new possibilities for creating highly flexible and configurable queries.

For a more detailed explanation and examples of each operation, please refer to the official [documentation page](https://eval-expression.net/overview).

## Getting Started

To get a feel for Z.Expressions.Eval, here is a simple example:

```csharp
using Z.Expressions;

public class Program
{
    public static void Main()
    {
        var result = Eval.Execute<int>("X*Y", new { X = 10, Y = 20 });
        Console.WriteLine(result); // Outputs: 200
    }
}
```

In the above example, the "X*Y" string expression is evaluated dynamically at runtime with the values of X and Y provided in an anonymous object. The result is then printed to the console.

Want to explore the library further? We offer an extensive collection of online examples showcasing the various functionalities and capabilities of C# Eval Expression:

- [C# Eval Expression - Online Examples](https://eval-expression.net/online-examples)

These examples are specifically designed to impart practical understanding of C# Eval Expression, demonstrating its potent features and adaptable options in a variety of scenarios.

## Advanced Usage

### Instance Context

Z.Expressions.Eval allows more advanced scenarios, such as operating in a specific context, using variables, and more. Here's an example illustrating the use of instance context:

```csharp
// CREATE a new instance of EvalContext
var context = new EvalContext();

// USE the `Execute` method from the context created
var list1 = context.Execute<List<int>>("new List<int>() { 1, 2, 3 };");
```

In this example, we first create a new instance of `EvalContext`. Then, we use the `Execute` method from the created context to evaluate and execute a string as a C# expression, which instantiates a new `List<int>`.

[Try it online](https://dotnetfiddle.net/ljr0y2)

### Register Type

The `RegisterType` method allows you to register all types provided for the context. The method also registers extension methods from those types but does not register other static members. For instance, if you register the type "Z.MyNamespace.MyClass", you can subsequently create an expression such as "new MyClass()" or any extension methods from this type.

In the following example, we'll register the types `MyClassHelper` and `MyExtensions` to include their extension methods. We'll first demonstrate how to use our class and extension methods in an arithmetic operation. We'll then show how to utilize the `IsRegisteredType` and `UnregisterType` methods with `MyClassHelper` to verify if the type is currently registered and to unregister it.

```csharp
using System;
using System.Collections.Generic;
using Z.Expressions;

public class Program
{
	public static void Main()
	{
		// Global Context: EvalManager.DefaultContext.RegisterType(typeof(Helper));
		
		var context = new EvalContext();
		context.RegisterType(typeof(MyClassHelper));
		context.RegisterType(typeof(MyExtensions));
		
		var r1 = context.Execute<int>("new MyClassHelper().MyClassHelperID + 2.AddMe(3)"); // return 6
		Console.WriteLine("1 - Result: " + r1);
		
		// Check if the type `MyClassHelper` is registered
		var r2 = context.IsRegisteredType(typeof(MyClassHelper)); // return true
		Console.WriteLine("2 - Result: " + r2);
		
		// Unregister the type `MyClassHelper`
		context.UnregisterType(typeof(MyClassHelper));
		
		// Check the type `MyClassHelper` has been succesfully unregistered
		var r3 = context.IsRegisteredType(typeof(MyClassHelper)); // return false
		Console.WriteLine("3 - Result: " + r3);
	}
	
	public class MyClassHelper
	{
		public int MyClassHelperID { get; set; } = 1;
	}
}

public static class MyExtensions
{	
	public static int AddMe(this int x, int y)
	{
		return x + y;
	}
}
```

[Try it online](https://dotnetfiddle.net/LCmJGy)

### Use Options

The `SafeMode` option allows you to set the context to only allow the usage of registered members and types within expressions. In essence, it enables a secure execution environment for user input, restricting usage to only what you explicitly permit. By default, `SafeMode` is set to `false`.

In this example, we demonstrate how `SafeMode` method. Please ensure to thoroughly understand the `SafeMode` documentation if you intend to use this option.

```csharp
// Global Context: EvalManager.DefaultContext.SafeMode = true;

var context = new EvalContext();

context.SafeMode = true;
context.UnregisterAll();

try
{
	var fail = context.Execute("Math.Min(1, 2)");
	Console.WriteLine(fail);
}
catch(Exception ex)
{
	Console.WriteLine("1 - Exception: " + ex.Message);
}

// try again by registering "System.Math" member
context.RegisterMember(typeof(System.Math));
var r2 = context.Execute("Math.Min(1, 2)");
Console.WriteLine("2 - Result: " + r2);
```

[Try it online](https://dotnetfiddle.net/ljr0y2)

## Release Notes

For a thorough record of enhancements, bug fixes, and upgrades in each iteration of C# Eval Expression, we recommend referring to the official [Release Notes](https://github.com/zzzprojects/Eval-Expression.NET/releases) located in our GitHub repository.

The Release Notes offer essential insights about each version, detailing new features, addressing resolved issues, and mentioning any breaking changes (if applicable). We strongly advise reviewing these notes prior to upgrading to a newer version. This practice not only ensures you leverage the full potential of the newly introduced features but also helps prevent unforeseen complications.


## License

C# Eval Expression utilizes a paid licensing model. To acquire a license, please visit our official [Pricing Page](https://eval-expression.net/pricing) on the Eval Expression website.