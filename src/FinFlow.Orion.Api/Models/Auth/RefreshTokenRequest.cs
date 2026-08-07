using System.ComponentModel.DataAnnotations;

namespace FinFlow.Orion.Api.Models.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}