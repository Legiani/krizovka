using Microsoft.AspNetCore.Authorization;
using Krizovka.Users.Application.Core.Search.Queries.SearchAccounts;
using Krizovka.Users.Application.Core.Search.DTOs;
using Krizovka.Users.Domain.Enums;
using Krizovka.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("search")]
[Authorize(AuthenticationSchemes = "Backoffice")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("accounts")]
    [RequirePermission("USERS", "READ")]
    public async Task<SearchPagedResponse> SearchAccounts(
        [FromQuery] string? q = null,
        [FromQuery] AccountType? type = null,
        [FromQuery] AccountStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _mediator.Send(new SearchAccountsQuery(q, type, status, page, pageSize));
        return result;
    }
}
