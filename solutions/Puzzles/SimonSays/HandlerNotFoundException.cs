namespace SimonSays;

using System;
using System.Text;

public class HandlerNotFoundException(string type) : Exception
{
    public override string Message => $"Could not find message handler for type '{type}'. Are you sure you registered a handler for it?";
}