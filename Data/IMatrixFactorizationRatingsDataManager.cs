using System.Collections.Generic;
using System.Threading.Tasks;
using Emby.MovieLens.Training;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace Emby.MovieLens.Data
{
    public interface IMatrixFactorizationRatingsDataManager
    {
        Task UpdateMovieRatingsData(QueryResult<BaseItem> libraryQuery);
    }
}