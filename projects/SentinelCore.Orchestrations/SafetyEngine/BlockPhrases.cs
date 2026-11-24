// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         BlockPhrases.cs
// Author: Kyle L. Crowder
// Build Num:  112101



namespace SentinelCore.Orchestrations.SafetyEngine;





public static class SafetyTriggerTerms
{
    public static readonly IEnumerable<SafetyIndicator> AllIndicators = new List<SafetyIndicator>
    {
            // Self-harm / crisis (Weight: 5, Review: true)
            new("suicide", "Self-harm", 5, true),
            new("kill myself", "Self-harm", 5, true),
            new("end my life", "Self-harm", 5, true),
            new("self harm", "Self-harm", 4, true),
            new("hurt myself", "Self-harm", 4, true),
            new("overdose", "Self-harm", 5, true),
            new("no reason to live", "Self-harm", 5, true),
            new("want to die", "Self-harm", 5, true),

            // Violence (Weight: 4, Review: true)
            new("kill", "Violence", 4, true),
            new("murder", "Violence", 4, true),
            new("assassinate", "Violence", 4, true),
            new("mass casualty", "Violence", 4, true),
            new("shooting", "Violence", 4, true),
            new("stabbing", "Violence", 4, true),
            new("eliminate target", "Violence", 4, true),
            new("violent revenge", "Violence", 3, false),
            new("bomb", "Violence", 5, true),
            new("explosive device", "Violence", 5, true),

            // Weapons (Weight: 3, Review: false)
            new("firearm modification", "Weapons", 3, false),
            new("concealed weapon", "Weapons", 3, false),
            new("ghost gun", "Weapons", 3, false),
            new("weapon construction", "Weapons", 3, false),
            new("improvised weapon", "Weapons", 3, false),

            // Criminal activity (Weight: 3, Review: false)
            new("break into", "Crime", 3, false),
            new("bypass security", "Crime", 3, false),
            new("steal", "Crime", 2, false),
            new("fraud", "Crime", 3, false),
            new("identity theft", "Crime", 3, false),
            new("counterfeit", "Crime", 2, false),
            new("money laundering", "Crime", 3, false),

            // Cybersecurity abuse (Weight: 4, Review: true)
            new("hack into", "Cybersecurity", 4, true),
            new("credential theft", "Cybersecurity", 4, true),
            new("password cracking", "Cybersecurity", 4, true),
            new("ransomware", "Cybersecurity", 5, true),
            new("malware", "Cybersecurity", 4, true),
            new("keylogger", "Cybersecurity", 4, true),
            new("privilege escalation", "Cybersecurity", 5, true),
            new("exploit vulnerability", "Cybersecurity", 5, true),
            new("data exfiltration", "Cybersecurity", 5, true),
            new("phishing campaign", "Cybersecurity", 4, true),
            new("botnet", "Cybersecurity", 4, true),

            // Dangerous chemicals (Weight: 5, Review: true)
            new("poison", "Chemicals", 5, true),
            new("toxic substance", "Chemicals", 4, true),
            new("chemical weapon", "Chemicals", 5, true),
            new("nerve agent", "Chemicals", 5, true),

            // Extremism indicators (Weight: 4, Review: true)
            new("violent extremist", "Extremism", 4, true),
            new("terrorist attack", "Extremism", 4, true),
            new("recruitment", "Extremism", 3, false),
            new("martyrdom operation", "Extremism", 5, true),

            // Coercion / abuse (Weight: 4, Review: true)
            new("blackmail", "Abuse", 4, true),
            new("extortion", "Abuse", 4, true),
            new("kidnap", "Abuse", 4, true),
            new("hostage", "Abuse", 4, true),

            // Financial scams (Weight: 3, Review: false)
            new("investment scam", "Finance", 3, false),
            new("pump and dump", "Finance", 3, false),
            new("wire fraud", "Finance", 3, false),
            new("account takeover", "Finance", 3, false),

            // Privacy invasion (Weight: 3, Review: false)
            new("track without consent", "Privacy", 3, false),
            new("spy on", "Privacy", 3, false),
            new("surveillance target", "Privacy", 3, false),
            new("doxxing", "Privacy", 4, true),
            new("personal information leak", "Privacy", 3, false),

            // Evasion language (Weight: 3, Review: false)
            new("undetectable", "Evasion", 3, false),
            new("avoid detection", "Evasion", 3, false),
            new("bypass monitoring", "Evasion", 3, false),
            new("cover my tracks", "Evasion", 3, false),
            new("leave no evidence", "Evasion", 3, false),
            new("anonymous attack", "Evasion", 3, false)
    };

    public static readonly IEnumerable<SafetyIndicator> ContextEscalationTerms = new List<SafetyIndicator>
    {
            new("urgent", "Context", 2, false),
            new("immediately", "Context", 2, false),
            new("must succeed", "Context", 3, true),
            new("guaranteed", "Context", 3, true),
            new("highest damage", "Context", 3, false),
            new("maximum impact", "Context", 3, false),
            new("most effective", "Context", 3, false),
            new("best target", "Context", 3, false),
            new("largest disruption", "Context", 3, false)
    };

    public static readonly IEnumerable<SafetyIndicator> SuspiciousPhrases = new List<SafetyIndicator>
    {
            new("how do i get away with", "Circumvention", 3, false),
            new("without getting caught", "Circumvention", 3, false),
            new("completely anonymous", "Circumvention", 2, false),
            new("untraceable method", "Circumvention", 3, false),
            new("step by step instructions", "Instruction", 2, false),
            new("best way to attack", "Instruction", 3, false),
            new("easiest target", "Instruction", 3, false),
            new("remove all evidence", "Circumvention", 3, false),
            new("disable protection", "Circumvention", 3, false),
            new("circumvent safeguards", "Circumvention", 3, false),
            new("bypass restrictions", "Circumvention", 3, false),
            new("evade law enforcement", "Circumvention", 3, false),
            new("avoid monitoring", "Circumvention", 3, false),
            new("hide my activity", "Circumvention", 3, false),
            new("erase logs", "Circumvention", 3, false)
    };








    /// <summary>
    ///     Gets a comprehensive list of all defined safety indicators (terms and phrases) across all categories.
    /// </summary>
    /// <returns>A collection of SafetyIndicator records.</returns>
    public static IEnumerable<SafetyIndicator> GetAllIndicators()
    {
        return AllIndicators.Concat(SuspiciousPhrases).Concat(ContextEscalationTerms);
    }








    /// <summary>
    ///     Represents a term or phrase that may trigger a safety assesment of the prompt before the prompt sees the context or
    ///     any other agent.
    /// </summary>
    /// <param name="Term">The word or phrase</param>
    /// <param name="Category">A descriptive name for the group of terms, hate, sex, violence etc.</param>
    /// <param name="Weight">
    ///     An accumulative weight for all the identified terms in the prompt, when the weight exceeds a threshold the prompt
    ///     is flagged for review.  The
    ///     accumulation of weights trigger when limit is hit. This is designed to catch pattern of behavior that may be
    ///     dangerous or harmful.
    ///     The prompt is not blocked, but should be reviewed to ensure nothing slips by and a warning issued just to alert
    ///     user they are approaching a guardrail.
    ///     A system message is also passed into the users context so TheCore will be aware of the potential risk and can
    ///     adjust its responses accordingly.
    ///     A perfect example of this scenario might be someone with a potty mouth, they may use a lot of curse words and the
    ///     weight of those words may offend others.
    ///     The value should be set with this in mind: the value should be bumped up the more deterministic the term is, and
    ///     leaves very little room for interpretation.
    ///     The value should be lowered the more ambiguous the term is, and leaves a lot of room for interpretation, the review
    ///     agent fills this gap to ensure accurate assessment.
    /// </param>
    /// <param name="RequiresImmediateReview">
    ///     Determines if term immediately triggers review or if more terms are required
    ///     (danger quotiant)
    /// </param>
    public sealed record SafetyIndicator(string Term, string Category, int Weight, bool RequiresImmediateReview);
}