using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IImageService
    {
        Task<Image> SaveImageAsync(Stream imageStream, string fileName, Guid sectionId, Guid? uploadedById = null);
    }
}