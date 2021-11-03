using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnumsNET;
using LightLib.Models;
using LightLib.Models.DTOs;
using LightLib.Models.DTOs.Assets;
using LightLib.Service.Interfaces;
using LightLib.Web.Models.Catalog;
using LightLib.Web.Models.CheckoutModels;
using LightLib.Web.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace LightLib.Web.Controllers
{

    public class CatalogController : LibraryController
    {

        private readonly ILibraryAssetService _assetsService;
        private readonly ICheckoutService _checkoutsService;
        private readonly IHoldService _holdService;
        private readonly ILibraryBranchService _libraryBranchService;
        private readonly IStatusService _statusService;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(
            ILibraryAssetService assetsService,
            IHoldService holdService,
            ICheckoutService checkoutsService,
            ILibraryBranchService libraryBranchService,
            IStatusService statusService,
            ILogger<CatalogController> logger)
        {
            _assetsService = assetsService;
            _checkoutsService = checkoutsService;
            _holdService = holdService;
            _libraryBranchService = libraryBranchService;
            _statusService = statusService;
            _logger = logger;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            try
            {
                var assets = await _assetsService.GetPaginated(page, perPage);

                if (assets != null && assets.Results.Any())
                {
                    var viewModel = new AssetIndexModel
                    {
                        PageOfAssets = assets
                    };

                    return View(viewModel);
                }

                var emptyModel = new AssetIndexModel
                {
                    PageOfAssets = new PaginationResult<LibraryAssetDto>()
                    {
                        Results = new List<LibraryAssetDto>(),
                        PerPage = perPage,
                        PageNumber = page
                    }
                };

                return View(emptyModel);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Exception in Get Catalog: {ex.Message}");
                Console.WriteLine(ex.Message);
                _logger.LogError(ex, ex.StackTrace);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<IActionResult> Detail(Guid id)
        {
            var asset = await _assetsService.Get(id);
            var checkOutHistory = await _checkoutsService.GetCheckoutHistory(id, 1, 10).ConfigureAwait(false);
            var assetHolds = await _holdService.GetCurrentHoldsPaginated(id, 1, 10).ConfigureAwait(false);

            //var assetLocation = await _assetsService.GetCurrentLocation(id);
            //var paginated = await _assetsService.GetPaginated(1, 10);
            //var found = await _assetsService.MarkFound(id);
            //var lost = await _assetsService.MarkLost(id);

            var model = new AssetDetailModel()
            {
                AssetId = asset.Id.ToString(),
                Title = asset.Title,
                Year = asset.Year,
                Status = asset.Status.Name,
                CurrentLocation = asset.Location.Name,
                AuthorOrDirector = asset.Author,
                Cost = asset.Cost,
                ImageUrl = asset.ImageUrl,
                CheckoutHistory = checkOutHistory.Results.AsEnumerable(),
                CurrentHolds = assetHolds.Results.AsEnumerable(),
                Type = asset.AssetType.AsString(EnumFormat.Description)
            };

            return View(model);

        }

        [HttpGet]
        //[Route("")]
        public async Task<IActionResult> Checkout(Guid id)
        {
            var asset = await _assetsService.Get(id);
            var model = new CheckoutModel()
            {
                AssetId = id,
                ImageUrl = asset.ImageUrl,
                IsCheckedOut = false,
                //HoldCount = 
            };

            return View(model);
        }

        public async Task<IActionResult> CheckIn(Guid assetId)
        {
            await _checkoutsService.CheckInItem(assetId);
            return Redirect($"{AppConstants.UrlPrefix}/Catalog/Detail/" + assetId);
        }

        public async Task<IActionResult> MarkLost(Guid assetId)
        {
            await _assetsService.MarkLost(assetId);
            return Redirect($"{AppConstants.UrlPrefix}/Catalog/Detail/" + assetId);
        }

        public async Task<IActionResult> MarkFound(Guid assetId)
        {
            await _assetsService.MarkFound(assetId);
            return Redirect($"{AppConstants.UrlPrefix}/Catalog/Detail/" + assetId);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceCheckout(Guid assetId, int libraryCardId)
        {
            await _checkoutsService.CheckOutItem(assetId, libraryCardId);
            return Redirect($"{AppConstants.UrlPrefix}/Catalog/Detail/"+assetId);
        }

        [HttpGet]
        //[Route("")]
        public async Task<IActionResult> Hold(Guid assetId)
        {
            var asset = await _assetsService.Get(assetId);
            var model = new CheckoutModel()
            {
                AssetId = assetId,
                ImageUrl = asset.ImageUrl,
                IsCheckedOut = true,
                //HoldCount = 
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceHold(Guid assetId, int libraryCardId)
        {
            await _holdService.PlaceHold(assetId, libraryCardId);
            return Redirect($"{AppConstants.UrlPrefix}/Catalog/Detail/"+assetId);
        }

        [HttpGet]
        public async Task<IActionResult> AddAsset()
        {
            var branches = await _libraryBranchService.Get().ConfigureAwait(false);
            var selectListItems = branches.Select(b => new SelectListItem { Text = b.Name, Value = b.Id.ToString() }).ToList();
            var branchesList = new SelectList(selectListItems, "Value", "Text");
            ViewData["Branches"] = branchesList;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddAsset(CreateAssetModel addAsset)
        {
            var assetDto = new LibraryAssetDto()
            {
                Title = addAsset.Title,
                AssetType = addAsset.AssetType,
                Cost = addAsset.Cost,
                Year = addAsset.Year,
                ImageUrl = addAsset.ImageUrl,
                AvailabilityStatusId = (await _statusService.GetStatusByName("New")).Id,
                LocationId = (await _libraryBranchService.Get(addAsset.LocationId)).Id
            };

            var newAssetId = await _assetsService.Add(assetDto).ConfigureAwait(false);

            var asset = await _assetsService.Get(newAssetId);

            var model = new AssetDetailModel()
            {
                AssetId = asset.Id.ToString(),
                Title = asset.Title,
                Year = asset.Year,
                Status = asset.Status.Name,
                CurrentLocation = asset.Location.Name,
                Cost = asset.Cost,
                ImageUrl = asset.ImageUrl,
                CheckoutHistory = new List<CheckoutHistoryDto>(),
                CurrentHolds = new List<HoldDto>(),
            };

            return View("Detail", model);
        }
    }
}