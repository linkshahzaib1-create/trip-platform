using TripPlatform.Domain.Common;

namespace TripPlatform.Domain.Entities;

public class TravelerCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<TravelerSubcategory> Subcategories { get; set; } = new List<TravelerSubcategory>();
}

public class TravelerSubcategory : AuditableEntity
{
    public int CategoryId { get; set; }
    public TravelerCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DefaultRecommendationBehavior { get; set; }
}

public class AgeTier : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
}

public class Priority : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CurrencyRate : AuditableEntity
{
    public string FromCurrency { get; set; } = "USD";
    public string ToCurrency { get; set; } = "USD";
    public decimal Rate { get; set; } = 1m;
    public DateTime AsOfUtc { get; set; } = DateTime.UtcNow;
}
