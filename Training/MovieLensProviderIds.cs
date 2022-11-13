using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;

namespace Emby.MovieLens.Training
{
    public class MovieLensProviderIds
    {
        public float movieId { get; set; }
        public string imdbId { get; set; }
        public string tmdbId { get; set; }

    }

}
