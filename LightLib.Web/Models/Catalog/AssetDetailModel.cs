using System.Collections.Generic;
using LightLib.Data.Models;
using LightLib.Models.DTOs;

namespace LightLib.Web.Models.Catalog
{
    public class AssetDetailModel
    {
        public string AssetId { get; set; }
        public string Title { get; set; }
        public string AuthorOrDirector { get; set; }
        public string Type { get; set; }
        public int Year { get; set; }
        public string Isbn { get; set; }
        public string Dewey { get; set; }
        public string Status { get; set; }
        public decimal Cost { get; set; }
        public string CurrentLocation { get; set; }
        public string ImageUrl { get; set; }
        public string PatronName { get; set; }
        public Checkout LatestCheckout { get; set; }
        public LibraryCard CurrentAssociatedLibraryCard { get; set; }
        public IEnumerable<CheckoutHistoryDto> CheckoutHistory { get; set; }
        public IEnumerable<HoldDto> CurrentHolds { get; set; }
    }

    public class AssetHoldModel
    {
        public string PatronName { get; set; }
        public string HoldPlaced { get; set; }
    }
}