using TripPlatform.Domain.Common;

namespace TripPlatform.Domain.Entities;

/// <summary>
/// Trip package inclusion / exclusion line (Tripsite-style breakdown).
/// PackageTier examples: SelfGuided, Guided, PremiumGuided, Standard, AllVersions
/// </summary>
public class DestinationInclusion : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }

    /// <summary>True = What's Included; False = What's Not Included</summary>
    public bool IsIncluded { get; set; }

    /// <summary>SelfGuided | Guided | PremiumGuided | Standard | Family | AllVersions</summary>
    public string PackageTier { get; set; } = "Standard";

    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Highlight critical exclusions (e.g. insurance) in UI</summary>
    public bool IsCritical { get; set; }
}
