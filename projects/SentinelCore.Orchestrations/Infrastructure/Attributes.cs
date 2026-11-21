//Intentionally in root namespace for availability across the project without needing to add using statements in every file.
namespace SentinelCore;


/// <summary>
/// Attribute indicates member must not exist in production
/// </summary>
[AttributeUsage(AttributeTargets.Method |
                AttributeTargets.Class |
                AttributeTargets.Property)]
public sealed class TemporaryAttribute : Attribute
{
    public string Reason { get; }

    public TemporaryAttribute(string reason)
    {
        Reason = reason;
    }
}
