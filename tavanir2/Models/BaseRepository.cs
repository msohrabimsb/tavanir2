using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace tavanir2.Models
{
    public class BaseRepository : IBaseRepository
    {
        private readonly string _connectionString;
        private readonly IHttpContextAccessor contextAccessor;

        public BaseRepository(IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _connectionString = configuration.GetConnectionString("Conn");
            this.contextAccessor = contextAccessor;
        }

        public void ExecuteCommand(Action<SqlConnection> action)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                action(conn);
                conn.Close();
            }
        }

        public TEntity ExecuteCommand<TEntity>(Func<SqlConnection, TEntity> func)
        {
            TEntity res = default;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                res = func(conn);
                conn.Close();
            }
            return res;
        }

        public bool ValidationToken()
        {
            if (!Guid.TryParse(contextAccessor.HttpContext.Session.GetString("LoginToken"), out Guid token) || token == null)
            {
                contextAccessor.HttpContext.Session.SetString("LoginToken", string.Empty);
                return false;
            }
            if (!Guid.TryParse(contextAccessor.HttpContext.Session.GetString("CompanyId"), out Guid companyId) || companyId == null)
            {
                contextAccessor.HttpContext.Session.SetString("LoginToken", string.Empty);
                return false;
            }

            var res = ExecuteCommand(conn =>
                conn.Query<Guid>("SELECT [Token] FROM [TavanirStage].[Stage].[AuthorizationTokens] WHERE [Token] = @Token AND [CompanyId] = @CompanyId",
                    new { @Token = token, @CompanyId = companyId }).FirstOrDefault());

            if (res != null && res == token)
                return true;

            contextAccessor.HttpContext.Session.SetString("LoginToken", string.Empty);
            return false;
        }
    }
}
