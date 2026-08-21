# LEARNING_PROTOCOL.md — How AI is used in this repo (and how it isn't)

Companion to PLAN.md. Purpose: finish the project WITH AI speed while actually
learning the two target skills — **Azure** and **CI/CD**. Everything else
(C#, Clean Architecture, CQRS, EF Core, testing) is already production-level
and gets delegated to AI freely under review, exactly like at work.

---

## The four global rules

### Rule 1 — Split by skill, not by task
- **Already-known territory** (EF Core config/migrations, minimal API wiring,
  DI setup, xUnit/Testcontainers scaffolding, boilerplate):
  Claude Code writes it, you review it. Zero guilt. This is your day job.
- **Learning territory A — DDD (new) + Clean Architecture/CQRS (deliberate
  refresh):** the Domain and Application layers are HAND-WRITTEN. AI's role
  here is teacher-before and critic-after — never author. See Phase 1.
- **Learning territory B — Azure + CI/CD** (portal/CLI, pipeline YAML,
  Managed Identity, Key Vault, Service Bus config, Functions runtime, slot
  swaps): **First time by hand. Second time with AI.** Manual once = you own
  the mental model; automating it after with AI is then genuinely safe.

### Rule 2 — The hint ladder (when stuck in learning territory)
Self-debug for **30 minutes** (logs, portal, error message, official docs) before
asking AI. Then escalate one rung at a time — never jump to the top:
1. "Explain the concept behind this error" (no solution)
2. "What area should I look at?" (direction, not answer)
3. "Here's what I tried and what happened — what am I missing?" (diagnosis)
4. Full solution — allowed only after rungs 1–3, and you must write it out
   yourself rather than pasting.
For already-known territory, skip the ladder — debug like you do at work.

### Rule 3 — Explain-back checkpoints gate every phase
At the end of each phase, open a chat with Claude and say:
"Quiz me on Phase N of Shortly — hostile interviewer mode."
If you can't answer a checkpoint question below without looking, that item gets
REDONE by hand (tear it down in the portal and rebuild it). This is the
single mechanism that separates "it works" from "I learned it."

### Rule 4 — The learning journal
Keep `docs/journal.md`. After each session, 3–5 bullets: what you did, what
surprised you, what broke and why. Two payoffs: it becomes the raw material
for `docs/architecture.md`, and it becomes your interview answers — "what
surprised me about Azure SQL serverless was..." is a great senior answer.

---

## Phase-by-phase split

### Phase 0 — Skeleton + CI
**AI does:** solution structure, project references, NetArchTest architecture
tests, test project scaffolding. (You've built this shape before.)
**You do by hand:** write `ci.yml` yourself from the GitHub Actions docs —
checkout, setup-dotnet, restore, build, test. It's ~25 lines and it's your
first CI/CD learning rep. Make it fail once on purpose (break a test) and
watch the red X.
**AI's role on ci.yml:** review it AFTER it's green. Ask: "critique this
workflow — caching, triggers, matrix, anything a senior DevOps person would flag."
**Checkpoint:** What triggers the workflow? What's a runner? Where does
`setup-dotnet` come from? Why cache NuGet packages?

### Phase 1 — Working domain, locally  ← the DDD school
This phase runs a **learn → build → critique** loop. Budget it honestly: it
will take 2–3x longer than delegating, and that's the point.

**Step 1 — Concept first (before writing code, ~3–4 hrs total):**
Micro-syllabus, in order. Structured input first (your learning style), then
build:
1. "Domain-Driven Design Distilled" (Vaughn Vernon) — chapters on aggregates,
   value objects, domain events only. Short book; skip strategic-design
   chapters for now (one bounded context here anyway).
2. Video reinforcement: Amichai Mantinband's DDD series or Milan Jovanović's
   DDD videos (both .NET-native, both free) — aggregates, value objects,
   domain events, and "encapsulate invariants" episodes.
3. Ask Claude: "Teach me aggregate design rules — invariants, boundaries,
   why small aggregates, reference-by-id — with .NET examples. Then quiz me."

**Step 2 — Build by hand (all of it):**
- `ShortCode` and `TargetUrl` value objects — validation and equality live
  here, constructors that make invalid states unrepresentable.
- `ShortLink` aggregate — private setters, factory method, behavior methods
  (`Disable()`, `RecordVisit()`), events raised inside behaviors, invariants
  enforced in one place.
- **So there are real invariants to protect** (DDD without invariants is just
  classes), include: expiry (`ExpiresAt`) with a rule like "a disabled or
  expired link cannot be visited or re-enabled past expiry" — now `Disable`,
  `Visit`, and expiry interact, and the aggregate earns its existence.
- Application layer BY HAND too (this is the CQRS refresh): command/query
  objects, handlers, the `IEventPublisher`/`ILinkRepository` abstractions,
  and the decision of what returns what. Write it the way you'd defend it.
**AI does:** EF Core configuration + migrations for YOUR model, endpoint
wiring, Testcontainers setup, unit-test bulk (against behaviors you specify).

**Step 3 — Critique after (two review passes, in this order):**
1. "Review my Domain and Application layers as a strict DDD practitioner —
   anemic model smells, leaked invariants, aggregate boundary mistakes,
   primitive obsession, misplaced logic. Be harsh; cite the principle behind
   every criticism." Fix by hand; ask WHY on anything surprising.
2. "Now review as a pragmatist: where is this over-engineered for the domain?"
   — because knowing where DDD is too much is the senior half of the skill,
   and it's the half that keeps the README's 'deliberately over-architected'
   framing honest.

**Checkpoint:** Why is `ShortCode` a value object and not a string? What
invariant does the aggregate protect and where exactly is it enforced? Why
raise events inside behavior methods instead of in handlers? Where's the
aggregate boundary and why isn't `ClickStatsDaily` inside it? What makes a
model "anemic" and how would this one become anemic? Why does Domain
reference nothing? What did you deliberately NOT do (domain services,
repositories per aggregate, specifications) and why?

### Phase 2 — First Azure deploy + pipeline  ← the heart of the learning
**You do by hand — ALL of it, portal/CLI, no AI driving:**
1. Create the resource group in the portal. Read what a resource group IS first.
2. Create App Service (Linux, B1) — clicking through: what's an App Service
   Plan vs the app? Why does that distinction exist?
3. Create Azure SQL serverless (free offer). Configure the firewall yourself.
   Connect from SSMS on your machine — feel the firewall rule work.
4. Deploy MANUALLY first: `az webapp deploy` (or VS publish) — see the app run
   in the cloud before any pipeline exists.
5. Set up OIDC federated credentials by hand from Microsoft's guide (create
   the Entra app registration, the federated credential, the RBAC role
   assignment). This is identity work — your specialty — mapped onto Azure.
6. Write `deploy.yml` YOURSELF: login via OIDC → build → test → EF migrations
   (migrations bundle) → deploy. Expect it to fail 5+ times. That's the course.
**AI's role:** concept explanations on demand ("explain workload identity
federation like I'm an OIDC expert who's new to Azure"), hint ladder when
stuck, and a full review of `deploy.yml` once it's green.
**Checkpoint:** Walk through exactly what happens when you push to main —
every hop from git push to the app restarting. What does the federated
credential trust? Why is there no secret in GitHub? Where do migrations run
and what happens if they fail mid-deploy?

### Phase 3 — Key Vault + Managed Identity + slot swap
**You do by hand:** create Key Vault, move the connection string, enable
system-assigned Managed Identity on the App Service, grant it Key Vault
access, verify the app boots with zero secrets in config. Then: scale to S1,
create the staging slot, do ONE manual slot swap from the portal and watch
what happens to traffic.
**AI does:** the .NET config-provider code change (Key Vault configuration
source), and the pipeline edit to deploy-to-slot + swap — but only after
your manual swap worked.
**Checkpoint:** What token does the app present to Key Vault and who issued
it? What exactly swaps in a slot swap (and what DOESN'T — sticky settings)?
Why can slot swap + EF migrations be dangerous together, and what's your answer
for it? (Hint: backward-compatible migrations — you'll enjoy this one.)

### Phase 4 — Event-driven analytics
**You do by hand:** create the Service Bus namespace and queue in the portal;
send/receive a test message with Service Bus Explorer BEFORE any code exists.
Write your FIRST Azure Function trigger yourself (the runtime model —
bindings, host.json, local settings — is the new material). Run it locally
against the real queue.
**AI does:** the `ServiceBusEventPublisher` implementation, the idempotent
upsert logic (you've designed idempotency before — review, don't retype),
Function deployment additions to the pipeline.
**Checkpoint:** Queue vs topic — why a queue here? What happens to a message
the Function throws on — retries, then where? What's the dead-letter queue
and what would you do with it in production? At-least-once delivery: where
exactly does your idempotency guard live and why is it safe?

### Phase 5 — Docs
**You do by hand:** write `docs/architecture.md` decisions table and the
README architecture section yourself, from your journal — this IS your
interview prep, don't outsource your own voice.
**AI does:** editing pass for clarity, diagram generation from your description.

---

## The meta-payoff
This protocol is itself an interview answer. You set AI guardrails for a team
at GTG; here you applied the same discipline to yourself as the learner:
delegated what you could verify, hand-built what you were learning, and gated
progress on being able to explain everything under questioning. If a hiring
manager asks "how do you learn new tech in the AI era" — this document,
verbatim, is the answer.
