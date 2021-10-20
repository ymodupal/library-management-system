using System.ComponentModel;

namespace LightLib.Models
{

    /// <summary>
    /// Any LibraryAsset is a particular AssetType
    /// </summary>
    public enum AssetType
    {
        [Description("Book")]
        Book,
        [Description("Audio Book")]
        AudioBook,
        [Description("Audio CD")]
        AudioCd,
        [Description("DVD")]
        DVD,
    }
}