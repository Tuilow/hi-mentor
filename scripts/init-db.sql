-- Criação de schemas
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS learning;
CREATE SCHEMA IF NOT EXISTS streaming;
CREATE SCHEMA IF NOT EXISTS subscription;
CREATE SCHEMA IF NOT EXISTS learner_profile;
CREATE SCHEMA IF NOT EXISTS assessment;
CREATE SCHEMA IF NOT EXISTS gamification;
CREATE SCHEMA IF NOT EXISTS community;
CREATE SCHEMA IF NOT EXISTS ai_tutor;
CREATE SCHEMA IF NOT EXISTS notifications;

-- Extensões
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

COMMENT ON SCHEMA identity IS 'Autenticação e gestão de usuários';
COMMENT ON SCHEMA catalog IS 'Cursos, módulos, aulas e trilhas';
COMMENT ON SCHEMA learning IS 'Matrículas, progresso e certificados';
COMMENT ON SCHEMA streaming IS 'Vídeos Cloudflare Stream';
COMMENT ON SCHEMA subscription IS 'Planos e assinaturas Asaas';
COMMENT ON SCHEMA learner_profile IS 'Perfis de aprendizado dos alunos';
COMMENT ON SCHEMA assessment IS 'Avaliações por vídeo';
COMMENT ON SCHEMA gamification IS 'Pontos, medalhas e conquistas';
COMMENT ON SCHEMA community IS 'Fórum e grupos';
COMMENT ON SCHEMA ai_tutor IS 'IA Tutora (assistente de aprendizado)';
COMMENT ON SCHEMA notifications IS 'Notificações';
