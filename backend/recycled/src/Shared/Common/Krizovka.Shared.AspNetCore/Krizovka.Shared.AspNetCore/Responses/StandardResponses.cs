// =============================================================================
// StandardResponses — Standardizované API response typy
//
// Tyto record typy nahrazují anonymous JSON odpovědi (new { ... }) v controllerech.
// Používej je místo anonymních objektů pro RFC 7807 kompatibilitu a konzistenci.
//
// AI Agent Instrukce:
//   NIKDY nevracej Ok(new { ... }) z controlleru. Použij jeden z těchto response typů:
//   - IdResponse — pro create operace vracející ID
//   - MessageResponse — pro operace vracející textovou zprávu
//   - SuccessResponse — pro operace vracející prostý success/fail
//   - OperationResponse — pro operace vracející success + detail
//   Pokud žádný nevyhovuje, vytvoř vlastní response record v Application vrstvě.
// =============================================================================
namespace Krizovka.Shared.AspNetCore.Responses;

/// <summary>Response containing a single ID (e.g., created resource).</summary>
public record IdResponse(Guid Id);

/// <summary>Response containing a message string.</summary>
public record MessageResponse(string Message);

/// <summary>Response indicating operation success.</summary>
public record SuccessResponse(bool Success = true);

/// <summary>Response for operations with success flag and detail.</summary>
public record OperationResponse(bool Success, string? Detail = null);

/// <summary>Response containing status and a body payload.</summary>
public record StatusBodyResponse(string Status, object Body);
