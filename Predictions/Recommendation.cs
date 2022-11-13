using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.MovieLens.Predictions
{
    public class Recommendation
    {
        public long ItemInternalId { get; set; }
        public string MovieName { get; set; }
        public int MovieYear { get; set; }
        public double Score { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
       
    }
}
