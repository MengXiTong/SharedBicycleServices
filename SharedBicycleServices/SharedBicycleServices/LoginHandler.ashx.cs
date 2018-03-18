using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;

namespace SharedBicycleServices
{
    /// <summary>
    /// LoginHandler 的摘要说明
    /// </summary>
    public class LoginHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                context.Response.ContentType = "text/plain";
                SqlConnection con = new SqlConnection("server=localhost;database=SharedBicycle;user id=sa;password=123456");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                StreamReader sr = new StreamReader(context.Request.InputStream);
                string data = sr.ReadToEnd().ToLower();
                //string data1 = context.Request.QueryString["age"];
                if (context.Request.HttpMethod.ToUpper() == "GET")
                {
                    cmd.CommandText = "select * from tblUser";
                    con.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    String str = "";
                    while (dr.Read())
                    {
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            str += dr[i].ToString() + ",";
                        }
                    }
                    dr.Close();
                    con.Close();
                    context.Response.Write(str);
                    //context.Response.Write("参数1"+data);
                    //context.Response.Write("参数2"+data1);
                }
            }
            catch (Exception error)
            {
                context.Response.ContentType = "text/plain";
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