namespace TripPlatform.Application.Common;

/// <summary>
/// Curated public image URLs (Wikimedia Commons) for destinations — authentic photos without paid CDNs.
/// </summary>
public static class DestinationMediaCatalog
{
    private static readonly Dictionary<string, string[]> Photos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lahore"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c4/Badshahi_Mosque_July_1_2005_pic32_by_Ali_Imran_%28cropped%29.jpg/640px-Badshahi_Mosque_July_1_2005_pic32_by_Ali_Imran_%28cropped%29.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ea/Lahore_Fort_view.jpg/640px-Lahore_Fort_view.jpg"
        ],
        ["islamabad"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4f/Faisal_Mosque%2C_Islamabad%2C_Pakistan.jpg/640px-Faisal_Mosque%2C_Islamabad%2C_Pakistan.jpg"
        ],
        ["murree"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8e/Murree_Hills.jpg/640px-Murree_Hills.jpg"
        ],
        ["hunza"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5e/Rakaposhi_view.jpg/640px-Rakaposhi_view.jpg"
        ],
        ["dubai"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/0/09/Dubai_Marina_Skyline.jpg/640px-Dubai_Marina_Skyline.jpg",
            "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c7/Burj_Khalifa.jpg/640px-Burj_Khalifa.jpg"
        ],
        ["istanbul"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/7/7c/Hagia_Sophia_Mars_2013.jpg/640px-Hagia_Sophia_Mars_2013.jpg"
        ],
        ["bangkok"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d8/Wat_Arun_Ratchawararam_Ratchawaramahawihan.jpg/640px-Wat_Arun_Ratchawararam_Ratchawaramahawihan.jpg"
        ],
        ["maldives"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4b/Maldives_islands.jpg/640px-Maldives_islands.jpg"
        ],
        ["paris"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a8/Tour_Eiffel_Wikimedia_Commons.jpg/640px-Tour_Eiffel_Wikimedia_Commons.jpg"
        ],
        ["helsinki"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Helsinki_July_2013-27a.jpg/640px-Helsinki_July_2013-27a.jpg"
        ]
    };

    public static IReadOnlyList<string> GetPhotos(string slugOrName)
    {
        var key = slugOrName.Trim().ToLowerInvariant().Replace(' ', '-');
        if (Photos.TryGetValue(key, out var list))
            return list;
        return Array.Empty<string>();
    }

    public static string? HeroFor(string slugOrName) => GetPhotos(slugOrName).FirstOrDefault();
}
