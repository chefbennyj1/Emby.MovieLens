using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Plugins;

namespace Emby.MovieLens
{
    public class MovieLensServerEntryPoint : IServerEntryPoint
    {
        private IApplicationPaths ApplicationPaths { get; set; }
        
        public MovieLensServerEntryPoint(IApplicationPaths appPaths)
        {
            ApplicationPaths = appPaths;
        }
        public void Dispose()
        {
            
        }

        public void Run()
        {
            if (File.Exists(Path.Combine(ApplicationPaths.DataPath, "learning", "model.zip")))
            {
                var creation = File.GetCreationTime(Path.Combine(ApplicationPaths.DataPath, "learning", "model.zip"));
                var config = Plugin.Instance.Configuration;
                config.LastTrainedDate = creation;
                Plugin.Instance.UpdateConfiguration(config);
            }
        }

    }
}
