using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using tavanir2.Models;

namespace tavanir2.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ValidationTokenController : ControllerBase
    {
        private readonly IBaseRepository baseRepository;

        public ValidationTokenController(IBaseRepository baseRepository)
        {
            this.baseRepository = baseRepository;
        }

        [HttpGet]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Validation([FromRoute] string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    Guid companyId = baseRepository.ExecuteCommand(conn =>
                        conn.Query<Guid>("SELECT [Id] FROM [TavanirStage].[Basic].[Companies] WHERE CONVERT(VARCHAR(36), HashBytes('MD5', CONVERT(VARCHAR(36), CONVERT(VARCHAR(36), HashBytes('MD5', CONVERT(VARCHAR(36), [Id], 2)), 2), 2)), 2) = @Id AND [Enabled] = '1'",
                        new { @Id = id }).FirstOrDefault());

                    if (companyId != null && !Equals(companyId, Guid.Empty))
                    {
                        return Ok(companyId.ToString());
                    }

                    return Unauthorized();
                }
                catch (Exception)
                {
                    return Unauthorized();
                }
            }
            else
                return BadRequest();
        }
    }
}
