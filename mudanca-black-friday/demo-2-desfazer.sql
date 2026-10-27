-- Desfaz a demonstração do jeito errado: a coluna volta a se chamar cashback, e as duas versões voltam a funcionar.
ALTER TABLE clean.purchases RENAME COLUMN cashback_total TO cashback;
