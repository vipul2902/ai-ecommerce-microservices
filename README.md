# AI-Powered E-Commerce Microservices Platform

A production-style **learning project**: a small e-commerce platform built as
independently deployable .NET microservices, developed incrementally to
practice distributed-systems and cloud-native engineering end to end.

> **Status:** Phase 3 in progress. Repository foundation and architecture docs are in
> place (Phase 1). **User Service** (Phase 2) and **Product Service** (Phase 3) are
> implemented — see [src/UserService/README.md](src/UserService/README.md) and
> [src/ProductService/README.md](src/ProductService/README.md) for their endpoints,
> databases, and design notes. All other services are still placeholders. See
> [docs/development-roadmap.md](docs/development-roadmap.md) for the phased
> plan.

## Project Purpose

This repository exists to learn, by building, the technologies and patterns
used in real-world distributed systems: service decomposition, inter-service
communication, data ownership, messaging, caching, containerization, cloud
deployment, observability, and AI/LLM integration. The e-commerce domain is
a vehicle for this — familiar enough to reason about, rich enough to justify
multiple services with genuine business boundaries.

## Business Scenario

An online store where customers can browse a product catalog, place orders,
and pay for them, with the platform sending notifications about order
activity. An AI-powered capability (product search / recommendations via
RAG) sits on top of this core flow. This is deliberately a small, familiar
domain — the learning focus is the *architecture*, not the business
complexity.

## Planned Microservices

| Service | Responsibility |
|---|---|
| **API Gateway** | Single entry point for clients; routes requests to backend services |
| **User Service** ✅ | User registration, authentication, profile data — [implemented](src/UserService/README.md) |
| **Product Service** ✅ | Product catalog, categories, pricing — [implemented](src/ProductService/README.md) |
| **Order Service** | Order lifecycle and orchestration |
| **Payment Service** | Payment processing and payment state |
| **Notification Service** | Outbound notifications (order updates, etc.) |
| **AI Service** | AI/LLM-powered features (e.g. RAG-based product search) |

Each service is planned to own its own data and be independently
deployable. Details and rationale are in
[docs/architecture.md](docs/architecture.md).

## Technology Stack

- **Backend:** .NET / ASP.NET Core
- **Data:** SQL Server, Entity Framework Core
- **Messaging:** RabbitMQ
- **Caching:** Redis
- **Gateway:** API Gateway (technology choice to be decided when Phase 6 is reached)
- **Containers:** Docker
- **Orchestration:** Kubernetes
- **Cloud:** Microsoft Azure
- **CI/CD:** GitHub Actions
- **Observability:** To be decided when Phase 11 is reached
- **AI/LLM:** LLM integration with RAG / vector search

Technologies are introduced only when the phase that needs them is reached
— see the roadmap below.

## High-Level Architecture

```
                        +----------------+
                        |   API Gateway  |
                        +--------+-------+
                                 |
        +---------+---------+---+---+---------+------------+
        |         |         |       |         |            |
   +----v---+ +---v----+ +--v---+ +-v------+ +v-----------+ +-----------+
   |  User  | |Product | |Order | |Payment | |Notification| | AI Service|
   |Service | |Service | |Svc   | |Service | |  Service   | |           |
   +----+---+ +---+----+ +--+---+ +---+----+ +-----+------+ +-----+-----+
        |         |         |         |             |             |
   +----v---+ +---v----+ +--v---+ +---v----+   (subscribes    (reads from
   |  DB    | |  DB    | |  DB  | |  DB    |    to events)     other data)
   +--------+ +--------+ +------+ +--------+

   Cross-cutting: RabbitMQ (async events) · Redis (caching)
```

Each box is its own deployable unit with its own database — no shared
database across services. Full rationale, communication patterns, and the
role of each supporting technology are documented in
[docs/architecture.md](docs/architecture.md).

## Repository Structure

```
ai-ecommerce-microservices/
├── src/                    # Service source code (placeholders for now)
│   ├── ApiGateway/
│   ├── UserService/
│   ├── ProductService/
│   ├── OrderService/
│   ├── PaymentService/
│   ├── NotificationService/
│   └── AIService/
├── tests/                  # Test projects (added alongside each service)
├── docs/                   # Architecture and planning documentation
├── infrastructure/         # IaC, Docker/Kubernetes/Azure configs (later phases)
├── .gitignore
└── README.md
```

## Learning Objectives

- Design and reason about microservices with real business boundaries
  (not arbitrary technical splits).
- Practice database-per-service and the data-consistency trade-offs it
  creates.
- Implement synchronous (HTTP) and asynchronous (event-driven via RabbitMQ)
  service-to-service communication.
- Use Redis effectively for caching without treating it as a source of truth.
- Build and operate an API Gateway as a single entry point.
- Containerize services with Docker and orchestrate them with Kubernetes.
- Deploy to Azure and build a CI/CD pipeline with GitHub Actions.
- Add observability (logging, metrics, tracing) to a distributed system.
- Integrate an AI Service using LLMs and RAG / vector search.
- Make deliberate, incremental architectural decisions and document the
  reasoning behind them.

## Roadmap

Development proceeds in phases, from repository foundation through to
production hardening. See
[docs/development-roadmap.md](docs/development-roadmap.md) for the full
17-phase plan.

## Architecture Documentation

See [docs/architecture.md](docs/architecture.md) for the detailed
architecture discussion, including monolith-vs-microservices reasoning,
per-service responsibilities, communication patterns, and the planned role
of each supporting technology.
