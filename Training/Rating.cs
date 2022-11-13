namespace Emby.MovieLens.Training
{
    public class Rating
    {
        public string userId { get; set; }
        public string movieId { get; set; }
        public float rating { get; set; }
        public string timestamp { get; set; }
    }
}
