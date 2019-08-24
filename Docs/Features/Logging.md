Logging in P#
=============
The P# runtime uses the `RuntimeLogWriter` to format and log all runtime messages. To achieve this, it uses the `IRuntimeLogFormatter` interface for formatting messages, and the `ILogger` interface for logging messages.

By default, the P# runtime writes all log output to `Console` during production (when verbosity is enabled). During testing, the log output is redirected to an in-memory writer (which dumps it to a file when a bug is found). This behavior can be overriden by changing the default log-related interfaces to custom ones. Further, `RuntimeLogWriter` can be subclassed to change the behavior of how the runtime logs messages.

# Using and replacing the logger
The `ILogger` interface is responsible for writing log messages using the `Write` and `WriteLine` methods. The `IsVerbose` property is `true` when verbosity is enabled, and `false` when disabled.
```C#
public interface ILogger : IDisposable
{
  bool IsVerbose { get; set; }

  void Write(string value);
  void Write(string format, object arg0);
  void Write(string format, object arg0, object arg1);
  void Write(string format, object arg0, object arg1, object arg2);
  void Write(string format, params object[] args);
  
  void WriteLine(string value);
  void WriteLine(string format, object arg0);
  void WriteLine(string format, object arg0, object arg1);
  void WriteLine(string format, object arg0, object arg1, object arg2);
  void WriteLine(string format, params object[] args);
}
```

The runtime logger can be accessed via the following `IMachineRuntime`, `Machine` or `Monitor` property:
```C#
ILogger Logger { get; }
```

It is possible to replace the default logger with a custom one that implements the `ILogger` and `IDisposable` interfaces. For example someone could write the following `CustomLogger` that uses a `StringBuilder` for writing the log:
```C#
public class CustomLogger : ILogger
{
  private StringBuilder StringBuilder;

  public bool IsVerbose { get; set; } = false;

  public CustomLogger(bool isVerbose)
  {
    this.StringBuilder = new StringBuilder();
    this.IsVerbose = isVerbose;
  }

  public void Write(string value) => this.StringBuilder.Append(value);

  public void Write(string format, object arg0) =>
    this.StringBuilder.AppendFormat(format, arg0.ToString());

  public void Write(string format, object arg0, object arg1) =>
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());

  public void Write(string format, object arg0, object arg1, object arg2) =>
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());

  public void Write(string format, params object[] args) =>
    this.StringBuilder.AppendFormat(format, args);

  public void WriteLine(string value) =>
    this.StringBuilder.AppendLine(value);

  public void WriteLine(string format, object arg0)
  {
    this.StringBuilder.AppendFormat(format, arg0.ToString());
    this.StringBuilder.AppendLine();
  }

  public void WriteLine(string format, object arg0, object arg1)
  {
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());
    this.StringBuilder.AppendLine();
  }

  public void WriteLine(string format, object arg0, object arg1, object arg2)
  {
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
    this.StringBuilder.AppendLine();
  }

  public void WriteLine(string format, params object[] args)
  {
    this.StringBuilder.AppendFormat(format, args);
    this.StringBuilder.AppendLine();
  }

  public override string ToString() => this.StringBuilder.ToString();

  public void Dispose()
  {
    this.StringBuilder = null;
  }
}
```

To replace the default logger, call the following `IMachineRuntime` method:
```C#
void SetLogger(ILogger logger);
```
The above method replaces the previously installed logger on the `RuntimeLogWriter`, and installs the specified one.

Note that `SetLogger` is _not_ calling `Dispose` on the previously installed logger. This must be called explicitly by the user. This allows the logger to be accessed and used after being removed from the P# runtime.

# Using and replacing the formatter
The `IRuntimeLogFormatter` interface is responsible for formatting the various runtime log messages.
```C#
public interface IRuntimeLogFormatter
{
  string FormatOnEnqueueLogMessage(MachineId machineId, string eventName);
  string FormatOnDequeueLogMessage(MachineId machineId, string currStateName, string eventName);
  string FormatOnDefaultLogMessage(MachineId machineId, string currStateName);
  string FormatOnGotoLogMessage(MachineId machineId, string currStateName, string newStateName);
  string FormatOnPushLogMessage(MachineId machineId, string currStateName, string newStateName);
  string FormatOnPopLogMessage(MachineId machineId, string currStateName, string restoredStateName);
  string FormatOnPopUnhandledEventLogMessage(MachineId machineId, string currStateName, string eventName);
  string FormatOnReceiveLogMessage(MachineId machineId, string currStateName, string eventName, bool wasBlocked);
  string FormatOnWaitLogMessage(MachineId machineId, string currStateName, Type eventType);
  string FormatOnWaitLogMessage(MachineId machineId, string currStateName, params Type[] eventTypes);
  string FormatOnSendLogMessage(MachineId targetMachineId, MachineId senderId, string senderStateName,
    string eventName, Guid opGroupId, bool isTargetHalted);
  string FormatOnCreateMachineLogMessage(MachineId machineId, MachineId creator);
  string FormatOnCreateMonitorLogMessage(string monitorTypeName, MachineId monitorId);
  string FormatOnCreateTimerLogMessage(TimerInfo info);
  string FormatOnStopTimerLogMessage(TimerInfo info);
  string FormatOnHaltLogMessage(MachineId machineId, int inboxSize);
  string FormatOnRandomLogMessage(MachineId machineId, object result);
  string FormatOnMachineStateLogMessage(MachineId machineId, string stateName, bool isEntry);
  string FormatOnMachineEventLogMessage(MachineId machineId, string currStateName, string eventName);
  string FormatOnMachineActionLogMessage(MachineId machineId, string currStateName, string actionName);
  string FormatOnMachineExceptionThrownLogMessage(MachineId machineId, string currStateName,
    string actionName, Exception ex);
  string FormatOnMachineExceptionHandledLogMessage(MachineId machineId, string currStateName,
    string actionName, Exception ex);
  string FormatOnMonitorStateLogMessage(string monitorTypeName, MachineId monitorId, string stateName,
    bool isEntry, bool? isInHotState);
  string FormatOnMonitorEventLogMessage(string monitorTypeName, MachineId monitorId, string currStateName,
    string eventName, bool isProcessing);
  string FormatOnMonitorActionLogMessage(string monitorTypeName, MachineId monitorId, string currStateName,
    string actionName);
  string FormatOnErrorLogMessage(string text);
  string FormatOnStrategyErrorLogMessage(SchedulingStrategy strategy, string strategyDescription);
}
```

The installed formatter is called by the `RuntimeLogWriter` to format the log messages before logging them using the installed `ILogger`. By creating a custom `IRuntimeLogFormatter`, you can create your own log message format.

To replace the default formatter, call the following `IMachineRuntime` method:
```C#
void SetLogFormatter(IRuntimeLogFormatter logFormatter);
```
The above method replaces the previously installed formatter on the `RuntimeLogWriter`, and installs the specified one.

# Customizing the runtime log writer
The `RuntimeLogWriter` is used by the runtime to log both internal messages (e.g. a machine was created, or an event was sent), as well as user messages (e.g. via directly using `ILogger` through a machine or monitor). Although typically you would not want to modify the default implementation of the `RuntimeLogWriter`, you have the flexibility to subclass it and set a custom version that suits your needs.

To do this, first subclass `RuntimeLogWriter` and override the methods that you are interested in:
```C#
internal class CustomLogWriter : RuntimeLogWriter
{
  public override void OnEnqueue(MachineId machineId, string eventName)
  {
    // Do something.
  }

  public override void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName,
      Guid opGroupId, bool isTargetHalted)
  {
    // Do something.
  }

  // More overriden methods.
}
```


Finally, set the new implementation using the following `IMachineRuntime` method:
```C#
void SetLogWriter(RuntimeLogWriter logWriter);
```
The above method replaces the previously installed log writer, and installs the specified one. The runtime is going to set the previously installed `ILogger` and `IRuntimeLogFormatter` on the new `RuntimeLogWriter`, so you do not need to reset them.
