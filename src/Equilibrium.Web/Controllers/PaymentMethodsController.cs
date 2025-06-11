using Equilibrium.Resources.Web.Enums;
using Equilibrium.Resources.Web.Helpers;
using Equilibrium.Web.Filters;
using Equilibrium.Web.Interfaces;
using Equilibrium.Web.Models.PaymentMethod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Equilibrium.Web.Controllers
{
    [Authorize]
    [RequirePermission("paymentmethods.view")]
    public class PaymentMethodsController : BaseController
    {
        private readonly IPaymentMethodService _paymentMethodService;
        private readonly IPaymentService _paymentService;

        public PaymentMethodsController(IPaymentMethodService paymentMethodService, IPaymentService paymentService)
        {
            _paymentMethodService = paymentMethodService;
            _paymentService = paymentService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = GetToken();
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
                var token = GetToken();
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
                var token = GetToken();
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
                var token = GetToken();
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
                var token = GetToken();
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
                var token = GetToken();
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
                var token = GetToken();
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
                var token = GetToken();
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("paymentmethods.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            return await HandleGenericDelete(
                id,
                _paymentMethodService,
                async (service, itemId, token) => await service.DeletePaymentMethodAsync(itemId, token),
                async (service, itemId, token) => await service.GetPaymentMethodByIdAsync(itemId, token),
                "m�todo de pagamento",
                null,
                async (item) => {
                    if (item is PaymentMethodModel method)
                    {
                        if (method.Type == 2 && method.CreditCards?.Any() == true)
                        {
                            return (false, $"Este m�todo possui {method.CreditCards.Count} cart�es associados. Exclua os cart�es primeiro.");
                        }

                        var paymentsCount = await _paymentService.GetPaymentsByMethodAsync(method.Id, GetToken());
                        if (paymentsCount.Any())
                        {
                            return (false, $"Este m�todo possui {paymentsCount.Count()} pagamentos associados. N�o � poss�vel excluir.");
                        }
                    }
                    return (true, null);
                }
            );
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
    }
}
