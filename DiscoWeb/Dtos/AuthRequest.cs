using System.ComponentModel.DataAnnotations;

namespace DiscoWeb.Dtos;

public class AuthRequest
{
    [Required] public required string Hash { get; set; }
}