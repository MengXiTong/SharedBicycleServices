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
        public String message { get; set; }
        public Boolean login { get; set; }
        public User user { get; set; }
        public Trip trip { get; set; }
        public List<CouponType> couponTypeList { get; set; }
        public List<Coupon> couponList { get; set; }
        public List<Bike> bikeList { get; set; }
        public List<Model> modelList { get; set; }
        public List<State> stateList { get; set; }
    }

    public class LoginHandler : IHttpHandler
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
                    String pwd = context.Request.QueryString["Passward"];
                    cmd.CommandText = "select * from tblUser where tblUser.UserID = '" + userID + "'";
                    SqlDataReader dr = cmd.ExecuteReader();
                    String str = "";
                    result.login = false;
                    if (dr.Read())
                    {
                        str = dr["Passward"].ToString();
                        if (str.Equals(pwd))
                        {
                            result.login = true;
                        }
                    }
                    result.status = true;
                    context.Response.Write(JsonConvert.SerializeObject(result));
                    dr.Close();
                    con.Close();
                }
                con.Close();
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