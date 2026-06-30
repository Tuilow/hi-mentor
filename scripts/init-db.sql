-- Criação de schemas
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS learning;
CREATE SCHEMA IF NOT EXISTS streaming;
CREATE SCHEMA IF NOT EXISTS subscription;
CREATE SCHEMA IF NOT EXISTS dog_profile;
CREATE SCHEMA IF NOT EXISTS assessment;
CREATE SCHEMA IF NOT EXISTS gamification;
CREATE SCHEMA IF NOT EXISTS community;
CREATE SCHEMA IF NOT EXISTS ai_trainer;
CREATE SCHEMA IF NOT EXISTS notifications;

-- Extensões
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

COMMENT ON SCHEMA identity IS 'Autenticação e gestão de usuários';
COMMENT ON SCHEMA catalog IS 'Cursos, módulos, aulas e trilhas';
COMMENT ON SCHEMA learning IS 'Matrículas, progresso e certificados';
COMMENT ON SCHEMA streaming IS 'Vídeos Cloudflare Stream';
COMMENT ON SCHEMA subscription IS 'Planos e assinaturas Asaas';
COMMENT ON SCHEMA dog_profile IS 'Perfis dos cães';
COMMENT ON SCHEMA assessment IS 'Avaliações por vídeo';
COMMENT ON SCHEMA gamification IS 'Pontos, medalhas e conquistas';
COMMENT ON SCHEMA community IS 'Fórum e grupos';
COMMENT ON SCHEMA ai_trainer IS 'IA Treinadora';
COMMENT ON SCHEMA notifications IS 'Notificações';
