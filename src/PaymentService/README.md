# Payment Service (placeholder)

**Status:** Not yet implemented (planned in Phase 9 of the [development roadmap](../../docs/development-roadmap.md)).

## Planned Responsibility
Owns payment processing and payment state for orders. Will own its own
database, separate from all other services, and is expected to communicate
with the Order Service primarily through asynchronous events.

See [docs/architecture.md](../../docs/architecture.md) for how this fits into
the overall system.
