using MySql.Data.MySqlClient;
using SendEmailViaSMTP.Models;
using System.Data;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Transactions;

namespace SendEmailViaSMTP.DAL_Services
{
    public class DAL
    {
       

        private readonly string str = "server=localhost;port=3306;uid=root;pwd=sobiazafar@2023;database=mvc_crud";
       
        
        // ---------------Transaction Control Start with master detail operation---------------------

        public bool TransactionScope(TransactionModel mdl)
        {
            using (var txscope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                try
                {
                    using (MySqlConnection objConn = new MySqlConnection(str))
                    {
                        objConn.Open();
                       // string query1 = "insert into Applicant(ProjectID,Name) values (LAST_INSERT_ID(),@name)";
                        //string query2 = "insert into tblProjectMember(MemberID,ProjectID) values(@mid,@pid)";
                        string query1 = "insert into Applicant(name,gender,age,qualificaion,total_experience) values (@name,@gender,@age,@qualificaion,@total_experience)";
                        string query2 = "insert into experience(company_name,designation,years_worked,app_id) values(@company_name,@designation,@years_worked,@app_id)";

                        MySqlCommand cmd1 = new MySqlCommand(query1, objConn);
                        // cmd1.Parameters.AddWithValue("@name", mdl.Name);
                        cmd1.Parameters.AddWithValue("@name", mdl.Name);
                        cmd1.Parameters.AddWithValue("@gender", mdl.Gender);
                        cmd1.Parameters.AddWithValue("@age", mdl.Age);
                        cmd1.Parameters.AddWithValue("@qualificaion", mdl.Qualificaion);
                        cmd1.Parameters.AddWithValue("@total_experience", mdl.Total_Experience);
                        cmd1.ExecuteNonQuery();
                        long  appid= cmd1.LastInsertedId;
                       
                        foreach(var experience in mdl.Experiences)
                        {
                            MySqlCommand cmd2 = new MySqlCommand(query2, objConn);
                            cmd2.Parameters.AddWithValue("@company_name", experience.company_name);
                            cmd2.Parameters.AddWithValue("@designation", experience.designation);
                            cmd2.Parameters.AddWithValue("@years_worked", experience.years_worked);
                            cmd2.Parameters.AddWithValue("@app_id", appid);
                            cmd2.ExecuteNonQuery();

                        }
                        // cmd1.Parameters.AddWithValue("_id", mdl.ProjectID);      
                       // cmd2.Parameters.AddWithValue("@mid", mdl.MemberID);
                      //  cmd2.Parameters.AddWithValue("@pid",lastinsertedid);
                       // cmd1.ExecuteNonQuery();
                       // cmd2.ExecuteNonQuery(); // Throws exception due to foreign key constraint    

                        //The Transaction will be completed      
                        txscope.Complete();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Log error      
                    txscope.Dispose();
                    return false;
                }
            }
        }
        public bool BeginTransaction(TransactionModel mdl)
        {
           
            MySqlTransaction objTrans = null;

            using (MySqlConnection objConn = new MySqlConnection(str))
            {
                objConn.Open();
                objTrans = objConn.BeginTransaction();
                MySqlCommand objCmd1 = new MySqlCommand("insert into tblProject values(7, 'beginfrom')", objConn);
                MySqlCommand objCmd2 = new MySqlCommand("insert into tblProjectMember(MemberID, ProjectID) values(9, 7)", objConn);
                try
                {
                    //objCmd1.ExecuteNonQuery();
                    //objCmd2.ExecuteNonQuery(); // Throws exception due to foreign key constraint   
                    //objTrans.Commit();
                    //return true;
                    using (MySqlConnection connection = new MySqlConnection(str))
                    {
                        connection.Open();
                        // string query1 = "insert into Applicant(ProjectID,Name) values (LAST_INSERT_ID(),@name)";
                        //string query2 = "insert into tblProjectMember(MemberID,ProjectID) values(@mid,@pid)";
                        string query1 = "insert into Applicant(name,gender,age,qualificaion,total_experience) values (@name,@gender,@age,@qualificaion,@total_experience)";
                        string query2 = "insert into experience(company_name,designation,years_worked,app_id) values(@company_name,@designation,@years_worked,@app_id)";

                        MySqlCommand cmd1 = new MySqlCommand(query1, connection);
                        // cmd1.Parameters.AddWithValue("@name", mdl.Name);
                        cmd1.Parameters.AddWithValue("@name", mdl.Name);
                        cmd1.Parameters.AddWithValue("@gender", mdl.Gender);
                        cmd1.Parameters.AddWithValue("@age", mdl.Age);
                        cmd1.Parameters.AddWithValue("@qualificaion", mdl.Qualificaion);
                        cmd1.Parameters.AddWithValue("@total_experience", mdl.Total_Experience);
                        cmd1.ExecuteNonQuery();
                        long appid = cmd1.LastInsertedId;

                        foreach (var experience in mdl.Experiences)
                        {
                            MySqlCommand cmd2 = new MySqlCommand(query2, objConn);
                            cmd2.Parameters.AddWithValue("@company_name", experience.company_name);
                            cmd2.Parameters.AddWithValue("@designation", experience.designation);
                            cmd2.Parameters.AddWithValue("@years_worked", experience.years_worked);
                            cmd2.Parameters.AddWithValue("@app_id", appid);
                            cmd2.ExecuteNonQuery();

                        }
                        objTrans.Commit();
                        return true;
                    }
                    }
                catch (Exception)
                {
                    objTrans.Rollback();
                    return false;
                }
                finally
                {
                    objConn.Close();
                }
            }

        }

        public List<TransactionModel> GetfromMasterDEtail()
        {
            using (MySqlConnection conn = new MySqlConnection(str))
            {
                DataTable tbl = new DataTable();
                List<TransactionModel> lst = new List<TransactionModel>();
               // TransactionModel obj = new TransactionModel();
                conn.Open();
                string query = "select a.app_id,e.app_id,a.name, a.age ,a.qualificaion,a.total_experience ,e.company_name,e.designation ,e.years_worked from \r\nApplicant a join experience e on a.app_id=e.app_id";
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = query;
                        MySqlDataReader dr = cmd.ExecuteReader();
                        tbl.Load(dr);
                        conn.Close();
                        foreach (DataRow row in tbl.Rows)
                        {
                         TransactionModel obj=   new TransactionModel()
                            {
                                app_id = (int)row["app_id"],
                                Name = row["name"].ToString(),
                                Age = (int)row["age"],
                                Qualificaion = row["qualificaion"].ToString(),
                                Total_Experience = row["total_experience"].ToString(),
                                Experiences = new List<Experience>()
                            };

                            Experience exp = new Experience()
                            {
                                app_id = (int)row["app_id"],
                                company_name = row["company_name"].ToString(),
                                designation = row["designation"].ToString(),
                                years_worked = (int)row["years_worked"]
                            };
                            obj.Experiences.Add(exp);
                            //check in the list if applicant already exit
                            var exitingobj = lst.FirstOrDefault(x => x.app_id == obj.app_id);
                            if (exitingobj != null)
                            {
                                exitingobj.Experiences.Add(exp);
                            }
                            else
                            {
                                lst.Add(obj);
                            }


                        }
                    }
                    return lst ;

                }

                catch (Exception)
                {

                    throw;
                    
                }
            }
        }

        public bool Update_master_detail(TransactionModel modl)
        {
            using (var transscope = new TransactionScope(TransactionScopeOption.RequiresNew))
            {


                try
                {
                    using (MySqlConnection conn = new MySqlConnection(str))
                    {
                        conn.Open();
                        string query1 = "update Applicant set name=@name,gender=@gender,age=@age,qualificaion=@qualificaion,total_experience=@total_experience where app_id=@app_id";
                        MySqlCommand cmd1 = new MySqlCommand(query1, conn);
                        cmd1.Parameters.AddWithValue("@name", modl.Name);
                        cmd1.Parameters.AddWithValue("@gender", modl.Gender);
                        cmd1.Parameters.AddWithValue("@age", modl.Age);
                        cmd1.Parameters.AddWithValue("@qualificaion", modl.Qualificaion);
                        cmd1.Parameters.AddWithValue("@total_experience", modl.Total_Experience);
                        cmd1.Parameters.AddWithValue("@app_id", modl.app_id);
                        cmd1.ExecuteNonQuery();

                        cmd1.CommandText = "delete from experience where app_id=@app_id";
                        cmd1.ExecuteNonQuery();

                        foreach (var experience in modl.Experiences)
                        {
                            string query = "insert into experience (company_name,designation,years_worked,app_id)values(@company_name,@designation,@years_worked,@app_id)";
                            MySqlCommand cmd2 = new MySqlCommand(query, conn);
                            cmd2.Parameters.AddWithValue("@company_name", experience.company_name);
                            cmd2.Parameters.AddWithValue("@designation", experience.designation);
                            cmd2.Parameters.AddWithValue("@years_worked", experience.years_worked);
                            cmd2.Parameters.AddWithValue("@app_id", modl.app_id);
                            cmd2.ExecuteNonQuery();
                            cmd2.Parameters.Clear();

                        }

                    }
                    transscope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    transscope.Dispose();
                    throw;
                    return false;
                }
            }
        }


        public bool Delete_master_Detail(int id)
        {
            using(var transsscope=new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(str))
                    {
                        conn.Open();
                        //first we should delete child record because if we delete parent then child refernce will be lost and it become free wastage
                        string childquery = "delete from experience where app_id=@id";
                        using (MySqlCommand cmd1 = new MySqlCommand(childquery, conn))
                        {
                            cmd1.Parameters.AddWithValue("@id", id);
                            cmd1.ExecuteNonQuery();

                        }
                        //2nd step is to delete parent table            
                        string parentquery = "delete from Applicant where app_id=@id";
                        using (MySqlCommand cmd2 = new MySqlCommand(parentquery, conn))
                        {
                            cmd2.Parameters.AddWithValue("@id", id);
                            cmd2.ExecuteNonQuery();
                            conn.Close();
                        }

                    }
                    transsscope.Complete();
                    return true;
                }
                catch (Exception)
                {
                    transsscope.Dispose();
                    throw;
                    return false;
                }
                

            }
        }
        // -----------------Transactopn Control End -----------------------------



        // -----------------Redis cache Start -----------------------------

        public  List<ArticleMatrix> ListOfMatrix(string authorid)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(str))
                {
                    conn.Open();
                    string query = "select * from ArticleMatrices where AuthorId =@AuthorId ORDER By PubDate DESC;";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AuthorId", authorid);

                        MySqlDataReader dr = cmd.ExecuteReader();
                 
                        DataTable tbl = new DataTable();
                        tbl.Load(dr);
                        List<ArticleMatrix> lst = new List<ArticleMatrix>();
                        conn.Close();
                        foreach (DataRow row in tbl.Rows)
                        {
                            ArticleMatrix obj = new ArticleMatrix
                            {
                                Id = (int)row["Id"],
                                AuthorId = (string)row["AuthorId"],
                                Author = (string)row["Author"],
                                Link = (string)row["Link"],
                                Title = (string)row["Title"],
                                Type = (string)row["Type"],
                                Category = (string)row["Category"],
                                Views = (string)row["Views"],
                                ViewsCount = (decimal)row["ViewsCount"],
                                Likes = (int)row["Likes"],
                                PubDate = (DateTime)row["PubDate"]

                            };
                            lst.Add(obj);
                        }
                        return lst;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        public List<UserModel> GetEmployee()
        {
            List<UserModel> lst = new List<UserModel>();
            MySqlConnection con = new MySqlConnection(str);
            MySqlDataReader dr;
            DataTable tbl = new DataTable();
            con.Open();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "sp_display_user";
            dr = cmd.ExecuteReader();
            tbl.Load(dr);
            con.Close();
            foreach (DataRow dar in tbl.Rows)
            {
                lst.Add(new UserModel
                {
                    Id = (int)dar["id"],
                    Name = Convert.ToString(dar["name"]),
                    Email = Convert.ToString(dar["email"]),
                    Age = dar["age"] != DBNull.Value ? Convert.ToInt32(dar["age"]) : int.MinValue,
                    //Email = !Convert.IsDBNull(dar["email"]) ? (int?)dar["email"] :

                    //Join_date = ((DateTime)dar["join_date"]),
                    Join_date = dar["join_date"] != DBNull.Value ? Convert.ToDateTime(dar["join_date"]) : DateTime.MinValue,
                    End_date = dar["end_date"] != DBNull.Value ? Convert.ToDateTime(dar["end_date"]) : DateTime.MinValue

                });

            }

            return lst;
        }
        public bool Add_user(UserModel user)
        {
            MySqlConnection conn = new MySqlConnection(str);
            MySqlCommand cmd = new MySqlCommand();
            conn.Open();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "sp_insert_user";
            cmd.Parameters.AddWithValue("_name", user.Name);
            cmd.Parameters.AddWithValue("_email", user.Email);
            cmd.Parameters.AddWithValue("_age", user.Age);
            cmd.Parameters.AddWithValue("_join_date", user.Join_date);
            cmd.Parameters.AddWithValue("_end_date", user.End_date);
            try
            {
                int i = cmd.ExecuteNonQuery();
                conn.Close();
                if (i > 0)
                    return true;
                else
                    return false;
            }
            catch (NullReferenceException ex)
            {
                throw new NullReferenceException(ex.Message);
            }

        }
        public bool Update(UserModel user)
        {
            MySqlConnection conn = new MySqlConnection(str);
            MySqlCommand cmd = new MySqlCommand();
            conn.Open();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "sp_update";
            cmd.Parameters.AddWithValue("_id", user.Id);
            cmd.Parameters.AddWithValue("_name", user.Name);
            cmd.Parameters.AddWithValue("_email", user.Email);
            cmd.Parameters.AddWithValue("_age", user.Age);
            cmd.Parameters.AddWithValue("_join_date", user.Join_date);
            cmd.Parameters.AddWithValue("_end_date", user.End_date);
            try
            {
                int i = cmd.ExecuteNonQuery();
                conn.Close();
                if (i > 0) return true;
                else
                    return false;
            }
            catch (NullReferenceException ex)
            {
                throw new NullReferenceException(ex.Message);
            }

        }
        public bool Delete(int id)
        {
            MySqlConnection conn = new MySqlConnection(str);
            conn.Open();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "sp_delete_user";
            cmd.Parameters.AddWithValue("_id",id);
            try
            {
                int i = cmd.ExecuteNonQuery();
                conn.Close();
                if (i > 0) return true;
                else
                    return false;
            }
            catch (NullReferenceException ex)
            {
                throw new NullReferenceException(ex.Message);
            }





        }
        public int Sum(int a,int b)
        {
            return a + b;
        }
        public bool CheckDuplication(UserModel user)
        {
            using(MySqlConnection conn=new MySqlConnection(str))
            {
                conn.Open();
                using(MySqlCommand cmd=new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Duplicacy";
                    cmd.Parameters.AddWithValue("_id", user.Id);
                    List<UserModel> list = new List<UserModel>();
                    DataTable tbl = new DataTable();
                    MySqlDataReader dr= cmd.ExecuteReader();
                    tbl.Load(dr);
                    foreach (DataRow rows in tbl.Rows)
                    {
                        UserModel obj = new UserModel() { Id = (int)rows["id"] };
                        list.Add(obj);
                    }

                    if (list.Count > 0)
                    {
                        return true;
                    }
                    else 
                    {
                        return false;
                    }
                }
            }
        }
    }
}
