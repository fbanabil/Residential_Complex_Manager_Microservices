using System.Text.RegularExpressions;

namespace ResidentialAreas.API.Helpers.ImageSaver
{
    public class ImageSaver : IImageSaver
    {
        public async Task<string> SaveImageAsync(string base64ImageString, string saveDirectory)
        {
            if (string.IsNullOrEmpty(base64ImageString))
            {
                throw new ArgumentException("Base64 image string cannot be null or empty.");
            }

            var match = Regex.Match(base64ImageString, @"data:image/(?<type>.+?);base64,(?<data>.+)");
            if (!match.Success)
            {
                throw new ArgumentException("Invalid base64 image string format.");
            }

            string imageType = match.Groups["type"].Value;
            string base64Data = match.Groups["data"].Value;

            if (imageType != "jpeg" && imageType != "png" && imageType != "jpg")
            {
                throw new ArgumentException("Unsupported image type. Only JPEG and PNG are allowed.");
            }

            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 image data.");
            }

            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            string fileName = $"{Guid.NewGuid()}.{imageType}";
            string filePath = Path.Combine(saveDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);
            filePath = filePath.Replace("wwwroot/", "");
            return filePath;
        }

    }
}
