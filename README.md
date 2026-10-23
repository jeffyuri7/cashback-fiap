# App de cashback · FIAP · Arquitetura e Desenvolvimento de Software

O app de cashback que evolui aula a aula no curso. Este repositório traz o código **no fim da aula 3** (DevOps como elemento arquitetural): a API Clean, o painel, o banco, o pipeline no GitHub Actions, a campanha de Black Friday atrás de uma feature flag, o canário e as migrações por expandir, migrar e contrair.

Todos os dados são fictícios (3 clientes e 4 lojas de exemplo).

## Começando

1. Clique em **Use this template → Create a new repository** para ter uma cópia sua (pública ou privada). Assim o GitHub Actions já vem ligado e o pipeline roda no seu repositório.
2. Clone a sua cópia e instale **Docker** e o **.NET 10 SDK**.

```bash
docker compose up -d --wait   # banco (localhost:5433) com as tabelas e os dados de exemplo + painel (http://localhost:8080) + gateway (:5100)
./migrar.sh                   # aplica as migrações do banco (db/migracoes, 001 a 004): a API deste ponto precisa delas
./rodar-apis.sh               # a API Clean com dotnet watch (alterou, recompilou); Ctrl+C para parar
```

- **Painel:** `http://localhost:8080`, em três abas:
  - **App:** clientes com saldo e teto do mês, nova compra numa loja parceira (com o percentual e o cashback calculados), resgate via PIX e extrato;
  - **Loja parceira (HTTP):** a requisição como o sistema da loja a envia (versão do contrato, corpo editável, `Idempotency-Key`, timeout simulado, retry) e a resposta com status e headers;
  - **Operação:** as feature flags, ligadas e desligadas sem deploy.
- **Voltar ao início:** `./reset.sh` (recria as tabelas, os dados de exemplo e aplica as migrações). Reinicie também a API (Ctrl+C e `./rodar-apis.sh`), para zerar qualquer estado guardado em memória.
- **No Rider ou no Visual Studio:** `clean/Cashback.Clean.sln`.
- **Requisições prontas:** `requests.http` (a API Clean é a porta 5104).
- **Testes** (a regra testada sem banco): `dotnet test clean`.

## O app

- Lojas parceiras confirmam as compras; o cliente ganha um percentual de volta pela **categoria da loja**:
  - supermercado 2% (+2% da campanha "supermercado em dobro" em 2026);
  - combustível 3%;
  - demais 1%;
  - farmácia +5% até o fim de 2026 (a campanha da aula 1);
  - Black Friday (aula 3): +5% em todas as lojas, menos combustível, só com a flag ligada.
- **Teto:** R$ 100 de cashback por cliente por mês.
- **Resgate:** via PIX, para a chave cadastrada; o saldo nunca fica negativo.

**Contrato das lojas (aula 2, em `contratos/`, OpenAPI 3.1):**

| Método | Rota | O que faz |
|---|---|---|
| `GET` | `/stores` | lojas parceiras |
| `GET` | `/customers` | clientes e saldos |
| `POST` | `/purchases` | v1 (depreciada): registra a compra (`customerId`, `storeId`, `amount`), `Idempotency-Key` opcional, `200 OK` com o saldo; responde com `Deprecation`, `Sunset` e `Link` |
| `POST` | `/v2/purchases` | v2: `Idempotency-Key` obrigatório, `201 Created` + `Location`, resposta sem o saldo |
| `GET` | `/v2/purchases/{id}` | v2: consultar uma compra (por exemplo, depois de um timeout) |
| `GET` | `/customers/{id}/balance` | saldo, crédito do mês e teto |
| `GET` | `/customers/{id}/purchases` | extrato (últimas 10) |
| `POST` | `/customers/{id}/redemptions` | resgate via PIX (`amount`) |
| `GET` · `PUT` | `/feature-flags` · `/feature-flags/{nome}` | as flags (aula 3); o `PUT` recebe `{ "state": "off" \| "test" \| "on" }` |

Erros: `400` com `{ "errors": [...] }` (dados inválidos, loja inexistente, saldo insuficiente), `404` sem corpo (cliente inexistente), `409` (repetição enquanto a primeira ainda processa) e `422` (mesma chave com outra compra).

## Onde olhar

| Abra primeiro | O que mostra |
|---|---|
| `Domain/Entities/CashbackAccount.cs` | a entidade que protege as próprias regras |
| `Application/UseCases/` | um caso de uso por pasta; as dependências apontando para dentro (Api → Application → Domain; Infrastructure → Application) |
| `Domain/Rules/` | cada campanha é uma classe; a calculadora soma todas (aberto e fechado / Strategy); a Black Friday diz qual flag a liga |
| `Application/DependencyInjectionExtension.cs` | injeção de dependência e tempos de vida |
| `tests/` | repositórios falsos e um relógio fixo (`TimeProvider`): a regra testada sem banco, com a flag ligada e desligada |

O banco é um PostgreSQL só (`db/`). A API Clean usa o schema `clean`; o script ainda cria os schemas das outras três APIs da aula 1 (big ball of mud, camadas e hexagonal), que ninguém mais usa.

## Aula 3 · A Black Friday: pipeline, flag, canário e banco

O requisito: a campanha entra na sexta 27/11 às 00h e sai no domingo 29/11 às 23h59, **sem deploy de madrugada**, e **pode ser desligada** se der problema.

- **Pipeline** (`.github/workflows/`):
  - `pipeline` roda a cada push: build e testes, scan de dependências, migrações num banco de teste e o pacote, gerado uma vez. Veja na aba **Actions** do seu repositório; o número do run aparece no resumo.
  - `promover` leva **esse** pacote a um ambiente: em **Actions → promover → Run workflow**, informe o número do run e o ambiente. É o portão humano. O deploy em si é simulado, porque a API roda na sua máquina.
  - Sem internet ou sem GitHub: `./scripts/pipeline-local.sh` roda as mesmas etapas na sua máquina.
- **Migrações** (`db/migracoes/`): `./migrar.sh` aplica as pendentes, numa etapa própria, antes de subir a versão que precisa delas; `./migrar.sh status` mostra o que já foi aplicado. As quatro migrações contam a história da aula: as flags (001) e o cashback guardado por origem, em três passos: expandir (002), migrar (003) e contrair (004).
- **Relógio da demonstração:** no topo do painel, "pular para" quinta 17h, sexta 00h40, sábado 10h... A API usa essa hora (guardada no arquivo `.relogio`). Para voltar, escolha **hora real** (ou `DELETE /demo/clock`).
- **Feature flag `black-friday`** (aba **Operação**): `off` (ninguém), `test` (só o grupo de teste, a Ana, a qualquer hora) e `on` (todos, dentro da janela da campanha). Ela começa desligada.
- **Canário:** `./canario.sh subir` publica e sobe a **estável** (o último commit, :5104) e o **canário** (o código como está agora, com as suas alterações ainda sem commit, :5105); o **gateway** (nginx, :5100) divide o tráfego. `./canario.sh 50` muda a fatia, e `./canario.sh 0` é o rollback. O painel passa a falar com o gateway, e cada resposta diz quem atendeu (header `X-Versao`). Para ver duas versões diferentes, altere o código antes de subir.
- **Outras portas** (se a 5433, a 8080 ou a 5104 já estiverem ocupadas): `CASHBACK_PORTA_BANCO`, `CASHBACK_PORTA_PAINEL`, `CASHBACK_PORTA_GATEWAY` e `CASHBACK_PORTA_API`, junto com `COMPOSE_PROJECT_NAME`; o painel aceita `?api=<porta>`.
