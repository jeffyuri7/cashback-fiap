# App de cashback · FIAP · Arquitetura e Desenvolvimento de Software

O app de cashback que evolui aula a aula no curso. Este repositório traz o código **no início da aula 3** (DevOps como elemento arquitetural), com tudo o que é preciso para refazer, na sua máquina, as mudanças feitas ao vivo na aula: o pipeline barrando uma mudança, a campanha de Black Friday atrás de uma feature flag, a correção subindo por canário e a mudança no banco por expandir, migrar e contrair.

Todos os dados são fictícios (3 clientes e 4 lojas de exemplo).

## Começando

**1. A sua cópia.** Clique em **Use this template → Create a new repository** (pública ou privada). Assim o GitHub Actions roda no **seu** repositório, e você pode mandar push à vontade. Depois, clone a sua cópia.

**2. O que instalar:**

| Ferramenta | Para quê | Conferir |
|---|---|---|
| **Docker** (Docker Desktop no Mac e no Windows) | o banco, o painel e o gateway do canário | `docker info` |
| **.NET 10 SDK** | a API Clean | `dotnet --version` (10.x) |
| **git** e um terminal **bash** | os scripts (`.sh`) | `bash --version` |
| GitHub CLI (`gh`), opcional | mostrar o número do run nos cenários do pipeline | `gh auth status` |

- **Mac e Linux:** o terminal de sempre.
- **Windows:** use o **WSL 2** (Ubuntu), com o Docker Desktop integrado ao WSL (*Settings → Resources → WSL integration*), e clone o repositório **dentro** do WSL. Os scripts usam ferramentas do Linux (`lsof`, `pgrep`) que o Git Bash não tem.
- **Linux, só no canário:** o gateway encontra a API em `host.docker.internal`, que o Docker Desktop cria sozinho. No Docker Engine do Linux, veja o comentário no `docker-compose.yml` (aparece no patch 2).

**3. Subir o app:**

```bash
./subir.sh
```

O script sobe o **banco** (PostgreSQL, `localhost:5433`) e o **painel** (`http://localhost:8080`) no Docker, aplica as migrações pendentes e roda a **API Clean** (`localhost:5104`) com `dotnet watch`: alterou o código, ela recompila sozinha. Deixe esse terminal aberto. Ctrl+C para a API; o banco e o painel continuam no Docker (para desligar: `docker compose down`).

Se preferir os passos separados:

```bash
docker compose up -d --wait   # banco + painel
./rodar-apis.sh               # a API Clean
```

**4. Abrir o painel:** `http://localhost:8080`. No topo, o indicador da API fica verde quando ela responde. São as abas:
- **App:** clientes com saldo e teto do mês, nova compra numa loja parceira (com o percentual e o cashback calculados), resgate via PIX e extrato;
- **Loja parceira (HTTP):** a requisição como o sistema da loja a envia (versão do contrato, corpo editável, `Idempotency-Key`, timeout simulado, retry) e a resposta com status e headers;
- **Operação** (depois do patch 1 da aula 3): as feature flags, ligadas e desligadas sem deploy.

**5. Refazer a aula 3:** siga o [guia da aula 3](mudanca-black-friday/README.md).

### Outros comandos

- **Testes** (a regra testada sem banco): `dotnet test clean`.
- **Banco de volta aos dados de exemplo:** `./reset.sh`. Reinicie também a API (Ctrl+C e `./rodar-apis.sh`).
- **Migrações:** `./migrar.sh` aplica as pendentes; `./migrar.sh status` mostra o que já foi aplicado.
- **Pipeline sem GitHub:** `./scripts/pipeline-local.sh` (build, testes, scan, migrações num banco de teste e o pacote).
- **No Rider, no Visual Studio ou no VS Code:** `clean/Cashback.Clean.sln`.
- **Requisições prontas:** `requests.http` (a API Clean é a porta 5104).
- **Olhar o banco** (DBeaver, extensão do VS Code etc.): `localhost:5433`, banco `cashback`, usuário `cashback`, senha `cashback` (só de demonstração local), schema `clean`.
- **Portas ocupadas?** Mude com `CASHBACK_PORTA_BANCO`, `CASHBACK_PORTA_PAINEL`, `CASHBACK_PORTA_GATEWAY` e `CASHBACK_PORTA_API` (junto com `COMPOSE_PROJECT_NAME`); o painel aceita `?api=<porta>` no endereço.

## O app

- Lojas parceiras confirmam as compras; o cliente ganha um percentual de volta pela **categoria da loja**:
  - supermercado 2% (+2% da campanha "supermercado em dobro" em 2026);
  - combustível 3%;
  - demais 1%;
  - farmácia +5% até o fim de 2026 (a campanha da aula 1);
  - Black Friday (aula 3, depois do patch 1): +5% em todas as lojas, menos combustível, só com a flag ligada.
- **Teto:** R$ 100 de cashback por cliente por mês.
- **Resgate:** via PIX, para a chave cadastrada; o saldo nunca fica negativo.
- **Dados de exemplo:** Ana, Bruno e Carla; mercado, posto, farmácia e livraria. A Ana é a funcionária (o grupo de teste da flag).

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
| `GET` · `PUT` | `/feature-flags` · `/feature-flags/{nome}` | as flags (aula 3, depois do patch 1); o `PUT` recebe `{ "state": "off" \| "test" \| "on" }` |

Erros: `400` com `{ "errors": [...] }` (dados inválidos, loja inexistente, saldo insuficiente), `404` sem corpo (cliente inexistente), `409` (repetição enquanto a primeira ainda processa) e `422` (mesma chave com outra compra).

## Onde olhar

| Abra primeiro | O que mostra |
|---|---|
| `Domain/Entities/CashbackAccount.cs` | a entidade que protege as próprias regras |
| `Application/UseCases/` | um caso de uso por pasta; as dependências apontando para dentro (Api → Application → Domain; Infrastructure → Application) |
| `Domain/Rules/` | cada campanha é uma classe; a calculadora soma todas (aberto e fechado / Strategy) |
| `Application/DependencyInjectionExtension.cs` | injeção de dependência e tempos de vida |
| `tests/` | repositórios falsos e um relógio fixo (`TimeProvider`): a regra testada sem banco |
| `.github/workflows/` | o `pipeline` (a cada push) e o `promover` (o portão humano) |
| `db/migracoes/` | as mudanças de esquema, aplicadas pelo `./migrar.sh` numa etapa própria |

O banco é um PostgreSQL só (`db/`). A API Clean usa o schema `clean`; o script ainda cria os schemas das outras três APIs da aula 1 (big ball of mud, camadas e hexagonal), que ninguém mais usa.

## Aula 3 · O que já está aqui e o que vem nos patches

**Já está no código (o início da aula):**
- o **pipeline** (`.github/workflows/pipeline.yml`): roda a cada push, em qualquer branch; build e testes, scan de dependências, migrações num banco de teste e o pacote, gerado uma vez. Veja na aba **Actions** do seu repositório;
- o **promover** (`.github/workflows/promover.yml`): em **Actions → promover → Run workflow**, informe o número do run e o ambiente. Ele leva **o pacote daquele run**, sem compilar de novo. O deploy em si é simulado, porque a API roda na sua máquina;
- o `./migrar.sh` e a pasta `db/migracoes/`;
- o **relógio da demonstração**: no topo do painel, "pular para" quinta 17h, sexta 00h40, sábado 10h... A API usa essa hora (guardada no arquivo `.relogio`). Para voltar, escolha **hora real**;
- o header `X-Versao` em toda resposta, para saber qual versão atendeu.

**Vem nos patches** (`mudanca-black-friday/`, na ordem da aula): a campanha atrás da flag `black-friday`, a correção da farmácia no **canário** (`./canario.sh`, com o gateway nginx na porta 5100) e o cashback guardado por origem (expandir, migrar, contrair). Depois dos quatro patches, o código fica como no fim da aula 3.
