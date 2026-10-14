#!/usr/bin/env bash
# Run in the intended deployment environment before promoting a clinic release.
# Never print or save production secret values.
set -euo pipefail

failed=0
check() {
  local condition="$1" description="$2"
  if [[ "$condition" == "1" ]]; then
    printf 'PASS: %s\n' "$description"
  else
    printf 'FAIL: %s\n' "$description" >&2
    failed=1
  fi
}

[[ "${ASPNETCORE_ENVIRONMENT:-}" == "Production" ]] && env_ok=1 || env_ok=0
check "$env_ok" 'Production ASP.NET environment'

sql_connection="${ConnectionStrings__DefaultConnection:-}"
[[ -n "$sql_connection" ]] && db_ok=1 || db_ok=0
check "$db_ok" 'SQL connection string injected by secret store'

# Keep connection strings out of logs. A valid encrypted SQL connection must
# validate its server certificate, not silently trust a self-signed endpoint.
normalized_sql="${sql_connection,,}"
if [[ "$normalized_sql" =~ (^|\;)encrypt=(true|yes|mandatory|strict)(\;|$) ]] &&
   [[ "$normalized_sql" =~ (^|\;)trustservercertificate=(false|no)(\;|$) ]]; then
  sql_transport_ok=1
else
  sql_transport_ok=0
fi
check "$sql_transport_ok" 'SQL transport encrypts and validates certificate'

signing_key="${Jwt__SigningKey:-}"
[[ ${#signing_key} -ge 32 ]] && jwt_ok=1 || jwt_ok=0
check "$jwt_ok" 'JWT signing key at least 32 characters'

[[ "${PlatformBootstrap__Enabled:-false}" != "true" ]] && bootstrap_ok=1 || bootstrap_ok=0
check "$bootstrap_ok" 'One-time Platform Bootstrap disabled'

[[ "${FileDeletionOutbox__Enabled:-true}" != "false" ]] && outbox_ok=1 || outbox_ok=0
check "$outbox_ok" 'Durable file deletion outbox worker enabled'

case "${FileStorage__RootPath:-}" in
  /*) storage_ok=1 ;;
  *) storage_ok=0 ;;
esac
check "$storage_ok" 'Absolute, explicitly configured file storage path'

[[ "${FileStorage__OrphanCleanupEnabled:-false}" != "true" ]] && orphan_ok=1 || orphan_ok=0
check "$orphan_ok" 'Heuristic orphan cleanup remains disabled unless separately approved'

cors_origin="${Cors__AllowedOrigins__0:-}"
[[ "$cors_origin" =~ ^https://[^/[:space:]]+(/)?$ ]] && cors_ok=1 || cors_ok=0
check "$cors_ok" 'Primary CORS origin must be explicit HTTPS'

if [[ "$failed" -ne 0 ]]; then
  echo 'Release promotion BLOCKED: configuration preflight failed.' >&2
  exit 1
fi

echo 'Configuration preflight passed. Human clinical, backup, and security sign-off remain mandatory.'
