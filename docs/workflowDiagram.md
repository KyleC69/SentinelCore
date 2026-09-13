PatternCheckExecutor
        |
        v
SafetyAgentBinding
        |
        v
ClassifierExec
        |
        +------------------------------+
        |                              |
        |   Switch: NextStep           |
        |                              |
        |   Investigate  ---> NewCaseExecutor -----------+
        |   RedAlert     ---> CriticalAlert               |
        |   MoreInfo     ---> MoreInformationExecutor     |
        |   Escalate     ---> HumanOperatorExecutor       |
        |   DirectAnswer ---> DirectAnswerExecutor        |
        |   Default      ---> NewCaseExecutor             |
        +------------------------------+                  |
                                                         |
CriticalAlert ---------------------------> HumanOperatorExecutor
MoreInformationExecutor -----------------> HumanOperatorExecutor

NewCaseExecutor --------------------------> SentinelCoreExec
SentinelCoreExec -------------------------> EvidenceGatheringBinding
EvidenceGatheringBinding -----------------> AggregationExecutor
AggregationExecutor ----------------------> SentinelCoreExec   (loop)
