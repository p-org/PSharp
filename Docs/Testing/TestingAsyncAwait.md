Testing async/await code using P#
=================================
The P# language was designed to ease the development of event-driven programs. P# forces the programmer to think of their design in terms of state machines driven by events passed back-and-forth between them. This style lends itself naturally to the asynchronous world. Tasks are kept short and their continuations are stitched together through events that arrive asynchronously. The complexity of testing and exploration is managed by the `PSharpTester` that systematically enumerates different interleavings and provides high "concurrency" coverage. While the intended use-case of P# is for the development of new systems, we do acknowledge that the more common need is going to be for testing of existing code. Can P# help there? We think so, especially for code that is primarily `async`-`await` C#. But we first need to understand how `PSharpTester` works. 

## PSharpTester Requirements
In P#, a `Machine` is the unit the concurrency. It represents a _state machine_, but for the purpose of this article, we can ignore this aspect of a `Machine` and simply treat it as a building block for concurrency, similar to a `Task` or a `Thread`. A `Machine` is internally sequential but different `Machines` all execute concurrently. A `Machine` is always in an event-driven loop until it halts; it waits for an event to arrive in its `Inbox` and fires an `action` in response. The `action`, in addition to calling P# APIs for sending and receiving events, can execute _arbitrary_ `C#` code to mutate the state of the program. It is this usage of _arbitrary_ that we must now understand for using `PsharpTester` on existing code.

In the simplest form, the `C#` code should be sequential so that all concurrency is delegated to the P# runtime (at least while testing). In other words, the code executed by a machine action must not spawn `Tasks` or `Threads` neither must it do any synchronization operation other than the P# `Send` or `Receive` (i.e., no use of `locks`, `mutexes` or similar constructs). This goes along with the recommendation that different `Machines` must not share object references (use [Shared Objects](../Features/ObjectSharing.md) if you must).

The reason for all these restrictions is that `PSharpTester` needs to be aware of all concurrency in the program in order to control it. `PSharpTester` keeps track of all live `Machines` in the program and takes over the scheduling. At any point during the execution of the program, it will determine the next `Machine` to schedule and give it a chance to execute. The machine will execute its action without interference from other machines until it finishes its current action or it enters the P# runtime again via a `Send` or `Receive` (the only available synchronization primitives). At this point, the `PSharpTester` scheduler takes control, suspends the currently-scheduled machine and then decides on the next one to schedule. The `PSharpTester` essentially serializes the entire execution to a single thread. By controlling the scheluding decisions during an execution, `PSharpTester` can explore different interleavings for a program. The exact choice of which `Machine` to schedule is determined by a `SchedulingStrategy`. `PSharpTester` has several strategies and we recommend using a [portfolio](../Features/TestingMethodology.md#parallel-and-portfolio-testing) of them. The strategies have been crafted from over a decade of research on finding concurrency bugs efficiently in practice. (See, for example, [this paper](http://dl.acm.org/citation.cfm?id=2786861).)

Despite serializing the execution on a single thread, the restrictions that we had outlined above guarantee that `PSharpTester` will cover all behaviors of the program in the limit. Getting there will, of course, take infinite time because there may be infinitly many executions of the program, nonetheless, _completeness-in-the-limit_ is an important guarantee for a testing solution to have. (Testing concurrent programs natively on the hardware without `PSharpTester` does not offer this guarantee.)
 
Relaxing the restrictions on the `C#` code can either cause `PSharpTester` to lose completeness or it may start to deadlock or crash. The former outcome is the acceptable one: we still dramatically gain testing coverage by using `PSharpTester` over naïve testing. (It also makes way for a pay-as-you-go-model: as more code is made P#-compliant, the coverage keeps improving and fully-P#-compliant code offers completeness.) It is the latter (deadlocks and crashes) that we must avoid. 

## Async-Await Code
To make the discussion meaningful, we restrict our attention to mostly `async`-`await` code. By this we mean a software component that asynchronously handles client requests that may arrive at any time, and sends back a response when it's done servicing them. Internally, it may use other such components: it delegates work to them asynchronously and waits for their response. Such components are typical in web services, where for example, users pump in requests at any time and the service must process them asynchronously; it cannot afford to block subsequent requests before it finishes the first. Further, the service might use a backing store for persistence and fault tolerance. Lets take an example. Suppose that our component offers the following methods for processing client requests:
```C#
async Task<Response> HandleRequest1(...);
async Task<Response> HandleRequest2(...);
```

And these procedures call external methods for interacting with a persistent store:
```C#
async Task<Data> Read(int index);
async Task Write(int index, Data data);
```

For unit-testing such code, one might write a test method that invokes these methods in parallel and use mocks of the storage that relies on locking to be thread-safe.
```C#
void Test()
{
   Task.Run(async () => await HandleRequest1(...));
   Task.Run(async () => await HandleRequest2(...));
}

// mock
async Task<Data> Read(int index) 
{
   lock(lck) 
   {
      return Task.FromResult(...);
   }
}
```

Note: Similar scenarios also exist very commonly inside the operating systems such as [drivers](https://blogs.msdn.microsoft.com/b8/2011/08/22/building-robust-usb-3-0-support/), but the language of choice there is `C` or `C++`, not a managed language like `C#`. Use [P](https://github.com/p-org/P) or [P3](https://github.com/p-org/P3) if you operate in that world. 

## Testing Strategy
To use `PSharpTester` we must tame the `C#` code and work towards exposing the concurrency to P#. First and foremost, _the code must not spawn `Tasks` (same applies to `Threads`)_. This is the most important rule to follow. Creation of `Tasks` will surely make `PSharpTester` unusable. To eliminate `Task` creation, try replacing them with `Machine` creation instead, which should work for the most part. For our running example, we modify our `Test` method to instead create machines:
```C#
[Microsoft.PSharp.Test]
void Test(PSharpRuntime runtime)
{
   runtime.CreateMachine(typeof(RunTask), new TaskPayload(async () => await HandleRequest1(...)));
   runtime.CreateMachine(typeof(RunTask), new TaskPayload(async () => await HandleRequest2(...)));
}
```

Here, `RunTask` is a special machine that simply invokes the payload method given to it. Look at the sample [here](https://github.com/p-org/PSharpLab/tree/master/Samples/Experimental/SingleTaskMachine) to get a hang of it. Or one may create their own special machine for invoking `HandleRequest1` or `HandleRequest2`. Any way of replacing `Task` creation with `Machine` creation is fine.

Another example: your code may be using a `Timer` to register a periodic callback. Instead, create a `TimerMachine` that either invokes the callback periodically (or non-deterministically using P#'s `Random`) or sends an event to the `Task` (now a `Machine`) that created the `Timer`. Sample code is [here](https://github.com/p-org/PSharp/tree/master/Samples/Raft/Raft.PSharpLibrary/Timers).

Once the `Task` creation is eliminated, the next item of focus is the use of synchronization. When multiple `Tasks` can share a reference to the same object, they will use synchronization in the form of `locks` to guard access to that object. When `Tasks` get converted to `Machines`, this implies actions of different machines might share objects and invoke non-P# synchronization. For `PSharpTester` scheduling to work without causing deadlocks, one must be careful with such synchronization. _A simple rule of thumb is that a P# API should not be invoked while holding a lock_. Short synchronization blocks that guard access to a flag or a simple container should be likely be fine, except that `PSharpTester` loses completeness (practically, there is a loss in coverage). To regain more coverage, _consider lifting the synchronization blocks to be hosted in their own `Machine`_. For our running example, we can write a machine `StorageMachine` to mock calls to `Read` and `Write` and do something like the following instead of locking:
```C#
async Task<Data> Read(int index) 
{
   Send(typeof(StorageMachine), new ReadEvent(index));
   var r = await Receive(typeof(ReadResponse));
   return r.Data;
}
```

The `StorageMachine` can perform `Read` and `Write` functionality atomically (without needing to grab a lock as `P#` guarantees methods of a single machine are not executed in parallel). This mocking will ensure `PSharpTester` considers different orders of execution of the `Read/Write` critical section. 

There are further standard guidelines to writing a P# test. _The test must be idempotent and set up for repeated execution_. In simple terms, it must reset its state before starting the test. This is required because `PSharpTester` execute the test method repeatedly. 

If you have code that mostly uses `async`-`await` constructs then it would be an easy porting exercise, which may just be confined to your test code. A detailed account of how we applied this strategy to test the `ExtentManager` of Azure Storage vNext is given in our [paper](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/04/paper-1.pdf) (see section 3). 

## Controlled Non-Determinism
There is an additional requirement for `PSharpTester`. The C# code must be deterministic once the concurrent interleaving between `Machines` is fixed. This means, for instance, the code should not make branching decisions based on the current time. In order to simulate timeout, one can instead rely on P# runtime's `Random` API, which in turn will provide higher coverage during testing (by exploring both the non-timeout as well as timeout scenarios). This requirement is necessary to reproduce a trace reported by `PSharpTester` but can also help in achieving higher coverage. Some of `PSharpTester` strategies rely on controlled non-determinism to systematically explore different interleavings.
