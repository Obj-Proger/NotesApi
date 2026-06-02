using Microsoft.AspNetCore.Mvc;
using NotesApi.Exceptions;
using System.Security.Claims;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Base controller providing shared utilities for authenticated endpoints.
    /// Centralises GetUserId() so it isn't duplicated across every controller.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Extracts the authenticated user's ID from JWT claims.
        /// </summary>
        /// <exception cref="UnauthorizedException">Thrown when the NameIdentifier claim is missing or not a valid Guid.</exception>
        protected Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var userId))
                return userId;

            throw new UnauthorizedException("Invalid token claims.");
        }
    }
}