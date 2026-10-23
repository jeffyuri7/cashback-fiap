# Migrações do banco (aula 3 em diante)

Cada mudança de esquema é um arquivo `NNN-descricao.sql`, versionado junto com o código e aplicado pelo `./migrar.sh`, **numa etapa própria, antes de subir a versão da API que precisa dele**. O pipeline também aplica todas num banco de teste, para provar que elas rodam.

```bash
./migrar.sh            # aplica as pendentes, em ordem
./migrar.sh 003        # aplica só até a 003 (para separar "migrar os dados" de "contrair")
./migrar.sh status     # o que já foi aplicado e o que falta
```

Regras do curso:
- cada arquivo roda numa transação; o `search_path` já aponta para o schema `clean`;
- migração aplicada não se edita: a correção é uma migração nova;
- **expandir antes, contrair depois:** uma migração que remove ou renomeia algo só entra quando nenhuma versão em produção usa mais o antigo (aula 3, expand/contract).

O `./reset.sh` recria o banco e aplica as migrações que existirem neste ponto do código.
