-- O JEITO ERRADO (demonstração do bloco 6): renomear a coluna direto, com as duas versões no ar.
-- As duas ainda leem e escrevem "cashback": a partir daqui, toda compra e todo extrato dão erro, nas duas.
-- Para desfazer: demo-2-desfazer.sql (renomear é só metadado; por isso dá para voltar na demonstração).
ALTER TABLE clean.purchases RENAME COLUMN cashback TO cashback_total;
