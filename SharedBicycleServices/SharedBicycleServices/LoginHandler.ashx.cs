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
    /// LoginHandler 的摘要说明
    /// </summary>
    /// 

    class Result
    {
        public Boolean status { get; set; }
    }

    public class LoginHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            try
            {
                SqlConnection con = new SqlConnection("server=localhost;database=SharedBicycle;user id=sa;password=123456");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                if (context.Request.HttpMethod.ToUpper() == "GET")
                {
                    String userID = context.Request.QueryString["UserID"];
                    String pwd = context.Request.QueryString["Passward"];
                    cmd.CommandText = "select * from tblUser where tblUser.UserID = '" + userID + "'";
                    SqlDataReader dr = cmd.ExecuteReader();
                    String str = "";
                    Result result = new Result();
                    result.status = false;
                    if (dr.Read())
                    {
                        str = dr["Passward"].ToString();
                        if (str.Equals(pwd))
                        {
                            result.status = true;
                        }
                    }
                    context.Response.Write(JsonConvert.SerializeObject(result));
                    dr.Close();
                    con.Close();
                }
                con.Close();
            }
            catch (Exception error)
            {
                context.Response.Write("false:" + error.ToString());
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