using Explore.Application.DTOs.StorageObject;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Explore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageObjectsController : ControllerBase
    {

        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<StorageObjectsController> _logger;

        public StorageObjectsController(IMediator mediator, IHttpContextAccessor httpContextAccessor, ILogger<StorageObjectsController> logger)
        {
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // GET: api/<StorageObjectsController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<StorageObjectsController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<StorageObjectsController>
        [HttpPost("generate-upload-url")]
        public async Task<IActionResult> GetUploadUrl([FromBody] UploadRequestDto request)
        {
            var command = new GenerateUploadUrlCommand
            {
                FileName = request.FileName,
                ContentType = request.ContentType
            };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        // PUT api/<StorageObjectsController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<StorageObjectsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
