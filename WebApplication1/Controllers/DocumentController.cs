using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DMS.DocManage.WebApi.Controllers;

[ApiController]
[Route("api/document")]
public class DocumentController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Roles = "reader, writer")]
    //[Authorize(Policy = "AdminOrUser")]
    public IActionResult GetDocument(int id)
    {
        // This user can read
        return Ok($"Document content for #{id}");
    }

    [HttpPost("Create")]
    [Authorize(Roles = "writer")] // Only writer can create or edit
    public IActionResult CreateDocument()
    {
        // 403 if user is only 'reader'
        return Ok("Document created");
    }

    [HttpPost("Share")]
    [Authorize(Roles = "writer")] // Only writer can share or edit
    //[Authorize(Policy = "AdminOrUser")]
    public IActionResult ShareDocument(int id)
    {
        // 403 if user is only 'reader'
        return Ok("Document Shared");
    }

    [HttpGet("service2service")]
    [Authorize]
    public IActionResult Service2Service()
    {
        return Ok("Service-to-Service Authentication successful.");
    }
}