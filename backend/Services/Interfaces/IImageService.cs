using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IImageService
    {
        Task<Image> SaveImageAsync(Stream imageStream, string fileName, Guid? uploadedById = null);
        Task<bool> DeleteImageAsync(Guid imageId);
    }
}