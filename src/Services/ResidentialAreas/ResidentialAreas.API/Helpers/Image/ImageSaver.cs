using System.Text.RegularExpressions;

namespace ResidentialAreas.API.Helpers.ImageSaver
{
    public class ImageSaver : IImageSaver
    {

        public Task DeleteImages(List<string?>? existingImageUrls)
        {
            List<string?>? pathToDelete = existingImageUrls?.Select(url => url?.Split("images/").LastOrDefault()).ToList();


            foreach (var imagePath in pathToDelete ?? [])
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    string fullPath = Path.Combine("images", imagePath);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
            }
            return Task.CompletedTask;
        }



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

            if (imageType != "jpeg" && imageType != "png" && imageType != "jpg" && imageType != "webp")
            {
                throw new ArgumentException("Unsupported image type. Only JPEG, PNG, and WebP are allowed.");
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

        public async Task<List<string?>> SaveImageAsync(List<string?>? base64ImageStrings, string saveDirectory)
        {
            List<string?>? savedImagePaths = new List<string?>();
            if (base64ImageStrings != null)
            {
                foreach (var base64ImageString in base64ImageStrings)
                {
                    if (!string.IsNullOrEmpty(base64ImageString))
                    {
                        try
                        {
                            string savedPath = await SaveImageAsync(base64ImageString, saveDirectory);
                            savedImagePaths.Add(savedPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving image: {ex.Message}");
                            savedImagePaths.Add("images/default.jpg");
                        }
                    }
                }
            }
            return savedImagePaths;
        }

    }
}
