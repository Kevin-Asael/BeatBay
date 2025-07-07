using BeatBay.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace BeatBay.MVC.Controllers
{
    public class VAuthController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _apiBase;

        public VAuthController(IHttpClientFactory httpFactory, IConfiguration cfg)
        {
            _httpFactory = httpFactory;
            _apiBase = cfg["ApiSettings:BaseUrl"]!.TrimEnd('/');
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("JwtToken") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpFactory.CreateClient("BeatBay.API");
            var resp = await client.PostAsync("Auth/login",
                new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json"));

            var txt = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var err = JsonConvert.DeserializeObject<dynamic>(txt);
                ModelState.AddModelError("", (string?)err?.message ?? "Login failed");
                return View(model);
            }

            var dyn = JsonConvert.DeserializeObject<dynamic>(txt);
            if (dyn.requiresTwoFactor == true)
            {
                TempData["UserNameFor2FA"] = model.UserName;
                return RedirectToAction("TwoFactorSettings");
            }

            var auth = JsonConvert.DeserializeObject<AuthResponseDto>(txt)!;
            HttpContext.Session.SetString("JwtToken", auth.Token);
            HttpContext.Session.SetString("UserName", auth.User.UserName);

            // Opcional: cookie-auth
            var identity = new System.Security.Claims.ClaimsIdentity(
                CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new System.Security.Claims.Claim("userName", auth.User.UserName));
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorSettings()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (token == null) return RedirectToAction("Login");

            var client = _httpFactory.CreateClient("BeatBay.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync("Auth/2fa-status");
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo cargar 2FA status";
                return RedirectToAction("Login");
            }

            var dyn = JsonConvert.DeserializeObject<dynamic>(await resp.Content.ReadAsStringAsync())!;
            ViewBag.Is2FAEnabled = (bool)dyn.is2FAEnabled;
            ViewBag.RecoveryCodesLeft = (int)dyn.recoveryCodesLeft;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable2FA(string Password)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (token == null) return RedirectToAction("Login");

            var client = _httpFactory.CreateClient("BeatBay.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new Enable2FADto { Password = Password };
            var resp = await client.PostAsync("Auth/enable-2fa",
                new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json"));

            var txt = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var err = JsonConvert.DeserializeObject<dynamic>(txt);
                TempData["Error"] = (string?)err?.message ?? "Error al habilitar 2FA";
                return RedirectToAction("TwoFactorSettings");
            }

            var codes = JsonConvert.DeserializeObject<dynamic>(txt).recoveryCodes.ToObject<List<string>>();
            TempData["RecoveryCodes"] = JsonConvert.SerializeObject(codes);
            return RedirectToAction("RecoveryCodes");
        }

        [HttpGet]
        public IActionResult RecoveryCodes()
        {
            var json = TempData["RecoveryCodes"] as string ?? "[]";
            var codes = JsonConvert.DeserializeObject<List<string>>(json)!;
            ViewBag.RecoveryCodes = codes;
            return View();
        }
    }
}
