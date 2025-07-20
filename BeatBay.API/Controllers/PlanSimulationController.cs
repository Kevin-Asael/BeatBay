using BeatBay.Data;
using BeatBay.DTOs;
using BeatBay.Model;
using BeatBay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BeatBay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanSimulationController : ControllerBase
    {
        private readonly BeatBayDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly PayPalService _payPalService;

        public PlanSimulationController(
            BeatBayDbContext context,
            UserManager<User> userManager,
            PayPalService payPalService)
        {
            _context = context;
            _userManager = userManager;
            _payPalService = payPalService;
        }

        // 1. Obtener estado del plan del usuario actual
        [HttpGet("my-plan-status")]
        public async Task<ActionResult<UserPlanStatusDto>> GetMyPlanStatus()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var canPurchase = !roles.Contains("Admin") && !roles.Contains("Artist");
            string reasonCannotPurchase = "";

            if (!canPurchase)
                reasonCannotPurchase = roles.Contains("Admin")
                    ? "Los administradores no pueden comprar planes"
                    : "Los artistas no pueden comprar planes";

            // Suscripción activa
            var activeSub = await _context.PlanSubscriptions
                .Include(ps => ps.Plan)
                .Include(ps => ps.UserConnections)
                .FirstOrDefaultAsync(ps =>
                    ps.UserId == userId &&
                    ps.IsActive &&
                    ps.EndDate > DateTime.UtcNow);

            if (activeSub != null && canPurchase)
            {
                canPurchase = false;
                reasonCannotPurchase = "Ya tienes una suscripción activa";
            }

            // ¿Es hijo de otro plan?
            var isChild = await _context.UserConnections
                .AnyAsync(uc => uc.ChildUserId == userId && uc.IsActive);

            if (isChild && canPurchase)
            {
                canPurchase = false;
                reasonCannotPurchase = "Ya estás conectado a un plan familiar/empresarial";
            }

            var availablePlans = new List<PlanDto>();
            if (canPurchase)
            {
                availablePlans = await _context.Plans
                    .Where(p => p.Name != "Free")
                    .Select(p => new PlanDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        PriceUSD = p.PriceUSD,
                        MaxConnections = p.MaxConnections,
                        UserCount = p.Users.Count
                    })
                    .ToListAsync();
            }

            var dto = new UserPlanStatusDto
            {
                HasPlan = activeSub != null,
                CanPurchasePlan = canPurchase,
                ReasonCannotPurchase = reasonCannotPurchase,
                AvailablePlans = availablePlans
            };

            if (activeSub != null)
            {
                dto.CurrentSubscription = new PlanSubscriptionDto
                {
                    Id = activeSub.Id,
                    UserId = activeSub.UserId,
                    UserName = activeSub.User?.UserName ?? "Unknown",
                    PlanId = activeSub.PlanId,
                    PlanName = activeSub.Plan.Name,
                    PriceUSD = activeSub.Plan.PriceUSD,
                    MaxConnections = activeSub.Plan.MaxConnections,
                    UsedConnections = activeSub.UserConnections.Count(uc => uc.IsActive) + 1,
                    StartDate = activeSub.StartDate,
                    EndDate = activeSub.EndDate,
                    IsActive = activeSub.IsActive,
                    ConnectedUsers = activeSub.UserConnections
                        .Where(uc => uc.IsActive)
                        .Select(uc => new UserDto
                        {
                            Id = uc.ChildUser.Id,
                            UserName = uc.ChildUser.UserName,
                            Email = uc.ChildUser.Email,
                            Name = uc.ChildUser.Name,
                            IsActive = uc.ChildUser.IsActive
                        })
                        .ToList()
                };
            }

            return Ok(dto);
        }

        // 2. Crear pago y redirigir a PayPal
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchasePlan(PurchasePlanDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") || roles.Contains("Artist"))
                return BadRequest(new { message = "Los administradores y artistas no pueden comprar planes" });

            var hasActive = await _context.PlanSubscriptions
                .AnyAsync(ps => ps.UserId == userId && ps.IsActive && ps.EndDate > DateTime.UtcNow);
            if (hasActive)
                return BadRequest(new { message = "Ya tienes una suscripción activa" });

            var isChildPlan = await _context.UserConnections
                .AnyAsync(uc => uc.ChildUserId == userId && uc.IsActive);
            if (isChildPlan)
                return BadRequest(new { message = "Ya estás conectado a un plan familiar/empresarial" });

            var plan = await _context.Plans.FindAsync(dto.PlanId);
            if (plan == null) return NotFound(new { message = "Plan no encontrado" });
            if (plan.Name == "Free")
                return BadRequest(new { message = "No puedes comprar el plan Free" });

            // Crear pago en PayPal
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var payment = _payPalService.CreatePayment(
                plan.Name,
                plan.PriceUSD,
                plan.Id,
                userId,
                baseUrl
            );

            // Guardar registro local pendiente
            var paymentRecord = new Payment
            {
                UserId = userId,
                PlanId = plan.Id,
                Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                Amount = plan.PriceUSD
            };
            _context.Payments.Add(paymentRecord);
            await _context.SaveChangesAsync();

            var approvalUrl = payment.links
                .First(l => l.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))
                .href;

            return Ok(new
            {
                paymentId = payment.id,
                localPaymentId = paymentRecord.Id,
                approvalUrl
            });
        }

        // 3. Ejecutar pago tras aprobación en PayPal
        [HttpGet("execute-payment")]
        [AllowAnonymous]
        public async Task<IActionResult> ExecutePayment([FromQuery] string paymentId, [FromQuery] string PayerID, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(PayerID))
                return BadRequest(new { message = "Parámetros de pago inválidos" });

            var executed = _payPalService.ExecutePayment(paymentId, PayerID);
            if (executed.state.ToLower() != "approved")
                return BadRequest(new { message = "El pago no fue aprobado" });

            // Leer custom = "planId|userId"
            var parts = executed.transactions[0].custom?.Split('|');
            if (parts == null || parts.Length != 2)
                return BadRequest(new { message = "Datos de pago corruptos" });

            int planId = int.Parse(parts[0]), userId = int.Parse(parts[1]);

            var record = await _context.Payments
                .Where(p => p.UserId == userId
                         && p.PlanId == planId
                         && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefaultAsync();
            if (record == null)
                return BadRequest(new { message = "No se encontró el pago pendiente" });

            record.Status = PaymentStatus.Completed;

            var subscription = new PlanSubscription
            {
                UserId = userId,
                PlanId = planId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                AmountPaid = record.Amount,
                IsActive = true
            };
            _context.PlanSubscriptions.Add(subscription);

            await _context.SaveChangesAsync();

            // Redirigir al cliente a Plans/Index
            return Redirect("https://localhost:7194/Plans/Index");
        }

        // 4. Cancelar pago (usuario abandona PayPal)
        [HttpGet("cancel-payment")]
        [AllowAnonymous]
        public IActionResult CancelPayment([FromQuery] string token)
        {
            // (Opcional) marcar el pago como cancelado en BD
            return Ok(new { message = "Pago cancelado por el usuario" });
        }

        // 5. Consultar estado del pago en PayPal
        [HttpGet("payment-status/{paymentId}")]
        public IActionResult GetPaymentStatus(string paymentId)
        {
            try
            {
                var payment = _payPalService.GetPayment(paymentId);
                return Ok(new
                {
                    id = payment.id,
                    state = payment.state,
                    create_time = payment.create_time,
                    update_time = payment.update_time
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al obtener el estado del pago", error = ex.Message });
            }
        }

        // 6. Cambiar plan
        [HttpPost("change")]
        public async Task<IActionResult> ChangePlan(ChangePlanDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var active = await _context.PlanSubscriptions
                .Include(ps => ps.Plan)
                .Include(ps => ps.UserConnections)
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.IsActive && ps.EndDate > DateTime.UtcNow);
            if (active == null)
                return BadRequest(new { message = "No tienes una suscripción activa" });

            var newPlan = await _context.Plans.FindAsync(dto.NewPlanId);
            if (newPlan == null) return NotFound(new { message = "Plan no encontrado" });
            if (newPlan.Name == "Free")
                return BadRequest(new { message = "No puedes cambiar al plan Free" });
            if (active.PlanId == dto.NewPlanId)
                return BadRequest(new { message = "Ya tienes este plan activo" });

            var currentConns = active.UserConnections.Count(uc => uc.IsActive);
            if (currentConns > newPlan.MaxConnections)
                return BadRequest(new
                {
                    message = $"El nuevo plan solo permite {newPlan.MaxConnections} conexiones. Actualmente tienes {currentConns}"
                });

            active.IsActive = false;

            var migrated = new PlanSubscription
            {
                UserId = userId,
                PlanId = dto.NewPlanId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                AmountPaid = newPlan.PriceUSD,
                IsActive = true
            };
            _context.PlanSubscriptions.Add(migrated);

            // Migrar conexiones
            foreach (var oldConn in active.UserConnections.Where(uc => uc.IsActive).Take(newPlan.MaxConnections))
            {
                oldConn.IsActive = false;
                _context.UserConnections.Add(new UserConnection
                {
                    ParentSubscriptionId = migrated.Id,
                    ChildUserId = oldConn.ChildUserId,
                    ConnectedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            // Registrar pago simulado
            _context.Payments.Add(new Payment
            {
                UserId = userId,
                PlanId = dto.NewPlanId,
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow,
                Amount = newPlan.PriceUSD
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Plan cambiado exitosamente", newSubscriptionId = migrated.Id });
        }

        // 7. Agregar conexión de usuario hijo
        [HttpPost("add-connection")]
        public async Task<IActionResult> AddConnection(AddConnectionDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var active = await _context.PlanSubscriptions
                .Include(ps => ps.Plan)
                .Include(ps => ps.UserConnections)
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.IsActive && ps.EndDate > DateTime.UtcNow);
            if (active == null)
                return BadRequest(new { message = "No tienes una suscripción activa" });

            var used = active.UserConnections.Count(uc => uc.IsActive);
            if (used >= active.Plan.MaxConnections)
                return BadRequest(new { message = $"Has alcanzado el límite de conexiones ({active.Plan.MaxConnections})" });

            var child = await _userManager.FindByIdAsync(dto.ChildUserId.ToString());
            if (child == null) return NotFound(new { message = "Usuario no encontrado" });

            var childRoles = await _userManager.GetRolesAsync(child);
            if (childRoles.Contains("Admin") || childRoles.Contains("Artist"))
                return BadRequest(new { message = "No puedes agregar administradores o artistas como conexiones" });

            var childHasSub = await _context.PlanSubscriptions
                .AnyAsync(ps => ps.UserId == dto.ChildUserId && ps.IsActive && ps.EndDate > DateTime.UtcNow);
            if (childHasSub)
                return BadRequest(new { message = "El usuario ya tiene su propio plan" });

            var already = await _context.UserConnections
                .AnyAsync(uc => uc.ChildUserId == dto.ChildUserId && uc.IsActive);
            if (already)
                return BadRequest(new { message = "El usuario ya está conectado a un plan" });

            _context.UserConnections.Add(new UserConnection
            {
                ParentSubscriptionId = active.Id,
                ChildUserId = dto.ChildUserId,
                ConnectedAt = DateTime.UtcNow,
                IsActive = true
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario agregado exitosamente al plan" });
        }

        // 8. Remover conexión
        [HttpPost("remove-connection")]
        public async Task<IActionResult> RemoveConnection(RemoveConnectionDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var active = await _context.PlanSubscriptions
                .Include(ps => ps.UserConnections)
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.IsActive && ps.EndDate > DateTime.UtcNow);
            if (active == null)
                return BadRequest(new { message = "No tienes una suscripción activa" });

            var conn = active.UserConnections
                .FirstOrDefault(uc => uc.ChildUserId == dto.ChildUserId && uc.IsActive);
            if (conn == null)
                return NotFound(new { message = "Conexión no encontrada" });

            conn.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuario removido del plan exitosamente" });
        }

        // 9. Cancelar suscripción
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelSubscription()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var active = await _context.PlanSubscriptions
                .Include(ps => ps.UserConnections)
                .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.IsActive && ps.EndDate > DateTime.UtcNow);
            if (active == null)
                return BadRequest(new { message = "No tienes una suscripción activa" });

            active.IsActive = false;
            foreach (var c in active.UserConnections)
                c.IsActive = false;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Suscripción cancelada exitosamente" });
        }

        // 10. Historial de suscripciones
        [HttpGet("history")]
        public async Task<ActionResult<List<PlanSubscriptionDto>>> GetSubscriptionHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var list = await _context.PlanSubscriptions
                .Include(ps => ps.Plan)
                .Include(ps => ps.User)
                .Include(ps => ps.UserConnections)
                    .ThenInclude(uc => uc.ChildUser)
                .Where(ps => ps.UserId == userId)
                .OrderByDescending(ps => ps.CreatedAt)
                .Select(ps => new PlanSubscriptionDto
                {
                    Id = ps.Id,
                    UserId = ps.UserId,
                    UserName = ps.User.UserName,
                    PlanId = ps.PlanId,
                    PlanName = ps.Plan.Name,
                    PriceUSD = ps.Plan.PriceUSD,
                    MaxConnections = ps.Plan.MaxConnections,
                    UsedConnections = ps.UserConnections.Count(uc => uc.IsActive) + 1,
                    StartDate = ps.StartDate,
                    EndDate = ps.EndDate,
                    IsActive = ps.IsActive,
                    ConnectedUsers = ps.UserConnections
                        .Where(uc => uc.IsActive)
                        .Select(uc => new UserDto
                        {
                            Id = uc.ChildUser.Id,
                            UserName = uc.ChildUser.UserName,
                            Email = uc.ChildUser.Email,
                            Name = uc.ChildUser.Name,
                            IsActive = uc.ChildUser.IsActive
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}
