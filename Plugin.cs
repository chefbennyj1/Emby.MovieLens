using System;
using System.Collections.Generic;
using System.IO;
using Emby.MovieLens.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.MovieLens
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public static Plugin Instance { get; set; }
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Movie Lens";
        public override string Description => "Train a neural network to recommend movies.";

        public override Guid Id => new Guid("7E862375-3521-4A23-BA3E-8598EBAC4A50");
        
        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.jpg");
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Jpg;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "MovieLensRecommendationConfigurationPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.MovieLensRecommendationConfigurationPage.html"
                },
                new PluginPageInfo
                {
                    Name = "MovieLensRecommendationConfigurationPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.MovieLensRecommendationConfigurationPage.js"
                },
                new PluginPageInfo
                {
                    Name = "MovieLensPluginSettingsConfigurationPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.MovieLensPluginSettingsConfigurationPage.html"
                },
                new PluginPageInfo
                {
                    Name = "MovieLensPluginSettingsConfigurationPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.MovieLensPluginSettingsConfigurationPage.js"
                },
                
            };
        }
    }
}