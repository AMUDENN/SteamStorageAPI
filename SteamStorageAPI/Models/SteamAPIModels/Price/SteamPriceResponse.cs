﻿namespace SteamStorageAPI.Models.SteamAPIModels.Price
{
    public class SteamPriceResponse
    {
        public bool success { get; set; }
        public string lowest_price { get; set; }
        public string volume { get; set; }
        public string median_price { get; set; }
    }

}
