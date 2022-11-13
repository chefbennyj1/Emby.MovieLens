using System;
using System.Collections.Generic;
using Emby.MovieLens.Predictions;
using MediaBrowser.Model.Plugins;

namespace Emby.MovieLens.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int TrainingIterations { get; set; } = 20;
        public DateTimeOffset? LastTrainedDate { get; set; }
        public int LastPlayedMonths { get; set; } = 8;
        public double MaxRecommendationPredictionThreshold { get; set; } = 3.5;
        public bool FavorRecentlyAdded { get; set; } = false;
        public bool FavorNewReleases { get; set; } = false;
        public int? RecommendationMaxNumber { get; set; }

    }
}
