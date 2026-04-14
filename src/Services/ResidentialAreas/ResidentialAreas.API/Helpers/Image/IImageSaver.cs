namespace ResidentialAreas.API.Helpers.ImageSaver
{
    public interface IImageSaver
    {
        public Task<string> SaveImageAsync(string base64ImageString, string saveDirectory);
    }
}
