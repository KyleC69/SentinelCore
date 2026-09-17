// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         ChatMessagesTests.cs
// Author: Kyle L. Crowder
// Build Num:  091418

using SentinelCore.Orchestrations.Agents.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SentinelCore.Tests;

[TestClass]
public class ChatMessagesTests
{
    [TestMethod]
    public void ChatMessages_ShouldInitializeEmpty()
    {
        var messages = new ChatMessages();
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void ChatMessages_ShouldAddMessages()
    {
        var messages = new ChatMessages();
        var message = new ChatMessage(ChatRole.User, "Hello");
        
        messages.Add(message);
        
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Hello", messages[0].Text);
    }

    [TestMethod]
    public void ChatMessages_ShouldAddUserMessage()
    {
        var messages = new ChatMessages();
        
        messages.AddUserMessage("Hello");
        
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(ChatRole.User, messages[0].Role);
        Assert.AreEqual("Hello", messages[0].Text);
    }

    [TestMethod]
    public void ChatMessages_ShouldAddAssistantMessage()
    {
        var messages = new ChatMessages();
        
        messages.AddAssistantMessage("Hi there");
        
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(ChatRole.Assistant, messages[0].Role);
        Assert.AreEqual("Hi there", messages[0].Text);
    }

    [TestMethod]
    public void ChatMessages_ShouldAddSystemMessage()
    {
        var messages = new ChatMessages();
        
        messages.AddSystemMessage("You are a helpful assistant");
        
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(ChatRole.System, messages[0].Role);
        Assert.AreEqual("You are a helpful assistant", messages[0].Text);
    }

    [TestMethod]
    public void ChatMessages_ShouldClear()
    {
        var messages = new ChatMessages();
        messages.AddUserMessage("Hello");
        
        messages.Clear();
        
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void ChatMessages_ShouldRemoveAt()
    {
        var messages = new ChatMessages();
        messages.AddUserMessage("1");
        messages.AddUserMessage("2");
        
        messages.RemoveAt(0);
        
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("2", messages[0].Text);
    }
}
