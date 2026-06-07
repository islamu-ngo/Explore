ABOUTME: Generated AI agent contract inventory for registry-governed tools.
ABOUTME: Lists tool metadata, approval posture, HAL requirements, and safe invariants without content-bearing AI data.

# AI Agent Contract Inventory

> Generated from `AiToolContractRegistry`. Do not add prompts, provider responses, raw tool payloads, tenant/user identifiers, endpoint URLs, API keys, model secrets, or private event content.

## Manual Notes

<!-- BEGIN MANUAL NOTES -->
_Add local reviewer notes here. This section is preserved by the generator._
<!-- END MANUAL NOTES -->

## Global Invariants

- Registry catalog visibility is advisory and never grants execution authority.
- Mutating tools remain proposal-first and require human confirmation before CQRS/MediatR commands execute.
- UI mutation affordances must be gated by HAL link presence, not local role or claim inspection.
- MCP adapters must use the same registry contracts and must not write repositories directly.

## Tool Inventory

| Tool | Kind | Risk | Approval | Confirmation | HAL rel | Routes | Workflows | Contexts | Authorization | Mapper/Executor | Provider | MCP |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Create event draft | CreateEventDraft | Medium | HumanConfirmationRequired | Required | create-event | /calendar, /events, /events/new | event-drafting, event-planning | event, selected-references | islamuevent_event:create | CreateEventDraftAiActionMapper | yes | yes |

## Tool Instructions

### CreateEventDraft

- Availability: Available only when AI tool proposals are enabled and the current API/HAL context allows event creation.
- Follow-up policy: AskClarifyingQuestionBeforeProposal
- Safe action instructions: Create a draft proposal only. Do not publish, schedule, invite attendees, assign roles, or claim the event exists before the user confirms the proposal.
- Result card: event-draft-proposal-card

