using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.API.Models;
using Core.API.Onion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Onion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnionController : Controller
    {
        ITorProcessService _torProcessService;

        public OnionController(IHostedService onionService)
        {
            _torProcessService = (ITorProcessService)onionService;
        }

        [HttpGet("hsdetails")]
        public async Task<HiddenServiceDetails> GetHiddenServiceDetails()
        {
            return await _torProcessService.GetHiddenServiceDetailsAsync();
        }

        [HttpPost("sign")]
        public async Task<SignedHashResponse> Sign([FromBody] byte[] hash)
        {
            return await _torProcessService.SignedHashAsync(hash);
        }
    }
}
