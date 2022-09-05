using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Oauth_Identity.Configurations;
using Oauth_Identity.Integration;
using Oauth_Identity.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Oauth_Identity.Controllers
{
    [Route("api/auth")]
    public class AuthController : MainController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly TokenValidationParametersSettings _tokenValidationParametersSettings;

        //private IMessageBus _bus;

        public AuthController(UserManager<IdentityUser> userManager,
           IOptions<TokenValidationParametersSettings> tokenValidationParametersSettings,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _tokenValidationParametersSettings = tokenValidationParametersSettings.Value;
            _signInManager = signInManager;
            //_bus = bus;
        }

        [HttpPost("nova-conta")]
        public async Task<ActionResult> Registrar(UsuarioRegistro usuarioRegistro)
        {

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var user = new IdentityUser
            {
                UserName = usuarioRegistro.Email,
                Email = usuarioRegistro.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, usuarioRegistro.Senha);

            if (result.Succeeded)
            {

                // Integração Cadastro Jogador
                //var jogadorResult = await RegistrarJogador(usuarioRegistro);

                //if (!jogadorResult.ValidationResult.IsValid)
                //{
                //    await _userManager.DeleteAsync(user);
                //    return CustomResponse(jogadorResult.ValidationResult);
                //}

                return CustomResponse(await GerarJwt(usuarioRegistro.Email));
            }

            foreach (var error in result.Errors)
            {
                AdicionarErroProcessamento(error.Description);
            }

            return CustomResponse();

        }

        // Integration BUS
        //private async Task<ResponseMessage> RegistrarJogador(UsuarioRegistro usuarioRegistro)
        //{

        //    var user = await _userManager.FindByEmailAsync(usuarioRegistro.Email);
        //    //var jogadorRegistrado = new JogadorRegistradoIntegrationEvent(Guid.Parse(user.Id), usuarioRegistro.PeFavorito,
        //    //    usuarioRegistro.Altura, usuarioRegistro.Peso, usuarioRegistro.TamanhoChuteira, usuarioRegistro.TimeCoracao);

        //    try
        //    {
        //        //return await _bus.RequestAsync<JogadorRegistradoIntegrationEvent, ResponseMessage>(jogadorRegistrado);
        //    }
        //    catch (Exception)
        //    {
        //        await _userManager.DeleteAsync(user);
        //        throw;
        //    }

        //}

        [HttpPost("autenticar")]
        public async Task<ActionResult> Login(UsuarioLogin usuarioLogin)
        {

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var result = await _signInManager.PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha, false, true);

            if (result.Succeeded) return CustomResponse(await GerarJwt(usuarioLogin.Email));


            if (result.IsLockedOut)
            {
                AdicionarErroProcessamento("Usuário temporariamente bloqueado por tentativas inválidas");
                return CustomResponse();
            }

            AdicionarErroProcessamento("Usuário ou Senha incorretos");
            return CustomResponse();

        }

        private async Task<object> RegistrarCliente(UsuarioRegistro usuarioRegistro)
        {
            //var usuario = await _authenticationService.UserManager.FindByEmailAsync(usuarioRegistro.Email);

            //var usuarioRegistrado = new UsuarioRegistradoIntegrationEvent(
            //    Guid.Parse(usuario.Id), usuarioRegistro.Nome, usuarioRegistro.Email, usuarioRegistro.Cpf);

            //try
            //{
            //    return await _bus.RequestAsync<UsuarioRegistradoIntegrationEvent, ResponseMessage>(usuarioRegistrado);
            //}
            //catch
            //{
            //    await _authenticationService.UserManager.DeleteAsync(usuario);
            //    throw;
            //}

            return null;


        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromBody] string refreshToken)
        {
            //if (string.IsNullOrEmpty(refreshToken))
            //{
            //    AdicionarErroProcessamento("Refresh Token inválido");
            //    return CustomResponse();
            //}

            //var token = await _authenticationService.ObterRefreshToken(Guid.Parse(refreshToken));

            //if (token is null)
            //{
            //    AdicionarErroProcessamento("Refresh Token expirado");
            //    return CustomResponse();
            //}

            //return CustomResponse(await _authenticationService.GerarJwt(token.Username));

            return Ok();

        }

        private async Task<UsuarioRespostaLogin> GerarJwt(string email)
        {

            var user = await _userManager.FindByEmailAsync(email);
            var claims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));

            foreach (var role in userRoles) claims.Add(new Claim("role", role));

            var tokeHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_tokenValidationParametersSettings.IssuerSigningKey);
            var token = tokeHandler.CreateToken(new SecurityTokenDescriptor()
            {
                Issuer = _tokenValidationParametersSettings.ValidIssuer,
                Audience = _tokenValidationParametersSettings.ValidAudience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_tokenValidationParametersSettings.Expiration),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokeHandler.WriteToken(token);

            return new UsuarioRespostaLogin()
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(_tokenValidationParametersSettings.Expiration).TotalSeconds,
                UsuarioToken = new UsuarioToken()
                {
                    Id = user.Id,
                    Email = user.Email,
                    Claims = claims.Select(x => new UsuarioClaim { Type = x.Type, Value = x.Value })
                }
            };

        }

        private static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);


        

    }
}
