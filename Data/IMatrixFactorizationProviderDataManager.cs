using System.Collections.Generic;
using System.Threading.Tasks;
using Emby.MovieLens.Training;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace Emby.MovieLens.Data
{
    public interface IMatrixFactorizationProviderDataManager
    {
        List<MovieLensProviderIds> GetProviderIdsList();
        Task UpdateMovieLensProviderIds(QueryResult<BaseItem> libraryQuery, List<MovieLensProviderIds> movieLensProviderIds);
        string GetMovieLensId(BaseItem item);
        void SaveMovieLensProviderIdsJsonConfiguration(List<MovieLensProviderIds> providerIds);
    }
}