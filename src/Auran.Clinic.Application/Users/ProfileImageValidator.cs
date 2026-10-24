namespace Auran.Clinic.Application.Users;

public static class ProfileImageValidator
{
    public const int MaxBytes = 2 * 1024 * 1024;

    public static bool IsValid(ReadOnlySpan<byte> image, string contentType)
    {
        if (image.IsEmpty || image.Length > MaxBytes)
            return false;

        return contentType switch
        {
            "image/jpeg" => image.Length >= 4 &&
                image[0] == 0xff && image[1] == 0xd8 &&
                image[^2] == 0xff && image[^1] == 0xd9,
            "image/png" => image.Length >= 33 &&
                image.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
                image[12..16].SequenceEqual("IHDR"u8),
            "image/webp" => image.Length >= 16 &&
                image[..4].SequenceEqual("RIFF"u8) &&
                image[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
