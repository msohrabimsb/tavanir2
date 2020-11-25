using System;
using System.Data.SqlClient;

namespace tavanir2.Models
{
    public interface IBaseRepository
    {
        void ExecuteCommand(Action<SqlConnection> action);
        TEntity ExecuteCommand<TEntity>(Func<SqlConnection, TEntity> func);
        bool ValidationToken();
    }
}
