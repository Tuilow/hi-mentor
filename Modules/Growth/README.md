# Módulo Growth (stub)

Contexto de plataforma NOVO (não existe no código legado) — métricas de crescimento, indicações,
gamificação/engajamento de criadores e alunos. Estrutura mínima criada (só Domain, referenciando
SharedKernel) até haver requisitos de negócio concretos para desenhar o agregado principal.

Próximos passos quando este módulo for implementado:
1. Definir o(s) agregado(s) (ex.: `ReferralProgram`, `EngagementMetric`).
2. Completar as 4 camadas seguindo o padrão de Catalog/Learning.
3. Registrar `AddGrowthModule()` no `Program.cs` do Host quando pronto.
