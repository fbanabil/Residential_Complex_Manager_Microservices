namespace AuthenticationService.API.Helpers.Image
{
    public static class Base64StringImageValidator
    {

        public static bool IsBase64StringLiset(List<string?>? base64List)
        {
            if (base64List == null || base64List.Count == 0)
            {
                return true;
            }

            bool result = true;

            foreach (var base64 in base64List)
            {
                if (string.IsNullOrEmpty(base64))
                {
                    continue; 
                }
                if (!IsBase64StringImage(base64))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public static bool IsBase64StringImage(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
            {
                return false;
            }

            const string prefix = "data:image/";
            const string marker = ";base64,";

            if (!base64.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int markerIndex = base64.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return false;
            }

            string imageType = base64[prefix.Length..markerIndex];
            if (imageType is not ("jpg" or "jpeg" or "png" or "webp"))
            {
                return false;
            }

            string base64Data = base64[(markerIndex + marker.Length)..];

            Span<byte> buffer = new byte[base64Data.Length];
            return Convert.TryFromBase64String(base64Data, buffer, out _);
        }
    }
}
