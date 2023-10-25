using System.ComponentModel.DataAnnotations.Schema;

namespace Sig.App.Backend.DbModel
{
    public class SqlDateAttribute : ColumnAttribute
    {
        public SqlDateAttribute()
        {
            TypeName = "date";
        }
    }
}
