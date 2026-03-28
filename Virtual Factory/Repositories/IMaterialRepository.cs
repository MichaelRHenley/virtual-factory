using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <summary>Read/write store for <see cref="Material"/> records.</summary>
    public interface IMaterialRepository
    {
        /// <summary>Adds or replaces a material in the store.</summary>
        void Add(Material material);

        /// <summary>Returns all materials in the store.</summary>
        IReadOnlyList<Material> GetAll();

        /// <summary>Returns the material with the given code, or <c>null</c> if not found.</summary>
        Material? GetByCode(string code);
    }
}
