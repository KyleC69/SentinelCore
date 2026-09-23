# Safety Engine Implementation Checklist

This checklist reflects the transcript-backed architecture and the current codebase review. Each item is marked as:

- [x] = fully implemented to the design described in the transcript
- [~] = partially implemented or implemented but not fully aligned to the intended boundary
- [ ] = not yet implemented or not implemented correctly

---

## Core design decisions

### 1. Per-turn, prompt-scoped scoring

- [x] The system evaluates the current prompt for the current turn only.
- [x] The weighted model is not built as a persistent cumulative history.
- [ ] No explicit long-lived safety history or successive-turn accumulation is present in the checked implementation.

### 1a. Message Tagging **new**

- [x] Prompt messages that score any safety impact are tagged with a `SafetyScore` value using `WithAgentRequestMessageSource()`.
- [x] Prompt messages that trigger a safety decision are tagged with a `SafetyResult` value using `WithAgentRequestMessageSource()`.
- [x] The tag values are not cosmetic; they are a security metadata mechanism to record the message’s safety outcome so a prompt cannot spoof a clean result or hide a violation.
- [x] The final transcript guidance is reflected in the implementation via `new AgentRequestMessageSourceType("SafetyScore")` and `new AgentRequestMessageSourceType("SafetyResult")`, with the safety module owning the tag creation and verification.

Evidence: the transcript explicitly states: “tag any prompt message that scores a safety point with the score of that message >0” and “tagged Chatmessages with SafetyScore and SafetyResult”, and the current implementation now applies the tag creation inside the safety boundary.

### 2. `SafetyIndicator` is the canonical weighted model

- [x] `SafetyIndicator` exists as a structured record with `Term`, `Category`, `Weight`, and `RequiresImmediateReview`.
- [x] The canonical owner is `SafetyTriggerTerms`.
- [x] The indicator weights are part of the data, not detached policy logic.

Evidence: `SafetyTriggerTerms` defines `public sealed record SafetyIndicator(string Term, string Category, int Weight, bool RequiresImmediateReview);` and populates `AllIndicators`, `ContextEscalationTerms`, and `SuspiciousPhrases`.

### 3. Blocklist terms are weighted, not binary

- [x] `BlocklistRule` sums the matched indicator weights.
- [x] It returns a score and a list of matched indicators.
- [x] The weight score is a deterministic severity value.

Evidence: `BlocklistRule.EvaluateAsync` collects matching indicators, reduces overlapped terms, sums `indicator.Weight`, and returns `SafetyRuleResult.Warn` or `SafetyRuleResult.Block` with the score.

### 4. Immediate review is a first-class property

- [x] `RequiresImmediateReview` is part of the record and used by `BlocklistRule`.
- [x] An indicator marked for immediate review is typically used on terms that ambiguous in nature or has varied meaning depending on context. These types of terms must be viewed in context to properly determine their intent.
- [~] An indicator with this property set to `true` is scored and surfaced as a safety violation, but the full downstream review executor path is still only partially integrated at the workflow level.

Evidence: `BlocklistRule.EvaluateAsync` checks `uniqueIndicators.Any(indicator => indicator.RequiresImmediateReview)` before deciding the final action.

### 5. Warning / review / block thresholds are intentional design values

- [x] The rule uses threshold-based evaluation.
- [x] The transcript’s threshold pattern of warning and block bands is reflected in the implementation.
- [~] The thresholds are adjustable by operator in UI settings interface.

Evidence: Must be implement in UI as a setting.

### 6. Rule engine owns aggregation and orchestration

- [x] `SafetyRuleEngine` iterates all configured rules.
- [x] It aggregates the outcomes into `SafetyEvaluationResult`.
- [x] It supports stop-on-first-block behavior.

Evidence: `SafetyRuleEngine.EvaluateAsync` loops rules, collects `SafetyRuleResult`s, and returns `SafetyEvaluationResult.FromResults(results)`.

### 7. SafetyEvaluationResult is the aggregate contract

**REPLACE WITH TAG SYSTEM CHECKS**

- [x] It contains `TotalScore`, `MatchedIndicators`, `RuleResults`, `BlockingResult`, and `IsAllowed`.
- [x] It derives the overall conclusion from rule results.

Evidence: `SafetyEvaluationResult` includes the aggregate fields and uses `FromResults` to establish the final allow/block decision.

### 8. SafetyExecutor is the workflow gate

 **EXECUTOR IS MINIMAL AND SHOULD SURFACE RESULTS TO OPERATOR, HANDLE ERROR REPORTING AND STEP ROUTING IF ANY ONLY**

- [x] `SafetyExecutor` builds the evaluation context and calls the engine.
- [x] It blocks the message when `!result.IsAllowed`.
- [x] It is the workflow boundary for safety enforcement.

Evidence: `SafetyExecutor.ProcessMessageAsync` creates `SafetyEvaluationContext`, calls `_ruleEngine.EvaluateAsync`, and returns a blocked `ChatMessage` if not allowed.

### 9. Safety must win in any workflow conflict

ALWAYS ERROR ON THE SIDE OF CAUTION - BETTER TO SAY OOPS THAN HAVE TO SAY I'M SORRY!!

- [x] The code path is fail-safe.
- [x] The executor returns a blocked message whenever the evaluation result is not allowed.
- [~] Serious infractions are associated with explicit safety tags and a blocked result, but the broader workflow-level escalation path still needs final integration beyond the local gate.
- [ ] This is implemented as a runtime gate, but the broader workflow graph should still be audited for any bypass point outside the executor.

### 10. The reviewer is a conditional workflow branch, not a hidden state machine

- [ ] `SafetyReviewExecutor` is not a completed review path in the codebase.
- [ ] Messages are sent to this workflow executor to review message in context as a human would do and classify the intent safe/unsafe.
- [ ] Wired into TheCore workflow as an optional (conditional) next step after SafetyExecutor. Safe messages move through to next step in TheCore workflow.

Evidence: `SafetyReviewExecutor` is being added now---

---

## Ownership and boundary checks

### 11. Rule policy belongs in the rule layer

- [x] `BlocklistRule` owns matching and scoring.
- [x] The rule returns structured `SafetyRuleResult` details rather than raw booleans.
- [~] Some legacy comments in `SafetyExecutor` still suggest the executor or surrounding infrastructure was earlier treated as the policy owner.

### 12. Executor should not own the policy logic

- [~] The executor no longer contains the scoring logic in the final weighted blocklist implementation.
- [x] The engine + rule system now owns the scoring decision pipeline.
- [ ] The executor still contains some legacy or stale comments and a mixed policy-loading structure that should be cleaned up.

### 13. Trigger catalog ownership is centralized

- [x] `SafetyTriggerTerms` owns the canonical indicator list.
- [x] `GetAllIndicators()` centralizes the indicator catalog.

---

## Full implementation status

### Implemented to final transcript design

- [x] weighted `SafetyIndicator` model
- [x] weighted `BlocklistRule` evaluation
- [x] rule result score and matched indicators
- [x] engine aggregation and summary result
- [x] executor gate behavior
- [x] per-turn prompt-scoped evaluation

### Partially aligned or not fully implemented

- [~] central threshold policy configuration
- [~] workflow review branch wiring
- [~] cleanup of stale legacy comments and edge cases around executor responsibilities
- [x] security tag creation and verification for `SafetyScore` / `SafetyResult`
- [~] explicit serious-infraction action semantics and downstream escalation handling

### Still missing or needs explicit follow-up

- [ ] a real review workflow route from warning/review to `SafetyReviewExecutor`
- [ ] consistent threshold policy configuration outside hard-coded constants
- [ ] architectural cleanup of legacy comments and stale assumptions in executor code
- [ ] explicit validation that no workflow path bypasses the safety gate
- [ ] `ChatMessage` metadata tagging for any prompt scoring `> 0` with `WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyScore"), ...)`
- [ ] `ChatMessage` metadata tagging for block or safety outcome with `WithAgentRequestMessageSource(new AgentRequestMessageSourceType("SafetyResult"), "blocked" | "warn" | "review")`
- [ ] a formal serious-infraction policy describing which safety outcomes trigger escalation, quarantine, or human review

### Status grid

| Area | Status | Notes |
| --- | --- | --- |
| Weighted indicator model | [x] Complete | Canonical `SafetyIndicator` and per-turn scoring are in place. |
| Rule aggregation and thresholds | [x] Complete | `BlocklistRule` and `SafetyRuleEngine` aggregate weighted scores and apply threshold-based decisions. |
| Executor safety gate | [x] Complete | `SafetyExecutor` blocks or rejects non-allowed results at the workflow boundary. |
| Conditional review branch | [~] Partial | `SafetyReviewExecutor` exists as a dependent review path but is not yet fully wired end-to-end. |
| Message security tagging | [x] Complete | `SafetyTagger` creates and verifies `SafetyScore` and `SafetyResult` tags in the safety module. |
| Serious infraction actions | [~] Partial | The code blocks and tags severe violations, but the later workflow escalation path still needs final integration. |
| Legacy cleanup | [~] Partial | Some stale comments and assumptions remain around executor responsibilities. |

---

## Current verdict

The repository is now aligned with the transcript-backed design for the weighted evaluation model, canonical indicator model, engine aggregation, workflow gate, and the safety-owned security tag flow. The remaining work is primarily the full review-branch integration, explicit serious-infraction escalation semantics, and cleanup of stale design assumptions around the workflow boundary.
