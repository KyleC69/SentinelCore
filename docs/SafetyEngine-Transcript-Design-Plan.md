# Safety Engine Transcript Rebuild: Correct Architecture and Design Plan

## Source of truth

This plan is reconstructed from the transcript, not from earlier stale docs or implementation drift. The transcript repeatedly changes the design in important ways, and the final version is the operative source of truth:

- The scoring model is weighted, per-turn, and prompt-scoped.
- It is not cumulative across prior turns or a global state machine.
- The safety gate is a non-negotiable workflow boundary.
- The reviewer is a branch in the workflow, not a persistent global reviewer state.
- Safety wins over competing workflow logic.
- The rule engine owns evaluation logic; the executor owns workflow routing and fallback handling.

The user repeatedly corrected the earlier “minimal patch” interpretation and insisted on proper architecture over minimal code impact.

---

## 1. Transcript pattern and how the real design was recovered

The original transcript is a JSONL event stream with noisy metadata and tool logs. The real conversational design is not in the tool events; it is in the request/response pairs.

The reliable extraction method is:

1. Ignore tool events and other metadata noise.
2. Walk the top-level `requests[]` array in order.
3. Recover the user message from the request payload’s rendered user content.
4. Recover the assistant message from the text-bearing response payloads.
5. Use the final clarifying request and the following agent plan/decision response as the authoritative design pair.

Key design corrections surfaced in the transcript:

- The prior binary blocklist approach was rejected.
- The design was revised toward weighted safety terms.
- Thresholds changed from a simplistic yes/no model to a scored model with review and block bands.
- The earlier “successive scoring” idea was explicitly rejected as unnecessary complexity.
- The review branch was recast as a workflow branch rather than a persistent reviewer state.
- Safety logic ownership was clarified: engine evaluates, executor routes, rule implementations encode policy.

This was a real game-changing siting of the design.

---

## 2. The actual design decisions encoded in the transcript

### 2.1 Weighted per-turn scoring, not cumulative global scoring

The transcript is explicit that the safety evaluation should evaluate the current prompt as a whole for the current turn only.

Design intent:

- Each matching safety term contributes a weight.
- Weights are added across the prompt for that turn.
- The aggregate score is used to decide warn, review, or block.
- Prior prompts, prior turns, or multi-turn accumulation are not part of the design.

This prevents hidden state and makes the system deterministic and easy to reason about.

### 2.2 SafetyIndicator is the canonical model

The transcript makes the canonical structure clear:

- `Term` — the literal threat phrase or word
- `Category` — the class of risk
- `Weight` — numeric contribution to the cumulative score
- `RequiresImmediateReview` — a hard, explicit escalation flag

This is not just a string list. It is a structured record that carries both the meaning and the decision metadata.

### 2.3 BlocklistRule owns weighted evaluation logic

The rule should be responsible for:

- matching the prompt text against configured indicators
- normalizing and deduplicating indicator matches
- reducing overlap where phrases are redundant or nested
- summing the weights for the current turn
- applying warn/review/block decision thresholds
- returning the matched indicators and score in the result object

This means the rule is not a pass/fail binary string checker; it is a policy evaluator.

### 2.4 SafetyExecutor is a gate and router, not a scoring engine

The transcript repeatedly pushes this boundary:

- The executor should receive the final `SafetyEvaluationResult`.
- It should decide whether the workflow continues or blocks.
- It should not contain the scoring policy logic itself.
- It should handle null validation, logging, and error fallback behavior.

This is the correct system boundary for a safety gate.

### 2.5 SafetyRuleEngine is the orchestration layer

The engine is the orchestrator across all rules. It should:

- iterate the configured rules
- invoke each rule with the same evaluation context
- aggregate all rule results into a single `SafetyEvaluationResult`
- respect fail-safe behavior for exceptions
- short-circuit appropriately when one rule blocks and the option is configured to stop early

This is the correct placement for cross-rule aggregation and summary generation.

### 2.6 The reviewer is not a global state machine

The transcript emphasizes that a “reviewer” is a workflow branch for an elevated path, not a persistent global state or a hidden historical reviewer queue.

Design intent:

- A prompt may trigger a review path when the score crosses a review threshold or a hard review flag is present.
- That review path is a conditional workflow step.
- It is not retained as a long-lived system state.
- It does not become a broad orchestrator that tracks prior incidents beyond the current turn.

This avoids architectural drift and stale state.

### 2.7 Safety is non-negotiable and must win on conflicts

The transcript is very explicit that safety wins in conflict. This is not a soft policy.

Architectural consequence:

- If workflow logic, orchestrator logic, or reviewer branch logic conflicts with safety, safety must prevail.
- Blocking stays ahead of normal workflow execution.
- The safety gate is not optional

This is a design principle, not just a runtime choice.

---

## 3. Exact architecture boundary map

### 3.1 Rule-level policy

`ISafetyRule`

- Defines the contract for evaluating a prompt against one rule.
- Produces a `SafetyRuleResult`.

`SafetyRuleResult`

- Contains the rule name, action, severity, score, reason, and matched indicators.
- This is the unit-level decision from a single rule.

`BlocklistRule`

- Owns weighted blocklist evaluation.
- Translates prompt content into matched `SafetyIndicator` instances.
- Sums their weights.
- Applies thresholds and returns the correct action.

### 3.2 Rule orchestration

`SafetyRuleEngine`

- Collects all rules.
- Evaluates each rule in order.
- Aggregates results into `SafetyEvaluationResult`.
- decides whether to stop on first block.

`SafetyEvaluationResult`

- Summarizes aggregate rule outcomes.
- Tracks total score and highest severity.
- Provides overall allow/block status.

### 3.3 Workflow boundary

`SafetyExecutor`

- Accepts the incoming message.
- Builds the evaluation context.
- Invokes the engine.
- Routes the workflow based on the result.
- Returns blocked response or original message.
- Contains null validation, logging, and fallback behavior.

This is the correct boundary. It is not where policy logic is computed.

### 3.4 Review branch or workflow continuation

`SafetyReviewExecutor`

- Should represent a conditional review path when the evaluation indicates warning or review rather than a direct block.
- It should operate as a route or domain-specific review step, not as a tweaked alternate engine.
- It should not hold prompt-history state.
- It should not be the source of truth for risk calculation.

### 3.5 Catalog and data ownership

`SafetyTriggerTerms`

- Owns the canonical indicator catalog.
- Supplies `SafetyIndicator` definitions for the entire system.
- Ensures consistent weights and categories across code.

This is the real canonical source of `SafetyIndicator` definitions, not a scattered helper class.

---

## 4. Important subtle changes in the transcript that must not be missed

### 4.1 The initial direct “fix compile errors” task was not the real architecture

The transcript begins with compile repair, but the actual design changes are later and more important. Those changes override the earlier minimal repair work.

### 4.2 The safety model changed from binary to weighted

The earlier design was effectively “contains a blocked string = block.”
The final design is a weighted score with multiple thresholds and immediate-review terms.

### 4.3 The sum of weights is per-turn, not across time

This is one of the most important corrections. The transcript explicitly rejects successive scoring and says per-turn whole-prompt evaluation is sufficient.

### 4.4 The reviewer is a branch, not a core engine

This is another major architectural correction. The reviewer should not become a hidden orchestrator or a second decision engine.

### 4.5 The executor boundary is deliberate

There was a long working discussion about where logic belonged. The final direction is clear: engine + rule do the policy evaluation, executor routes the workflow.

---

## 5. Correct implementation sequence

### Phase 1: Data model and indicator ownership

- Define or confirm `SafetyIndicator` as the canonical record.
- Ensure the record is owned by the trigger catalog or its canonical owner.
- Keep weights and `RequiresImmediateReview` attached to the indicator itself.

### Phase 2: Rule-level scoring

- Update `BlocklistRule` to evaluate the whole prompt.
- Match all relevant indicators in the current turn.
- Deduplicate overlaps.
- Sum weights.
- Return `Warn`, `Review`, or `Block` based on thresholds.

### Phase 3: Engine aggregation

- Keep `SafetyRuleEngine` as the aggregate evaluator.
- Ensure it merges rule results into a single `SafetyEvaluationResult`.
- Make exceptions fail-safe but visible.

### Phase 4: Workflow gate

- Keep `SafetyExecutor` as the gate that checks `IsAllowed`.
- Return blocked output only when the engine says no.
- Leave scoring logic out of the executor.

### Phase 5: Review branch wiring

- Implement or hook a dedicated review path only when the rule result indicates a review or warning path.
- Do not create a persistent reviewer state.
- Keep review conditional and lightweight.

### Phase 6: Verification

- Add/retain tests covering:
  - no match
  - review threshold
  - warning threshold
  - block threshold
  - immediate-review flag
  - per-turn scoring only
  - category deduplication / overlap reduction

---

## 6. What is still missing as of this review

The transcript shows the intended architecture clearly, but the repository is still a mix of implemented and partially implemented steps:

- The weighted model exists in the indicator catalog and the rule result types.
- The aggregate rule engine exists.
- The executor already routes the final result, which is aligned with the architecture.
- However, some files still carry older assumptions or placeholder logic, especially around review branching and policy ownership.

The remaining work is not a fresh design from scratch; it is alignment and cleanup to the final transcript-driven architecture.

---

## 7. Final architectural conclusion

The correct architecture, as defined by the transcript, is:

- `SafetyTriggerTerms` owns the canonical `SafetyIndicator` catalog.
- `BlocklistRule` owns weighted rule evaluation.
- `SafetyRuleEngine` owns cross-rule aggregation.
- `SafetyEvaluationResult` owns end-of-pipeline summary.
- `SafetyExecutor` owns workflow gating and user-visible block responses.
- `SafetyReviewExecutor` is a conditional review branch, not a hidden stateful engine.
- The safety decision is per-turn, weighted, fail-safe, and non-negotiable.

This is the design the implementation should follow.
