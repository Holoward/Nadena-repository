namespace Domain.Common;

public abstract class AuditableBaseEntity
{
    public virtual int Id { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime Created { get; set; }
    public string? LastModifiedBY { get; set; }
    public DateTime? LastModified { get; set; }
}

public abstract class AuditableBaseEntityGuid
{
    public virtual Guid Id { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime Created { get; set; }
    public string? LastModifiedBY { get; set; }
    public DateTime? LastModified { get; set; }
}
