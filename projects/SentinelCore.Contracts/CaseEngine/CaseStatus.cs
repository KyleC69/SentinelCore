// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         CaseStatus.cs
// Author: Kyle L. Crowder
// Build Num:  081312

//AGENTS - DO NOT MODIFY THIS FILE - SOURCE OF TRUTH: THERE ARE NO VALID STATUS BEYOND THIS FILE.



namespace SentinelCore.CaseEngine;





/// <summary>
///     Status values for a case lifecycle.
/// </summary>
public enum CaseStatus
{

    /// <summary>
    ///     The status of initialized is a queue type of state. The case has not been passed into the workflow yes
    ///     The case has only been created and is waiting to be processed by TheCore. The system has an internal timer that
    ///     fires and any case that is
    ///     in this status will start being processed.
    /// </summary>
    Initialized,

    /// <summary>
    /// </summary>
    Open,

    /// <summary>
    ///     The case is being analyzed (hypotheses generation).
    /// </summary>
    Analysis,

    /// <summary>
    ///     CASE HAS BEEN TURNED OVER FOR INVESTIGATION. THE CORE IS NOW ATTEMPTING TO GATHER EVIDENCE AND TEST HYPOTHESES.
    ///     THE CASE SHOULD ONLY BE IN THIS STATUS FOR A LIMITED TIME. IF THE CASE IS IN THIS STATUS FOR TOO LONG, IT MAY BE
    ///     STUCK OR BLOCKED.
    ///     BASELINE TIME LIMITS FOR INVESTIGATION ARE 1-2 HOURS. IF THE CASE IS IN THIS STATUS FOR MORE THAN 2 HOURS, IT MUST
    ///     BE ESCALATED FOR HUMAN ATTENTION.
    ///     THE CASE MAY BE BLOCKED BY A SAFETY RULE, IN WHICH CASE IT WILL BE AUTOMATICALLY ESCALATED.
    /// </summary>
    Investigation,

    /// <summary>
    ///     The case is awaiting review/approval BY A HUMAN
    ///     IN THIS STATUS A HUMAN IS VALIDATING THE ANALYSIS AND INVESTIGATION RESULTS. IT CAN BE ACCEPTED OR REJECTED.
    ///     IF ACCEPTED, THE CASE WILL PROCEED TO RESOLVED. IF REJECTED, THE CASE WILL RETURN TO INVESTIGATION.
    /// </summary>
    Review,

    /// <summary>
    ///     THE CORE IS AWAITING FOR A HUMAN TO PROVIDE INPUT. THE CORE CANNOT PROCEED WITHOUT THIS INPUT.
    ///     THIS STATUS IS USED WHEN THE CORE REQUIRES ADDITIONAL INFORMATION OR CLARIFICATION FROM A HUMAN OPERATOR BEFORE IT
    ///     CAN CONTINUE WITH THE CASE.
    ///     THE CASE WILL REMAIN IN THIS STATUS UNTIL THE REQUIRED INPUT IS PROVIDED. ONCE THE INPUT IS RECEIVED, THE CASE WILL
    ///     RETURN TO INVESTIGATION STAGE.
    ///     THIS STATUS IS CRITICAL FOR ENSURING THAT THE CORE DOES NOT MAKE INCORRECT ASSUMPTIONS OR DECISIONS WITHOUT HUMAN
    ///     GUIDANCE.
    ///     IT IS A SAFETY MECHANISM TO PREVENT ERRORS AND ENSURE ACCURACY IN THE CASE HANDLING PROCESS.
    /// </summary>
    AwaitingInput,

    /// <summary>
    ///     THIS STATUS INDICATES THAT THE CASE HAS BEEN ESCALATED FOR HUMAN ATTENTION. THIS COULD BE DUE TO A BLOCKED
    ///     INVESTIGATION, A SAFETY RULE VIOLATION,
    ///     OR ANY OTHER REASON THAT REQUIRES HUMAN INTERVENTION. THE CASE WILL REMAIN IN THIS STATUS UNTIL A HUMAN OPERATOR
    ///     REVIEWS THE CASE AND TAKES APPROPRIATE ACTION.
    /// </summary>
    Escalated,

    /// <summary>
    ///     THIS IS A CRITICAL STATUS INDICATING THAT A SERIOUS ISSUE HAS BEEN DETECTED AND REQUIRES IMMEDIATE HUMAN ATTENTION.
    ///     THERE MAY BE AN UNEXPECTED CONDITION THAT COULD LEAD TO ERRORS OR HARM IF NOT ADDRESSED PROMPTLY.
    ///     IT COULD BE AN IMMINENT FAILURE OF HARDWARE, A CRITICAL SAFETY VIOLATION, OR ANY OTHER URGENT MATTER THAT NEEDS TO
    ///     BE RESOLVED IMMEDIATELY.
    ///     THE CASE WILL REMAIN IN THIS STATUS UNTIL A HUMAN OPERATOR TAKES ACTION
    ///     THIS WILL TRIGGER SOME FORM OF ALERT OR NOTIFICATION TO ENSURE THAT THE ISSUE IS ADDRESSED WITHOUT DELAY.
    /// </summary>
    Alerted,

    /// <summary>
    ///     THE CASE TRIGGERED A TERMINAL SAFETY RULE AND CANNOT PROCEED WITHOUT HUMAN INTERVENTION.
    /// </summary>
    Blocked,

    /// <summary>
    ///     THE LEVEL OF CONFIDENCE THRESHOLD HAS BEEN ACHIEVED AND THE CASE IS CONSIDERED COMPLETE BY THE AI.
    ///     THIS IS NOT A FINAL STATE IT SIMPLY MEANS THAT THE AI IS CONFIDENT ENOUGH IN ITS FINDINGS TO CONSIDER THE CASE
    ///     COMPLETE.
    ///     A HUMAN OPERATOR MUST CONFIRM RESOLUTION STEPS AND CLOSE THE CASE. THE CASE WILL REMAIN IN THIS STATUS UNTIL A
    ///     HUMAN OPERATOR REVIEWS AND CONFIRMS THE FINDINGS.
    /// </summary>
    Complete,

    /// <summary>
    ///     The investigation was cancelled by user. This is a terminal state and would require justification. The case could
    ///     be reopened by user if needed.
    /// </summary>
    Cancelled,

    /// <summary>
    ///     THIS IS A TERMINAL STATE INDICATING THAT THE CASE HAS BEEN CLOSED AND NO FURTHER ACTION IS REQUIRED. THE CASE IS
    ///     CONSIDERED RESOLVED.
    ///     /
    /// </summary>
    /// <remarks>
    ///     This status is typically set by a human operator after reviewing the case and confirming that all necessary actions
    ///     have been taken and that the case can be considered closed.
    ///     Once a case is in this status, it should not be reopened or modified without a valid reason and proper
    ///     authorization.
    /// </remarks>
    /// <example>
    ///     A case may be closed after a successful investigation and resolution of the issue, or after a human operator has
    ///     determined that no further action is required.
    /// </example>
    /// <seealso cref="CaseStatus.Complete" />
    /// <seealso cref="CaseStatus.Cancelled" />
    /// <seealso cref="CaseStatus.Escalated" />
    /// <seealso cref="CaseStatus.Alerted" />
    /// <seealso cref="CaseStatus.Blocked" />
    /// <seealso cref="CaseStatus.AwaitingInput" />
    /// <seealso cref="CaseStatus.Review" />
    /// <seealso cref="CaseStatus.Investigation" />
    /// <seealso cref="CaseStatus.Analysis" />
    /// <seealso cref="CaseStatus.Open" />
    Closed
}