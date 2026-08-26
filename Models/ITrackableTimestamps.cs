namespace BibekSchool.Models
{
    public interface ITrackableTimestamps
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}