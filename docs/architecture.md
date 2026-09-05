# Architecture

> This document describes the **planned** architecture for this learning
> project. It reflects decisions made so far and intentions for later
> phases — it is not a description of code that exists yet. See
> [development-roadmap.md](development-roadmap.md) for when each piece
> arrives, and the top-level [README.md](../README.md) for project context.

## 1. Monolith vs Microservices

A monolith is a single deployable application containing all business
logic and typically one shared database. It is simpler to build, test, and
deploy initially — one codebase, one deployment pipeline, no network calls
between business functions.

A microservices architecture splits an application into multiple
independently deployable services, each responsible for a specific business
capability, typically each with its own data store. This adds real
complexity: network calls replace in-process function calls, data
consistency across services becomes an explicit concern, and operating many
deployable units requires more infrastructure than operating one.

**Trade-off in plain terms:** a monolith trades scalability/flexibility for
simplicity; microservices trade simplicity for independent scalability,
independent deployability, and clearer ownership boundaries.

## 2. Why We Are Using Microservices Here

For a real e-commerce product at this scale, a monolith would very likely
be the pragmatic choice — the added complexity of microservices is only
worth it once a team or system runs into problems microservices actually
solve. This project takes on that complexity deliberately, as the point is
to *learn* microservices and the distributed-systems problems they create
(and how to solve them), not to build the simplest working store.

Given that, boundaries are chosen along real business capabilities (users,
catalog, orders, payments, notifications, AI) rather than arbitrary
technical splits, so each service still corresponds to something a domain
expert would recognize as a distinct responsibility.

## 3. Responsibility of Each Planned Service

- **API Gateway** — Single entry point for external clients. Routes
  requests to the correct backend service. Candidate home for cross-cutting
  concerns (auth forwarding, rate limiting) — decided when Phase 6 is
  reached.
- **User Service** *(implemented, Phase 2)* — User registration, authentication,
  and profile data. Owns identity; other services trust its authentication
  decisions rather than re-implementing them. JWTs are issued by this service
  and are expected to be validated (not re-authenticated) by whichever service
  receives them. See [src/UserService/README.md](../src/UserService/README.md)
  for its concrete endpoints, database schema, and auth flow.
- **Product Service** *(implemented, Phase 3)* — Product catalog: products,
  categories, pricing, and stock visibility. Currently has no authentication —
  all endpoints are public, since cross-service identity propagation is a
  Phase 5/6 decision not yet made (see sections 4 and 8 below). See
  [src/ProductService/README.md](../src/ProductService/README.md) for its
  concrete endpoints and database schema.
- **Order Service** — Order lifecycle: creating orders from a cart,
  tracking order status, and coordinating with Product (to check
  availability) and Payment (to request payment) services.
- **Payment Service** — Payment processing and payment state for orders.
  Kept separate from Order so payment provider integration and PCI-relevant
  concerns are isolated to one service.
- **Notification Service** — Sends outbound notifications (e.g. order
  confirmation, status changes), primarily driven by events from other
  services rather than direct calls.
- **AI Service** — AI/LLM-powered features layered on top of the platform's
  data, such as natural-language product search via RAG. Reads from other
  services' data rather than owning core transactional data.

## 4. Service-to-Service Communication

Two communication styles are planned, used for different purposes:

- **Synchronous (HTTP/REST)** — used when a service needs an immediate
  answer to proceed, e.g. Order Service checking product availability
  before confirming an order. Simple to reason about, but couples the
  caller to the callee's availability at call time.
- **Asynchronous (events via RabbitMQ)** — used when a service needs to
  react to something that already happened without blocking the original
  request, e.g. Notification Service reacting to an "OrderPlaced" event.
  Decouples services in time — the publisher does not need the subscriber
  to be up — at the cost of eventual (not immediate) consistency.

The general intent is: synchronous calls for queries that need an answer
now, events for reacting to state changes after the fact. Exact patterns
(request/reply over messaging, sagas, etc.) will be decided incrementally
as Phases 5 and 7 are reached, not decided upfront.

## 5. Database-per-Service

Each service is planned to own its own database, which no other service
accesses directly. A service exposes its data to others only through its
own API or through events it publishes.

This is a deliberate constraint that supports independent deployability
(a schema change in one service can't silently break another) and clear
ownership (there is exactly one service responsible for a given piece of
data). The cost is that queries spanning multiple services' data (e.g.
"orders with product names") require the calling service to gather data
via calls or events rather than a single SQL join — a trade-off accepted
here so that data ownership stays real, not a "shared database with
polite conventions" arrangement.

## 6. RabbitMQ's Planned Role

RabbitMQ is planned as the message broker for asynchronous, event-driven
communication between services (Phase 7) — e.g. Order Service publishing
an "OrderPlaced" event that Notification Service (and later others)
subscribe to. It is not planned as a replacement for synchronous calls
where an immediate response is required.

## 7. Redis's Planned Role

Redis is planned purely as a cache (Phase 8) to reduce load on service
databases for frequently read, rarely changed data (e.g. product catalog
reads). It is not planned as a system of record — the owning service's
database always remains the source of truth, and cache entries are
expected to be invalidated or expired rather than treated as durable state.

## 8. API Gateway's Planned Role

The API Gateway (Phase 6) is planned as the single entry point clients use
instead of calling each service directly. Candidate responsibilities
include request routing, and potentially cross-cutting concerns like
authentication forwarding and rate limiting — which of these it actually
takes on will be decided when that phase is reached, rather than assumed
now.

## 9. AI Service's Planned Role

The AI Service (Phases 12-13) is planned to provide LLM-powered features on
top of the platform's existing data — for example, natural-language product
search or recommendations implemented via Retrieval-Augmented Generation
(RAG) over a vector index of product data. It is planned to read from other
services (directly or via events) rather than own core transactional data
itself, keeping it an additive capability rather than a dependency other
services rely on to function.

## 10. Cloud Architecture Planned for Azure

Azure is the planned deployment target (Phase 14). Specific Azure services
(e.g. for compute, managed SQL, container hosting) will be chosen when that
phase is reached, guided by what each service actually needs rather than
adopted speculatively now.

## 11. DevOps / CI-CD Plan

GitHub Actions is the planned CI/CD tool (Phase 15), expected to build,
test, and eventually deploy each service independently — consistent with
the goal of independently deployable services. Pipeline design is deferred
until that phase.

## 12. Observability Plan

Observability (logging, metrics, and distributed tracing across services)
is planned for Phase 11, once there are multiple running services whose
interactions are worth observing. Specific tools will be chosen at that
point rather than upfront.

## 13. Kubernetes Plan

Kubernetes is the planned orchestration platform for running the
containerized services together (Phase 16), built on top of the Docker
work from Phase 10. Manifest design and specifics are deferred until that
phase.

---

**A note on this document itself:** sections above describing later phases
(Cloud, CI/CD, Observability, Kubernetes, AI) are intentionally high-level.
They state *intent and role*, not final design — the specific technology
choices and designs for those areas will be made, and this document
updated, when the corresponding roadmap phase is actually reached.
