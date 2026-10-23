-- Aula 3 · CONTRAIR (expand/contract): a coluna antiga sai.
-- Só roda quando nenhuma versão no ar usa mais a coluna cashback: a v4, que parou de escrever nela, em 100% do tráfego.
-- É uma porta de mão única: depois dela, nenhuma versão anterior à v3 funciona, e o dado da coluna não volta.
ALTER TABLE purchases DROP COLUMN cashback;
