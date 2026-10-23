#!/usr/bin/env bash
# Aplica as migrações do banco (db/migracoes/NNN-*.sql) que ainda não rodaram, em ordem. Aula 3 em diante.
#   ./migrar.sh            aplica todas as pendentes
#   ./migrar.sh 003        aplica as pendentes só até a 003
#   ./migrar.sh status     mostra as aplicadas e as pendentes
# Cada migração roda numa transação: se falhar, nada dela fica no banco, e as seguintes não rodam.
# Por padrão usa o banco do docker compose. O pipeline aponta outro banco (de teste) em CASHBACK_PSQL.
set -euo pipefail
cd "$(dirname "$0")"

PSQL="${CASHBACK_PSQL:-docker compose exec -T db psql -U cashback -d cashback}"
psql_db() { $PSQL -v ON_ERROR_STOP=1 -q -X "$@"; }

aplicadas="$(psql_db -At -c "SELECT version FROM clean.schema_migrations ORDER BY version")"
ja_rodou() { grep -qx "$1" <<<"$aplicadas"; }

modo=${1:-todas}
pendentes=()
for arquivo in db/migracoes/[0-9][0-9][0-9]-*.sql; do
  [ -e "$arquivo" ] || continue
  versao="$(basename "$arquivo" .sql)"
  ja_rodou "$versao" && continue
  if [[ "$modo" =~ ^[0-9]{3}$ ]] && [[ "${versao:0:3}" > "$modo" ]]; then continue; fi
  pendentes+=("$arquivo")
done

if [ "$modo" = "status" ]; then
  echo "Aplicadas:"; [ -n "$aplicadas" ] && sed 's/^/  ✓ /' <<<"$aplicadas" || echo "  (nenhuma)"
  echo "Pendentes:"; [ ${#pendentes[@]} -gt 0 ] && printf '  · %s\n' "${pendentes[@]##*/}" || echo "  (nenhuma)"
  exit 0
fi

if [ ${#pendentes[@]} -eq 0 ]; then
  echo "Banco em dia: nenhuma migração pendente."
  exit 0
fi

for arquivo in "${pendentes[@]}"; do
  versao="$(basename "$arquivo" .sql)"
  echo "Aplicando $versao..."
  { echo "BEGIN;"; echo "SET LOCAL search_path TO clean;"; cat "$arquivo"; echo
    echo "INSERT INTO clean.schema_migrations (version) VALUES ('$versao');"; echo "COMMIT;"; } | psql_db
  echo "  ✓ $versao"
done
