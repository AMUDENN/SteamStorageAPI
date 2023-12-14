namespace SteamStorageAPI.Models.SteamAPIModels.Games
{
    public class SteamGameResponse
    {
        public int status { get; set; }
        public string appid { get; set; }
        public string name { get; set; }
        public string strFullDescription { get; set; }
        public string strSnippet { get; set; }
        public Developer[] rgDevelopers { get; set; }
        public Publisher[] rgPublishers { get; set; }
        public object[] rgSocialMedia { get; set; }
    }
}
