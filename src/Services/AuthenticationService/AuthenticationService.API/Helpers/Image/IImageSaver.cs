
namespace ResidentialAreas.API.Helpers.ImageSaver
{
    public interface IImageSaver
    {
        Task DeleteImages(List<string?>? existingImageUrls);
        public Task<string> SaveImageAsync(string base64ImageString, string saveDirectory);
        public Task<List<string?>> SaveImageAsync(List<string?>? base64ImageStrings, string saveDirectory);
        public Task DeleteImage(string? existingImageUrl);

    }
}
