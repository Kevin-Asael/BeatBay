using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using BeatBay.DTOs;
using System.Security.Claims;

namespace BeatBay.MVC.Controllers
{
    public class PlansController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PlansController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private void SetAuthorizationHeader()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Vista principal de planes
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                // Obtener estado actual del usuario
                var response = await _httpClient.GetAsync($"{apiUrl}/PlanSimulation/my-plan-status");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var planStatus = JsonConvert.DeserializeObject<UserPlanStatusDto>(content);
                    return View(planStatus);
                }
                else
                {
                    ViewBag.ErrorMessage = "Error al obtener información de planes";
                    return View(new UserPlanStatusDto());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View(new UserPlanStatusDto());
            }
        }

        // Vista para comprar plan
        [HttpGet]
        public async Task<IActionResult> Purchase()
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var response = await _httpClient.GetAsync($"{apiUrl}/PlanSimulation/my-plan-status");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var planStatus = JsonConvert.DeserializeObject<UserPlanStatusDto>(content);

                    if (!planStatus.CanPurchasePlan)
                    {
                        TempData["ErrorMessage"] = planStatus.ReasonCannotPurchase;
                        return RedirectToAction("Index");
                    }

                    return View(planStatus);
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al obtener información de planes";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Procesar compra de plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(int planId)
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var purchaseDto = new PurchasePlanDto { PlanId = planId };
                var json = JsonConvert.SerializeObject(purchaseDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiUrl}/PlanSimulation/purchase", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "¡Plan comprado exitosamente!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    TempData["ErrorMessage"] = errorResponse?.message ?? "Error al procesar la compra";
                    return RedirectToAction("Purchase");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Purchase");
            }
        }

        // Vista para cambiar plan
        [HttpGet]
        public async Task<IActionResult> Change()
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var response = await _httpClient.GetAsync($"{apiUrl}/PlanSimulation/my-plan-status");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var planStatus = JsonConvert.DeserializeObject<UserPlanStatusDto>(content);

                    if (!planStatus.HasPlan)
                    {
                        TempData["ErrorMessage"] = "No tienes un plan activo para cambiar";
                        return RedirectToAction("Index");
                    }

                    return View(planStatus);
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al obtener información de planes";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Procesar cambio de plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(int newPlanId)
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var changeDto = new ChangePlanDto { NewPlanId = newPlanId };
                var json = JsonConvert.SerializeObject(changeDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiUrl}/PlanSimulation/change", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "¡Plan cambiado exitosamente!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    TempData["ErrorMessage"] = errorResponse?.message ?? "Error al cambiar el plan";
                    return RedirectToAction("Change");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Change");
            }
        }

        // Vista para gestionar conexiones
        [HttpGet]
        public async Task<IActionResult> ManageConnections()
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var response = await _httpClient.GetAsync($"{apiUrl}/PlanSimulation/my-plan-status");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var planStatus = JsonConvert.DeserializeObject<UserPlanStatusDto>(content);

                    if (!planStatus.HasPlan)
                    {
                        TempData["ErrorMessage"] = "No tienes un plan activo";
                        return RedirectToAction("Index");
                    }

                    if (planStatus.CurrentSubscription.MaxConnections <= 1)
                    {
                        TempData["ErrorMessage"] = "Tu plan no permite conexiones adicionales";
                        return RedirectToAction("Index");
                    }

                    return View(planStatus);
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al obtener información de planes";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Agregar conexión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddConnection(int childUserId)
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var addDto = new AddConnectionDto { ChildUserId = childUserId };
                var json = JsonConvert.SerializeObject(addDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiUrl}/PlanSimulation/add-connection", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Usuario agregado exitosamente al plan";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    TempData["ErrorMessage"] = errorResponse?.message ?? "Error al agregar usuario";
                }

                return RedirectToAction("ManageConnections");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("ManageConnections");
            }
        }

        // Remover conexión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveConnection(int childUserId)
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var removeDto = new RemoveConnectionDto { ChildUserId = childUserId };
                var json = JsonConvert.SerializeObject(removeDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{apiUrl}/PlanSimulation/remove-connection", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Usuario removido exitosamente del plan";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    TempData["ErrorMessage"] = errorResponse?.message ?? "Error al remover usuario";
                }

                return RedirectToAction("ManageConnections");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("ManageConnections");
            }
        }

        // Cancelar suscripción
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel()
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var response = await _httpClient.PostAsync($"{apiUrl}/PlanSimulation/cancel", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Suscripción cancelada exitosamente";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    TempData["ErrorMessage"] = errorResponse?.message ?? "Error al cancelar suscripción";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Ver historial de suscripciones
        [HttpGet]
        public async Task<IActionResult> History()
        {
            try
            {
                SetAuthorizationHeader();
                var apiUrl = _configuration["ApiSettings:BaseUrl"];

                var response = await _httpClient.GetAsync($"{apiUrl}/PlanSimulation/history");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var history = JsonConvert.DeserializeObject<List<PlanSubscriptionDto>>(content);
                    return View(history);
                }
                else
                {
                    ViewBag.ErrorMessage = "Error al obtener historial de suscripciones";
                    return View(new List<PlanSubscriptionDto>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View(new List<PlanSubscriptionDto>());
            }
        }
    }
}