# Case Management Data Model

> Status: Proposed schema. The tables below are not implemented by the current `SentinelCore.CaseFlowEngine` EF model. Use [PersistenceComponent.md](./PersistenceComponent.md) for current persistence status.

## Change Table

| Version | Date | Author | Change summary |
| --- | --- | --- | --- |
| 2.0.0 | unknown | unknown | Classified the organizations/analysts schema as a proposal rather than an implemented persistence model. |

┌──────────────┐   ┌──────────────┐   ┌────────────────────┐
│ organizations│   │   analysts   │   │   investigations   │
└──────────────┘   └──────────────┘   └────────────────────┘
                         │                      │
                         └──────┬───────────────┤
                                │               │
                         ┌──────▼──────┐  ┌──────▼───────┐
                         │investigation│  │   systems    │
                         │ _assignees  │  └──────────────┘
                         └─────────────┘         │
                                          ┌───────┼────────────┐
                                   ┌──────▼──┐ ┌──▼──────┐ ┌───▼──────────┐
                                   │system_  │ │system_│ │system_       │
                                   │accounts│ │components│ │net_interfaces│
                                   └─────────┘ └─────────┘ └──────────────┘
                                                    │
                                            ┌───────▼───────┐
                                            │system_artifacts│
                                            └────────────────┘
                                                    │
                                       ┌────────────┼──────────────┐
                                ┌──────▼────┐ ┌─────▼─────┐ ┌──────▼──────┐
                                │artifact_│ │ analyses  │ │baselines   │
                                │integrity  │ └─────┬─────┘ │_config     │
                                │_log       │       │       └─────────────┘
                                └───────────┘  ┌────▼─────┐
                                               │ findings │
                                               └────┬─────┘
                                         ┌──────────┼──────────────┐
                                  ┌──────▼────┐ ┌───▼─────────┐ ┌───▼──────────┐
                                  │  iocs     │ │remediation_ │ │investigation │
                                  │          │ │actions      │ │_reports      │
                                  └──────────┘ └─────────────┘ └──────────────┘
