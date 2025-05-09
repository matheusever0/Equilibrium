using Equilibrium.Resources.Web.Enums;
using Equilibrium.Resources.Web.Helpers;
using Equilibrium.Web.Extensions;
using Equilibrium.Web.Filters;
using Equilibrium.Web.Models.Filters;
using Equilibrium.Web.Models.PaymentMethod;
using Equilibrium.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Equilibrium.Web.Controllers
{
    [Authorize]
    [RequirePermission("paymentmethods.view")]
    public class PaymentMethodsController : Controller
    {
        private readonly IPaymentMethodService _paymentMethodService;

        public PaymentMethodsController(IPaymentMethodService paymentMethodService)
        {
            _paymentMethodService = paymentMethodService ?? throw new ArgumentNullException(nameof(paymentMethodService));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethods = await _paymentMethodService.GetAllPaymentMethodsAsync(token);
                return View(paymentMethods);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> System()
        {
            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethods = await _paymentMethodService.GetSystemPaymentMethodsAsync(token);
                ViewBag.IsSystemView = true;
                ViewBag.Title = "M�todos de Pagamento do Sistema";
                return View("Index", paymentMethods);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> User()
        {
            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethods = await _paymentMethodService.GetUserPaymentMethodsAsync(token);
                ViewBag.IsUserView = true;
                ViewBag.Title = "Meus M�todos de Pagamento";
                return View("Index", paymentMethods);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> ByType(int type)
        {
            if (type <= 0)
            {
                return BadRequest("Tipo de m�todo de pagamento inv�lido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethods = await _paymentMethodService.GetByTypeAsync(type, token);

                string typeDescription = GetPaymentMethodTypeDescription(type);
                ViewBag.Title = $"M�todos de Pagamento do Tipo: {typeDescription}";
                ViewBag.Type = type;

                return View("Index", paymentMethods);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do m�todo de pagamento n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id, token);

                return paymentMethod == null ? NotFound("M�todo de pagamento n�o encontrado") : View(paymentMethod);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [RequirePermission("paymentmethods.create")]
        public IActionResult Create()
        {
            ViewBag.PaymentMethodTypes = GetPaymentMethodTypes();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("paymentmethods.create")]
        public async Task<IActionResult> Create(CreatePaymentMethodModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PaymentMethodTypes = GetPaymentMethodTypes();
                return View(model);
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethod = await _paymentMethodService.CreatePaymentMethodAsync(model, token);
                TempData["SuccessMessage"] = MessageHelper.GetCreationSuccessMessage(EntityNames.PaymentMethod);
                return RedirectToAction(nameof(Details), new { id = paymentMethod.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, MessageHelper.GetCreationErrorMessage(EntityNames.PaymentMethod, ex));
                ViewBag.PaymentMethodTypes = GetPaymentMethodTypes();
                return View(model);
            }
        }

        [RequirePermission("paymentmethods.edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do m�todo de pagamento n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id, token);

                if (paymentMethod == null)
                {
                    return NotFound("M�todo de pagamento n�o encontrado");
                }

                if (paymentMethod.IsSystem)
                {
                    TempData["ErrorMessage"] = MessageHelper.GetCannotEditSystemEntityMessage(EntityNames.PaymentMethod);
                    return RedirectToAction(nameof(Details), new { id });
                }

                var model = new UpdatePaymentMethodModel
                {
                    Name = paymentMethod.Name,
                    Description = paymentMethod.Description
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("paymentmethods.edit")]
        public async Task<IActionResult> Edit(string id, UpdatePaymentMethodModel model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do m�todo de pagamento n�o fornecido");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id, token);

                if (paymentMethod == null)
                {
                    return NotFound("M�todo de pagamento n�o encontrado");
                }

                if (paymentMethod.IsSystem)
                {
                    TempData["ErrorMessage"] = MessageHelper.GetCannotEditSystemEntityMessage(EntityNames.PaymentMethod);
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _paymentMethodService.UpdatePaymentMethodAsync(id, model, token);
                TempData["SuccessMessage"] = MessageHelper.GetUpdateSuccessMessage(EntityNames.PaymentMethod);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, MessageHelper.GetUpdateErrorMessage(EntityNames.PaymentMethod, ex));
                return View(model);
            }
        }

        [RequirePermission("paymentmethods.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do m�todo de pagamento n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id, token);

                if (paymentMethod == null)
                {
                    return NotFound("M�todo de pagamento n�o encontrado");
                }

                if (paymentMethod.IsSystem)
                {
                    TempData["ErrorMessage"] = MessageHelper.GetCannotDeleteSystemEntityMessage(EntityNames.PaymentMethod);
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(paymentMethod);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("paymentmethods.delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do m�todo de pagamento n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id, token);

                if (paymentMethod == null)
                {
                    return NotFound("M�todo de pagamento n�o encontrado");
                }

                if (paymentMethod.IsSystem)
                {
                    TempData["ErrorMessage"] = MessageHelper.GetCannotDeleteSystemEntityMessage(EntityNames.PaymentMethod);
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _paymentMethodService.DeletePaymentMethodAsync(id, token);
                TempData["SuccessMessage"] = MessageHelper.GetDeletionSuccessMessage(EntityNames.PaymentMethod);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetDeletionErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        private List<SelectListItem> GetPaymentMethodTypes()
        {
            return
            [
                new() { Value = "1", Text = "Dinheiro" },
                new() { Value = "2", Text = "Cart�o de Cr�dito" },
                new() { Value = "3", Text = "Cart�o de D�bito" },
                new() { Value = "4", Text = "Transfer�ncia Banc�ria" },
                new() { Value = "5", Text = "Carteira Digital" },
                new() { Value = "6", Text = "Cheque" },
                new() { Value = "7", Text = "Outro" }
            ];
        }

        private static string GetPaymentMethodTypeDescription(int type)
        {
            return type switch
            {
                1 => "Dinheiro",
                2 => "Cart�o de Cr�dito",
                3 => "Cart�o de D�bito",
                4 => "Transfer�ncia Banc�ria",
                5 => "Carteira Digital",
                6 => "Cheque",
                7 => "Outro",
                _ => "Desconhecido"
            };
        }

        [HttpGet("filter")]
        [RequirePermission("paymentmethods.view")]
        public async Task<IActionResult> Filter(PaymentMethodFilter filter = null)
        {
            if (filter == null)
                filter = new PaymentMethodFilter();

            try
            {
                var token = HttpContext.GetJwtToken();
                var result = await _paymentMethodService.GetFilteredAsync(filter, token);

                // Add pagination headers
                Response.Headers.Add("X-Pagination-Total", result.TotalCount.ToString());
                Response.Headers.Add("X-Pagination-Pages", result.TotalPages.ToString());
                Response.Headers.Add("X-Pagination-Page", result.PageNumber.ToString());
                Response.Headers.Add("X-Pagination-Size", result.PageSize.ToString());

                ViewBag.Filter = filter;
                ViewBag.TotalCount = result.TotalCount;
                ViewBag.TotalPages = result.TotalPages;
                ViewBag.CurrentPage = result.PageNumber;
                ViewBag.PageSize = result.PageSize;

                return View("Index", result.Items);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.PaymentMethod, ex);
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet("api/filter")]
        [RequirePermission("paymentmethods.view")]
        public async Task<IActionResult> FilterJson([FromQuery] PaymentMethodFilter filter)
        {
            if (filter == null)
                filter = new PaymentMethodFilter();

            try
            {
                var token = HttpContext.GetJwtToken();
                var result = await _paymentMethodService.GetFilteredAsync(filter, token);

                return Json(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageNumber,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages,
                    hasPreviousPage = result.HasPreviousPage,
                    hasNextPage = result.HasNextPage
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
