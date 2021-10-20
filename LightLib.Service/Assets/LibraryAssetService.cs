using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LightLib.Data;
using LightLib.Data.Models.Assets;
using LightLib.Models;
using LightLib.Models.DTOs;
using LightLib.Models.DTOs.Assets;
using LightLib.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LightLib.Service.Assets
{

    public class LibraryAssetService : ILibraryAssetService
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;

        public LibraryAssetService(
            LibraryDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Guid> Add(LibraryAssetDto assetDto)
        {
            var newAsset = _mapper.Map<Asset>(assetDto);
            switch (assetDto.AssetType)
            {
                case AssetType.Book:
                    if (assetDto.Book == null) throw new Exception("Book properties are not set.");

                    newAsset.Book = new Book() 
                    { 
                        Title = assetDto.Book.Title, 
                        Author = assetDto.Book.Author, 
                        ISBN = assetDto.Book.ISBN,
                        PublicationYear = assetDto.Book.PublicationYear, 
                        Language = assetDto.Book.Language
                    };
                    break;
                case AssetType.AudioBook:
                    if (assetDto.AudioBook == null) throw new Exception("Audio Book properties are not set.");

                    newAsset.AudioBook = new AudioBook()
                    {
                        Title = assetDto.AudioBook.Title,
                        Author = assetDto.AudioBook.Author,
                        ASIN = assetDto.AudioBook.ASIN,
                        PublicationYear = assetDto.AudioBook.PublicationYear,
                        LengthMinutes = assetDto.AudioBook.LengthMinutes,
                        Publisher = assetDto.AudioBook.Publisher,
                        Language = assetDto.AudioBook.Language
                    };
                    break;
                case AssetType.AudioCd:
                    if (assetDto.AudioCdDto == null) throw new Exception("Audio CD properties are not set.");

                    newAsset.AudioCd = new AudioCd()
                    {
                        Title = assetDto.AudioCdDto.Title,
                        Artist = assetDto.AudioCdDto.Artist,
                        PublicationYear = assetDto.AudioCdDto.PublicationYear,
                        Label = assetDto.AudioCdDto.Label,
                        Language = assetDto.AudioCdDto.Language,
                        Genre = assetDto.AudioCdDto.Genre
                    };
                    break;
                case AssetType.DVD:
                    if (assetDto.Dvd == null) throw new Exception("DVD properties are not set.");

                    newAsset.DVD = new DVD()
                    {
                        Title = assetDto.Dvd.Title,
                        Year = assetDto.Dvd.Year,
                        Director = assetDto.Dvd.Director,
                        LengthMinutes = assetDto.Dvd.LengthMinutes,
                        Language = assetDto.Dvd.Language
                    };
                    break;
                default:
                    throw new Exception("Invalid Asset type");
            }
            //newAsset.AvailabilityStatus = _context.Statuses.FirstOrDefaultAsync(p => p.Name.ToLower() == "new");
            await _context.AddAsync(newAsset);
            await _context.SaveChangesAsync();
            return newAsset.Id;
        }

        public async Task<LibraryAssetDto> Get(Guid assetId)
        {
            //var asset = await _context.LibraryAssets
            //    .Include(a => a.AvailabilityStatus)
            //    .Include(a => a.Location)
            //    .FirstAsync(a => a.Id == assetId);

            var asset = from libAsset in _context.LibraryAssets
                                .Include(a => a.AvailabilityStatus)
                                .Include(a => a.Location)
                        join bookDts in _context.Books on libAsset.Id equals bookDts.AssetId into bks
                        from bookDts in bks.DefaultIfEmpty()
                        join audBooks in _context.AudioBooks on libAsset.Id equals audBooks.AssetId into abks
                        from audBooks in abks.DefaultIfEmpty()
                        join dvdDts in _context.Dvds on libAsset.Id equals dvdDts.AssetId into dvds
                        from dvdDts in dvds.DefaultIfEmpty()
                        join audioCdDts in _context.AudioCds on libAsset.Id equals audioCdDts.AssetId into acds
                        from audioCdDts in acds.DefaultIfEmpty()
                        where libAsset.Id == assetId
                        select new LibraryAssetDto
                        {
                            Id = libAsset.Id,
                            Title = bookDts != null ? bookDts.Title :
                                     audBooks != null ? audBooks.Title :
                                     dvdDts != null ? dvdDts.Title :
                                     audioCdDts != null ? audioCdDts.Title : string.Empty,
                            Year = bookDts != null ? bookDts.PublicationYear :
                                     audBooks != null ? audBooks.PublicationYear :
                                     dvdDts != null ? dvdDts.Year :
                                     audioCdDts != null ? audioCdDts.PublicationYear : 0,
                            Author = bookDts != null ? bookDts.Author :
                                    audBooks != null ? audBooks.Author :
                                    dvdDts != null ? dvdDts.Director :
                                    audioCdDts != null ? audioCdDts.Artist : string.Empty,
                            Cost = libAsset.Cost,
                            ImageUrl = libAsset.ImageUrl,
                            AssetType = bookDts != null ? AssetType.Book :
                                     audBooks != null ? AssetType.AudioBook :
                                     dvdDts != null ? AssetType.DVD :
                                     audioCdDts != null ? AssetType.AudioCd : AssetType.Book,
                            Status = _mapper.Map<StatusDto>(libAsset.AvailabilityStatus),
                            Location = _mapper.Map<LibraryBranchDto>(libAsset.Location)
                        };

            return await asset.FirstOrDefaultAsync();
        }

        public async Task<PaginationResult<LibraryAssetDto>> GetPaginated(int page, int perPage)
        {
            var masterAssets = _context.LibraryAssets;
            var pageOfAssets = await masterAssets.ToPaginatedResult(page, perPage);

            var assets = await (from libAssets in _context.LibraryAssets
                                .Include(a => a.AvailabilityStatus)
                                .Include(a => a.Location)
                                join bookDts in _context.Books on libAssets.Id equals bookDts.AssetId into bks
                                from bookDts in bks.DefaultIfEmpty()
                                join audBooks in _context.AudioBooks on libAssets.Id equals audBooks.AssetId into abks
                                from audBooks in abks.DefaultIfEmpty()
                                join dvdDts in _context.Dvds on libAssets.Id equals dvdDts.AssetId into dvds
                                from dvdDts in dvds.DefaultIfEmpty()
                                join audioCdDts in _context.AudioCds on libAssets.Id equals audioCdDts.AssetId into acds
                                from audioCdDts in acds.DefaultIfEmpty()
                                where pageOfAssets.Results.Select(a => a.Id).Contains(libAssets.Id)
                                select new LibraryAssetDto
                                {
                                    Id = libAssets.Id,
                                    Title = bookDts != null ? bookDts.Title :
                                            audBooks != null ? audBooks.Title :
                                            dvdDts != null ? dvdDts.Title :
                                            audioCdDts != null ? audioCdDts.Title : string.Empty,
                                    Year = bookDts != null ? bookDts.PublicationYear :
                                            audBooks != null ? audBooks.PublicationYear :
                                            dvdDts != null ? dvdDts.Year :
                                            audioCdDts != null ? audioCdDts.PublicationYear : 0,
                                    Author = bookDts != null ? bookDts.Author :
                                           audBooks != null ? audBooks.Author :
                                           dvdDts != null ? dvdDts.Director :
                                           audioCdDts != null ? audioCdDts.Artist : string.Empty,
                                    Cost = libAssets.Cost,
                                    ImageUrl = libAssets.ImageUrl,
                                    AssetType = bookDts != null ? AssetType.Book :
                                            audBooks != null ? AssetType.AudioBook :
                                            dvdDts != null ? AssetType.DVD :
                                            audioCdDts != null ? AssetType.AudioCd : AssetType.Book,
                                    Status = _mapper.Map<StatusDto>(libAssets.AvailabilityStatus),
                                    Location = _mapper.Map<LibraryBranchDto>(libAssets.Location)
                                }).ToListAsync();

            return new PaginationResult<LibraryAssetDto>
            {
                PageNumber = pageOfAssets.PageNumber,
                PerPage = pageOfAssets.PerPage,
                Results = assets,
                TotalCount = pageOfAssets.TotalCount,
                HasNextPage = pageOfAssets.HasNextPage,
                HasPreviousPage = pageOfAssets.HasPreviousPage
            };
        }

        public async Task<LibraryBranchDto> GetCurrentLocation(Guid assetId)
        {
            var asset = await _context
                .LibraryAssets
                .FirstAsync(a => a.Id == assetId);
            var location = asset.Location;
            return _mapper.Map<LibraryBranchDto>(location);
        }

        public async Task<bool> MarkLost(Guid assetId)
        {
            var item = await _context.LibraryAssets
                .FirstAsync(a => a.Id == assetId);
            _context.Update(item);
            // TODO
            item.AvailabilityStatus = _context.Statuses
                .First(a => a.Name == AssetStatus.Lost);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkFound(Guid assetId)
        {
            var libraryAsset = await _context.LibraryAssets
                .FirstAsync(a => a.Id == assetId);
            _context.Update(libraryAsset);
            libraryAsset.AvailabilityStatus = _context.Statuses
                .First(a => a.Name == AssetStatus.Available);
            var now = DateTime.UtcNow;

            // remove any existing checkouts on the item
            var checkout = _context.Checkouts
                .First(a => a.Asset.Id == assetId);
            if (checkout != null) _context.Remove(checkout);

            // close any existing checkout history
            var history = _context.CheckoutHistories
                .First(h =>
                    h.Asset.Id == assetId
                    && h.CheckedIn == null);

            if (history != null)
            {
                _context.Update(history);
                history.CheckedIn = now;
            }
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
