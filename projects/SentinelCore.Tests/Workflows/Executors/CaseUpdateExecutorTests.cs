// Solution: SentinelCore
// Project:   SentinelCore.Tests
// File:         CaseUpdateExecutorTests.cs
// Author: Kyle L. Crowder
// Build Num:  100310



using Microsoft.Agents.AI.Workflows;

using Moq;

using SentinelCore.CaseFlowEngine.Cfe;
using SentinelCore.Orchestrations.Workflows.Executors;




namespace SentinelCore.Tests.Workflows.Executors;





[TestClass]
public class CaseUpdateExecutorTests
{
    private CaseUpdateExecutor _executor;
    private Mock<ICaseFlowEngine> _mockEngine;








    [TestMethod]
    public async Task HandleStringAsync_ThrowsNotImplementedException()
    {
        // Arrange
        string message = "test update";
        Mock<IWorkflowContext> mockContext = new();

        // Act & Assert
        try
        {
            await _executor.HandleStringAsync(message, mockContext.Object, CancellationToken.None);
            Assert.Fail("Should have thrown NotImplementedException");
        }
        catch (NotImplementedException)
        {
            // Success
        }
    }








    [TestInitialize]
    public void Setup()
    {
        _mockEngine = new Mock<ICaseFlowEngine>();
        _executor = new CaseUpdateExecutor(_mockEngine.Object);
    }
}