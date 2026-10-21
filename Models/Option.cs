using System.ComponentModel.DataAnnotations;

namespace ProgramowanieZaawansowane.Survey.Models
{
    public class Option
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Treść odpowiedzi")]
        public string Text { get; set; } = string.Empty;

        // Relacja do Survey
        public int SurveyId { get; set; }
        public Survey Survey { get; set; }

        public ICollection<Vote> Votes { get; set; }
    }
}