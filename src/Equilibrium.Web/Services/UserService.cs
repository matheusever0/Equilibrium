using Equilibrium.Web.Interfaces;
using Equilibrium.Web.Models.Generics;
using Equilibrium.Web.Models.Login;
using Equilibrium.Web.Models.User;

namespace Equilibrium.Web.Services
{
    public class UserService : IUserService
    {
        private readonly IApiService _apiService;
        private readonly ILogger<UserService> _logger;

        public UserService(IApiService apiService, ILogger<UserService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<LoginResponseModel> LoginAsync(LoginModel model)
        {
            try
            {
                var response = await _apiService.PostAsync<LoginResponseModel>("/api/Auth/login", model);

                if (response == null)
                {
                    _logger.LogWarning($"Login falhou para {model.Username} - Resposta nula");
                    throw new Exception("N�o foi poss�vel autenticar o usu�rio");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Erro de comunica��o com a API durante login de {model.Username}");
                throw new Exception("N�o foi poss�vel conectar ao servidor. Verifique sua conex�o.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro geral durante o login de {model.Username}");
                throw;
            }
        }

        public async Task<IEnumerable<UserModel>> GetAllUsersAsync(string token)
        {
            return await _apiService.GetAsync<IEnumerable<UserModel>>("/api/Users", token);
        }

        public async Task<UserModel> GetUserByIdAsync(string id, string token)
        {
            return await _apiService.GetAsync<UserModel>($"/api/Users/{id}", token);
        }

        public async Task<UserModel> CreateUserAsync(CreateUserModel model, string token)
        {
            return await _apiService.PostAsync<UserModel>("/api/Users", model, token);
        }

        public async Task<UserModel> UpdateUserAsync(string id, UpdateUserModel model, string token)
        {
            return await _apiService.PutAsync<UserModel>($"/api/Users/{id}", model, token);
        }

        public async Task DeleteUserAsync(string id, string token)
        {
            await _apiService.DeleteAsync($"/api/Users/{id}", token);
        }
    }
}
