using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProgramowanieZaawansowane.Survey.Data;
using ProgramowanieZaawansowane.Survey.Models;

namespace ProgramowanieZaawansowane.Survey.Controllers
{
    [Authorize]
    public class SurveyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public SurveyController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ===============================
        // LISTA ANKIET (wszyscy zalogowani)
        // ===============================
        public async Task<IActionResult> Index()
        {
            var surveys = await _context.Surveys.ToListAsync();
            return View(surveys);
        }

        // ===============================
        // TWORZENIE ANKIETY (TYLKO ANKIETER)
        // ===============================
        [Authorize(Roles = "Ankieter")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Ankieter")]
        public async Task<IActionResult> Create(Models.Survey survey, List<string> options)
        {
            // usuń puste opcje
            options = options?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

            if (options == null || options.Count < 2)
            {
                ModelState.AddModelError("", "Ankieta musi mieć przynajmniej 2 opcje odpowiedzi.");
                return View(survey);
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    await _signInManager.SignOutAsync();
                    return RedirectToAction("Index", "Home");
                }

                survey.CreatedByUserId = user.Id;

                _context.Surveys.Add(survey);
                await _context.SaveChangesAsync();

                foreach (var optionText in options)
                {
                    var option = new Option
                    {
                        Text = optionText,
                        SurveyId = survey.Id
                    };

                    _context.Options.Add(option);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(survey);
        }

        // ===============================
        // SZCZEGÓŁY ANKIETY (GŁOSOWANIE)
        // ===============================
        public async Task<IActionResult> Details(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
                return NotFound();

          
            if (survey.Options == null || !survey.Options.Any())
            {
                TempData["Error"] = "Ta ankieta nie ma jeszcze żadnych opcji odpowiedzi.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);

            bool alreadyVoted = false;

            if (user != null)
            {
                alreadyVoted = survey.Options
                    .SelectMany(o => o.Votes)
                    .Any(v => v.UserId == user.Id);
            }

            ViewBag.AlreadyVoted = alreadyVoted;

            return View(survey);
        }
        // ===============================
        // GŁOSOWANIE (TYLKO RESPONDENT)
        // ===============================
        [HttpPost]
        [Authorize(Roles = "Respondent")]
        public async Task<IActionResult> Vote(int optionId, int surveyId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Index", "Home");

            // Sprawdzenie czy już głosował
            var alreadyVoted = await _context.Votes
                .Include(v => v.Option)
                .AnyAsync(v => v.UserId == user.Id &&
                               v.Option.SurveyId == surveyId);

            if (alreadyVoted)
            {
                TempData["Error"] = "Już zagłosowałeś w tej ankiecie.";
                return RedirectToAction("Details", new { id = surveyId });
            }

            var vote = new Vote
            {
                OptionId = optionId,
                UserId = user.Id
            };

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            return RedirectToAction("Results", new { id = surveyId });
        }
        [Authorize(Roles = "Ankieter")]
        public async Task<IActionResult> Votes(int id)
        {
            var survey = await _context.Surveys
      .Include(s => s.Options)
          .ThenInclude(o => o.Votes)
              .ThenInclude(v => v.User)
      .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
                return NotFound();

            return View(survey);
        }
        // ===============================
        // WYNIKI (wszyscy zalogowani)
        // ===============================
        public async Task<IActionResult> Results(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
                return NotFound();

            return View(survey);
        }
        [Authorize(Roles = "Ankieter")]
        public async Task<IActionResult> Delete(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // zabezpieczenie – może usuwać tylko swoją ankietę
            if (survey.CreatedByUserId != user!.Id)
                return Forbid();

            return View(survey);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Ankieter")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Options)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (survey.CreatedByUserId != user!.Id)
                return Forbid();

            _context.Surveys.Remove(survey);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}