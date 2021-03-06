using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models;

namespace WebApi.Services
{
    public interface IUserService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model);
        IEnumerable<User> GetAll();
        User GetSingle(int id);
    }

    public class UserService : IUserService
    {
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private List<User> _users = new List<User>
        { 
            new User { Id = 1, FirstName = "Test", LastName = "User", UserRole="User", Username = "test", Password = "1000.e1lXDDlDs99MvPmgauNoGQ==.batfYRhIg13vvhh0E9ZLD9JkhtN5dyjAcOPqQ3179OE=" }, 
            new User { Id = 2, FirstName = "Test2", LastName = "Admin", UserRole="Admin", Username = "admin", Password = "1000.V2xnyob1sFppaDlaRQWQAA==.LfKoRBJUsN7n4HLK9e/FdmBPiJhgh45lbj8CpjsIK8U=" }
        };

        private readonly AppSettings _appSettings;
        private readonly IPasswordHasher passwordHasher;

        public UserService(IOptions<AppSettings> appSettings, IPasswordHasher passwordHasher)
        {
            _appSettings = appSettings.Value;
            this.passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model)
        {
            //var pw1 = this.passwordHasher.Hash("test");
            //var pw2 = this.passwordHasher.Hash("asdf");

            var user = _users.SingleOrDefault(x => x.Username == model.Username && passwordHasher.Check(x.Password, model.Password).Verified);

            // return null if user not found
            if (user == null) return null;

            // authentication successful so generate jwt token
            var token = generateJwtToken(user);

            return new AuthenticateResponse(user, token);
        }

        public IEnumerable<User> GetAll()
        {
            return _users;
        }

        // helper methods

        private string generateJwtToken(User user)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.UserRole.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public User GetSingle(int id)
        {
            return _users.First(x => x.Id == id);
        }
    }
}