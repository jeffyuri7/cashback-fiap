#!/usr/bin/env bash
# Aula 3 · O pipeline barrando uma mudança: três cenários, cada um num branch do SEU repositório no GitHub.
#   ./mudanca-black-friday/cenarios-pipeline.sh teste-quebrado           o job "Build e testes" fica vermelho
#   ./mudanca-black-friday/cenarios-pipeline.sh dependencia-vulneravel   o job "Scan de dependências" fica vermelho
#   ./mudanca-black-friday/cenarios-pipeline.sh corrigido                o erro e a correção: fica verde e gera o pacote
#                                                                        (mostra o número do run, para usar no "promover")
#   ./mudanca-black-friday/cenarios-pipeline.sh local <cenário>          sem GitHub: roda o ./scripts/pipeline-local.sh com a mudança
#   ./mudanca-black-friday/cenarios-pipeline.sh limpar                   apaga os branches cenario/* do GitHub
# Nos dois vermelhos, o pacote não sai: o job "Pacote" depende dos outros três (needs).
# O seu código (o branch atual e o que não foi commitado) não é tocado: cada cenário é montado numa pasta
# temporária, a partir do início da aula 3 (o main do GitHub); os commits só vão para o branch cenario/<nome>.
# Precisa de um remoto "origin" no GitHub (a sua cópia do template). O gh (GitHub CLI) é opcional: com ele, o script
# mostra o número do run.
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd -P)"
BASE="${CASHBACK_BASE:-origin/main}"      # de onde os cenários partem: o início da aula 3
TMP="$(mktemp -d "${TMPDIR:-/tmp}/cashback-cenario.XXXXXX")"
REGRA="clean/src/Cashback.Clean.Domain/Rules/CategoryRule.cs"
CENARIOS=(teste-quebrado dependencia-vulneravel corrigido)

aplicar() {   # aplica a mudança do cenário na pasta $1
  case "$2" in
    teste-quebrado)
      # alguém sobe o mercado para 3% "só para testar" e esquece: a regra muda, e os testes da regra pegam
      perl -pi -e 's/"supermarket" => 0\.02m/"supermarket" => 0.03m/' "$1/$REGRA"
      MENSAGEM="Mercado com 3% de cashback"
      ;;
    dependencia-vulneravel)
      # uma biblioteca antiga, com vulnerabilidade de severidade alta (GHSA-5crp-9r3c-p9vr, corrigida na 13.0.1)
      perl -0pi -e 's|</Project>|  <ItemGroup>\n    <PackageReference Include="Newtonsoft.Json" Version="12.0.3" />\n  </ItemGroup>\n\n</Project>|' \
        "$1/clean/src/Cashback.Clean.Api/Cashback.Clean.Api.csproj"
      grep -q 'Newtonsoft.Json' "$1/clean/src/Cashback.Clean.Api/Cashback.Clean.Api.csproj" || { echo "Não consegui editar o .csproj."; exit 1; }
      MENSAGEM="Newtonsoft.Json para exportar o extrato"
      ;;
    corrigido) MENSAGEM="" ;;   # o código do início da aula, sem mudança (no GitHub, o branch leva o erro e a correção)
    *) echo "Cenários: ${CENARIOS[*]}"; exit 1 ;;
  esac
}

montar() {    # pasta temporária no commit de partida, com a mudança aplicada
  git -C "$REPO" fetch -q origin main 2>/dev/null || true
  git -C "$REPO" rev-parse -q --verify "$BASE^{commit}" >/dev/null || { echo "Não achei $BASE. Defina CASHBACK_BASE=<commit do início da aula>."; exit 1; }
  git -C "$REPO" worktree add -q --detach "$TMP" "$BASE"
  aplicar "$TMP" "$1"
  git -C "$TMP" --no-pager diff --stat
}

desmontar() { git -C "$REPO" worktree remove --force "$TMP" 2>/dev/null || rm -rf "$TMP"; git -C "$REPO" worktree prune; }
trap desmontar EXIT

# Espera o run do branch aparecer no GitHub e mostra o número (precisa do gh logado; sem ele, só avisa).
mostrar_run() {
  command -v gh >/dev/null || { echo "No GitHub: aba Actions → o run do branch cenario/$1."; return; }
  local id=""
  for _ in $(seq 1 20); do
    id="$(cd "$REPO" && gh run list --branch "cenario/$1" --workflow pipeline --limit 1 --json databaseId --jq '.[0].databaseId' 2>/dev/null || true)"
    [ -n "$id" ] && break
    sleep 3
  done
  if [ -n "$id" ]; then
    echo "Run do pipeline: $id  ($(cd "$REPO" && gh repo view --json url --jq .url)/actions/runs/$id)"
    [ "$1" = corrigido ] && echo "Quando ficar verde (~1 min), use $id no promover: Actions → promover → Run workflow."
  else
    echo "O run ainda não apareceu. No GitHub: aba Actions → o run do branch cenario/$1."
  fi
}

commitar() { git -C "$TMP" add -A && git -C "$TMP" commit -qm "$1"; }

case "${1:-}" in
  teste-quebrado|dependencia-vulneravel)
    montar "$1"
    commitar "$MENSAGEM"
    git -C "$TMP" push -qf origin "HEAD:refs/heads/cenario/$1"
    echo "Enviado: cenario/$1 ($(git -C "$TMP" rev-parse --short HEAD))."
    mostrar_run "$1"
    ;;
  corrigido)
    # a história completa: o erro entra, o teste pega, alguém corrige. O pipeline roda no último commit, que está certo.
    montar teste-quebrado
    commitar "$MENSAGEM"
    git -C "$TMP" checkout -q HEAD~1 -- "$REGRA"
    commitar "Mercado de volta a 2%: o teste pegou o erro"
    git -C "$TMP" push -qf origin "HEAD:refs/heads/cenario/corrigido"
    echo "Enviado: cenario/corrigido ($(git -C "$TMP" rev-parse --short HEAD), com o erro e a correção)."
    mostrar_run corrigido
    ;;
  local)
    montar "${2:-}"
    if [ "${2:-}" = corrigido ]; then
      echo "Rodando o pipeline local no código certo (deve passar e gerar o pacote):"
      (cd "$TMP" && ./scripts/pipeline-local.sh)
    else
      echo "Rodando o pipeline local com a mudança (deve parar no vermelho):"
      (cd "$TMP" && ./scripts/pipeline-local.sh) && echo "ATENÇÃO: o pipeline passou; o cenário não fez efeito" || echo "→ o pipeline parou: nenhum pacote foi gerado"
    fi
    ;;
  limpar)
    for b in "${CENARIOS[@]}"; do
      git -C "$REPO" push -q origin --delete "cenario/$b" 2>/dev/null && echo "Apagado no GitHub: cenario/$b" || true
    done
    ;;
  *) sed -n '2,13p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
esac
