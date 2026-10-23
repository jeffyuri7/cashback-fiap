-- Aula 3 · MIGRAR (expand/contract), na segunda-feira, depois da Black Friday.
-- Só roda quando a v1 não está mais no ar: ela não escreve nas colunas novas, e daqui em diante elas são obrigatórias.
-- Voltar para a v2 continua seguro (ela escreve nas três colunas). Voltar para a v1 deixa de ser: é o primeiro passo sem volta.

-- 1. As compras antigas ganham as colunas novas: todo o cashback delas era base (não havia bônus de campanha).
UPDATE purchases SET cashback_base = cashback, campaign_bonus = 0 WHERE cashback_base IS NULL;

-- 2. As colunas novas passam a ser obrigatórias; a antiga deixa de ser, para a v4 poder parar de escrever nela.
ALTER TABLE purchases ALTER COLUMN cashback_base SET NOT NULL;
ALTER TABLE purchases ALTER COLUMN campaign_bonus SET NOT NULL;
ALTER TABLE purchases ALTER COLUMN cashback DROP NOT NULL;
