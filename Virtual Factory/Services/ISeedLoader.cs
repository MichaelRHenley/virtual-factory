namespace Virtual_Factory.Services
{
    /// <summary>Loads seed data from JSON files into the in-memory repositories.</summary>
    public interface ISeedLoader
    {
        /// <summary>
        /// Reads all seed files from the <c>SeedData</c> folder and populates the
        /// repositories. Safe to call once at application startup.
        /// </summary>
        Task LoadAsync(CancellationToken cancellationToken = default);
    }
}
