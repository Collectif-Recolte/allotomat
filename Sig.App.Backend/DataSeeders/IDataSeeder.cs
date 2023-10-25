using System.Threading.Tasks;

namespace Sig.App.Backend.DataSeeders;

public interface IDataSeeder
{
    Task Seed();
}