// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         MagenticPromptOverrides.cs
// Author: Kyle L. Crowder
// Build Num:  100908



namespace SentinelCore.Orchestrations.Orchestrations;





public static class MagenticPromptOverrides
{

    /// <summary>The default template for synthesizing the final answer. Placeholders: <c>{task}</c>.</summary>
    public static readonly string FinalAnswerPrompt = """
                                                      We are working on the following task:

                                                      {task}

                                                      We have completed the task.

                                                      The above messages contain the conversation that took place to complete the task.

                                                      Based on the information gathered, provide the final answer to the original request.

                                                      The answer should be phrased as if you were speaking directly to the user.

                                                      Use the following structure exactly:

                                                      # Investigation Summary

                                                      Provide a concise summary of the findings.

                                                      # Evidence

                                                      For each significant piece of evidence provide:

                                                      Evidence ID: PENDING
                                                      Source:
                                                      Observation:
                                                      Reliability:
                                                      - High
                                                      - Medium
                                                      - Low

                                                      Evidence Classification:
                                                      - Direct Evidence
                                                      - Correlated Evidence
                                                      - Circumstantial Evidence
                                                      - Contradictory Evidence

                                                      Relevance:
                                                      Explain specifically how this evidence relates to the investigation objective.

                                                      Impact:
                                                      Explain how strongly this evidence influences the assessment.

                                                      # Supporting Correlations

                                                      List related observations that strengthen the findings and reference the associated Evidence IDs.

                                                      # Contradictions and Alternate Explanations

                                                      List evidence that weakens, contradicts, or fails to support the primary conclusion.

                                                      # Investigation Limitations

                                                      Document:
                                                      - Missing evidence
                                                      - Unavailable artifacts
                                                      - Failed tools
                                                      - Incomplete data

                                                      # Assessment

                                                      Known Facts:
                                                      Only include information directly supported by evidence.

                                                      Probable Cause:
                                                      
                                                      If you can't provide definitive proof to support a theory then try to gather proof or approach it from a different perspective. 
                                                      If you have supporting evidence, explain the causal relationship and cite the supporting Evidence IDs.
                                                      State the most likely explanation and cite the supporting Evidence IDs.

                                                      Alternative Explanations:
                                                      List other plausible explanations and explain why they were not selected.

                                                      Important Rules:

                                                      - Separate observations from conclusions.
                                                      - Do not present assumptions as facts.
                                                      - Every conclusion must be supported by one or more Evidence IDs.
                                                      - Evidence relevance must be explicitly stated and factual.
                                                      - Correlation does not imply causation unless supported by direct evidence.
                                                      - If evidence is insufficient, research other possibilities and gather more evidence, and state this clearly.
                                                      """;

    /// <summary>
    ///     The default progress-ledger template. Placeholders: <c>{task}</c>, <c>{team}</c>, <c>{questions}</c>,
    ///     <c>{schema}</c>. An override must keep <c>{schema}</c> so the JSON schema the response is parsed against can
    ///     be injected.
    /// </summary>
    public static readonly string ProgressLedgerPrompt = """
                                                         Recall we are working on the following request:

                                                         {task}

                                                         And we have assembled the following team:

                                                         {team}

                                                         To make progress on the request, please answer the following questions, including necessary reasoning:

                                                         {questions}

                                                         Please output an answer in pure JSON format according to the following schema. The JSON object must be parsable as-is.
                                                         DO NOT OUTPUT ANYTHING OTHER THAN JSON, AND DO NOT DEVIATE FROM THIS SCHEMA:

                                                         {schema}
                                                         """;

    /// <summary>The default template for gathering the initial fact sheet. Placeholders: <c>{task}</c>.</summary>
    public static readonly string TaskLedgerFactsPrompt = """
                                                          Below I will present a signal that you will be investigating. A signal is some condition or event that has been observed 
                                                          and needs to be investigated to determine the root cause. The signal may be a system event, a user request, telemetry, 
                                                          or any other type of observation that requires further analysis.
                                                          
                                                          Before we begin addressing the request, please answer the following pre-survey to the best of your ability.
                                                                  Keep in mind that a fact is a specific piece of information that is backed by irefutable evidence. 
                                                                  A fact is not an assumption, a guess, or a hunch. A fact is something that can be verified and supported by evidence. 
                                                                  When you are unsure about a fact, it is better to leave it out than to include it without verification.
                                                          
                                                                  Here is the InvestigationObjective object containing the original request and a summary of the signal to be investigated:
                                                          
                                                          {task}
                                                          
                                                              Here is the pre-survey:
                                                          
                                                              1. Please list any specific facts or figures that are GIVEN in the request itself. It is possible that
                                                                 there are none.
                                                              2. Please list any facts that may need to be looked up, and WHERE SPECIFICALLY they might be found.
                                                                 In some cases, authoritative sources are mentioned in the request itself.
                                                              3. Please list any potential causes or explanations for the observed behavior or event. These may be based on prior experience, domain knowledge, or other sources of information that 
                                                                 may be relevant to the investigation. These are not facts, but rather hypotheses that will need to be tested and verified.
                                                              4. Please list any facts that are recalled from memory, hunches, well-reasoned guesses, etc. that all need to be researched and proven. 
                                                              These are not facts, but rather educated guesses that may be useful in guiding the investigation.
                                                          
                                                          When answering this survey, keep in mind that "facts" will typically be specific names, dates, statistics, etc.
                                                          Your answer should use headings:
                                                          
                                                              1. GIVEN OR VERIFIED FACTS
                                                              2. FACTS TO LOOK UP
                                                              3. POTENTIAL CAUSES
                                                              4. EDUCATED GUESSES
                                                          
                                                          DO NOT include any other headings or sections in your response. DO NOT list next steps or plans until asked to do so.
                                                          """;



    /// <summary>The default template for updating the fact sheet on a replan. Placeholders: <c>{task}</c>, <c>{old_facts}</c>.</summary>
    public static readonly string TaskLedgerFactsUpdatePrompt = """
                                                                As a reminder, we are working to solve the following task:

                                                                {task}

                                                                It is clear we are not making as much progress as we would like, but we may have learned something new.
                                                                Please rewrite the following fact sheet, updating it to include anything new we have learned that may be helpful.

                                                                Here is the old fact sheet:

                                                                {old_facts}
                                                                """;

    /// <summary>
    ///     The default template for the full task ledger (the plan-event text combining facts and plan).
    ///     Placeholders: <c>{task}</c>, <c>{team}</c>, <c>{facts}</c>, <c>{plan}</c>.
    /// </summary>
    public static readonly string TaskLedgerFullPrompt = """
                                                         We are working to gather evidence to find root cause for the following signal:

                                                         {task}


                                                         To answer this request we have assembled the following team:

                                                         {team}


                                                         Here is an initial fact sheet to consider:

                                                         {facts}


                                                         Here is the plan to follow as best as possible:

                                                         {plan}
                                                         """;



    /// <summary>The default template for creating the initial plan. Placeholders: <c>{team}</c>.</summary>

    /// <summary>The default template for creating the initial plan. Placeholders: <c>{team}</c>.</summary>
    public static readonly string TaskLedgerPlanPrompt = """
                                                                 
                                                         We have been requested to find the root cause and source of the signal, a reported condition or system event or perform an investigation based on the users request.
                                                         We have the following team members to perform this investigation:
                                                                                                                  
                                                         {team}
                                                                         
                                                         Each member has their own unique role to perform in this investigation.
                                                         Please devise a plan of the evidence needed to collect from the system to find the cause or reason for the observed behavior or event described in the
                                                         original request in field 'OriginalSignal'. There is a generated 'SignalSummary' that may help you understand the signal better.
                                                         The primary goal of the investigation is to diagnose the root cause of the signal or condition and if possible list remediation steps.
                                                                 
                                                         """;

    /// <summary>The default template for updating the plan on a replan. Placeholders: <c>{team}</c>.</summary>
    public static readonly string TaskLedgerPlanUpdatePrompt = """
                                                               Please briefly explain what went wrong on this last run
                                                               (the root cause of the failure), and then come up with a new plan that takes steps and includes hints to overcome prior
                                                               challenges and especially avoids repeating the same mistakes. As before, the new plan should be concise, expressed in
                                                               bullet-point form. List any problems you encountered using tools or accessing information.

                                                               {team}
                                                               """;
}