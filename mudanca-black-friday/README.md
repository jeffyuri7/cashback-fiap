# Aula 3 · Refaça a Black Friday na sua máquina

O requisito: **a campanha de Black Friday entra na sexta 27/11 às 00h e sai no domingo 29/11 às 23h59, sem deploy de madrugada, e pode ser desligada se der problema.**

Este guia repete, na ordem, as mudanças que foram feitas ao vivo na aula. Cada mudança é um patch (um arquivo `.patch`, com as alterações prontas) que você aplica com `git apply`:

| Patch | O que muda | A versão |
|---|---|---|
| `1-campanha-com-flag.patch` | a campanha (+5%, menos combustível) atrás da flag `black-friday`, com horário; a aba **Operação** do painel; a migração `001` | v1, com um erro na farmácia |
| `2-correcao-no-canario.patch` | a correção da farmácia (e um teste por categoria); o bônus guardado à parte; o **canário** (gateway nginx + `canario.sh`); a migração `002` (**expandir**) | v2: escreve na coluna antiga e nas novas, lê da antiga |
| `3-migrar-e-ler-das-colunas-novas.patch` | o extrato mostra o bônus da Black Friday; a migração `003` (**migrar**) | v3: escreve nas três, lê das novas |
| `4-contrair.patch` | para de escrever na coluna antiga; a migração `004` (**contrair**: remove a coluna) | v4: só as colunas novas |

Também estão aqui:
- `cenarios-pipeline.sh`: três cenários para ver o pipeline barrar uma mudança (um teste quebrado e uma dependência vulnerável) e liberar a correção;
- `demo-1-renomear-direto.sql` e `demo-2-desfazer.sql`: o jeito errado de mudar o banco, e como desfazer.

## Antes de começar

Siga o **Começando** do [README principal](../README.md): a sua cópia do repositório no GitHub, Docker e .NET 10 instalados. Depois, crie um branch para fazer as mudanças. O `main` fica no início da aula, e é dele que os cenários do pipeline partem.

```bash
git switch -c aula-03
./subir.sh                    # banco + painel + a API (deixe este terminal aberto: é o terminal 1)
```

Abra `http://localhost:8080`. Você vai usar **dois terminais** na pasta do projeto: o **terminal 1** fica com a API (ou o canário) rodando; o **terminal 2** é para os comandos.

**Regras que valem para o guia inteiro:**
- **Depois de cada patch, reinicie a API:** Ctrl+C no terminal 1 e rode de novo o comando indicado. O `dotnet watch` recompila sozinho, mas não pega controller nem injeção de dependência novos.
- **Depois de cada patch, recarregue o painel sem cache:** Cmd+Shift+R no Mac, Ctrl+Shift+R no Windows e no Linux. Um F5 comum mantém o `app.js` antigo, e a aba **Operação** aparece em branco.
- **O relógio do app:** no topo do painel, "pular para" leva o app para quinta 17h, sexta 00h40, sábado 10h... Só existe para a demonstração (a hora fica no arquivo `.relogio`). Para voltar, escolha **hora real**.

## 1 · O pipeline

O pipeline roda no GitHub a cada push, em qualquer branch. Não precisa abrir pull request.

- Abra `.github/workflows/pipeline.yml`: testes e scan de dependências em paralelo, as migrações num banco de teste, e o **pacote, gerado uma vez**, só se tudo passou.
- Abra `.github/workflows/promover.yml`: ele leva **esse** pacote a um ambiente. O portão humano é o botão **Run workflow**. Ele baixa o pacote daquele run e confere o sha256: não compila de novo. O deploy em si é simulado, porque a API roda na sua máquina.

**Veja o pipeline barrar uma mudança** (no terminal 2):

```bash
./mudanca-black-friday/cenarios-pipeline.sh teste-quebrado           # o mercado vai a 3% "só para testar"
./mudanca-black-friday/cenarios-pipeline.sh dependencia-vulneravel   # uma biblioteca com vulnerabilidade grave
./mudanca-black-friday/cenarios-pipeline.sh corrigido                # o erro, o teste pegando, e a correção
```

Cada um vai para um branch `cenario/<nome>` no seu GitHub. O seu branch `aula-03` e os seus arquivos não são tocados.

| Branch | O que acontece | O pacote |
|---|---|---|
| `cenario/teste-quebrado` | **Build e testes** fica vermelho: os testes que esperam o mercado a 2% falham | não sai (o job aparece como pulado) |
| `cenario/dependencia-vulneravel` | **Scan de dependências** fica vermelho: `Newtonsoft.Json 12.0.3`, severidade alta. O build só avisa (`warning NU1903`); quem para o pipeline é o scan | não sai |
| `cenario/corrigido` | tudo verde | sai: o artefato `cashback-api` |

**Promova o pacote verde:** na aba **Actions**, abra o run do `cenario/corrigido` e copie o número dele (está no resumo e no endereço da página; com o `gh` instalado, o script já mostra). Depois, **Actions → promover → Run workflow**, com esse número e o ambiente.

- **Sem internet ou sem GitHub:** `./mudanca-black-friday/cenarios-pipeline.sh local teste-quebrado` roda as mesmas etapas na sua máquina (`./scripts/pipeline-local.sh`), com a mudança.
- **No fim:** `./mudanca-black-friday/cenarios-pipeline.sh limpar` apaga os três branches `cenario/*` do GitHub.

## 2 · A campanha atrás da flag

```bash
git apply mudanca-black-friday/1-campanha-com-flag.patch
./migrar.sh                   # 001: a tabela de flags e a flag black-friday, desligada
# terminal 1: Ctrl+C e ./rodar-apis.sh   ·   Cmd+Shift+R no painel
```

Relógio em **qui 26/11 · 17:00**. Faça compras de R$ 100 na aba **App**; a flag muda na aba **Operação**:

| Momento | Ação | Resultado |
|---|---|---|
| quinta 17:00 | Ana compra no mercado, flag **Desligada** | 4% (R$ 4) |
| | flag em **Grupo de teste** (a Ana é a funcionária) | Ana no mercado: **9%**; Bruno no mercado: 4% |
| | flag em **Todos, no horário** | "vale agora para: ninguém": a campanha ainda não começou; a Ana volta a 4% |
| quinta 23:59:40 | Bruno no mercado | 4% |
| (espere virar 00:00) | Bruno no mercado de novo | **9%**: a campanha ligou sozinha, sem ninguém acordado |
| sexta 00:40 | Carla na **farmácia** | **16%**: deveria ser 11% (1% + 5% da farmácia + 5% da Black Friday) |
| | flag **Desligada** (o kill switch) | Carla na farmácia: 6%. Desligou para todos, sem deploy, em segundos |

- **No código:** `BlackFridayCampaign.cs` declara a flag (e tem o erro: a farmácia com 10%, somando de novo a campanha da farmácia); `CashbackCalculator.cs` deixa de fora a regra com flag desligada (o **ponto de decisão**); `IFeatureFlags.cs` decide para quem e quando (o **roteador**).
- **No teste:** `BlackFridayCampaignTests.cs` testa mercado e combustível. Ninguém testou a farmácia.

Faça o commit. **Ele é necessário:** no passo 3, este commit vira a versão estável do canário. O push é opcional: serve para ver o pipeline passar, verde, com o erro dentro. "O pipeline só prova o que os testes cobrem."

```bash
git add -A && git commit -qm "Black Friday atrás da flag"
git push -u origin aula-03    # opcional
```

## 3 · A correção no canário (sábado 10h)

```bash
git apply mudanca-black-friday/2-correcao-no-canario.patch
./migrar.sh                   # 002: EXPANDIR (o passo 4 explica)
# terminal 1: Ctrl+C e
./canario.sh subir            # estável = o último commit (v1) · canário = o código atual (v2) · 10% no canário
# Cmd+Shift+R no painel: agora ele fala com o gateway (localhost:5100)
```

O `./canario.sh subir` leva cerca de um minuto (publica as duas versões). Ele deixa a **estável** na porta 5104, o **canário** na 5105 e o **gateway** (nginx, no Docker) na 5100, dividindo o tráfego. Toda resposta traz o header `X-Versao`, dizendo quem atendeu.

Relógio em **sáb 28/11 · 10:00**. A flag está desligada desde a sexta. Na aba **Operação**, ponha a flag em **Grupo de teste**: só a Ana vê a campanha, e os clientes não correm risco. No cartão **Canário**, faça uma rajada de 20 compras da **Ana**, na **farmácia**, de R$ 5, e mude a fatia no terminal 2:

| Fatia | Rajada | Resultado |
|---|---|---|
| `./canario.sh subir` (10%) | 20 compras | estável **18** (16%) · canário **2** (11%) |
| `./canario.sh 50` | 20 compras | estável 10 · canário 10 |
| `./canario.sh 0` (o **rollback**) | 20 compras | estável 20 |
| `./canario.sh 100` | 20 compras | canário 20 (11%) |
| flag em **Todos** | Bruno na farmácia | 11% para todos |

A divisão é aproximada: o nginx distribui pelos pesos, então 10% pode dar 1, 2 ou 3 no canário.

- A correção entrou por canário com a campanha **desligada para o público**; a campanha só voltou quando a versão nova estava em 100%. Deploy ≠ release.
- Veja `nginx/canario.conf` (os pesos), `canario.sh` (o reload do nginx, sem derrubar ninguém) e o teste novo por categoria em `BlackFridayCampaignTests.cs`.

Faça o commit (necessário: no passo 4, ele vira a versão estável). O push é opcional: mostra o pipeline com o teste novo.

```bash
git add -A && git commit -qm "Correção da farmácia no canário"
git push -u origin aula-03    # opcional
```

## 4 · O banco: expandir, migrar, contrair

Com o canário, duas versões ficam no ar ao mesmo tempo, **usando o mesmo banco**. Código se reimplanta; dado gravado não se desgrava.

**O jeito errado** (com as duas versões no ar, no terminal 2):

```bash
./canario.sh 50
docker compose exec -T db psql -q -U cashback -d cashback < mudanca-black-friday/demo-1-renomear-direto.sql
```

- Faça uma rajada com o **Bruno** (a Ana está perto do teto de R$ 100 do mês): **erro nas duas versões**, porque as duas ainda usam o nome antigo da coluna. No canário sempre existe uma versão antiga.
- Desfaça e veja a rajada voltar a funcionar:

```bash
docker compose exec -T db psql -q -U cashback -d cashback < mudanca-black-friday/demo-2-desfazer.sql
```

**O jeito certo** já foi feito no passo 3: veja `db/migracoes/002-expandir-cashback-por-origem.sql`. As colunas novas entraram **antes** da v2, aceitando vazio; a v1 nem as vê. Por isso o rollback do passo 3 foi seguro.

**Segunda-feira 30/11, 09:00 · migrar e ler das colunas novas:**

```bash
./canario.sh 100              # a v1 sai do tráfego: a 003 torna as colunas novas obrigatórias, e a v1 não as escreve
git apply mudanca-black-friday/3-migrar-e-ler-das-colunas-novas.patch
./migrar.sh 003               # 003: MIGRAR (copia o cashback antigo para as colunas novas)
# terminal 1: Ctrl+C e
./canario.sh subir            # estável = v2 (o commit do passo 3) · canário = v3
```

- `./canario.sh 100` e, na aba **App**, o extrato: na **Ana**, a coluna **Black Friday** com o bônus das compras do sábado; na **Carla**, a compra de 16% da sexta aparece com "—". A 003 não tem como separar o bônus das compras antigas: é uma decisão que se toma antes de migrar.
- Depois da 003, a v1 não grava mais: é o primeiro passo sem volta.

**Contrair:**

```bash
./canario.sh 100
git add -A && git commit -qm "Ler das colunas novas"
git apply mudanca-black-friday/4-contrair.patch
# terminal 1: Ctrl+C e
./canario.sh subir            # estável = v3 · canário = v4
# terminal 2:
./canario.sh 100              # a v4 em 100%: nenhuma versão no tráfego usa mais a coluna antiga
./migrar.sh                   # 004: CONTRAIR (remove a coluna cashback)
git add -A && git commit -qm "Contrair: sem a coluna antiga"
```

A ordem é a lição: **a coluna só sai quando nenhuma versão no ar a usa.** Depois da 004, nada anterior à v3 funciona.

### As versões e o banco, em resumo

| | grava em `cashback` (antiga) | grava nas novas | lê de | convive com |
|---|---|---|---|---|
| v1 (patch 1) | sim | não | antiga | v2 |
| v2 (patch 2, depois da `002`) | sim | sim | antiga | v1, v3 |
| v3 (patch 3, depois da `003`) | sim | sim | novas | v2, v4 |
| v4 (patch 4; a `004` vem depois dela em 100%) | não | sim | novas | v3 (até a `004`) |

## Voltar ao início

Pare a API ou o canário (Ctrl+C no terminal 1) e, no terminal 2:

```bash
git switch -f main            # volta ao início da aula 3, descartando o que não foi commitado
git clean -fd                 # apaga os arquivos que os patches criaram
git branch -D aula-03         # apaga o branch com os seus commits (opcional)
rm -f .relogio                # o relógio volta para a hora real
docker compose up -d --remove-orphans   # tira o gateway, que só existe a partir do patch 2
./reset.sh                    # o banco volta aos dados de exemplo
```

Se o `aula-03` já tinha ido para o GitHub e você recomeçar com um branch novo de mesmo nome, o primeiro push precisa de `-f`: `git push -f origin aula-03`.

## Se algo não funcionar

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| A aba **Operação** não aparece, aparece em branco, ou diz que a versão não tem feature flags | o navegador manteve o `app.js` antigo, ou a API não foi reiniciada depois do patch 1 | Cmd+Shift+R (Ctrl+Shift+R); se não resolver, Ctrl+C e `./rodar-apis.sh` |
| `git apply` diz *patch does not apply* | o patch já foi aplicado, ou um arquivo foi alterado à mão | `git status`; para recomeçar, veja **Voltar ao início** |
| O painel mostra **"API fora do ar (localhost:5100)"** depois do `./canario.sh subir` | o gateway não subiu, ou um container antigo ficou sem a configuração | `docker compose up -d --force-recreate gateway` e Cmd+Shift+R |
| `./canario.sh subir` diz que a porta 5104 está ocupada | sobrou uma API rodando | `./rodar-apis.sh parar` e de novo |
| Uma parte das compras dá erro, sem canário no ar | o gateway ainda manda tráfego para a 5105 | `./canario.sh 0` (100% na estável) ou suba o canário |
| A rajada com a Ana não credita mais nada | o teto de R$ 100 do mês | use o Bruno ou a Carla, ou `./reset.sh` |
| O run não aparece no GitHub | o Actions está desligado na sua cópia | aba **Actions** → habilite os workflows |

## Limites conhecidos (bons para discutir)

- **Sem transação entre a conta e a compra:** quando a gravação da compra falha (por exemplo, a v1 depois da `003`), o saldo já foi creditado. Em produção, os dois iriam na mesma transação.
- **A migração de dados só sabe o que a coluna antiga guardava:** a `003` copia o total para o cashback base e põe bônus zero nas compras antigas, inclusive nas da sexta-feira, que tiveram Black Friday.
- **O kill switch é grosso:** desligar a flag tira a campanha de todas as categorias, não só da farmácia.
- **A flag mora no banco do app** e é lida a cada compra. Em escala, entraria um cache curto ou um serviço de flags (OpenFeature).
- **O canário é por requisição** (`weight` no nginx): o mesmo cliente pode cair ora numa versão, ora na outra. Para fixar cada cliente numa versão, o nginx tem `split_clients`.
- **A estável sai do último commit:** faça o commit antes de `./canario.sh subir`, senão a "estável" não é o que estava em produção.
- O relógio ajustável e a rota `/demo/clock` existem só para a demonstração.
