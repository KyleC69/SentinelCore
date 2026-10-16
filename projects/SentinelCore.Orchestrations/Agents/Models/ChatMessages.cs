// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ChatMessages.cs
// Author: Kyle L. Crowder
// Build Num:  100908



using System.Collections;




namespace SentinelCore.Orchestrations.Agents.Models;





/// <summary>
/// A super convenient object that can be easily passed around the framework as a universal sharable
/// also encapsulates helper methods for easily adding chat messages.
/// By using this object it simplifies workflow/executor definitions and prevents type mismatch errors.
/// can also be passed in as input.
/// </summary>
public class ChatMessages : IEnumerable<ChatMessage>
{
    private static readonly List<ChatMessage> _messages = new();








    public ChatMessages()
    {
    }








    public ChatMessages(IEnumerable<ChatMessage> messages)
    {
        _messages.AddRange(messages);
    }








    public string Text
    {
        get
        {
            return string.Join("\n", _messages.Select(m => m.Text));
        }
    }



    public ChatMessage this[int index]
    {
        get => _messages[index];
        set => _messages[index] = value;
    }


    public int Count
    {
        get => _messages.Count;
    }








    public IEnumerator<ChatMessage> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }








    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }








    public void Add(ChatMessage? message)
    {
        if (message is not null)
        {
            _messages.Add(message);
        }
    }








    public ChatMessages AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.Assistant, content));
        return new ChatMessages(_messages);
    }








    public void AddRange(IEnumerable<ChatMessage> messages)
    {
        _messages.AddRange(messages);
    }








    public ChatMessages AddSystemMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.System, content));
        return new ChatMessages(_messages);
    }








    public ChatMessages AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
        return new ChatMessages(_messages);
    }








    public void Clear()
    {
        _messages.Clear();
    }








    public bool Remove(ChatMessage message)
    {
        // Is there a comparer needed here?
        return _messages.Remove(message);
    }








    public void RemoveAt(int index)
    {
        _messages.RemoveAt(index);
    }
}