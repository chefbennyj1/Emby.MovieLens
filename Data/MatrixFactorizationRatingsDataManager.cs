using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.MovieLens.ML_Net;
using Emby.MovieLens.Training;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;

namespace Emby.MovieLens.Data
{
    public class MatrixFactorizationRatingsDataManager : IMatrixFactorizationRatingsDataManager
    {
        private IApplicationPaths ApplicationPaths                            { get; }
        private IUserManager UserManager                                      { get; }
        private ILogger Log                                                   { get; }
        private IMatrixFactorizationProviderDataManager MatrixProviderManager { get; }

        public MatrixFactorizationRatingsDataManager(IApplicationPaths appPaths,
            ILogger log, IUserManager userManager, IMatrixFactorizationProviderDataManager matrixProviderManager)
        {
            ApplicationPaths      = appPaths;
            Log                   = log;
            UserManager           = userManager;
            MatrixProviderManager = matrixProviderManager;
        }
        public async Task UpdateMovieRatingsData(QueryResult<BaseItem> libraryQuery)
        {
            var ratings = new List<Rating>();

            var ratingsCsv = Path.Combine(ApplicationPaths.DataPath, "learning", "ratings.csv");
            if (!File.Exists(ratingsCsv)) await AssemblyManager.Instance.SaveEmbeddedResourceToFileAsync(AssemblyManager.Instance.GetEmbeddedResourceStream("ratings.csv"), ratingsCsv);

            //Our Emby User data
            var users = UserManager.GetUsers(new UserQuery()).Items;
            
            foreach (var user in users)
            {
                //We are going to make room for all the latest rating data from this user.
                //If the ratings csv contains data from a user with the same ID as this, or old data
                //from this user, remove it.
                RemoveCurrentUserRatingsFromCsv(ratingsCsv, user.InternalId);

                //Does the user actually use the rating system in Emby? DO they heart movies in the library.
                var userRatesMovies = UserRatesMovies(libraryQuery, user);

                foreach (var item in libraryQuery.Items)
                {
                    //Don't add rating data for items the user hasn't watched.
                    if (!item.IsPlayed(user)) continue;

                    var rating = new Rating
                    {
                        userId = user.InternalId.ToString(),
                        movieId = MatrixProviderManager.GetMovieLensId(item)
                    };
                    

                    //If a user doesn't rate movies, then we'll to them a favor and look at the community rating in order to help recommend movies to them.
                    switch (userRatesMovies)
                    {
                        case true  : rating.rating = item.IsFavoriteOrLiked(user) ? 5.0f : 2.5f; break;
                        case false : rating.rating = item.CommunityRating.HasValue ? Convert.ToSingle(5 * ((double)item.CommunityRating / 10.0)) : 2.5f; break;
                    }

                    ratings.Add(rating);
                }

                Log.Info($"Recommendation ratings updated for {user.Name}");
                
            }
            
            using (var sw = File.AppendText(ratingsCsv))
            {
                foreach (var rating in ratings)
                {
                    await sw.WriteLineAsync($"{rating.userId},{rating.movieId},{rating.rating}");
                }
            }

            Log.Info("Appending user ratings");

        }

        private bool UserRatesMovies(QueryResult<BaseItem> libraryQuery, User user)
        {
            return libraryQuery.Items.Count(item => item.IsFavoriteOrLiked(user)) > 5;
        }

        //private static float CreateNewMovieId(IEnumerable<ProviderIds> list) => list.Select(i => i.movieId).Max() + 1;
        private static void RemoveCurrentUserRatingsFromCsv(string ratingsCsvPath, long userId)
        {
            File.WriteAllLines(ratingsCsvPath, File.ReadAllLines(ratingsCsvPath).Where(l => l.Split(',')[0] != userId.ToString()));
        }
    }
}
