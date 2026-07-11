namespace DAL.Entities;

public enum DocumentStatus
{
    Uploading = 0,
    Processing = 1,
    Indexed = 2,
    Failed = 3,
    Archived = 4
}
