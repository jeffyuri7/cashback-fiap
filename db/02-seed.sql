-- Dados de exemplo, iguais nos quatro schemas. Lojas e pessoas são fictícias.
DO $$
DECLARE
    s text;
BEGIN
    FOREACH s IN ARRAY ARRAY['bigball', 'camadas', 'hexagonal', 'clean'] LOOP
        EXECUTE format($t$
            INSERT INTO %I.customers (id, name, pix_key) VALUES
                ('c-001', 'Ana Souza',   'ana.souza@email.com'),
                ('c-002', 'Bruno Lima',  '+55 11 98888-0002'),
                ('c-003', 'Carla Dias',  'carla.dias@email.com')
        $t$, s);

        EXECUTE format($t$
            INSERT INTO %I.stores (id, name, category) VALUES
                ('s-mercado',  'Mercado Bom Preço',    'supermarket'),
                ('s-posto',    'Posto Via Rápida',     'fuel'),
                ('s-farmacia', 'Farmácia Bem-Estar',   'pharmacy'),
                ('s-livraria', 'Livraria Página Um',   'books')
        $t$, s);

        EXECUTE format($t$
            INSERT INTO %I.cashback_accounts (customer_id, balance, credited_this_month, month_ref)
            SELECT id, 0, 0, to_char(now(), 'YYYY-MM') FROM %I.customers
        $t$, s, s);
    END LOOP;
END $$;
