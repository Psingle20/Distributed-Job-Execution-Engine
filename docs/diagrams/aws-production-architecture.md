# AWS Production Architecture

> **Status:** Proposed production target. The current local and community-call demo continues to use MiniStack and Docker Compose. This diagram shows how the same application boundaries and Dapr components can be deployed on AWS without changing the domain model.

![Distributed Job Execution Engine proposed AWS production architecture](./aws-production-architecture.svg)

## Runtime flow

1. A client submits or queries a job through Route 53, AWS WAF, and an Application Load Balancer. Only `JobService` requires public ingress; `WorkerService` remains private.
2. `JobService` validates the command, advances its authoritative job state machine, and commits both domain state and an outbox message in one PostgreSQL transaction.
3. The outbox publisher sends the work event through its Dapr sidecar. The production diagram maps the Dapr pub/sub component to Amazon ElastiCache for Redis.
4. A `WorkerService` replica consumes the work event through Dapr, deduplicates it with its inbox, claims or renews the lease, and runs the selected executor.
5. The worker atomically commits execution state and an outcome outbox message before acknowledging the incoming delivery.
6. The worker outbox publishes `Started`, heartbeat, completion, failure, or cancellation outcomes. `JobService` consumes those events and advances the authoritative state machine idempotently.
7. Both services emit OpenTelemetry data through an ADOT Collector to CloudWatch, Amazon Managed Service for Prometheus, AWS X-Ray, and Amazon Managed Grafana.

## AWS responsibilities

| Area | Proposed service | Responsibility |
|---|---|---|
| Compute | Amazon EKS | Runs independently scalable `JobService` and `WorkerService` deployments with Dapr sidecars. |
| Messaging | Amazon ElastiCache for Redis | Hosts the Dapr pub/sub component for work and outcome topics. The component can later be replaced without changing application code. |
| Persistence | Amazon RDS for PostgreSQL, Multi-AZ | Stores service-owned schemas/databases, outbox rows, inbox records, leases, and job history. |
| Images | Amazon ECR | Stores immutable, scanned service images promoted by CI/CD. |
| Secrets | AWS Secrets Manager | Supplies database and Dapr component secrets through workload identity and the CSI driver. |
| Identity | IAM roles for service accounts | Gives each Kubernetes workload least-privilege AWS permissions without static credentials. |
| Observability | ADOT, CloudWatch, Managed Prometheus, X-Ray, Managed Grafana | Collects correlated logs, metrics, and traces across HTTP, Dapr, database, and worker execution paths. |

## Boundary decisions

- `JobService` owns the job lifecycle and state machine. A worker reports facts; it does not directly mutate job state.
- Each service owns its persistence model. The two databases are shown inside one RDS boundary for a cost-conscious starting topology, but ownership remains separate and they can move to separate RDS instances later.
- Dapr sidecars own service invocation and pub/sub transport concerns. Application code depends on Dapr abstractions rather than AWS-specific messaging APIs.
- The EF Core outbox closes the database-to-message dual-write gap in both directions: commands become work events, and execution results become outcome events.
- At-least-once delivery is expected. Inbox deduplication, idempotent handlers, leases, and guarded state transitions provide correctness when messages are duplicated or workers crash.

## Production hardening still required

- Define this topology as infrastructure as code and add repeatable environment promotion.
- Add RDS backups, point-in-time recovery, restore drills, encryption, rotation, and connection-pool limits.
- Configure pod disruption budgets, topology spread constraints, resource limits, readiness probes, and autoscaling from queue depth and execution latency.
- Load-test Redis pub/sub and evaluate Amazon MSK or another durable broker when throughput, replay, or retention requirements outgrow the Redis topology.
- Add private VPC endpoints, egress controls, network policies, Dapr access-control policies, and supply-chain scanning.
- Turn the failure-injection scenarios into repeatable resilience tests and alert validation exercises.

## Diagram assets

AWS service symbols come from the [official AWS Architecture Icons package](https://aws.amazon.com/architecture/icons/). The Dapr mark comes from the [official Dapr repository](https://github.com/dapr/dapr/blob/master/img/dapr_logo.svg). PostgreSQL is represented by the official Amazon RDS service symbol and named explicitly in the data tier.

