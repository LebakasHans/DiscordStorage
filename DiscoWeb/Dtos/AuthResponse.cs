using System.ComponentModel.DataAnnotations;

namespace DiscoWeb.Dtos;

public class AuthResponse
{
    [Required] public required string Token { get; set; }
}