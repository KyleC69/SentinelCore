// Solution: SentinelCoreLib
// Project:   SentinelCore.Tests
// File:         Test1.cs
// Author: Kyle L. Crowder
// Build Date: 2026/07/07



namespace SentinelCore.Tests;





[TestClass]
public sealed class Test1
{

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // This method is called once for the test class, after all tests of the class are run.
    }








    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        // This method is called once for the test class, before any tests of the class are run.
    }








    [TestCleanup]
    public void TestCleanup()
    {
        // This method is called after each test method.
    }








    [TestInitialize]
    public void TestInit()
    {
        // This method is called before each test method.
    }








    [TestMethod]
    public void TestMethod1()
    {
    }
}