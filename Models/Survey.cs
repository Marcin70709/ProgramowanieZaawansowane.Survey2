using System;
using System.ComponentModel.DataAnnotations;

namespace ProgramowanieZaawansowane.Survey.Models
{
    public class Survey
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tytuł ankiety")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Opis")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CreatedByUserId { get; set; }

        public ICollection<Option> Options { get; set; }
    }
}