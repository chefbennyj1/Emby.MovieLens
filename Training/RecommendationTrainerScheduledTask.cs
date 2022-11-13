using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.MovieLens.Data;
using Emby.MovieLens.ML_Net;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace Emby.MovieLens.Training
{
    public class RecommendationTrainerScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private ILogger Log { get; }
        private IApplicationPaths ApplicationPaths { get; }
        private IUserManager UserManager { get; }
        private ILibraryManager LibraryManager { get; }
        private IJsonSerializer JsonSerializer { get; }

        public RecommendationTrainerScheduledTask(IJsonSerializer jsonSerializer, IApplicationPaths appPaths, ILogManager logManager, IUserManager userManager, ILibraryManager libraryManager)
        {
            Log = logManager.GetLogger(Plugin.Instance.Name);
            ApplicationPaths = appPaths;
            UserManager = userManager;
            LibraryManager = libraryManager;
            JsonSerializer = jsonSerializer;

        }
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            try
            {
                var machineLearningDataFolder = Path.Combine(ApplicationPaths.DataPath, "learning");
                if (!Directory.Exists(machineLearningDataFolder)) Directory.CreateDirectory(machineLearningDataFolder);

                progress.Report(10.0);

                var libraryQuery = LibraryManager.GetItemsResult(new InternalItemsQuery()
                {
                    IncludeItemTypes = new[] { "Movie" },
                    Recursive = true

                });

                var matrixFactorizationProviderDataManger = new MatrixFactorizationProviderDataManager(JsonSerializer, ApplicationPaths);
                var providerIds = matrixFactorizationProviderDataManger.GetProviderIdsList();
                await matrixFactorizationProviderDataManger.UpdateMovieLensProviderIds(libraryQuery, providerIds);

                progress.Report(50.0);

                var matrixFactorizationRatingDataManager = new MatrixFactorizationRatingsDataManager(ApplicationPaths, Log, UserManager, matrixFactorizationProviderDataManger);
                await matrixFactorizationRatingDataManager.UpdateMovieRatingsData(libraryQuery);

                var ratingsCsv = Path.Combine(ApplicationPaths.DataPath, "learning", "ratings.csv");
                if (!File.Exists(ratingsCsv))
                {
                    Log.Warn("Ratings.csv doesn't exists");
                }
                    //await AssemblyManager.Instance.SaveEmbeddedResourceToFileAsync(
                    //    AssemblyManager.Instance.GetEmbeddedResourceStream("ratings.csv"), ratingsCsv);

                progress.Report(60.0);

                var testCsv = Path.Combine(ApplicationPaths.DataPath, "learning", "test.csv");
                if (!File.Exists(testCsv))
                    await AssemblyManager.Instance.SaveEmbeddedResourceToFileAsync(
                        AssemblyManager.Instance.GetEmbeddedResourceStream("test.csv"), testCsv);

                progress.Report(65.0);

                var mlContext = new MLContext();
                (IDataView trainingDataView, IDataView testDataView) = LoadData(mlContext, ratingsCsv, testCsv);

                ITransformer model = BuildAndTrainModel(mlContext, trainingDataView);

                EvaluateModel(mlContext, testDataView, model);

                progress.Report(70.0);

                SaveModel(mlContext, trainingDataView.Schema, model);

                var creation = File.GetCreationTime(Path.Combine(ApplicationPaths.DataPath, "learning", "model.zip"));
                var config = Plugin.Instance.Configuration;
                config.LastTrainedDate = creation;
                Plugin.Instance.UpdateConfiguration(config);

                Log.Info("Recommendation model successfully saved.");

                progress.Report(100.0);

            }catch {}
        }

        private static (IDataView training, IDataView test) LoadData(MLContext mlContext, string trainingDataRatingPath, string testDataRatingPath)
        {
            var ratingsDataPathOutput = trainingDataRatingPath;
            var testDataPathOutput = testDataRatingPath;
            IDataView trainingDataView = mlContext.Data.LoadFromTextFile<MovieRating>(ratingsDataPathOutput, hasHeader: true, separatorChar: ',');
            IDataView testDataView = mlContext.Data.LoadFromTextFile<MovieRating>(testDataPathOutput, hasHeader: true, separatorChar: ',');

            return (trainingDataView, testDataView);
        }

        static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
        {

            IEstimator<ITransformer> estimator = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "userIdEncoded", inputColumnName: "userId")
                .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "movieIdEncoded", inputColumnName: "movieId"));

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "userIdEncoded",
                MatrixRowIndexColumnName    = "movieIdEncoded",
                LabelColumnName             = "Label",
                NumberOfIterations          = Plugin.Instance.Configuration.TrainingIterations,
                ApproximationRank           = 100
            };

            var trainerEstimator = estimator.Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));

            ITransformer model = trainerEstimator.Fit(trainingDataView);

            return model;
        }

        private void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
        {
            Log.Info("Evaluating movie recommendation model...");
            var prediction = model.Transform(testDataView);
            var metrics = mlContext.Regression.Evaluate(prediction, labelColumnName: "Label", scoreColumnName: "Score");
            Log.Info("Root Mean Squared Error : " + metrics.RootMeanSquaredError);
            Log.Info("RSquared: " + metrics.RSquared);
        }

        private void SaveModel(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
        {
            var modelPath = Path.Combine(ApplicationPaths.DataPath, "learning", "model.zip");

            Log.Info("Saving movie recommendation model to a file");
            mlContext.Model.Save(model, trainingDataViewSchema, modelPath);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type           = TaskTriggerInfo.TriggerWeekly,
                    DayOfWeek      = DayOfWeek.Sunday,
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
                }
            };
        }

        public string Name        => "Train neural network movie recommendation model";
        public bool IsHidden      => true;
        public bool IsEnabled     => true;
        public bool IsLogged      => false;
        public string Key         => "TrainMovieRecommendationModel";
        public string Description => "Train Movie Recommendations.";
        public string Category    => "Recommendations";
    }
}
