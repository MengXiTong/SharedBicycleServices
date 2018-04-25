using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;

namespace SharedBicycleServices
{
    /// <summary>
    /// RepairHandler 的摘要说明
    /// </summary>

    class Repair
    {
        public String RepairID { get; set; }
        public String BikeID { get; set; }
        public String UserID { get; set; }
        public String RepairState { get; set; }
        public String RepairTime { get; set; }
        public String RepairContent { get; set; }
    }

    public class RepairHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            Result result = new Result();
            result.status = false;
            try
            {
                SqlConnection con = new SqlConnection("server=localhost;database=SharedBicycle;user id=sa;password=123456");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                if (context.Request.HttpMethod.ToUpper() == "GET")
                {
                    String userID = context.Request.QueryString["UserID"];
                    String pageNum = context.Request.QueryString["PageNum"];
                    int end = int.Parse(pageNum) * 10;
                    int start = (int.Parse(pageNum) - 1) * 10 + 1;
                    cmd.CommandText = "select * from (select row_number()over(order by RepairID Desc)rownumber,* from tblRepair where UserID='" + userID + "')a where rownumber between " + start + " and " + end;
                    SqlDataReader dr = cmd.ExecuteReader();
                    List<Repair> repairList = new List<Repair>();
                    while (dr.Read())
                    {
                        Repair repair = new Repair();
                        repair.RepairID = dr["RepairID"].ToString();
                        repair.BikeID = dr["BikeID"].ToString();
                        repair.UserID = dr["UserID"].ToString();
                        repair.RepairState = dr["RepairState"].ToString();
                        repair.RepairTime = (Convert.ToDateTime(dr["RepairTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                        repair.RepairContent = dr["RepairContent"].ToString();
                        repairList.Add(repair);
                    }
                    result.repairList = repairList;
                    result.status = true;
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Repair repair = JsonConvert.DeserializeObject<Repair>(data);
                    cmd.CommandText = "select * from tblBike where BikeID='" + repair.BikeID + "'";
                    SqlDataReader drBike = cmd.ExecuteReader();
                    if (drBike.Read())
                    {
                        if (drBike["StateID"].ToString() == "3")
                        {
                            drBike.Close();
                            cmd.CommandText = "update tblBike set StateID = 4 where BikeID='" + repair.BikeID + "'";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "insert into tblRepair(BikeID,UserID,RepairState,RepairTime,RepairContent) values(@BikeID,@UserID,@RepairState,@RepairTime,@RepairContent)";
                            cmd.Parameters.AddWithValue("@BikeID", repair.BikeID);
                            cmd.Parameters.AddWithValue("@UserID", repair.UserID);
                            cmd.Parameters.AddWithValue("@RepairState", "unfinish");
                            cmd.Parameters.AddWithValue("@RepairTime", DateTime.Now.ToString());
                            cmd.Parameters.AddWithValue("@RepairContent", "接到报修车辆");
                            cmd.ExecuteNonQuery();
                            result.status = true;
                        }
                        else
                        {
                            result.message = "该车未处于报修状态";
                            drBike.Close();
                        }
                    }
                    else
                    {
                        result.message = "查不到该车信息";
                        drBike.Close();
                    }
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Repair repair = JsonConvert.DeserializeObject<Repair>(data);
                    cmd.CommandText = "update tblRepair set RepairState=@RepairState,RepairContent=@RepairContent where tblRepair.RepairID=@RepairID";
                    cmd.Parameters.AddWithValue("@RepairState", "finish");
                    cmd.Parameters.AddWithValue("@RepairContent", repair.RepairContent);
                    cmd.Parameters.AddWithValue("@RepairID", repair.RepairID);
                    cmd.ExecuteNonQuery();
                    result.status = true;
                }
                con.Close();
                context.Response.Write(JsonConvert.SerializeObject(result));
            }
            catch (Exception error)
            {
                result.message = error.ToString();
                context.Response.Write(JsonConvert.SerializeObject(result));
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}