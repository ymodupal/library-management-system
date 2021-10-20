using LightLib.Models;
using System.ComponentModel.DataAnnotations;

namespace LightLib.Web.Models.Catalog
{
    public class CreateAssetModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        public decimal Cost { get; set; }
        [Required]
        public AssetType AssetType { get; set; }
        public string ImageUrl { get; set; }
        [Required]
        public int LocationId { get; set; }
    }
}
