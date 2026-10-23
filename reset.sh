#!/usr/bin/env bash
# Volta o banco ao estado inicial (recria os schemas e os dados de exemplo).
set -euo pipefail
cd "$(dirname "$0")"
docker compose exec -T db psql -q -U cashback -d cashback \
  -f /docker-entrypoint-initdb.d/01-schema.sql \
  -f /docker-entrypoint-initdb.d/02-seed.sql
# aula 3 em diante: as migrações que existem neste ponto do código
if ls db/migracoes/[0-9][0-9][0-9]-*.sql >/dev/null 2>&1; then
  ./migrar.sh >/dev/null
fi
echo "Banco reiniciado: 3 clientes com saldo zero, 4 lojas parceiras."
