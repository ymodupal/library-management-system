using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LightLib.Data;
using LightLib.Data.Models;
using LightLib.Models;
using LightLib.Models.DTOs;
using LightLib.Models.Email;
using LightLib.Models.Exceptions;
using LightLib.Service.Email;
using LightLib.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LightLib.Service.Checkout
{

    public class CheckoutService : ICheckoutService
    {
        private readonly LibraryDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHoldService _holdService;
        private readonly ILibraryAssetService _assetService;
        private readonly ILibraryCardService _libraryCardService;
        private readonly ILogger<CheckoutService> _logger;
        private readonly IEmailService _emailService;

        private const int DefaultDateDueDays = 30;

        public CheckoutService(
            LibraryDbContext context,
            IHoldService holdService,
            ILibraryAssetService assetService,
            ILibraryCardService libraryCardService,
            IEmailService emailService,
            ILogger<CheckoutService> logger,
            IMapper mapper)
        {
            _context = context;
            _holdService = holdService;
            _assetService = assetService;
            _libraryCardService = libraryCardService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PaginationResult<CheckoutDto>> GetPaginated(int page, int perPage)
        {
            var checkouts = _context.Checkouts;
            var pageOfCheckouts = await checkouts.ToPaginatedResult(page, perPage);
            var pageOfAssetDtos = _mapper.Map<List<CheckoutDto>>(pageOfCheckouts.Results);
            return new PaginationResult<CheckoutDto>
            {
                PageNumber = pageOfCheckouts.PageNumber,
                PerPage = pageOfCheckouts.PerPage,
                Results = pageOfAssetDtos
            };
        }

        public async Task<PaginationResult<CheckoutHistoryDto>> GetCheckoutHistory(
            Guid assetId,
            int page,
            int perPage)
        {

            var checkoutHistories = _context.CheckoutHistories
                .Include(a => a.Asset)
                .Include(a => a.LibraryCard)
                .Where(a => a.Asset.Id == assetId);

            var pageOfHistory = await checkoutHistories.ToPaginatedResult(page, perPage);
            var pageOfHistoryDto = _mapper.Map<List<CheckoutHistoryDto>>(pageOfHistory.Results);
            return new PaginationResult<CheckoutHistoryDto>
            {
                PageNumber = pageOfHistory.PageNumber,
                PerPage = pageOfHistory.PerPage,
                Results = pageOfHistoryDto
            };
        }

        public async Task<CheckoutDto> Get(int checkoutId)
        {
            var checkout = await _context.Checkouts.FirstAsync(p => p.Id == checkoutId);
            return _mapper.Map<CheckoutDto>(checkout);
        }

        public async Task<CheckoutDto> GetLatestCheckout(Guid assetId)
        {
            var latest = await _context.Checkouts
                .Where(c => c.Asset.Id == assetId)
                .OrderByDescending(c => c.CheckedOutSince)
                .FirstAsync();
            return _mapper.Map<CheckoutDto>(latest);
        }

        public async Task<bool> IsCheckedOut(Guid assetId)
            => await _context.Checkouts.AnyAsync(a => a.Asset.Id == assetId);

        public async Task<string> GetCurrentPatron(Guid assetId)
        {
            var checkout = await _context.Checkouts
                .Include(a => a.Asset)
                .Include(a => a.LibraryCard)
                .FirstAsync(a => a.Asset.Id == assetId);

            if (checkout == null)
            {
                // TODO
            }

            var cardId = checkout.LibraryCard.Id;

            var patron = await _context.Patrons
                .Include(p => p.LibraryCard)
                .FirstAsync(c => c.LibraryCard.Id == cardId);

            return $"{patron.FirstName} {patron.LastName}";
        }

        public async Task<bool> Add(CheckoutDto newCheckoutDto)
        {
            var checkoutEntity = _mapper.Map<Data.Models.Checkout>(newCheckoutDto);
            try
            {
                await _context.AddAsync(checkoutEntity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new LibraryServiceException(Reason.UncaughtError);
            }
        }

        public async Task<bool> CheckOutItem(Guid assetId, int libraryCardId)
        {
            try
            {
                var now = DateTime.UtcNow;

                var isAlreadyCheckedOut = await IsCheckedOut(assetId);

                if (isAlreadyCheckedOut)
                {
                    return false;
                }

                var libraryAsset = await _context.LibraryAssets
                   .Include(a => a.AvailabilityStatus)
                   .FirstAsync(a => a.Id == assetId);

                _context.Update(libraryAsset);

                // TODO
                libraryAsset.AvailabilityStatus = await _context.Statuses
                    .FirstAsync(a => a.Name == "Checked Out");

                var libraryCard = await _context.LibraryCards
                    .Include(c => c.Checkouts)
                    .FirstAsync(a => a.Id == libraryCardId);

                var checkout = new Data.Models.Checkout
                {
                    Asset = libraryAsset,
                    LibraryCard = libraryCard,
                    CheckedOutSince = now,
                    CheckedOutUntil = GetDefaultDateDue(now)
                };

                await _context.AddAsync(checkout);

                var checkoutHistory = new CheckoutHistory
                {
                    CheckedOut = now,
                    Asset = libraryAsset,
                    LibraryCard = libraryCard
                };

                await _context.AddAsync(checkoutHistory);
                await _context.SaveChangesAsync();

                var assetDto = await _assetService.Get(assetId).ConfigureAwait(false);
                var libraryCardDto = await _libraryCardService.Get(libraryCardId).ConfigureAwait(false);

                if (assetDto != null)
                {
                    var emailModel = new EmailModel()
                    {
                        To = libraryCardDto.Patron.Email,
                        Action = "Checkout",
                        AssetType = assetDto.AssetType.ToString(),
                        AssetName = assetDto.Title,
                        PatronName = libraryCardDto.Patron.FirstName + " " + libraryCardDto.Patron.LastName
                    };

                    await SendEmail(emailModel).ConfigureAwait(false);
                }

                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception in Checkout service");
                return false;
            }
        }

        public async Task<bool> CheckInItem(Guid assetId)
        {

            var now = DateTime.UtcNow;

            var libraryAsset = await _context.LibraryAssets
                .FirstAsync(a => a.Id == assetId);

            //_context.Update(libraryAsset);

            // remove any existing checkouts on the item
            var checkout = await _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.LibraryCard)
                .FirstOrDefaultAsync(a => a.Asset.Id == assetId);

            if (checkout != null)
            {
                _context.Remove(checkout);
            }
            else
            {
                return false;
            }

            // close any existing checkout history
            var history = await _context.CheckoutHistories
                .Include(h => h.Asset)
                .Include(h => h.LibraryCard)
                .FirstAsync(h =>
                    h.Asset.Id == assetId
                    && h.CheckedIn == null);

            if (history != null)
            {
                _context.Update(history);
                history.CheckedIn = now;
            }

            // if there are current holds, check out the item to the earliest
            // TODO
            var wasCheckedOutToOldestHold = await CheckoutToEarliestHold(assetId);

            if (wasCheckedOutToOldestHold)
            {
                // TODO
                //libraryAsset.AvailabilityStatus = await _context.Statuses
                //    .FirstAsync(a => a.Name == "Checked Out");
            }
            else
            {
                // otherwise, set item status to available
                // TODO magic string
                libraryAsset.AvailabilityStatus = await _context.Statuses
                    .FirstAsync(a => a.Name == "Available");
            }

            await _context.SaveChangesAsync();

            var assetDto = await _assetService.Get(assetId).ConfigureAwait(false);
            var libraryCardDto = await _libraryCardService.Get(checkout.LibraryCardId).ConfigureAwait(false);

            if (assetDto != null)
            {
                var emailModel = new EmailModel()
                {
                    To = libraryCardDto.Patron.Email,
                    Action = "Checkin",
                    AssetType = assetDto.AssetType.ToString(),
                    AssetName = assetDto.Title,
                    PatronName = libraryCardDto.Patron.FirstName + " " +  libraryCardDto.Patron.LastName
                };

                await SendEmail(emailModel).ConfigureAwait(false);
            }

            return true;
        }

        private async Task<bool> CheckoutToEarliestHold(Guid assetId)
        {

            var earliestHoldDto = await _holdService.GetEarliestHoldAsync(assetId);

            if (earliestHoldDto == null)
            {
                return false;
            }

            var card = earliestHoldDto.LibraryCard;

            _context.Remove(earliestHoldDto);
            await _context.SaveChangesAsync();

            // TODO
            var checkOutResult = await CheckOutItem(assetId, card.Id);

            return checkOutResult;
        }

        private static DateTime GetDefaultDateDue(DateTime now) => now.AddDays(DefaultDateDueDays);

        private async Task SendEmail(EmailModel model)
        {
            try
            {
                await _emailService.Send(model).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception in SendEmail method");
            }
        }
    }
}
