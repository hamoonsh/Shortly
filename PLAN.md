# Shortly — URL Shortener on Azure

A deliberately over-architected URL shortener: DDD tactical patterns, Clean Architecture,
CQRS, and event-driven analytics — deployed to Azure with a full GitHub Actions pipeline.
The domain is trivial by design; the infrastructure and architecture are the product.

**Resume goals this project must earn:** Azure (App Service, Azure SQL, Key Vault,
Service Bus, Functions), CI/CD (GitHub Actions incl. pipeline-run migrations and
zero-downtime slot swap), event-driven design in a cloud context.

---

## Architecture at a glance

- **Style:** Modular monolith — ONE deployable API + ONE Azure Function app. Not microservices.
- **Bounded context:** One (`Links`). Don't invent more.
- **Aggregate:** `ShortLink` — identity `ShortCode`, target URL, active/disabled,
  and `ExpiresAt` expiry. Real invariants: a disabled or expired link cannot be
  visited; a link cannot be re-enabled past its expiry. (The expiry rule exists
  so the aggregate has genuine behavior to protect — see LEARNING_PROTOCOL.md.)
- **Value objects:** `ShortCode`, `TargetUrl` (validation lives here, not in controllers).
- **Domain events:** `ShortLinkCreated`, `ShortLinkVisited`.
- **Event-driven slice (the interview story):**
  - `GET /{code}` (redirect) is the hot path — respond 301/302 immediately.
  - Publish `ShortLinkVisited` to a Service Bus **queue** (fire-and-forget).
  - Azure Function consumes the queue, aggregates into a `ClickStatsDaily` read model
    (idempotent upsert: date + code + count, referrer, user-agent class).
  - `GET /api/links/{code}/stats` reads ONLY the read model. Genuine CQRS: separate
    write path and read path, eventual consistency by design.
- **Documented trade-off (put this in the README verbatim-ish):** losing an occasional
  click event is acceptable for analytics; an outbox pattern would guarantee delivery
  at the cost of latency and complexity — considered and deliberately deferred.
- **Short code strategy:** base62-encode a sequential DB identity (with a small offset),
  vs. random codes. Trade-offs: enumeration exposure vs. collision handling. Pick
  sequential+base62, document the alternative. (Good README "decisions" material.)

## Solution structure

```
Shortly.sln
src/
  Shortly.Domain           # aggregate, value objects, domain events — zero dependencies
  Shortly.Application      # CQRS command/query handlers, IEventPublisher, ILinkRepository
  Shortly.Infrastructure   # EF Core (SQL Server), Service Bus publisher, Key Vault config
  Shortly.Api              # ASP.NET Core minimal API, DI composition root
  Shortly.Functions        # Service Bus-triggered Function: click aggregation
tests/
  Shortly.Domain.Tests             # pure unit tests
  Shortly.Application.Tests        # handler tests, fake publisher/repo
  Shortly.Api.IntegrationTests     # WebApplicationFactory + Testcontainers SQL Server
  Shortly.Architecture.Tests       # NetArchTest: Domain refs nothing; Application refs
                                   # only Domain; handlers depend on abstractions
.github/workflows/
  ci.yml                   # build + test on every push/PR
  deploy.yml               # build → test → migrate DB → deploy to staging slot → swap
docs/
  architecture.md          # diagram + decisions-and-trade-offs table
```

Architecture tests and Testcontainers integration tests mirror what you already run at
GTG — this makes that practice publicly visible.

---

## Phases — repo must be demoable at the end of each one

### Phase 0 — Skeleton + CI (one evening)
- [ ] Create public GitHub repo `shortly`, solution + projects per structure above
- [ ] Add `ci.yml`: checkout → setup .NET → restore → build → test (all four test projects)
- [ ] Add architecture tests FIRST (they're cheap and they gate everything after)
- [ ] README stub with the "deliberately over-architected" framing sentence

### Phase 1 — Working domain, locally (2–3 sessions — this is the DDD school)
**Work split is governed by LEARNING_PROTOCOL.md Phase 1: Domain and Application
layers are hand-written by Hamoon; AI does EF config, endpoint wiring, and test
scaffolding only. AI agents: do NOT generate Domain/Application code in this phase.**
- [ ] `ShortLink` aggregate + value objects + domain events (incl. expiry
      invariants), fully unit-tested — HAND-WRITTEN
- [ ] Commands: `CreateShortLink`, `DisableShortLink`; Queries: `GetLinkStats`
      (stub) — HAND-WRITTEN
- [ ] Two AI critique passes (strict DDD, then pragmatist) completed and fixes applied
- [ ] Endpoints: `POST /api/links`, `GET /{code}` (redirect), `GET /api/links/{code}`
- [ ] EF Core + migrations, Testcontainers integration tests green
- [ ] In-process event publisher stub (logs the event) behind `IEventPublisher`

### Phase 2 — First Azure deploy + pipeline (weekend day 2)
- [ ] Azure: resource group, App Service (Linux, F1/B1 to start), Azure SQL serverless
      (free offer tier)
- [ ] GitHub → Azure auth via **OIDC federated credentials** (no publish-profile
      secrets in the repo — this is the senior signal)
- [ ] `deploy.yml`: build → test → run EF migrations against Azure SQL (`dotnet ef
      database update` step or migrations bundle) → deploy
- [ ] App reachable on `*.azurewebsites.net` — FIRST CLOUD DEPLOY ✅

### Phase 3 — Secrets + zero-downtime (evening or two)
- [ ] Key Vault; move connection string + any secrets there
- [ ] App Service **system-assigned Managed Identity** granted Key Vault access —
      zero secrets in config or GitHub
- [ ] Scale to S1 temporarily; add **staging slot**; pipeline deploys to staging,
      health-checks, then **slot-swaps** to production
- [ ] Scale back down after capturing screenshots/notes for README

### Phase 4 — Event-driven analytics (weekend 2)
- [ ] Service Bus namespace + queue `link-visited` (Basic tier)
- [ ] Real `ServiceBusEventPublisher` in Infrastructure (fire-and-forget on redirect)
- [ ] `Shortly.Functions`: Service Bus trigger → idempotent upsert into `ClickStatsDaily`
- [ ] Functions deployed via same pipeline (consumption plan, Managed Identity to
      Service Bus + SQL)
- [ ] `GET /api/links/{code}/stats` reads the aggregated model — CQRS loop closed

### Phase 5 — The artifact hiring managers actually read (one evening)
- [ ] `docs/architecture.md`: request-flow diagram + "Decisions & trade-offs" table
      (base62 vs random, fire-and-forget vs outbox, monolith vs microservices,
      per-day aggregation granularity, slot swap vs direct deploy)
- [ ] README: what it is, live URL, architecture summary, how the pipeline works,
      cost notes (what runs free, what needed S1)
- [ ] Pin repo on GitHub profile; add link to resume/LinkedIn

---

## Cost guardrails
- App Service F1/B1 and Azure SQL serverless free offer: ~$0
- S1 only while building/demoing slot swap (covered by the $200/30-day credit)
- Service Bus Basic + Functions consumption: effectively $0 at this volume
- Set a budget alert at $10 in the Azure portal on day one

## Deliberately out of scope (say so in the README)
Cosmos DB, containers/AKS, VMs, custom domain/TLS beyond defaults, rate limiting,
auth on the public API (note it as "next steps"), front end beyond a one-page form.

## Resume bullet this project unlocks (once Phase 4 is done)
> Designed and deployed a production-grade .NET API on Azure — App Service with
> zero-downtime staging-slot deploys, Azure SQL with pipeline-run EF Core migrations,
> Key Vault via Managed Identity, and event-driven click analytics over Service Bus
> and Azure Functions — fully automated through GitHub Actions with OIDC (no stored
> cloud credentials).
