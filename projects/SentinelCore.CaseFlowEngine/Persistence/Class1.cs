// Solution: SentinelCore
// Project:   SentinelCore.CaseFlowEngine
// File:         Class1.cs
// Author: Kyle L. Crowder
// Build Num:  110415



namespace SentinelCore.CaseFlowEngine.Persistence;





//Used as example only do not touch





// Principal Entity
public class Blog
{
    public int Id { get; set; }

    public string Name { get; set; }

    // One-to-Many relationship: A Blog can have many Posts 
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}





// Dependent Entity
public class Post
{
    public Blog Blog { get; set; } // Navigation Property
    public int BlogId { get; set; } // Foreign Key
    public int Id { get; set; }
    public string Title { get; set; }
}