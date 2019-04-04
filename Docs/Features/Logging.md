Logging in P#
=============
By default, the P# runtime logger writes all log output to `Console` during production (when verbosity is enabled). During testing, the log output is redirected to an in-memory writer (which dumps it to a file when a bug is found).

The runtime logger can be accessed via the following `IMachineRuntime`, `Machine` or `Monitor` property:
```C#
ILogger Logger { get; }
```

# Using a custom logger
It is possible to replace the default P# runtime logger with a custom one that implements the `ILogger` and `IDisposable` interfaces:
```C#
public interface ILogger : IDisposable
{
  void Write(string value);
  void Write(string format, params object[] args);
  void WriteLine(string value);
  void WriteLine(string format, params object[] args);
}
```
To replace the default runtime logger, call the following `IMachineRuntime` method:
```C#
void SetLogger(ILogger logger);
```
The above method replaces the previously installed logger, and installs the specified logger.

Note that `SetLogger` is _not_ calling `Dispose` on the previously installed logger. This must be called explicitly by the user. This allows the logger to be accessed and used after being removed from the P# runtime.
