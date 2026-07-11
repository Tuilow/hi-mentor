# Módulo Channel (stub)

Contexto de plataforma NOVO (não existe no código legado `src/Tuilow.*`) — canais de criadores,
membros de canal (`ChannelOwner`, `ChannelMember`) e o vínculo com a role `ChannelMember` de
`IdentidadeAcesso`. Estrutura de 4 camadas já criada (Domain/Application/Infrastructure/Api)
seguindo o mesmo padrão de Catalog/Learning, mas ainda sem entidades — aguardando definição do
modelo de domínio (o que é um "canal", quem pode criar, relação com Catalog.Course, etc.).

Próximos passos quando este módulo for implementado:
1. Definir agregado `Channel` (Domain) com `OwnerId` (referência a IdentidadeAcesso.User.Id).
2. Definir `ChannelMember` como entidade/agregado próprio (associação usuário↔canal).
3. Repositórios em Infrastructure com `DbContext` genérico (mesmo padrão dos outros módulos).
4. Registrar `AddChannelModule()` no `Program.cs` do Host quando pronto.
