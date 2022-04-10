﻿using AspNetCore.JWTDemo.Configurations;
using AspNetCore.JWTDemo.Dtos;
using AspNetCore.JWTDemo.EntityFrameworkCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AspNetCore.JWTDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly JwtBearerSettings _jwtBearerSettings;
        public AccountController(UserManager<User> userManager, IOptions<JwtBearerSettings> jwtBearerSettingsOption)
        {
            _userManager = userManager;
            _jwtBearerSettings = jwtBearerSettingsOption.Value;
        }
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(UserRegisterDto registerDto)
        {
            var user = new User
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber
            };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                return Ok(user);
            }
            return BadRequest(result.Errors);
        }

        [HttpPost]
        [Route("auth")]
        public async Task<IActionResult> Authentication(UserAuthDto authDto)
        {
            User user;
            if (authDto.UserName != null)
            {
                user = await _userManager.FindByNameAsync(authDto.UserName);
            }
            else if (authDto.Email != null)
            {
                user = await _userManager.FindByEmailAsync(authDto.Email);
            }
            else
            {
                throw new InvalidOperationException("username and email can't be null or empty at the same time");
            }
            if (user == null)
            {
                return NotFound("user does not exists.");
            }
            return Ok(new { access_token = GenerateAccessToken(user) });
        }

        [HttpGet]
        [Route("currentuserinformation")]
        [Authorize]
        public async Task<IActionResult> CurrentUserInformation()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user != null)
            {
                return Ok(user);
            }
            return NotFound();
        }

        private string GenerateAccessToken(User user)
        {
            var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtBearerSettings.IssuerSigningKey));
            var credentials = new SigningCredentials(issuerSigningKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.Email,user.Email)
            };
            var token = new JwtSecurityToken(_jwtBearerSettings.Issuer, _jwtBearerSettings.Audience, claims, expires: DateTime.Now.AddHours(1), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}