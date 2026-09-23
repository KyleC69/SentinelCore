// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         BlockPhrases.cs
// Author: Kyle L. Crowder
// Build Num:  092308



namespace SentinelCore.Orchestrations.SafetyEngine;





public static class SafetyTriggerTerms
{
    public static readonly IEnumerable<SafetyIndicator> AllIndicators = new List<SafetyIndicator>
    {
            // Self-harm / crisis (Weight: 5, Review: true)
            new SafetyIndicator("suicide", "Self-harm", 5, true),
            new SafetyIndicator("kill myself", "Self-harm", 5, true),
            new SafetyIndicator("end my life", "Self-harm", 5, true),
            new SafetyIndicator("self harm", "Self-harm", 4, true),
            new SafetyIndicator("hurt myself", "Self-harm", 4, true),
            new SafetyIndicator("overdose", "Self-harm", 5, true),
            new SafetyIndicator("no reason to live", "Self-harm", 5, true),
            new SafetyIndicator("want to die", "Self-harm", 5, true),

            // Violence (Weight: 4, Review: true)
            new SafetyIndicator("kill", "Violence", 4, true),
            new SafetyIndicator("murder", "Violence", 4, true),
            new SafetyIndicator("assassinate", "Violence", 4, true),
            new SafetyIndicator("mass casualty", "Violence", 4, true),
            new SafetyIndicator("shooting", "Violence", 4, true),
            new SafetyIndicator("stabbing", "Violence", 4, true),
            new SafetyIndicator("eliminate target", "Violence", 4, true),
            new SafetyIndicator("violent revenge", "Violence", 3, false),
            new SafetyIndicator("bomb", "Violence", 5, true),
            new SafetyIndicator("explosive device", "Violence", 5, true),

            // Weapons (Weight: 3, Review: false)
            new SafetyIndicator("firearm modification", "Weapons", 3, false),
            new SafetyIndicator("concealed weapon", "Weapons", 3, false),
            new SafetyIndicator("ghost gun", "Weapons", 3, false),
            new SafetyIndicator("weapon construction", "Weapons", 3, false),
            new SafetyIndicator("improvised weapon", "Weapons", 3, false),

            // Criminal activity (Weight: 3, Review: false)
            new SafetyIndicator("break into", "Crime", 3, false),
            new SafetyIndicator("bypass security", "Crime", 3, false),
            new SafetyIndicator("steal", "Crime", 2, false),
            new SafetyIndicator("fraud", "Crime", 3, false),
            new SafetyIndicator("identity theft", "Crime", 3, false),
            new SafetyIndicator("counterfeit", "Crime", 2, false),
            new SafetyIndicator("money laundering", "Crime", 3, false),

            // Cybersecurity abuse (Weight: 4, Review: true)
            new SafetyIndicator("hack into", "Cybersecurity", 4, true),
            new SafetyIndicator("credential theft", "Cybersecurity", 4, true),
            new SafetyIndicator("password cracking", "Cybersecurity", 4, true),
            new SafetyIndicator("ransomware", "Cybersecurity", 5, true),
            new SafetyIndicator("malware", "Cybersecurity", 4, true),
            new SafetyIndicator("keylogger", "Cybersecurity", 4, true),
            new SafetyIndicator("privilege escalation", "Cybersecurity", 5, true),
            new SafetyIndicator("exploit vulnerability", "Cybersecurity", 5, true),
            new SafetyIndicator("data exfiltration", "Cybersecurity", 5, true),
            new SafetyIndicator("phishing campaign", "Cybersecurity", 4, true),
            new SafetyIndicator("botnet", "Cybersecurity", 4, true),

            // Dangerous chemicals (Weight: 5, Review: true)
            new SafetyIndicator("poison", "Chemicals", 5, true),
            new SafetyIndicator("toxic substance", "Chemicals", 4, true),
            new SafetyIndicator("chemical weapon", "Chemicals", 5, true),
            new SafetyIndicator("nerve agent", "Chemicals", 5, true),

            // Extremism indicators (Weight: 4, Review: true)
            new SafetyIndicator("violent extremist", "Extremism", 4, true),
            new SafetyIndicator("terrorist attack", "Extremism", 4, true),
            new SafetyIndicator("recruitment", "Extremism", 3, false),
            new SafetyIndicator("martyrdom operation", "Extremism", 5, true),

            // Coercion / abuse (Weight: 4, Review: true)
            new SafetyIndicator("blackmail", "Abuse", 4, true),
            new SafetyIndicator("extortion", "Abuse", 4, true),
            new SafetyIndicator("kidnap", "Abuse", 4, true),
            new SafetyIndicator("hostage", "Abuse", 4, true),

            // Financial scams (Weight: 3, Review: false)
            new SafetyIndicator("investment scam", "Finance", 3, false),
            new SafetyIndicator("pump and dump", "Finance", 3, false),
            new SafetyIndicator("wire fraud", "Finance", 3, false),
            new SafetyIndicator("account takeover", "Finance", 3, false),

            // Privacy invasion (Weight: 3, Review: false)
            new SafetyIndicator("track without consent", "Privacy", 3, false),
            new SafetyIndicator("spy on", "Privacy", 3, false),
            new SafetyIndicator("surveillance target", "Privacy", 3, false),
            new SafetyIndicator("doxxing", "Privacy", 4, true),
            new SafetyIndicator("personal information leak", "Privacy", 3, false),

            // Evasion language (Weight: 3, Review: false)
            new SafetyIndicator("undetectable", "Evasion", 3, false),
            new SafetyIndicator("avoid detection", "Evasion", 3, false),
            new SafetyIndicator("bypass monitoring", "Evasion", 3, false),
            new SafetyIndicator("cover my tracks", "Evasion", 3, false),
            new SafetyIndicator("leave no evidence", "Evasion", 3, false),
            new SafetyIndicator("anonymous attack", "Evasion", 3, false)
    };

    public static readonly IEnumerable<SafetyIndicator> ContextEscalationTerms = new List<SafetyIndicator>
    {
            new SafetyIndicator("urgent", "Context", 2, false),
            new SafetyIndicator("immediately", "Context", 2, false),
            new SafetyIndicator("must succeed", "Context", 3, true),
            new SafetyIndicator("guaranteed", "Context", 3, true),
            new SafetyIndicator("highest damage", "Context", 3, false),
            new SafetyIndicator("maximum impact", "Context", 3, false),
            new SafetyIndicator("most effective", "Context", 3, false),
            new SafetyIndicator("best target", "Context", 3, false),
            new SafetyIndicator("largest disruption", "Context", 3, false)
    };

    public static readonly IEnumerable<SafetyIndicator> SuspiciousPhrases = new List<SafetyIndicator>
    {
            new SafetyIndicator("how do i get away with", "Circumvention", 3, false),
            new SafetyIndicator("without getting caught", "Circumvention", 3, false),
            new SafetyIndicator("completely anonymous", "Circumvention", 2, false),
            new SafetyIndicator("untraceable method", "Circumvention", 3, false),
            new SafetyIndicator("step by step instructions", "Instruction", 2, false),
            new SafetyIndicator("best way to attack", "Instruction", 3, false),
            new SafetyIndicator("easiest target", "Instruction", 3, false),
            new SafetyIndicator("remove all evidence", "Circumvention", 3, false),
            new SafetyIndicator("disable protection", "Circumvention", 3, false),
            new SafetyIndicator("circumvent safeguards", "Circumvention", 3, false),
            new SafetyIndicator("bypass restrictions", "Circumvention", 3, false),
            new SafetyIndicator("evade law enforcement", "Circumvention", 3, false),
            new SafetyIndicator("avoid monitoring", "Circumvention", 3, false),
            new SafetyIndicator("hide my activity", "Circumvention", 3, false),
            new SafetyIndicator("erase logs", "Circumvention", 3, false)
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
    ///     An accumulative weight for all the identified terms in the prompt, when the weight exceeds a threshold the prompt is flagged for review.  The
    ///     accumulation of weights trigger when limit is hit. This is designed to catch pattern of behavior that may be dangerous or harmful.
    ///     The prompt is not blocked, but should be reviewed to ensure nothing slips by and a warning issued just to alert user they are approaching a guardrail.
    ///     A system message is also passed into the users context so TheCore will be aware of the potential risk and can adjust its responses accordingly.
    ///     A perfect example of this scenario might be someone with a potty mouth, they may use a lot of curse words and the weight of those words may offend others.
    ///     The value should be set with this in mind: the value should be bumped up the more deterministic the term is, and leaves very little room for interpretation.
    ///     The value should be lowered the more ambiguous the term is, and leaves a lot of room for interpretation, the review agent fills this gap to ensure accurate assessment.
    /// </param>
    /// <param name="RequiresImmediateReview">
    ///     Determines if term immediately triggers review or if more terms are required
    ///     (danger quotiant)
    /// </param>
    public sealed record SafetyIndicator(string Term, string Category, int Weight, bool RequiresImmediateReview);
}