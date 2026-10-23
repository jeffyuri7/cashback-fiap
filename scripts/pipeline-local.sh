#!/usr/bin/env bash
# Plano B do pipeline (aula 3): as mesmas etapas do .github/workflows/pipeline.yml, rodando nesta máquina.
# Serve para a aula sem internet, ou para mostrar o que cada etapa prova antes de olhar o GitHub.
#   ./scripts/pipeline-local.sh
# Etapas: build → testes → scan de dependências → migrações num banco de teste descartável → pacote.
# Qualquer etapa que falha para o pipeline: o pacote só sai se tudo passou.
set -euo pipefail
cd "$(dirname "$0")/.."

SOLUCAO=clean/Cashback.Clean.sln
VERSAO="$(git rev-parse --short HEAD)$( [ -n "$(git status --porcelain)" ] && echo '-com-alteracoes' || true)"
inicio_total=$SECONDS

etapa() { printf '\n\033[1m▶ %s\033[0m\n' "$1"; inicio=$SECONDS; }
ok()    { printf '\033[32m✓ %s\033[0m (%ss)\n' "$1" "$((SECONDS - inicio))"; }

etapa "1/5 Build: o código compila"
dotnet build "$SOLUCAO" -c Release -v q -nologo
ok "build"

etapa "2/5 Testes: as regras funcionam (sem banco, em segundos)"
dotnet test "$SOLUCAO" -c Release --no-build -v q -nologo
ok "testes"

etapa "3/5 Scan: nenhuma dependência com vulnerabilidade alta ou crítica"
dotnet package list --project "$SOLUCAO" --vulnerable --include-transitive --format json > /tmp/cashback-scan.json
graves="$(jq -r '[.projects[].frameworks[]? | (.topLevelPackages[]?, .transitivePackages[]?)
  | select(any(.vulnerabilities[]?; .severity == "High" or .severity == "Critical"))
  | "\(.id) \(.resolvedVersion)"] | unique | .[]' /tmp/cashback-scan.json)"
if [ -n "$graves" ]; then
  echo "Pacotes com vulnerabilidade alta ou crítica:"; echo "$graves" | sed 's/^/  ✗ /'
  exit 1
fi
ok "scan (nenhuma vulnerabilidade alta ou crítica)"

etapa "4/5 Migrações: aplicam do zero num banco de teste descartável"
banco="cashback-pipeline-$$"
docker run -d --rm --name "$banco" -e POSTGRES_USER=cashback -e POSTGRES_PASSWORD=cashback -e POSTGRES_DB=cashback \
  postgres:17-alpine >/dev/null
trap 'docker rm -f "$banco" >/dev/null 2>&1 || true' EXIT
until docker exec "$banco" pg_isready -U cashback -d cashback -q 2>/dev/null; do sleep 1; done
sleep 1
docker exec -i "$banco" psql -q -v ON_ERROR_STOP=1 -U cashback -d cashback < db/01-schema.sql
docker exec -i "$banco" psql -q -v ON_ERROR_STOP=1 -U cashback -d cashback < db/02-seed.sql
CASHBACK_PSQL="docker exec -i $banco psql -U cashback -d cashback" ./migrar.sh
ok "migrações"

etapa "5/5 Pacote: gerado uma vez, com a impressão digital (sha256)"
destino="artefatos/$VERSAO"
rm -rf "$destino" && mkdir -p "$destino"
dotnet publish clean/src/Cashback.Clean.Api/Cashback.Clean.Api.csproj -c Release --no-build -o "$destino/api" -v q -nologo
cp -R db "$destino/db"
printf 'commit %s\ngerado em %s\n' "$VERSAO" "$(date '+%d/%m/%Y %H:%M:%S')" > "$destino/versao.txt"
(cd "$destino" && find api db versao.txt -type f | sort | xargs shasum -a 256 > ../"$VERSAO".sha256)
ok "pacote em $destino"

printf '\n\033[1mPipeline local aprovado em %ss.\033[0m O pacote %s é o que seria promovido (o mesmo, sem compilar de novo).\n' \
  "$((SECONDS - inicio_total))" "$VERSAO"
