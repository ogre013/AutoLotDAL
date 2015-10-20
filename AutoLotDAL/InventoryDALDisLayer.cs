using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace AutoLotDisconnectedLayer {
    public class InventoryDALDisLayer {

        private string conStr;
        private SqlDataAdapter dAdapt;

        public InventoryDALDisLayer(string connectionString) {
            conStr = connectionString;
            ConfigureAdapter();
        }

        private void ConfigureAdapter() {
            dAdapt = new SqlDataAdapter("select * from Inventory i with(nolock)", conStr);
            SqlCommandBuilder builder = new SqlCommandBuilder(dAdapt);
        }

        public DataTable GetAllInventory() {
            DataTable inv = new DataTable("Inventory");
            dAdapt.Fill(inv);
            return inv;
        }

        public void UpdateInventory(DataTable modifiedInventory) {
            dAdapt.Update(modifiedInventory);
        }
    }
}
