#!/usr/bin/env bash
# Sobe a API Clean com "dotnet watch": cada alteração no código recompila e reinicia sozinha.
#   ./rodar-apis.sh          a API Clean (:5104), o projeto que evolui da aula 2 em diante
#   ./rodar-apis.sh clean    o mesmo
#   ./rodar-apis.sh parar    só encerra o que tiver sobrado rodando (não sobe nada)
# Ctrl+C para parar. (O banco e o painel sobem com: docker compose up -d)
# Portas diferentes (para ensaiar numa cópia): CASHBACK_PORTA_API (padrão 5104) e CASHBACK_PORTA_BANCO (padrão 5433).
#
# O Ctrl+C sozinho nem sempre encerra o "dotnet watch": as cópias que sobram continuam vigiando os arquivos
# e disputam a porta a cada alteração. Por isso o script encerra (kill -9) tudo deste projeto ao começar e ao sair.
set -uo pipefail
cd "$(dirname "$0")"
RAIZ="$(pwd -P)"
PORTAS="${CASHBACK_PORTA_API:-5104}"

# A API lê estas variáveis no lugar do appsettings.json:
export Urls="http://localhost:$PORTAS"
export Demo__ClockFile="$RAIZ/.relogio"   # o relógio da demonstração (aula 3): o painel pode "pular" para outra hora
if [ -n "${CASHBACK_PORTA_BANCO:-}" ]; then
  export ConnectionStrings__Cashback="Host=localhost;Port=$CASHBACK_PORTA_BANCO;Database=cashback;Username=cashback;Password=cashback;Search Path=clean"
fi

# Processos deste projeto: o dotnet watch e as APIs que ele sobe, reconhecidos por rodarem dentro desta pasta.
# (Assim o script não encerra o VS Code, o Rider nem um dotnet watch de outro projeto.)
processos_do_projeto() {
  local pid pasta
  for pid in $(pgrep -f "dotnet watch|dotnet-watch|DOTNET_WATCH=1|/bin/Debug/net10.0/Cashback\."); do
    pasta="$(lsof -a -p "$pid" -d cwd -Fn 2>/dev/null | sed -n 's/^n//p')"
    case "$pasta" in "$RAIZ" | "$RAIZ"/*) echo "$pid" ;; esac
  done
}

parar() {
  local pids tentativa
  for tentativa in 1 2 3; do
    pids="$(processos_do_projeto)"
    [ -z "$pids" ] && return 0
    kill -9 $pids 2>/dev/null
    sleep 1
  done
}

if [ "${1:-}" = "parar" ]; then
  parar
  echo "APIs do cashback encerradas."
  exit 0
fi

case "${1:-clean}" in
  clean) ;;
  *) echo "Uso: ./rodar-apis.sh [clean|parar]"; exit 1 ;;
esac

# 1. Limpa o que sobrou de execuções anteriores
sobras="$(processos_do_projeto | wc -l | tr -d ' ')"
if [ "$sobras" -gt 0 ]; then
  echo "Encerrando $sobras processos que sobraram de execuções anteriores..."
  parar
fi

# 2. A porta precisa estar livre
if lsof -nP -iTCP:"$PORTAS" -sTCP:LISTEN >/dev/null 2>&1; then
  echo "A porta $PORTAS está ocupada por outro programa:"
  lsof -nP -iTCP:"$PORTAS" -sTCP:LISTEN
  echo "Encerre-o (kill -9 <PID>) e rode de novo."
  exit 1
fi

# 3. Sobe a API
rodar() {
  local nome=$1 projeto=$2
  DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 dotnet watch run --non-interactive --project "$projeto" 2>&1 \
    | sed -u "s/^/[$nome] /" &
}

trap 'trap - INT TERM EXIT; echo; echo "Encerrando a API..."; parar; exit 0' INT TERM
trap 'parar' EXIT

rodar "clean" clean/src/Cashback.Clean.Api/Cashback.Clean.Api.csproj
echo "Subindo a API Clean em :$PORTAS · painel em http://localhost:${CASHBACK_PORTA_PAINEL:-8080} · Ctrl+C para parar"
echo "A API está no ar quando o log mostra \"Now listening on\"."
wait
