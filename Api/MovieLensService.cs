using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.MovieLens.Predictions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;

namespace Emby.MovieLens.Api
{
    public class MovieLensService : IService
    {
        [Route("/Recommendations", "GET", Summary = "The entire list of recommendation")]
        public class RecommendationsRequest : IReturn<List<Recommendation>>
        {
            
        }

        [Route("/Recommendations/{id}", "GET", Summary = "User Recommendation")]
        public class UserRecommendationsRequest : IReturn<List<Recommendation>>
        {
            [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
            public string Id { get; set; }
        }

        private IJsonSerializer JsonSerializer { get; }
        private IApplicationPaths ApplicationPaths { get; }
        public MovieLensService(IJsonSerializer jsonSerializer, IApplicationPaths applicationPaths)
        {
            ApplicationPaths = applicationPaths;
            JsonSerializer = jsonSerializer;
        }

        public List<Recommendation> Get(RecommendationsRequest request)
        {
            var recommendationJson = Path.Combine(ApplicationPaths.DataPath,"learning", "recommendations.json");
            return !File.Exists(recommendationJson) ? new List<Recommendation>() : //Empty
                JsonSerializer.DeserializeFromFile<List<Recommendation>>(recommendationJson).Take(20).ToList();
        }

        public List<Recommendation> Get(UserRecommendationsRequest request)
        {
            var recommendationJson = Path.Combine(ApplicationPaths.DataPath, "learning", "recommendations.json");
            return !File.Exists(recommendationJson) ? new List<Recommendation>() : //Empty
                JsonSerializer.DeserializeFromFile<List<Recommendation>>(recommendationJson).Where(r => r.UserId.ToString().Replace("-", string.Empty) == request.Id).ToList();
        }
    }
}
