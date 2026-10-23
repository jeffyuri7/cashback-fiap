-- Aula 3 · EXPANDIR (expand/contract): o cashback passa a ser guardado em duas partes, o base e o bônus da campanha.
-- As colunas novas entram ao lado da antiga (cashback, o total), anuláveis e sem default:
--   · a versão em produção (que não conhece as colunas novas) continua funcionando: ela nem as vê;
--   · a versão nova escreve nas três e ainda lê da antiga;
--   · voltar para a versão anterior (rollback) é seguro: nada que ela lê mudou.
-- No PostgreSQL 11+, ADD COLUMN sem default não reescreve a tabela (é só metadado).
-- A coluna antiga só sai na contração (004), quando nenhuma versão no ar a usar mais.
ALTER TABLE purchases ADD COLUMN cashback_base numeric(12, 2);
ALTER TABLE purchases ADD COLUMN campaign_bonus numeric(12, 2);
