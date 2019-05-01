Programming model: asynchronous machine tasks
=============================================
The _asynchronous machine tasks_ programming model of P# is based on the [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).

This programming model exposes the `MachineTask` type, which can be used with the `async` and `await` C# keywords, similar to the `System.Threading.Tasks.Task` type. The `MachineTask` type can be used as a drop-in replacement for the `System.Threading.Tasks.Task` type. However, `MachineTask` can also be used alongside `System.Threading.Tasks.Task` to easily invoke external asynchronous APIs from your code. We will discuss this in more detail below.

The benefit of using `MachineTask` is that during testing, the P# runtime will control the schedule of each `MachineTask` and thoroughly explore all possible asynchronous interlevings to automatically find and report hard-to-reproduce concurrency bugs. In production, `MachineTask` executes efficiently, as it is implemented using a `System.Threading.Tasks.Task`.

Overview
========
The core of the P# asynchronous machine tasks programming model is the `MachineTask` and `MachineTask<T>` objects, which model asynchronous operations. They are supported by the `async` and `await` keywords.

This programming model is fairly simple in most cases:
- For _I/O-bound_ code, you `await` an operation which returns a `MachineTask` or `MachineTask<T>` inside of an `async` method.
- For _CPU-bound_ code, you `await` an operation which is started on a background thread with the `MachineTask.Run` method.

In more detail, a `MachineTask` is a construct used to implement what is known as the [promise model of concurrency](https://en.wikipedia.org/wiki/Futures_and_promises). A `MachineTask` basically offers you a _promise_ that work will be completed at a later point, letting you coordinate with this promise using `async` and `await`. A `MachineTask` represents a single operation which does not return a value. A `MachineTask<T>` represents a single operation which returns a value of type `T`. It is important to reason about tasks as abstractions of work happening asynchronously, and not an abstraction over threading. By default, a `MachineTask` executes (using a `System.Threading.Tasks.Task`) on the current thread and delegates work to the operating system, as appropriate. Optionally, a `MachineTask` can be explicitly requested to run on a separate thread via the `MachineTask.Run` API.

The `await` keyword is where the magic happens. Using `await` yields control to the caller of the method that performed `await`, allowing your program to be responsive or a service to be elastic, since it can now perform useful work while a `MachineTask` is running on the background. Your code does not need to rely on callbacks or events to continue execution after the task has been completed. The C# language does that for you. If you’re using `MachineTask<T>`, the `await` keyword will additionally _unwrap_ the value returned when the `MachineTask` is complete. The details of how `await` works are further explained in the C# [documentation](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).

During testing, using `await` allows the P# runtime to automatically inject scheduling points and thoroughly explore asynchronous interleavings to find concurrency bugs.

What happens under the covers
=============================
The C# compiler transforms an `async` method into a state-machine, which keeps track of things like yielding execution when an `await` is reached and resuming execution when a background job has finished.

The `MachineTask` type uses a C# 7 feature known as `async task types` (see [here](https://github.com/dotnet/roslyn/blob/master/docs/features/task-types.md)) that allows framework developers to create custom task types that can be used with `async` and `await`. This is where the magic happens. In production, `MachineTask` enables C# to build a custom asynchronous state machine that uses regular `System.Threading.Tasks.Task` objects. However, during testing, P# uses dependency injection to supply a custom asynchronous state machine that allows controling the scheduling of `MachineTask` objects, and thus systematically exploring their interleavings.

How to use machine tasks
========================
We will now show how to write a program using the P# asynchronous task programming model. As mentioned before, the `MachineTask` type is a drop-in replacement for the `System.Threading.Tasks.Task` type, and thus any prior experience writing asynchronous code using `async` and `await` is useful and relevant. If you are not already familiar with `async` and `await`, you can learn more in the C# [documentation](https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth).

Say that you have the following simple C# program:
```c#
private class SharedEntry
{
    public int Value = 0;
}

public async MachineTask WriteWithDelayAsync(SharedEntry entry, int value)
{
    await MachineTask.Delay(100);
    entry.Value = value;
}

public async MachineTask RunAsync()
{
    SharedEntry entry = new SharedEntry();

    MachineTask task1 = WriteWithDelayAsync(entry, 3);
    MachineTask task2 = WriteWithDelayAsync(entry, 5);

    await MachineTask.WhenAll(task1, task2);

    Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
}
```

The above program contains a `SharedEntry` type that implements a shared container for an `int` value. The `WriteWithDelayAsync` is a C# `async` method that asynchronously waits for a `MachineTask` to complete after `100`ms (created via the `MachineTask.Delay(100)` call), and then modifies the value of the `SharedEntry` object.

The `RunAsync` asynchronous method is creating a new `SharedEntry` object, and then twice invokes the `WriteWithDelayAsync` method by passing the values `3` and `5` respectively. Each method call returns a `MachineTask` object, which can be awaited using `await`. The `RunAsync` method first invokes the two asynchronous method calls and then calls `MachineTask.WhenAll(...)` to `await` on the completion of both tasks.

Because `WriteWithDelayAsync` method awaits a `MachineTask.Delay` to complete, it will yield control to the caller of the method, which is the `RunAsync` method. However, the `RunAsync` method is not awaiting immediately upon invoking the `WriteWithDelayAsync` method calls. This means that the two calls can happen _asynchronously_, and thus the value in the `SharedEntry` object can be either `3` or `5` after `MachineTask.WhenAll(...)` completes.

Using `Specification.Assert`, P# allows you to write assertions that check these kinds of safety properties. In this case, the assertion will check if the value is `5` or not, and if not it will throw an exception, or report an error together with a reproducable trace during testing.
