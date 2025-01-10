using System.Data.Entity;

namespace ClassModel
{
    public class ModelOracleConfig : DbConfiguration
    {
        public ModelOracleConfig()
        {
            SetDefaultConnectionFactory(new Oracle.ManagedDataAccess.EntityFramework.OracleConnectionFactory());

            SetProviderServices("Oracle.ManagedDataAccess.Client", Oracle.ManagedDataAccess.EntityFramework.EFOracleProviderServices.Instance);
        }
    }

    [DbConfigurationType(typeof(ModelOracleConfig))]
    public partial class ModelOracle : DbContext
    {
        public string DefaultSchema { get; set; }

        public ModelOracle(string Connection, string Schema) : base(Connection)
        {
            DefaultSchema = Schema;
        }
    }
}
