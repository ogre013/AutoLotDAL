using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AutoLotConnectedLayer 
{
    public sealed class InventoryDAL
    {
        private SqlConnection sqlCn;

        public InventoryDAL() {
            sqlCn = null;        
        }

        public void OpenConnection(string connectionString) {
            sqlCn = new SqlConnection();
            sqlCn.ConnectionString = connectionString;
            sqlCn.Open();
        }

        public void CloseConnection() {
            if (sqlCn != null)
                sqlCn.Close();
        }

        public void InsertAuto(string color, string make, string petName) {
            string sql = String.Format("insert into inventory (Make, Color, PetName) Values (@Make, @Color, @PetName)");
            using (SqlCommand sqlcmd = new SqlCommand(sql, sqlCn)) {
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@Make";
                param.SqlDbType = SqlDbType.VarChar;
                param.Value = make;
                sqlcmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@Color";
                param.SqlDbType = SqlDbType.VarChar;
                param.Value = color;
                sqlcmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@PetName";
                param.SqlDbType = SqlDbType.VarChar;
                param.Value = petName;
                sqlcmd.Parameters.Add(param);

                sqlcmd.ExecuteNonQuery();
            }
        }

        public void InsertAuto(NewCar car) {
            string sql = String.Format("insert into inventory (Make, Color, PetName) Values ('{0}','{1}','{2}')", car.Make, car.Color, car.PetName);
            using (SqlCommand sqlcmd = new SqlCommand(sql, sqlCn)) {
                sqlcmd.ExecuteNonQuery();
            }
        }

        public void DeleteCar(int CarId) {
            string sql = String.Format("delete from Inventory where CarId = {0}", CarId);
            using (SqlCommand sqlcmd = new SqlCommand(sql, sqlCn)) {
                try {
                    sqlcmd.ExecuteNonQuery();
                }
                catch(SqlException ex) {
                    throw new Exception("Sorry! That cat is on order!", ex);
                }
            }
        }

        public void UpdateCarPetName(int CarId, string PetName) {
            string sql = String.Format("update Inventory set PetName = '{0}' where CarId = {1}", PetName, CarId);
            using (SqlCommand sqlcmd = new SqlCommand(sql, sqlCn)) {
                sqlcmd.ExecuteNonQuery();
            }
        }

        public List<NewCar> GetAllInventoryAsList() {
            List<NewCar> Cars = new List<NewCar>();
            string sql = String.Format("select * from Inventory i with(nolock)");
            using (SqlCommand sqlcmd = new SqlCommand(sql, sqlCn)) {
                SqlDataReader sdr = sqlcmd.ExecuteReader();
                while(sdr.Read()) {
                    Cars.Add(new NewCar() {
                        CarId = (int)sdr["CarId"],
                        Make = (string)sdr["Make"],
                        Color = (string)sdr["Color"],
                        PetName = (string)sdr["PetName"]
                    });
                }
                sdr.Close();
            }
            return Cars;
        }

        public string LookUpPetName(int CarId) {
            string carPetName = string.Empty;
            using (SqlCommand sc = new SqlCommand("GetPetName", sqlCn)) {
                sc.CommandType = CommandType.StoredProcedure;

                SqlParameter sp = new SqlParameter();
                sp.ParameterName = "@CarID";
                sp.SqlDbType = SqlDbType.Int;
                sp.Value = CarId;
                sp.Direction = ParameterDirection.Input;
                sc.Parameters.Add(sp);

                sp = new SqlParameter();
                sp.ParameterName = "@PetName";
                sp.SqlDbType = SqlDbType.VarChar;
                sp.Size = 10;
                sp.Direction = ParameterDirection.Output;
                sc.Parameters.Add(sp);

                sc.ExecuteNonQuery();

                carPetName = (string)sc.Parameters ["@PetName"].Value;
            }

            return carPetName;
        }

        public void ProcessCreditRisk(bool throwEx, int custID) {
            string fName = string.Empty;
            string lName = string.Empty;

            SqlCommand cmdSelect = new SqlCommand("SELECT * FROM Customers AS c WITH(NOLOCK) WHERE c.CustID = @CustID", sqlCn);
            cmdSelect.Parameters.Add("@CustID", SqlDbType.Int);
            cmdSelect.Parameters ["@CustID"].Value = custID;

            using (SqlDataReader dr = cmdSelect.ExecuteReader()) {
                if (dr.HasRows) {
                    dr.Read();
                    fName = (string)dr ["FirstName"];
                    lName = (string)dr ["Lastname"];
                }
                else
                    return;
            }

            SqlCommand cmdRemove = new SqlCommand(
                "DELETE FROM Customers WHERE CustID = @CustID", sqlCn);
            cmdRemove.Parameters.Add("@CustID", SqlDbType.Int);
            cmdRemove.Parameters ["@CustID"].Value = custID;

            SqlCommand cmdInsert = new SqlCommand(
                @"
                    INSERT INTO CreditRisks
                    (
                        CustID,
	                    FirstName,
	                    LastName
                    )
                    VALUES
                    (
                        @CustID,
                        @FirstName,
                        @LastName
                    )"
            , sqlCn);

            cmdInsert.Parameters.Add("@CustID", SqlDbType.Int);
            cmdInsert.Parameters ["@CustID"].Value = custID;
            cmdInsert.Parameters.Add("@FirstName", SqlDbType.VarChar);
            cmdInsert.Parameters ["@FirstName"].Value = fName;
            cmdInsert.Parameters.Add("@LastName", SqlDbType.VarChar);
            cmdInsert.Parameters ["@LastName"].Value = lName;

            SqlTransaction tx = null;
            try {
                tx = sqlCn.BeginTransaction();

                cmdInsert.Transaction = tx;
                cmdRemove.Transaction = tx;

                cmdInsert.ExecuteNonQuery();
                cmdRemove.ExecuteNonQuery();

                if (throwEx)
                    throw new Exception("Sorry! Database error! Tx failed.....");
                tx.Commit();
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
                tx.Rollback();
            }
        }
    }
}
