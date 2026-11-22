using System.ComponentModel.DataAnnotations;

namespace CvBank.Api.Models;

/// <summary>One master data row plus how many stored records still point at it.</summary>
public sealed record MasterDataItem(int Id, string Name, bool IsActive, int UsedByRecords);

public sealed record MasterDataManageView(
    List<MasterDataItem> Concerns,
    List<MasterDataItem> Designations,
    List<MasterDataItem> JobPositions);

public class MasterDataRenameRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;
}

public class MasterDataActiveRequest
{
    public bool IsActive { get; set; }
}
