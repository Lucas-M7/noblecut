using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberShop.Application.Services;

public class CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
{
    private readonly Cloudinary _cloudinary = new(new Account(
        config["Cloudinary:CloudName"],
        config["Cloudinary:ApiKey"],
        config["Cloudinary:ApiSecret"]
    ));

    // Faz upload de uma imagem e retorna a URL segura
    public async Task<string> UploadProfilePhotoAsync(Stream imageStream, string fileName)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, imageStream),

            // Pasta organizada por tipo de conteúdo
            Folder = "barbershop/profiles",

            // Transforma a imagem para 400x400 centrado
            // Reduz tamanho de armazenamento e padroniza o formato
            Transformation = new Transformation()
                .Width(400).Height(400)
                .Crop("fill")
                .Gravity("face")
                .Quality("auto")
                .FetchFormat("auto"),

            // Sobrescreve se já existir (evita acúmulo de imagens orphans)
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error is not null)
        {
            logger.LogError("Cloudinary upload falhou: {Error}", result.Error.Message);
            throw new InvalidOperationException("Falha ao fazer upload da imagem.");
        }

        logger.LogInformation("Upload realizado com sucesso. PublicId: {PublicId}", result.PublicId);
        return result.SecureUrl.ToString();
    }

    // Remove a imagem antiga quando o barbeiro troca a foto
    public async Task DeleteAsync(string photoUrl)
    {
        // Extrai o PublicId da URL do Cloudinary
        // ex: https://res.cloudinary.com/cloud/image/upload/v123/barbershop/profiles/abc.jpg
        //     → barbershop/profiles/abc
        try
        {
            var uri = new Uri(photoUrl);
            var segments = uri.AbsolutePath.Split('/');
            var uploadIndex = Array.IndexOf(segments, "upload");

            if (uploadIndex < 0) return;

            // Pula o segmento de versão (v123) e junta o restante sem a extensão
            var publicIdSegments = segments[(uploadIndex + 2)..];
            var publicId = string.Join("/", publicIdSegments);
            publicId = Path.GetFileNameWithoutExtension(publicId);
            publicId = "barbershop/profiles/" + publicId.Split('/').Last();

            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);

            logger.LogInformation("Imagem removida do Cloudinary. PublicId: {PublicId}", publicId);
        }
        catch (Exception ex)
        {
            // Não bloqueia o fluxo se falhar — a imagem antiga simplesmente fica no Cloudinary
            logger.LogWarning("Falha ao remover imagem antiga do Cloudinary: {Error}", ex.Message);
        }
    }
}