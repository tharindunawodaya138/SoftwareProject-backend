﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Govimithuro.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Govimithuro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    { 
        
        ////////////////////////////////////////////////

        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly IConfigurationSection _jwtSettings;

        public AccountsController(IMapper mapper, UserManager<User> userManager, IConfiguration configuration)
        {
            _mapper = mapper;
            _userManager = userManager;
            _jwtSettings = configuration.GetSection("JwtSettings");
        }

        /////////// <testing authorization for different roles>
        
        [Authorize(Roles = "Buyer")]
        [HttpGet("testBuyer")]
        public String TestBuyer()
        {    return "Hello Buyer";      }

        [Authorize(Roles = "Seller")]
        [HttpGet("testSeller")]
        public String TestSeller()
        { return "Hello Seller"; }

        [Authorize(Roles = "Administrator")]
        [HttpGet("testAdmin")]
        public String TestAdmin()
        { return "Hello Admin"; }

        ///////// </test>



        // this is for customer registration
        [HttpPost("Customer")]
        public async Task<ActionResult> Customer(UserRegistrationModel userModel)
        {
            var user = _mapper.Map<User>(userModel);
            var result = await _userManager.CreateAsync(user, userModel.Password);
            if (!result.Succeeded)
            {
                return Ok(result.Errors);
            }
            await _userManager.AddToRoleAsync(user, "Buyer");

            return StatusCode(201);
        }


        // this is for farmer registration
        [HttpPost("Farmer")]
        public async Task<ActionResult> Farmer(UserRegistrationModel userModel)
        {
            var user = _mapper.Map<User>(userModel);
            var result = await _userManager.CreateAsync(user, userModel.Password);
            if (!result.Succeeded)
            {
                return Ok(result.Errors);
            }
            await _userManager.AddToRoleAsync(user, "Seller");

            return StatusCode(201);
        }



        // this is for Administrator registration
        [HttpPost("Administrator")]
        public async Task<ActionResult> Administrator(UserRegistrationModel userModel)
        {
            var user = _mapper.Map<User>(userModel);
            var result = await _userManager.CreateAsync(user, userModel.Password);
            if (!result.Succeeded)
            {
                return Ok(result.Errors);
            }
            await _userManager.AddToRoleAsync(user, "Administrator");

            return StatusCode(201);
        }


        // All the login activities are done through this. Token will be issued depending on the role
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginModel userModel)
        {
            var user = await _userManager.FindByEmailAsync(userModel.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, userModel.Password))
            {
                var signingCredentials = GetSigningCredentials();
                var claims = GetClaims(user);
                var tokenOptions = GenerateTokenOptions(signingCredentials, await claims);
                var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
                
                return Ok(new { UserDetail = user,  Token = token });
            }
            return Unauthorized("Invalid Authentication");

        }


        // support functions 
        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.GetSection("securityKey").Value);
            var secret = new SymmetricSecurityKey(key);

            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
                issuer: _jwtSettings.GetSection("validIssuer").Value,
                audience: _jwtSettings.GetSection("validAudience").Value,
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings.GetSection("expiryInMinutes").Value)),
                signingCredentials: signingCredentials);

            return tokenOptions;
        }

        private async Task<List<Claim>> GetClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email)
            };
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            return claims;
        }


        ////////////////////////////////////////// </summary>

    }
}
