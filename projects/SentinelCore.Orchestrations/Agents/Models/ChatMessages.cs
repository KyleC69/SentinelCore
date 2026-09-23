// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         ChatMessages.cs
// Author: Kyle L. Crowder
// Build Num:  092308



using System.Collections;




namespace SentinelCore.Orchestrations.Agents.Models;





// An IEnumerable of ChatMessage objects that can be used to represent a conversation or a series of messages exchanged between participants. This class can be used to store and manage chat messages in a structured manner, allowing for easy access and manipulation of the messages within the context of an application or system that handles chat interactions.
public class ChatMessages : IEnumerable<ChatMessage>
{
    private readonly List<ChatMessage> _messages = new();








    public ChatMessages()
    {
    }








    public ChatMessages(IEnumerable<ChatMessage> messages)
    {
        _messages.AddRange(messages);
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








    public void Add(ChatMessage message)
    {
        _messages.Add(message);
    }








    public void AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.Assistant, content));
    }








    public void AddRange(IEnumerable<ChatMessage> messages)
    {
        _messages.AddRange(messages);
    }








    public void AddSystemMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.System, content));
    }








    public void AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
    }








    public void Clear()
    {
        _messages.Clear();
    }








    public bool Remove(ChatMessage message)
    {
        return _messages.Remove(message);
    }








    public void RemoveAt(int index)
    {
        _messages.RemoveAt(index);
    }
}