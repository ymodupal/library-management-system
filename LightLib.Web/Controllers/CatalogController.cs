using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightLib.Models;
using LightLib.Models.DTOs;
using LightLib.Models.DTOs.Assets;
using LightLib.Service.Interfaces;
using LightLib.Web.Models.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace LightLib.Web.Controllers {
    
    public class CatalogController : LibraryController {
        
        private readonly ILibraryAssetService _assetsService;
        private readonly ICheckoutService _checkoutsService;
        private readonly IHoldService _holdService;

        public CatalogController(
            ILibraryAssetService assetsService, 
            IHoldService holdService,
            ICheckoutService checkoutsService) {
            _assetsService = assetsService;
            _checkoutsService = checkoutsService;
            _holdService = holdService;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int perPage = 10) {

            //var paginationServiceResult = await _assetsService.GetPaginated(page, perPage);
            //if (paginationServiceResult != null && paginationServiceResult.Results.Any())
            //{
            //    var allAssets = paginationServiceResult.Results.ToList();
            //    var viewModel = new AssetIndexModel
            //    {
            //        PageOfAssets = new PaginationResult<LibraryAssetDto>
            //        {
            //            Results = allAssets 
            //        }
            //    };
            //
            //    return View(viewModel);
            //}

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

        public async Task<IActionResult> Detail(Guid id) {

            var asset = await _assetsService.Get(id);
            //var assetLocation = await _assetsService.GetCurrentLocation(id);
            //var paginated = await _assetsService.GetPaginated(1, 10);
            //var found = await _assetsService.MarkFound(id);
            //var lost = await _assetsService.MarkLost(id);

            var model = new AssetDetailModel()
            {
                AssetId = asset.Id,
                Title = asset.Title,
                Year = asset.Year,
                Status = asset.Status.Name,
                CurrentLocation = asset.Location.Name,
                Cost = asset.Cost,
                ImageUrl = asset.ImageUrl
            };

            return View(model);

        }

        public async Task<IActionResult> VisitCheckOutPage(int id) {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> CheckIn(string assetId) {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> MarkLost(string assetId) {
            var assetGuid = Guid.Parse(assetId);
            await _assetsService.MarkLost(assetGuid);
            return RedirectToAction("Detail", new {assetGuid});
        }

        public async Task<IActionResult> MarkFound(string assetId) {
            var assetGuid = Guid.Parse(assetId);
            await _assetsService.MarkFound(assetGuid);
            return RedirectToAction("Detail", new {assetGuid});
        }

        [HttpPost]
        public async Task<IActionResult> PlaceCheckout(string assetId, int libraryCardId) {
            var assetGuid = Guid.Parse(assetId);
            await _checkoutsService.CheckOutItem(assetGuid, libraryCardId);
            return RedirectToAction("Detail", new {id = assetId});
        }
        
        [HttpPost]
        public async Task<IActionResult> PlaceHold(string assetId, int libraryCardId) {
            var assetGuid = Guid.Parse(assetId);
            await _holdService.PlaceHold(assetGuid, libraryCardId);
            return RedirectToAction("Detail", new {id = assetId});
        }
    }
}