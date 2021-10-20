using System;

namespace LightLib.Models.DTOs.Assets
{
    public class LibraryAssetDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public string Author {  get; set; }
        public int AvailabilityStatusId { get; set; }
        public StatusDto Status { get; set; }
        public decimal Cost { get; set; }
        public string ImageUrl { get; set; }
        public AssetType AssetType { get; set; }
        public int LocationId { get; set; }
        public BookDto Book = new BookDto();
        public AudioBookDto AudioBook = new AudioBookDto();
        public AudioCdDto AudioCdDto = new AudioCdDto();
        public DvdDto Dvd = new DvdDto();
        public virtual LibraryBranchDto Location { get; set; }
    }
}