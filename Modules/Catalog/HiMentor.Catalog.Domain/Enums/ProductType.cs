namespace HiMentor.Catalog.Domain.Enums;

/// <summary>
/// Tipo de produto digital. A entrega (módulos/aulas/vídeo) continua sendo o único mecanismo
/// de conteúdo que a plataforma sabe processar — os tipos abaixo servem para o criador
/// classificar a oferta na Jornada Guiada (cards do passo 0) e para exibição/relatórios;
/// não mudam o pipeline de conteúdo por trás. Armazenado como string (varchar(50)) em
/// Course.ProductType, então adicionar valores novos aqui não exige migração de schema.
/// </summary>
public enum ProductType { Course, Ebook, Bundle, Subscription, Mentoring, Event, Service }
