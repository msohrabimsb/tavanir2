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
            if (!contextAccessor.HttpContext.Session.HasKey("CompanyId") || !Guid.TryParse(contextAccessor.HttpContext.Session.GetString("CompanyId"), out Guid companyId) || companyId == null || Equals(companyId, Guid.Empty))
            {
                return false;
            }

            Guid? res = ExecuteCommand(conn =>
                conn.Query<Guid>("SELECT [Id] FROM [TavanirStage].[Basic].[Companies] WHERE [Id] = @Id",
                    new { @Id = companyId })?.FirstOrDefault());

            if (res.HasValue && !Equals(res.Value, Guid.Empty) && res.Value == companyId)
                return true;

            contextAccessor.HttpContext.Session.Remove("CompanyId");
            return false;
        }
    }
}
