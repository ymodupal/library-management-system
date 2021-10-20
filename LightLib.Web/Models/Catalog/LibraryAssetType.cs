using System.ComponentModel.DataAnnotations;

namespace LightLib.Web.Models.Catalog
{
    public enum LibraryAssetType
    {
        [Display(Name = "Book")] Book,
        [Display(Name = "Audio Book")] AudioBook,
        [Display(Name = "Audio Cd")] AudioCd,
        [Display(Name = "DVD")] DVD,
    }
}
