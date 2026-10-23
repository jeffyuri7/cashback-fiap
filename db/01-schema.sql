SET client_min_messages TO warning;
-- =====================================================================================
--  Banco do app de cashback (aula 1).
--  Um schema por API, todos com as MESMAS tabelas e os MESMOS dados de exemplo:
--  assim uma compra feita numa API não mexe no saldo mostrado pelas outras,
--  e dá para comparar as quatro lado a lado.
--  O script recria tudo do zero (é o mesmo usado pelo reset).
-- =====================================================================================

DO $$
DECLARE
    s text;
BEGIN
    FOREACH s IN ARRAY ARRAY['bigball', 'camadas', 'hexagonal', 'clean'] LOOP
        EXECUTE format('DROP SCHEMA IF EXISTS %I CASCADE', s);
        EXECUTE format('CREATE SCHEMA %I', s);

        -- quem usa o app
        EXECUTE format($t$
            CREATE TABLE %I.customers (
                id       text PRIMARY KEY,
                name     text NOT NULL,
                pix_key  text NOT NULL
            )$t$, s);

        -- lojas parceiras: cada uma tem uma categoria, que define o percentual
        EXECUTE format($t$
            CREATE TABLE %I.stores (
                id        text PRIMARY KEY,
                name      text NOT NULL,
                category  text NOT NULL
            )$t$, s);

        -- o saldo de cashback e quanto já foi creditado no mês (teto mensal)
        EXECUTE format($t$
            CREATE TABLE %I.cashback_accounts (
                customer_id          text PRIMARY KEY REFERENCES %I.customers (id),
                balance              numeric(12, 2) NOT NULL DEFAULT 0,
                credited_this_month  numeric(12, 2) NOT NULL DEFAULT 0,
                month_ref            char(7) NOT NULL
            )$t$, s, s);

        -- compras confirmadas pelas lojas parceiras
        EXECUTE format($t$
            CREATE TABLE %I.purchases (
                id           uuid PRIMARY KEY,
                customer_id  text NOT NULL REFERENCES %I.customers (id),
                store_id     text NOT NULL REFERENCES %I.stores (id),
                amount       numeric(12, 2) NOT NULL,
                category     text NOT NULL,
                percentage   numeric(6, 4) NOT NULL,
                cashback     numeric(12, 2) NOT NULL,
                created_at   timestamptz NOT NULL
            )$t$, s, s, s);

        -- resgates via PIX
        EXECUTE format($t$
            CREATE TABLE %I.redemptions (
                id           uuid PRIMARY KEY,
                customer_id  text NOT NULL REFERENCES %I.customers (id),
                amount       numeric(12, 2) NOT NULL,
                pix_key      text NOT NULL,
                created_at   timestamptz NOT NULL
            )$t$, s, s);
    END LOOP;
END $$;

-- =====================================================================================
--  Aula 2 em diante: só o projeto clean evolui.
--  Chaves de idempotência: a loja manda a mesma chave no retry e recebe a resposta guardada,
--  em vez de registrar (e pagar) a compra de novo. "response" fica vazio enquanto a compra é processada.
-- =====================================================================================
CREATE TABLE clean.idempotency_keys (
    scope        text NOT NULL,
    key          text NOT NULL,
    fingerprint  text NOT NULL,
    response     jsonb,
    created_at   timestamptz NOT NULL,
    PRIMARY KEY (scope, key)
);

-- =====================================================================================
--  Aula 3 em diante: as mudanças de esquema viram migrações (db/migracoes/NNN-*.sql), aplicadas pelo
--  ./migrar.sh como uma etapa própria, antes de subir a versão nova da API. Esta tabela guarda quais já rodaram.
-- =====================================================================================
CREATE TABLE clean.schema_migrations (
    version     text PRIMARY KEY,
    applied_at  timestamptz NOT NULL DEFAULT now()
);
