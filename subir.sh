#!/usr/bin/env bash
# Sobe tudo o que o app precisa na sua máquina, num comando só:
#   o banco (PostgreSQL, localhost:5433) e o painel (http://localhost:8080), no Docker;
#   o gateway (localhost:5100), depois do patch 2 da aula 3;
#   as migrações pendentes do banco;
#   e a API Clean (localhost:5104), com dotnet watch: alterou o código, ela recompila sozinha.
#
#   ./subir.sh       depois, abra http://localhost:8080 no navegador. Ctrl+C para parar a API
#                    (o banco e o painel continuam no Docker; para desligar: docker compose down)
set -euo pipefail
cd "$(dirname "$0")"

command -v docker >/dev/null || { echo "Instale o Docker (Docker Desktop no Mac e no Windows)."; exit 1; }
command -v dotnet >/dev/null || { echo "Instale o .NET 10 SDK: https://dotnet.microsoft.com/download"; exit 1; }
docker info >/dev/null 2>&1 || { echo "O Docker não está rodando: abra o Docker Desktop e tente de novo."; exit 1; }

echo "Subindo o banco e o painel..."
docker compose up -d --wait

# Patch 2 em diante: o painel fala com o gateway. Aqui só a API estável roda (sem canário),
# então o gateway manda 100% do tráfego para ela.
if docker compose config --services | grep -qx gateway; then
  ./canario.sh 0 >/dev/null
fi

./migrar.sh

echo "Depois de cada patch, recarregue o painel sem cache: Cmd+Shift+R (Mac) ou Ctrl+Shift+R."
exec ./rodar-apis.sh
