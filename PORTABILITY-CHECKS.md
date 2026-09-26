# Separate-machine portability checks

This register tracks the recurring check in R-012. Record each final published EXE's SHA-256, whether it was launched alone on a separate clean Windows x64 machine, and the result. A deferred check remains visible even if a later publication replaces the EXE; a later successful test verifies only the binary actually tested.

| Lot | Published EXE SHA-256 | Status | Evidence or next action |
|---|---|---|---|
| LOT-002 | `59204A2BCC32D46C7E37742FB818CA6FE3E3A20BC7641D4EC3B574001D224D0D` | SUPERSEDED UNTESTED | No second machine was available. This binary was replaced after Olivier found window placement and multiple-instance defects; do not claim it was tested remotely. |
| LOT-002 | `D9B7890D45B4CA458DEF74A533D940909586062EACF4477C7F4D6FC20DD19A4B` | PASS | Olivier confirmed on 2026-09-26 that this final LOT-002 EXE, copied alone to a second Windows x64 machine, launched successfully and displayed About. Its local canonical copy still has this SHA-256. The check was DEFERRED at lot closure and completed afterward. |

The older `59204A2B…D224D0D` candidate remains untested on a second machine; the successful check above applies only to the final EXE identified in its row. No separate .NET runtime inventory or broader application compatibility test is claimed.
