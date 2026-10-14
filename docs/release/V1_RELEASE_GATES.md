# Auran Clinic V1 — Production Release Gates

**Status: NOT APPROVED FOR PRODUCTION.** A passing CI pipeline is evidence of code health, not approval to process live patient data. Every item below requires an identified owner, evidence link, timestamp, and sign-off in a release ticket. Do not infer sign-off from a checked CI run.

## 1. Clinical governance (Clinical Owner approval required)

- [ ] Clinicians approve the specialty-specific required-documentation policies in [#114](https://github.com/Auran-Tech/auran-clinic-backend/issues/114); validate clinic-specific policy selection, versioning, translations and legacy encounters.
- [ ] Verify explicit completion is intentional and audited; closing a session or queue does **not** imply the documentation is clinically complete.
- [ ] Clinicians execute supervised end-to-end acceptance: registration, check-in, queue move, visit documentation, result attachments, follow-up and correction/retention procedures.
- [ ] Approve historical-chart immutability and any legally required record correction/amendment process, not silent edits.
- [ ] Sign off both English and Arabic copy, time zones and medical terminology.

## 2. Security and privacy (Security/Privacy Owner approval required)

- [ ] Test an explicit **role × endpoint × action** matrix: Clinic Super User, ADMIN, DOCTOR, RECEPTIONIST, NURSE, unknown/disabled users, cross-tenant users and Platform actors. Denied paths must return 401/403/404 without exposing PHI.
- [ ] Test clinic-to-clinic isolation for patient, visits, sessions, queue, orders, files, measurements, follow-ups, audit and settings both via API and through the UI.
- [ ] Configure TLS at ingress with trusted proxies, strict CORS, network segmentation, managed secret rotation, no one-time bootstrap in production, rate limiting, security headers, and restricted SQL access.
- [ ] Perform external vulnerability review, penetration testing, malicious file/polyglot scanning and file-storage permissions. Signature validation alone is **not** antivirus scanning.
- [ ] Document access logging, privileged access approval, audit retention and incident response without storing patient note bodies in routine application logs.

## 3. Storage and deletion reliability (Infrastructure/Privacy Owners approval required)

- [x] Durable local file deletion outbox implemented in PR #128, including retryable job lease and SQL-backed tests. **This is not a production storage certification.**
- [ ] Confirm deployment storage is a persistent private volume with restricted read/write access and durable backups; files must not live inside a replaceable application container or public webroot.
- [ ] Test deleted-file recovery after process crash, disk-unavailable retry, multiple API replicas, redeployment and restored SQL backups. Confirm outbox is processed exactly once in outcome (at-least-once safe execution).
- [ ] Decide retention, legal hold, deletion approval and secure wiping policies; ensure cleanup never removes a referenced file.
- [ ] Keep heuristic orphan cleanup disabled until explicit approval and multi-replica timing/race review. Monitor outbox backlog, oldest pending age and retry failure count.
- [ ] If using S3/Azure or another object store, implement and test an IFileStorageService provider and worker handling for that provider before enablement. Current worker supports **Local** only.

## 4. Deployment and disaster recovery (Operations Owner approval required)

- [ ] Run `bash scripts/production-preflight.sh` in the target environment with secrets injected by a secret manager. It checks configuration only; it does **not** complete this checklist.
- [ ] Deploy to non-production staging with production-like multi-instance, storage, SQL, reverse proxy and secret configuration; run smoke tests after promotion.
- [ ] Establish encrypted SQL full/differential/log backups as appropriate, offsite/immutable retention, storage snapshots, access controls, alerting and retention in accordance with approved RPO/RTO.
- [ ] Restore both SQL and stored file objects into an isolated **non-production** recovery environment and verify clinic tenant boundaries, audit history, file integrity and clinical workflows. Record measured RPO/RTO and restore evidence.
- [ ] Verify backup-key availability, schema migration rollback strategy, deployment rollback plan, worker overlap, and rotation procedures.
- [ ] Configure health probes, correlation/log alerts, file deletion backlog alerts, error budgets, on-call and incident-response drill.
- [ ] Obtain release approval from clinical, engineering, security/privacy and operations owners before accepting real patient data.

## Automated evidence already covered by CI

- .NET 10 warnings-as-errors Release build, NuGet audit, SQL Server migrations and model parity.
- Unit, SQL-backed integration and hosted API smoke tests; frontend combined-stack Playwright flow.
- Clinical document finalization, queue concurrency, cross-doctor pending visibility, file authorization and file-link integrity.
- Disposable SQL backup/restore/DBCC test from PR #129 when its CI merge is confirmed. This proves only disposable CI recovery, **not** production backup integrity or RTO.

## Release evidence record (complete per deployment)

| Gate | Owner | Evidence / run | UTC date | Signed? |
|---|---|---|---|---|
| Clinical specialty policy | Clinical owner | #114 | — | No |
| Auth/privacy matrix | Security owner | Pending comprehensive review | — | No |
| Storage retention and deletion | Privacy + infra | #120 / PR #128 | — | No |
| SQL + blob restore rehearsal | Operations | CI is insufficient | — | No |
| Staging E2E & load | QA + Engineering | Pending staging run | — | No |
| Final go/no-go | Release manager | All previous gates | — | No |

### Safe rollback principle

If any clinical, security, file integrity or data-loss alert occurs, halt writes or disable affected functionality using a reviewed runbook; preserve logs/SQL/storage snapshots and do not attempt an untested backward migration against the only copy of patient data. Always rehearse recovery in an isolated environment first.

*Never put production credentials, patient data, or identifiable logs into GitHub Issues, CI artifacts, or ChatGPT.*
