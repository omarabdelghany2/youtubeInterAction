using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SignalRGame.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SignalRGame.Models;


using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System;
using SignalRGame.Models; // Adjust to your namespace
using SignalRGame.Data;   // Your DbContext namespace
using BCrypt.Net;
using SignalRGame;  // or SignalRGame.Models if your context is there
using SignalRGame.Data;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{


    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public TestController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [Authorize]
    [HttpGet("secure")]
    public IActionResult SecurePing()
    {
        return Ok("Authorized!");
    }


    [HttpGet("public")]
    public IActionResult PublicPing()
    {
        return Ok("No auth needed.");
    }
}
