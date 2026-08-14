namespace HiMentor.SharedKernel.Application.Interfaces;

/// <summary>
/// Porta para montar URLs absolutas do frontend a partir de um path relativo (ex.: "/acesso?token=...")
/// -- evita duplicar a leitura da configuracao "FrontendUrl" em cada modulo que precisa gerar um
/// link (mesmo valor ja lido internamente por EmailService). Ver FrontendUrlProvider
/// (SharedKernel.Infrastructure) para a implementacao real.
/// </summary>
public interface IFrontendUrlProvider
{
    /// <summary>Retorna a URL absoluta do frontend para o path informado (deve comecar com "/").</summary>
    string BuildUrl(string path);
}
