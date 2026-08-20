# Cross-Border Settlement Platform

A reference implementation of a cross-border payments settlement system, built in .NET 10.

Money moving between countries is one of the few domains where every hard distributed
systems problem shows up at once and none of them can be waved away: the write must be
exactly once, the ledger must balance, the network will partition mid-transaction, and a
retry that creates a second payment is not a bug report — it is somebody's money. This
repository works through those problems deliberately, one at a time, with the reasoning
written down.

The domain is not incidental. I have implemented ACH/NACHA file processing directly from
the specification and built general-ledger commission systems with compensating
transactions, so the design decisions here come from having had to solve them, not from
having read about them.

---

## Status

**Active development.** This section is the honest one — it says what runs today, not what
is planned. Nothing is listed as done until it is committed and provable.

| Capability | Status |
|---|---|
| Solution structure, domain-inward references | ✅ Done |
| `Payment` aggregate + EF Core 9 / PostgreSQL persistence | 🔨 In progress |
| `POST /payments`, `GET /payments/{id}`, RFC 9457 problem details | 🔨 In progress |
| Idempotency enforced at the database boundary | 🔨 In progress |
| Structured logging (Serilog, compact JSON) | 🔨 In progress |
| Integration tests against real Postgres (Testcontainers) | 🔨 In progress |
| Double-entry ledger service | ⬜ Planned |
| gRPC for internal service-to-service calls | ⬜ Planned |
| Kafka settlement event stream, partitioned by corridor | ⬜ Planned |
| Transactional outbox | ⬜ Planned |
| Settlement saga with compensating transactions | ⬜ Planned |
| Redis FX rate cache | ⬜ Planned |
| GraphQL gateway | ⬜ Planned |
| OpenTelemetry traces, metrics and logs | ⬜ Planned |
| CI/CD, Helm, AKS, GitOps | ⬜ Planned |
| Load test results | ⬜ Planned |

---

## The problem

A payment leaves an account in one country and arrives in another. Between those two
facts sit an FX conversion, a correspondent banking relationship, a sanctions screen, a
settlement window that is closed on weekends, and a ledger that must balance at every
instant regardless of what failed.

The specific problems this repository is built to answer:

- **Exactly-once under retry.** The client times out and retries. The network duplicated
  the request. Two API instances process it simultaneously. In all three cases exactly one
  payment must exist.
- **Consistency without distributed transactions.** A payment spans several services and
  two-phase commit is not on the table. State must converge under partial failure, and
  every step must be reversible.
- **A ledger that always balances.** Double-entry, with compensating entries rather than
  deletes, because the audit trail *is* the product in a regulated domain.
- **Traceability across boundaries.** When a payment is stuck, the operator needs to know
  which hop it is stuck on, in one query, without SSH-ing into anything.

---

## Architecture

**Target state.** Components marked ⬜ in the status table above do not exist yet.

```
                    ┌─────────────┐  ┌──────────┐  ┌────────────┐
   Edge             │   GraphQL   │  │   REST   │  │  SignalR   │
                    │   gateway   │  │ Minimal  │  │ live status│
                    └──────┬──────┘  └────┬─────┘  └─────┬──────┘
                           └──────────────┼──────────────┘
                                          │  gRPC internal
        ┌──────────────┬──────────────────┼──────────────────┐
   Svc  │   Payment    │    Ledger        │   FX & routing   │  Compliance
        │ saga, outbox │ double-entry GL  │  rates, corridors│  screening
        └──────┬───────┴────────┬─────────┴────────┬─────────┴──────┘
               │                │                  │
   Messaging   ├── Kafka: settlement events, partitioned by corridor ──┤
               └── RabbitMQ (MassTransit): commands, retries, DLQ ─────┘
               
   Data        PostgreSQL (write)  ·  Redis (FX cache, idempotency)
               Cosmos DB (read models)  ·  MongoDB (audit archive)
```

### Why these choices

Short version; the long version lives in [`docs/adr/`](docs/adr/).

- **PostgreSQL for the write model.** Payments need real transactions and real
  constraints. The unique index on `IdempotencyKey` is not an optimisation — it is the
  correctness guarantee, and the application-level check in front of it is only there to
  avoid the exception on the happy path.
- **`decimal(18,4)` → `numeric`.** Four places rather than two because FX conversion
  produces intermediate values that two will not hold. See ADR 0001.
- **Kafka partitioned by settlement corridor.** Ordering matters within a corridor and
  does not matter across them, which is exactly the shape a partition key is for.
- **Outbox over dual writes.** Writing to the database and publishing to a broker are two
  operations that cannot be made atomic. The outbox makes them one write plus a
  dispatcher.
- **Domain project references nothing.** Not a stylistic preference — three storage
  technologies eventually touch this system, and none of them gets a vote on what a
  payment is.

---

## Domain model

| Concept | Meaning |
|---|---|
| **Payment** | An instruction to move value from a source country to a destination country. Has a lifecycle: `Accepted → Settling → Settled`, or `Failed`. |
| **Corridor** | A source/destination country pair. The unit of partitioning, rate lookup and routing. |
| **Idempotency key** | Client-supplied. Two requests carrying the same key are the same payment, no matter how they arrive. |
| **Ledger entry** | One half of a double-entry pair. Never updated, never deleted — corrections are compensating entries. |
| **Settlement** | The point at which funds are irrevocably transferred. Distinct from acceptance, which is when we agree to try. |

---

## Running locally

**Requires:** .NET 9 SDK, Docker.

```bash
git clone https://github.com/falcon262/cross-border-settlement.git
cd cross-border-settlement

docker compose up -d              # PostgreSQL, with a healthcheck
dotnet run --project src/Settlement.Payments.Api
```

The API listens on the port printed at startup. Swagger UI is available in Development.

```bash
dotnet test                       # integration tests spin real Postgres via Testcontainers
```

> These instructions describe the target of the current work in progress. Steps that are
> not yet committed are marked 🔨 in the status table.

---

## Project structure

```
src/
  Settlement.Payments.Api              HTTP surface, composition root
  Settlement.Payments.Domain           entities, value objects, invariants — references nothing
  Settlement.Payments.Infrastructure   EF Core, persistence, external adapters
tests/
  Settlement.Payments.Tests            unit + integration, real dependencies via Testcontainers
docs/
  adr/                                 architecture decision records
algorithms/                            unrelated to the platform; C# practice, kept in the open
```

The dependency arrow points inward only: `Api → Infrastructure → Domain`. `Domain` has no
project references, and `dotnet list src/Settlement.Payments.Domain reference` returning
empty is treated as a standing invariant.

---

## Architecture decision records

Every significant decision is written down the day it is made, with the alternatives that
were rejected and why. The rejected options are the useful part.

| # | Decision |
|---|---|
| [0001](docs/adr/0001-monetary-amounts-and-persistence.md) | Monetary amounts as `decimal(18,4)` over `long` minor units |

---

## Engineering conventions

- **Nothing is claimed that the repository cannot show.** If it is in the README as done,
  there is a commit behind it.
- **Decisions are recorded the day they are made,** never reconstructed later.
- **Tests run against real dependencies.** Postgres in a container, not an in-memory
  provider that behaves differently from the database in production.
- **Logs are structured from the first line of code,** because retrofitting correlation
  onto string-interpolated logs is not worth doing twice.

---

## Author

**Joseph Kofi Asante** — backend engineer working in payments and distributed systems.
ACH/NACHA processing implemented from specification; general-ledger systems with
compensating transactions; team lead.

[LinkedIn](https://www.linkedin.com/in/joseph-asante-864892185) · open to Senior backend and platform roles.

## Licence

MIT. See [LICENSE](LICENSE).