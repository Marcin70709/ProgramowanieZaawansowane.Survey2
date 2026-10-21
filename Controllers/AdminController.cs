using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ProgramowanieZaawansowane.Survey.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            // Administratora nie można zmieniać przez ten formularz.
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Forbid();

            // Dozwolone role, które administrator może przypisać.
            if (role != "Ankieter" && role != "Respondent")
                return BadRequest("Nieprawidłowa rola.");

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(
                user, currentRoles);

            if (!removeResult.Succeeded)
                return BadRequest("Nie udało się usunąć poprzedniej roli.");

            var addResult = await _userManager.AddToRoleAsync(user, role);

            if (!addResult.Succeeded)
            {
                // Przywróć poprzednie role, jeśli przypisanie się nie powiedzie.
                if (currentRoles.Count > 0)
                    await _userManager.AddToRolesAsync(user, currentRoles);

                return BadRequest("Nie udało się przypisać nowej roli.");
            }

            return RedirectToAction(nameof(Index));
        }
    
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteUser(string userId)
{
    var user = await _userManager.FindByIdAsync(userId);

    if (user == null)
        return NotFound();

    // Nie pozwalamy usunąć konta administratora.
    if (await _userManager.IsInRoleAsync(user, "Admin"))
        return Forbid();

    // Usuń głosy użytkownika, aby nie naruszyć relacji w bazie.
    var dbContext = HttpContext.RequestServices
        .GetRequiredService<ProgramowanieZaawansowane.Survey.Data.ApplicationDbContext>();

    var votes = dbContext.Votes.Where(v => v.UserId == userId);
    dbContext.Votes.RemoveRange(votes);
    await dbContext.SaveChangesAsync();

    var result = await _userManager.DeleteAsync(user);

    if (!result.Succeeded)
        return BadRequest("Nie udało się usunąć konta.");

    return RedirectToAction(nameof(Index));
}}
}