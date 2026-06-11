using System.ComponentModel.DataAnnotations;

namespace BAD_PROJECT_PWMANAGER.ViewModels
{
    public class PasswordGeneratorViewModel
    {
        [Display(Name = "Lengte")]
        [Range(8, 128, ErrorMessage = "De lengte moet tussen 8 en 128 tekens liggen.")]
        public int Length { get; set; } = 16;

        [Display(Name = "Hoofdletters")]
        public bool IncludeUppercase { get; set; } = true;

        [Display(Name = "Kleine letters")]
        public bool IncludeLowercase { get; set; } = true;

        [Display(Name = "Cijfers")]
        public bool IncludeNumbers { get; set; } = true;

        [Display(Name = "Speciale tekens")]
        public bool IncludeSymbols { get; set; } = true;

        [Display(Name = "Vermijd gelijkaardige tekens")]
        public bool ExcludeSimilarCharacters { get; set; }

        [Display(Name = "Vermijd verwarrende symbolen")]
        public bool ExcludeAmbiguousSymbols { get; set; }

        public string? GeneratedPassword { get; set; }
    }
}
