using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.MovieLens.Data;
using Emby.MovieLens.Training;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.ML;

namespace Emby.MovieLens.Predictions
{
    public class RecommendationPredictionsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private ILogger Log                        { get; }
        private IApplicationPaths ApplicationPaths { get; }
        private IUserManager UserManager           { get; }
        private ILibraryManager LibraryManager     { get; }
        private IJsonSerializer JsonSerializer     { get; }

        public RecommendationPredictionsScheduledTask(IJsonSerializer jsonSerializer, IApplicationPaths appPaths, ILogManager logManager, IUserManager userManager, ILibraryManager libraryManager)
        {
            Log              = logManager.GetLogger(Plugin.Instance.Name);
            ApplicationPaths = appPaths;
            UserManager      = userManager;
            LibraryManager   = libraryManager;
            JsonSerializer   = jsonSerializer;

        }
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            try
            {
                var recommendations = new Dictionary<BaseItem, double>();

                var mlContext = new MLContext();
                //Define DataViewSchema for data preparation pipeline and trained model
                DataViewSchema modelSchema;

                // Load trained model
                ITransformer model =
                    mlContext.Model.Load(Path.Combine(ApplicationPaths.DataPath, "learning", "model.zip"),
                        out modelSchema);

                Log.Info("Making a recommendation prediction.");
                var predictionEngine = mlContext.Model.CreatePredictionEngine<MovieRating, MoviePrediction>(model);

                var matrixFactorizationProviderDataManager = new MatrixFactorizationProviderDataManager(JsonSerializer, ApplicationPaths);

                var users = UserManager.GetUsers(new UserQuery()).Items;
                    //.FirstOrDefault(u => u.Policy.IsAdministrator);

                var config = Plugin.Instance.Configuration;

                var resultRecommendations = new List<Recommendation>();

                foreach (var user in users)
                {
                    var internalItemQuery = new InternalItemsQuery(user)
                    {
                        IncludeItemTypes = new[] { "Movie" },
                        Recursive = true,
                        DtoOptions = new DtoOptions(true)
                    };


                    if (config.FavorNewReleases)
                    {
                        internalItemQuery.MinPremiereDate = DateTimeOffset.Now.AddMonths(-10);
                    }

                    if (config.FavorRecentlyAdded)
                    {
                        internalItemQuery.MinDateCreated = DateTimeOffset.Now.AddMonths(-4);
                    }

                    var libraryQuery = LibraryManager.GetItemsResult(internalItemQuery);

                    var items = libraryQuery.Items.ToList().Where(item => !item.Path.EndsWith(".strm"));


                    var options = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 4
                    };

                    Parallel.ForEach(items, options, item =>
                    {
                        //Only use items that haven't been played in the last 8 months.
                        if (item.IsPlayed(user) && item.LastPlayedDate.HasValue)
                        {
                            if (item.LastPlayedDate.Value.DateTime < DateTime.Now.AddMonths(-config.LastPlayedMonths))
                                return;
                        }

                        var movieId = matrixFactorizationProviderDataManager.GetMovieLensId(item);

                        var testInput = new MovieRating
                            { userId = Convert.ToSingle(user.InternalId), movieId = Convert.ToSingle(movieId) };

                        var movieRatingPrediction = predictionEngine.Predict(testInput);

                        var predictionScore = Math.Round(movieRatingPrediction.Score, 1);

                        if (predictionScore > config.MaxRecommendationPredictionThreshold)
                        {
                            recommendations.Add(item, predictionScore);
                        }
                    });

                    var ordered = recommendations.OrderByDescending(r => r.Value)
                        .Take(config.RecommendationMaxNumber ?? recommendations.Count);

                    foreach (var item in ordered)
                    {
                        resultRecommendations.Add(new Recommendation()
                        {
                            ItemInternalId = item.Key.InternalId,
                            Score = item.Value,
                            UserId = user.Id.ToString(),
                            UserName = user.Name,
                            MovieName = item.Key.Name,
                            MovieYear = item.Key.ProductionYear ?? 0
                        });
                    }

                    
                }

                JsonSerializer.SerializeToFile(resultRecommendations, Path.Combine(ApplicationPaths.DataPath, "learning", "recommendations.json"));

                
            }

            catch { }

        }
        

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] 
            {             
                new TaskTriggerInfo 
                { 
                    Type = TaskTriggerInfo.TriggerWeekly, 
                    DayOfWeek = DayOfWeek.Sunday, 
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks 
                }
            };
        }

        public string Name        => "Neural network movie recommendation predictions";
        public bool IsHidden      => true;
        public bool IsEnabled     => true;
        public bool IsLogged      => false;
        public string Key         => "PredictMovieRecommendationModel";
        public string Description => "Predict Movie Recommendations.";
        public string Category    => "Recommendations";

    }
}
