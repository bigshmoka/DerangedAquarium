using System;

// Base blueprint for all developer commands
public class DevCommandBase
{
    public string CommandId { get; private set; }
    public string CommandDescription { get; private set; }
    public string CommandFormat { get; private set; }

    public DevCommandBase(string id, string description, string format)
    {
        this.CommandId = id;
        this.CommandDescription = description;
        this.CommandFormat = format;
    }
}

// Subclass for commands with zero arguments (e.g., "help")
public class DevCommand : DevCommandBase
{
    private Action commandAction;

    public DevCommand(string id, string description, string format, Action action) : base(id, description, format)
    {
        this.commandAction = action;
    }

    public void Invoke()
    {
        commandAction?.Invoke();
    }
}

// Subclass for commands with one parameter argument (e.g., "money 500", "spawn goldfish")
public class DevCommand<T1> : DevCommandBase
{
    private Action<T1> commandAction;

    public DevCommand(string id, string description, string format, Action<T1> action) : base(id, description, format)
    {
        this.commandAction = action;
    }

    public void Invoke(T1 value)
    {
        commandAction?.Invoke(value);
    }
}