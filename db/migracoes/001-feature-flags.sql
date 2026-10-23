-- Aula 3 · as feature flags do app, com horário.
-- state: off (ninguém) · test (só os clientes de test_customers, a qualquer hora) · on (todos, entre starts_at e ends_at).
CREATE TABLE feature_flags (
    name            text PRIMARY KEY,
    description     text NOT NULL,
    state           text NOT NULL DEFAULT 'off' CHECK (state IN ('off', 'test', 'on')),
    test_customers  text[] NOT NULL DEFAULT '{}',
    starts_at       timestamptz,
    ends_at         timestamptz,
    changed_by      text,
    changed_at      timestamptz
);

-- A Black Friday de 2026: sexta 27/11 00h00 até domingo 29/11 23h59, no horário de Brasília.
-- O grupo de teste é a Ana Souza (c-001), funcionária do app, que testa a campanha em produção antes de todos.
INSERT INTO feature_flags (name, description, test_customers, starts_at, ends_at) VALUES
    ('black-friday', 'Black Friday: +5% em todas as lojas, menos combustível', '{c-001}',
     '2026-11-27 00:00:00-03', '2026-11-30 00:00:00-03');
