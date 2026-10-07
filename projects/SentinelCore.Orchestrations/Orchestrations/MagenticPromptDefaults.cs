namespace SentinelCore.Orchestrations.Orchestrations;


public static class MagenticPromptDefaults
{
    /// <summary>The default template for gathering the initial fact sheet. Placeholders: <c>{task}</c>.</summary>
    public static readonly string TaskLedgerFactsPrompt = """
                                                          You will be presented with an Investigation Objective object that contains a hypothesis or an educated guess as to what the root cause of a problem or event may be. 
                                                          The object will also contain a Signal Summary field that contains an observed problem or event log entry or other automated input device. 
                                                          Your task is to devise a plan to gather evidence from the system to prove or refute TheCore's hypothesis.

                                                              Here is the signal object:

                                                          {task}

                                                    

                                                          DO NOT fabricate any facts. You must be able to show the evidence for any fact you present.
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



    /// <summary>The default template for creating the initial plan. Placeholders: <c>{team}</c>.</summary>
    public static readonly string TaskLedgerPlanPrompt = """
        
Fantastic. To address this request we have assembled the following team:
                                                         
{team}
                
Each member is equipped with the same tools and can be used in parallel on complicated tasks.
Please devise a short bullet-point plan for gathering evidence for the
original request.
        
""";

    /// <summary>The default template for updating the plan on a replan. Placeholders: <c>{team}</c>.</summary>
    public static readonly string TaskLedgerPlanUpdatePrompt = """
Please briefly explain what went wrong on this last run
(the root cause of the failure), and then come up with a new plan that takes steps and includes hints to overcome prior
challenges and especially avoids repeating the same mistakes. As before, the new plan should be concise, expressed in
bullet-point form. List any problems you encountered using tools or accessing information.

{team}
""";

    /// <summary>
    /// The default template for the full task ledger (the plan-event text combining facts and plan).
    /// Placeholders: <c>{task}</c>, <c>{team}</c>, <c>{facts}</c>, <c>{plan}</c>.
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

    /// <summary>
    /// The default progress-ledger template. Placeholders: <c>{task}</c>, <c>{team}</c>, <c>{questions}</c>,
    /// <c>{schema}</c>. An override must keep <c>{schema}</c> so the JSON schema the response is parsed against can
    /// be injected.
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

    /// <summary>The default template for synthesizing the final answer. Placeholders: <c>{task}</c>.</summary>
    public static readonly string FinalAnswerPrompt = """
We are working on the following task:
{task}

We have completed the task.

The above messages contain the conversation that took place to complete the task.

Present the evidence in table format with the following columns: "Subsystem - where", "Task - property or setting", "Evidence - value", and "Notes".
The evidence should be presented in a concise and clear manner, with each row representing a specific piece of evidence gathered during the investigation and easily ingested by AI systems. 
The "Notes" column should provide any additional context or information that may be relevant to understanding the evidence.
""";
}
