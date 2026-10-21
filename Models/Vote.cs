using Microsoft.AspNetCore.Identity;

namespace ProgramowanieZaawansowane.Survey.Models
{
    public class Vote
    {
        public int Id { get; set; }

        public int OptionId { get; set; }
        public Option Option { get; set; }

        // Respondent
        public string UserId { get; set; } = string.Empty;

        public IdentityUser User { get; set; } = null!;
    }
}