# Development Roadmap

This project is built incrementally, one phase at a time. Each phase should
result in something runnable/reviewable before moving to the next — later
phases are not started early, and technology is not introduced ahead of the
phase that needs it. See [architecture.md](architecture.md) for the
reasoning behind the planned design.

| Phase | Focus |
|---|---|
| 1 | Repository + architecture |
| 2 | User Service + authentication |
| 3 | Product Service |
| 4 | Order Service |
| 5 | Service-to-service communication |
| 6 | API Gateway |
| 7 | RabbitMQ / event-driven architecture |
| 8 | Redis / caching |
| 9 | Payment + Notification services |
| 10 | Docker + local containerized environment |
| 11 | Testing + observability |
| 12 | AI Service + LLM integration |
| 13 | RAG + vector search |
| 14 | Azure cloud deployment |
| 15 | GitHub Actions CI/CD |
| 16 | Kubernetes |
| 17 | Production hardening, resilience, scaling and security |

## Phase Details

### Phase 1 — Repository + Architecture
Establish repository structure, `.gitignore`, and architecture
documentation. No application code. *(This phase.)*

### Phase 2 — User Service + Authentication ✅
Implement the User Service: registration, login, and authentication.
First real ASP.NET Core Web API and first EF Core + SQL Server database.
See [src/UserService/README.md](../src/UserService/README.md) for what was built.

### Phase 3 — Product Service ✅
Implement the Product Service: catalog, categories, pricing. Second
independent service with its own database.
See [src/ProductService/README.md](../src/ProductService/README.md) for what was built.

### Phase 4 — Order Service
Implement the Order Service: creating and tracking orders. Introduces the
first cross-service data need (orders reference products/users).

### Phase 5 — Service-to-Service Communication
Introduce synchronous HTTP communication between services (e.g. Order
Service calling Product Service) and establish patterns for resilience
(timeouts, retries) around those calls.

### Phase 6 — API Gateway
Introduce a single entry point in front of the services built so far, and
decide which cross-cutting concerns (routing, auth forwarding, rate
limiting) it takes on.

### Phase 7 — RabbitMQ / Event-Driven Architecture
Introduce RabbitMQ and move suitable interactions (e.g. order-placed
notifications) from synchronous calls to asynchronous events.

### Phase 8 — Redis / Caching
Introduce Redis as a cache in front of read-heavy, rarely-changing data
(e.g. product catalog reads), with an explicit invalidation strategy.

### Phase 9 — Payment + Notification Services
Implement the Payment Service (payment processing/state) and Notification
Service (outbound notifications), integrated primarily via the
event-driven infrastructure from Phase 7.

### Phase 10 — Docker + Local Containerized Environment
Containerize each service and stand up a local multi-container environment
(e.g. via Docker Compose) so the full system can run together locally.

### Phase 11 — Testing + Observability
Build out automated testing (unit, integration) across services and add
observability: structured logging, metrics, and distributed tracing across
service boundaries.

### Phase 12 — AI Service + LLM Integration
Implement the AI Service with a first LLM integration (e.g. an
LLM-assisted product search or Q&A endpoint).

### Phase 13 — RAG + Vector Search
Extend the AI Service with Retrieval-Augmented Generation over a vector
index of product data, for grounded, relevant AI responses.

### Phase 14 — Azure Cloud Deployment
Deploy the containerized system to Azure, choosing specific managed
services based on what each service actually needs.

### Phase 15 — GitHub Actions CI/CD
Automate build, test, and deployment for each service independently via
GitHub Actions pipelines.

### Phase 16 — Kubernetes
Move orchestration from Docker Compose to Kubernetes, building on the
containerization work from Phase 10 and the cloud environment from
Phase 14.

### Phase 17 — Production Hardening, Resilience, Scaling and Security
Revisit the system as a whole: resilience patterns (circuit breakers,
retries, timeouts), scaling behavior, and security hardening across all
services.

## Working Principles Across All Phases

- Keep services independently deployable.
- Prefer clear business boundaries over arbitrary technical separation.
- Each microservice should own its own data.
- Avoid unnecessary abstractions — don't build for hypothetical future
  requirements.
- Don't add a technology before the phase that needs it.
- Keep the architecture understandable for someone learning microservices.
- Architectural decisions are made incrementally, phase by phase, and
  recorded in [architecture.md](architecture.md) as they're made.
