using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <summary>Read/write store for <see cref="Asset"/> records.</summary>
    public interface IAssetRepository
    {
        /// <summary>Adds or replaces an asset in the store.</summary>
        void Add(Asset asset);

        /// <summary>Returns all assets in the store.</summary>
        IReadOnlyList<Asset> GetAll();

        /// <summary>Returns the asset with the given id, or <c>null</c> if not found.</summary>
        Asset? GetById(string id);

            /// <summary>Returns all direct children of the asset with the given <paramref name="parentId"/>.</summary>
                IReadOnlyList<Asset> GetChildren(string parentId);

                /// <summary>Returns the asset at the given slash-delimited path, or <c>null</c> if not found.</summary>
                Asset? GetByPath(string path);
            }
        }
