using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Emby.MovieLens.Training;

namespace Emby.MovieLens.Data
{
    public class MatrixFactorizationProviderDataManager : IMatrixFactorizationProviderDataManager
    {
        private IJsonSerializer JsonSerializer { get; }
        private IApplicationPaths ApplicationPaths { get;}
        
        public MatrixFactorizationProviderDataManager(IJsonSerializer jsonSerializer, IApplicationPaths appPaths)
        {
            JsonSerializer = jsonSerializer;
            ApplicationPaths = appPaths;
        }

        private List<MovieLensProviderIds> GetSerializedProviderIdsFromFile()
        {
            return JsonSerializer.DeserializeFromFile<List<MovieLensProviderIds>>(Path.Combine(ApplicationPaths.DataPath, "learning", "links.json"));
        }

        private List<MovieLensProviderIds> GetSerializedProviderIdsEmbeddedResource() //default list of movie links from MovieLens
        {
            var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(s => s.Contains("links.json"));
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                var linkData = JsonSerializer.DeserializeFromStream<List<MovieLensProviderIds>>(stream);
                SaveMovieLensProviderIdsJsonConfiguration(linkData);
                return linkData;
            }
        }

        public List<MovieLensProviderIds> GetProviderIdsList()
        {
            return !File.Exists(Path.Combine(ApplicationPaths.DataPath, "learning", "links.json")) ? GetSerializedProviderIdsEmbeddedResource() : GetSerializedProviderIdsFromFile();
        }

        public async Task UpdateMovieLensProviderIds(QueryResult<BaseItem> libraryQuery, List<MovieLensProviderIds> movieLensProviderIds)
        {
            foreach (var item in libraryQuery.Items)
            {
                //Add the data to the link list if it doesn't exist yet. MovieLens hasn't updated since 2019... yikes
                if (item.ProviderIds.ContainsKey("Tmdb") && item.ProviderIds.ContainsKey("Imdb"))
                {
                    if (!movieLensProviderIds.Exists(l =>
                            l.tmdbId == item.ProviderIds["Tmdb"] &&
                            l.imdbId == item.ProviderIds["Imdb"].Replace("tt", string.Empty)))
                    {
                        movieLensProviderIds.Add(new MovieLensProviderIds()
                        {
                            imdbId = item.ProviderIds["Imdb"].Replace("tt", string.Empty),
                            tmdbId = item.ProviderIds["Tmdb"],
                            movieId = CreateNewMovieLensId(movieLensProviderIds)
                        });

                        continue;
                    }
                }

                if (!item.ProviderIds.ContainsKey("Tmdb") && item.ProviderIds.ContainsKey("Imdb"))
                {
                    if (!movieLensProviderIds.Exists(l => l.imdbId == item.ProviderIds["Imdb"].Replace("tt", string.Empty)))
                    {
                        movieLensProviderIds.Add(new MovieLensProviderIds()
                        {
                            imdbId = item.ProviderIds["Imdb"].Replace("tt", string.Empty),
                            movieId = CreateNewMovieLensId(movieLensProviderIds)
                        });

                        continue;
                    }
                }

                if (item.ProviderIds.ContainsKey("Tmdb") && !item.ProviderIds.ContainsKey("Imdb"))
                {
                    movieLensProviderIds.Add(new MovieLensProviderIds()
                    {
                        tmdbId = item.ProviderIds["Tmdb"],
                        movieId = CreateNewMovieLensId(movieLensProviderIds)
                    });
                }
            }
            
            SaveMovieLensProviderIdsJsonConfiguration(movieLensProviderIds);

        }

        public string GetMovieLensId(BaseItem item)
        {
            var providerIds = !File.Exists(Path.Combine(ApplicationPaths.DataPath, "learning", "links.json")) ? GetSerializedProviderIdsEmbeddedResource() : GetSerializedProviderIdsFromFile();
            var movieId = string.Empty;

            //Does our library item have a tmdbid
            if (item.ProviderIds.ContainsKey("Tmdb"))
            {
                //Does our link data have the same tmdbid
                if (providerIds.Exists(l => l.tmdbId == item.ProviderIds["Tmdb"]))
                {
                    movieId = providerIds.FirstOrDefault(l => l.tmdbId == item.ProviderIds["Tmdb"])?.movieId.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(movieId))
            {
                // Does Our Library item have a Imdb Id
                if (item.ProviderIds.ContainsKey("Imdb")) //no tmdbid in our library item metadata
                {
                    //Does out library item have imdbid
                    //does our link data have the same imdbid
                    if (providerIds.Exists(l => l.tmdbId == item.ProviderIds["Imdb"].Replace("tt", string.Empty)))
                    {
                        movieId = providerIds.FirstOrDefault(l => l.imdbId == item.ProviderIds["Imdb"].Replace("tt", string.Empty))?.movieId.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            if (!string.IsNullOrEmpty(movieId)) return movieId;

            //The Link Data doesn't currently contain this movie. Create a new movieId, and save the TMDB, and IMDB data to the linkData.
            movieId = CreateNewMovieLensId(providerIds).ToString(CultureInfo.InvariantCulture);

            var provider = new MovieLensProviderIds();

            if (item.ProviderIds.ContainsKey("Imdb"))
            {
                provider.imdbId = item.ProviderIds["Imdb"].Replace("tt", string.Empty);
            }

            if (item.ProviderIds.ContainsKey("Tmdb"))
            {
                provider.tmdbId = item.ProviderIds["Tmdb"];
            }

            if (!string.IsNullOrEmpty(provider.tmdbId) || !string.IsNullOrEmpty(provider.imdbId))
            {
                provider.movieId = Convert.ToSingle(movieId);
                providerIds.Add(provider);
                SaveMovieLensProviderIdsJsonConfiguration(providerIds);
            }
            
            return movieId;
        }

        public void SaveMovieLensProviderIdsJsonConfiguration(List<MovieLensProviderIds> providerIds)
        {
            JsonSerializer.SerializeToFile(providerIds, Path.Combine(ApplicationPaths.DataPath, "learning", "links.json"));
        }

        private static float CreateNewMovieLensId(IEnumerable<MovieLensProviderIds> list) => list.Select(i => i.movieId).Max() + 1;

    }
}
