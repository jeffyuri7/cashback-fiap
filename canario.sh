#!/usr/bin/env bash
# O canário da aula 3: duas versões da API no ar, e o gateway (nginx) dividindo o tráfego entre elas.
#   ./canario.sh subir      publica as duas versões e sobe ambas (Ctrl+C para parar):
#                             estável = o último commit (o que já está "em produção") · porta 5104
#                             canário = o código como está agora, com as mudanças   · porta 5105
#                           e começa com 10% do tráfego no canário
#   ./canario.sh 50         muda a fatia do canário (0 a 100) sem derrubar ninguém (reload do nginx)
#   ./canario.sh 0          tira todo o tráfego do canário: é o rollback
#   ./canario.sh status     mostra a divisão atual
# O painel passa a falar com o gateway (localhost:5100), e cada resposta diz qual versão atendeu (header X-Versao).
set -euo pipefail
cd "$(dirname "$0")"
RAIZ="$(pwd -P)"

PORTA_ESTAVEL="${CASHBACK_PORTA_API:-5104}"
PORTA_CANARIO="$((PORTA_ESTAVEL + 1))"
CONF=nginx/canario.conf

# Escreve a divisão e recarrega o nginx. O reload é gracioso: as requisições em andamento terminam com a configuração antiga.
dividir() {
  local canario=$1 estavel=$((100 - $1)) linha_estavel linha_canario
  linha_estavel="server host.docker.internal:$PORTA_ESTAVEL weight=$estavel resolve;"
  linha_canario="server host.docker.internal:$PORTA_CANARIO weight=$canario resolve;"
  [ "$estavel" -eq 0 ] && linha_estavel="server host.docker.internal:$PORTA_ESTAVEL down resolve;"
  [ "$canario" -eq 0 ] && linha_canario="server host.docker.internal:$PORTA_CANARIO down resolve;"
  cat > "$CONF" <<NGINX
# Escrito pelo ./canario.sh: canário com $canario% do tráfego, estável com $estavel%.
# A API roda na máquina (host.docker.internal). Só IPv4: o endereço IPv6 que o Docker também devolve não responde.
resolver 127.0.0.11 ipv6=off valid=30s;

upstream api {
    zone api 64k;                 # a contagem é uma só para todos os workers do nginx: 10% é 1 em cada 10
    $linha_estavel   # estável
    $linha_canario   # canário
}

server {
    listen 80;
    location / {
        proxy_pass http://api;
        proxy_next_upstream off;  # erro no canário aparece como erro; não é escondido mandando de novo para a estável
    }
}
NGINX
  if docker compose ps --status running -q gateway 2>/dev/null | grep -q .; then
    docker compose exec -T gateway nginx -t -q && docker compose exec -T gateway nginx -s reload 2>/dev/null
  else
    docker compose up -d gateway >/dev/null 2>&1
  fi
  # os workers novos assumem em instantes (os antigos terminam o que estavam atendendo) e o nome da máquina é resolvido de novo
  sleep 2
  echo "Gateway: canário com $canario% do tráfego · estável com $estavel%."
}

publicar() {
  local nome=$1 fonte=$2
  echo "Publicando a versão $nome..."
  dotnet publish "$fonte/clean/src/Cashback.Clean.Api/Cashback.Clean.Api.csproj" -c Release -o ".versoes/$nome" -v q -nologo >/dev/null
}

rodar() {
  local nome=$1 porta=$2
  (
    cd ".versoes/$nome"
    export Versao="$nome" Urls="http://localhost:$porta" Demo__ClockFile="$RAIZ/.relogio"
    if [ -n "${CASHBACK_PORTA_BANCO:-}" ]; then
      export ConnectionStrings__Cashback="Host=localhost;Port=$CASHBACK_PORTA_BANCO;Database=cashback;Username=cashback;Password=cashback;Search Path=clean"
    fi
    exec dotnet Cashback.Clean.Api.dll
  ) 2>&1 | sed -u "s/^/[$nome] /" &
}

case "${1:-}" in
  subir)
    ./rodar-apis.sh parar >/dev/null
    for porta in "$PORTA_ESTAVEL" "$PORTA_CANARIO"; do
      if lsof -nP -iTCP:"$porta" -sTCP:LISTEN >/dev/null 2>&1; then
        echo "A porta $porta está ocupada:"; lsof -nP -iTCP:"$porta" -sTCP:LISTEN; exit 1
      fi
    done
    # a estável sai do último commit, sem as mudanças ainda não commitadas
    rm -rf .versoes/fonte-estavel && mkdir -p .versoes/fonte-estavel
    git archive HEAD clean | tar -x -C .versoes/fonte-estavel
    publicar estavel .versoes/fonte-estavel
    publicar canario .
    trap 'trap - INT TERM; echo; echo "Encerrando as duas versões..."; kill $(jobs -p) 2>/dev/null; wait; exit 0' INT TERM
    rodar estavel "$PORTA_ESTAVEL"
    rodar canario "$PORTA_CANARIO"
    dividir 10
    echo "Estável em :$PORTA_ESTAVEL · canário em :$PORTA_CANARIO · gateway em localhost:${CASHBACK_PORTA_GATEWAY:-5100} · Ctrl+C para parar"
    wait
    ;;
  status)
    grep -E '^# Escrito' "$CONF" 2>/dev/null || echo "Sem divisão ainda: ./canario.sh subir"
    ;;
  ''|*[!0-9]*)
    sed -n '2,10p' "$0" | sed 's/^# \{0,1\}//'
    exit 1
    ;;
  *)
    [ "$1" -le 100 ] || { echo "A fatia vai de 0 a 100."; exit 1; }
    dividir "$1"
    ;;
esac
