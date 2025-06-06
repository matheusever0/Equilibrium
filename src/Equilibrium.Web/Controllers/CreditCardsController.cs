using Equilibrium.Resources.Web;
using Equilibrium.Resources.Web.Enums;
using Equilibrium.Resources.Web.Helpers;
using Equilibrium.Web.Extensions;
using Equilibrium.Web.Filters;
using Equilibrium.Web.Interfaces;
using Equilibrium.Web.Models.CreditCard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Equilibrium.Web.Controllers
{
    [Authorize]
    [RequirePermission("creditcards.view")]
    public class CreditCardsController : Controller
    {
        private readonly ICreditCardService _creditCardService;
        private readonly IPaymentMethodService _paymentMethodService;

        private const int CREDIT_CARD_PAYMENT_TYPE = 2;

        public CreditCardsController(
            ICreditCardService creditCardService,
            IPaymentMethodService paymentMethodService)
        {
            _creditCardService = creditCardService ?? throw new ArgumentNullException(nameof(creditCardService));
            _paymentMethodService = paymentMethodService ?? throw new ArgumentNullException(nameof(paymentMethodService));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCards = await _creditCardService.GetAllCreditCardsAsync(token);
                return View(creditCards);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.CreditCard, ex);
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do cart�o de cr�dito n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCard = await _creditCardService.GetCreditCardByIdAsync(id, token);

                return creditCard == null ? NotFound("Cart�o de cr�dito n�o encontrado") : View(creditCard);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.CreditCard, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [RequirePermission("creditcards.create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCardPaymentMethods = await _paymentMethodService.GetByTypeAsync(CREDIT_CARD_PAYMENT_TYPE, token);
                ViewBag.PaymentMethods = creditCardPaymentMethods;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ResourceFinanceWeb.Error_PreparingForm;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("creditcards.create")]
        public async Task<IActionResult> Create(CreateCreditCardModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadPaymentMethodsForView();
                return View(model);
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCard = await _creditCardService.CreateCreditCardAsync(model, token);
                TempData["SuccessMessage"] = MessageHelper.GetCreationSuccessMessage(EntityNames.CreditCard);
                return RedirectToAction(nameof(Details), new { id = creditCard.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, MessageHelper.GetCreationErrorMessage(EntityNames.CreditCard, ex));
                await LoadPaymentMethodsForView();
                return View(model);
            }
        }

        [RequirePermission("creditcards.edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do cart�o de cr�dito n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCard = await _creditCardService.GetCreditCardByIdAsync(id, token);

                if (creditCard == null)
                {
                    return NotFound("Cart�o de cr�dito n�o encontrado");
                }

                var model = new UpdateCreditCardModel
                {
                    Name = creditCard.Name,
                    ClosingDay = creditCard.ClosingDay,
                    DueDay = creditCard.DueDay,
                    Limit = creditCard.Limit
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.CreditCard, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("creditcards.edit")]
        public async Task<IActionResult> Edit(string id, UpdateCreditCardModel model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do cart�o de cr�dito n�o fornecido");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                await _creditCardService.UpdateCreditCardAsync(id, model, token);
                TempData["SuccessMessage"] = MessageHelper.GetUpdateSuccessMessage(EntityNames.CreditCard);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, MessageHelper.GetUpdateErrorMessage(EntityNames.CreditCard, ex));
                return View(model);
            }
        }

        [RequirePermission("creditcards.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do cart�o de cr�dito n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCard = await _creditCardService.GetCreditCardByIdAsync(id, token);

                return creditCard == null ? NotFound("Cart�o de cr�dito n�o encontrado") : View(creditCard);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetLoadingErrorMessage(EntityNames.CreditCard, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("creditcards.delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID do cart�o de cr�dito n�o fornecido");
            }

            try
            {
                var token = HttpContext.GetJwtToken();
                await _creditCardService.DeleteCreditCardAsync(id, token);
                TempData["SuccessMessage"] = MessageHelper.GetDeletionSuccessMessage(EntityNames.CreditCard);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = MessageHelper.GetDeletionErrorMessage(EntityNames.CreditCard, ex);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("creditcards.edit")]
        public async Task<IActionResult> PayInvoice(string id, [FromBody] decimal amount)
        {
            try
            {
                var token = HttpContext.GetJwtToken();

                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Token de autentica��o n�o encontrado." });
                }

                if (amount <= 0)
                {
                    return Json(new { success = false, message = "O valor do pagamento deve ser maior que zero." });
                }

                var currentCard = await _creditCardService.GetCreditCardByIdAsync(id, token);
                var usedAmount = currentCard.Limit - currentCard.AvailableLimit;

                if (amount > usedAmount)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"O valor do pagamento (R$ {amount:N2}) n�o pode ser maior que o valor utilizado (R$ {usedAmount:N2})."
                    });
                }

                var updatedCard = await _creditCardService.UpdateLimitCreditCardAsync(id, amount, token);

                TempData["SuccessMessage"] = $"Pagamento de {amount:C2} registrado com sucesso! Novo limite dispon�vel: {updatedCard.GetFormattedAvailableLimit()}.";

                return Json(new
                {
                    success = true,
                    message = "Pagamento registrado com sucesso!",
                    newAvailableLimit = updatedCard.GetFormattedAvailableLimit(),
                    newUsedLimit = updatedCard.GetFormattedUsedLimit(),
                    paymentAmount = amount.ToString("C2")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Erro interno. Tente novamente." });
            }
        }

        private async Task LoadPaymentMethodsForView()
        {
            try
            {
                var token = HttpContext.GetJwtToken();
                var creditCardPaymentMethods = await _paymentMethodService.GetByTypeAsync(CREDIT_CARD_PAYMENT_TYPE, token);
                ViewBag.PaymentMethods = creditCardPaymentMethods;
            }
            catch
            {
                ViewBag.PaymentMethods = new List<object>();
            }
        }
    }
}
