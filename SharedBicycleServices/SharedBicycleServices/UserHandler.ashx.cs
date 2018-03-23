using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;
using System.Drawing;

namespace SharedBicycleServices
{
    /// <summary>
    /// UserHandler 的摘要说明
    /// </summary>

    class User
    {
        public String UserID { get; set; }
        public String Passward { get; set; }
        public String Name { get; set; }
        public String Sex { get; set; }
        public String Birthday { get; set; }
        public String IdentityID { get; set; }
        public String Phone { get; set; }
        public String CreditScore { get; set; }
        public String Photo { get; set; }
        public String Balance { get; set; }
        public String Deposit { get; set; }
    }

    class Param
    {
        public String type { get; set; }
        public User user { get; set; }
    }

    public class UserHandler : IHttpHandler
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
                    cmd.CommandText = "select * from tblUser where tblUser.UserID = '" + userID + "'";
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        User user = new User();
                        user.UserID = dr["UserID"].ToString();
                        user.Passward = dr["Passward"].ToString();
                        user.Name = dr["Name"].ToString();
                        user.Sex = dr["Sex"].ToString();
                        user.Birthday = dr["Birthday"].ToString();
                        user.IdentityID = dr["IdentityID"].ToString();
                        user.Phone = dr["Phone"].ToString();
                        user.CreditScore = dr["CreditScore"].ToString();
                        user.Photo = dr["Photo"].ToString();
                        user.Balance = dr["Balance"].ToString();
                        user.Deposit = dr["Deposit"].ToString();
                        context.Response.Write(JsonConvert.SerializeObject(user));
                    }
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    string data = sr.ReadToEnd();
                    Param param = JsonConvert.DeserializeObject<Param>(data);
                    switch (param.type)
                    {
                        //照片
                        case "image":
                            context.Response.Write(Base64StringToImage(param.user.Photo, param.user.UserID));
                            //cmd.CommandText = "update tblUser set Photo='" + param.user.Photo + "' where UserID='" + param.user.UserID + "'";
                            break;
                        //个人信息
                        case "update":
                            cmd.CommandText = "update tblUser set Name='" + param.user.Name + "', Sex='" + param.user.Sex + "', Birthday='" + param.user.Birthday + "', Phone='" + param.user.Phone + "', Photo='" + param.user.Photo + "' where UserID='" + param.user.UserID + "'";
                            break;
                        //密码
                        case "passward":
                            cmd.CommandText = "update tblUser set Passward = '" + param.user.Passward + "' where UserID = '" + param.user.UserID + "'";
                            break;
                        //身份
                        case "identityID":
                            cmd.CommandText = "update tblUser set IdentityID = '" + param.user.IdentityID + "' where UserID = '" + param.user.UserID + "'";
                            break;
                        //信用分
                        case "creditScore":
                            cmd.CommandText = "update tblUser set CreditScore = '" + param.user.CreditScore + "' where UserID = '" + param.user.UserID + "'";
                            break;
                        //充值
                        case "recharge":
                            cmd.CommandText = "update tblUser set Balance = '" + param.user.Balance + "' where UserID = '" + param.user.UserID + "'";
                            break;
                        //消费
                        case "consumption":
                            cmd.CommandText = "update tblUser set Balance = '" + param.user.Balance + "' where UserID = '" + param.user.UserID + "'";
                            break;
                        //押金
                        case "deposit":
                            cmd.CommandText = "update tblUser set Deposit = '" + param.user.Deposit + "' where UserID = '" + param.user.UserID + "'";
                            break;
                    }
                    //cmd.ExecuteNonQuery();
                    
                }
                con.Close();
            }
            catch (Exception error)
            {
                context.Response.Write("false:" + error.ToString());
            }
        }

        //图片转为base64编码的字符串
        protected string ImgToBase64String(string Imagefilename)
        {
            try
            {
                Bitmap bmp = new Bitmap(Imagefilename);
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();
                return Convert.ToBase64String(arr);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //base64编码的字符串转为图片  
        protected Boolean Base64StringToImage(String strbase64, String userId)
        {
            try
            {
                strbase64 = strbase64.Substring(strbase64.IndexOf(',') + 1);
                strbase64 = strbase64.Trim();
                byte[] arr = Convert.FromBase64String(strbase64);
                MemoryStream ms = new MemoryStream(arr);
                Bitmap bmp = new Bitmap(ms);
                bmp.Save(@"d:\image\"+userId+".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                ms.Close();
                return true;
            }
            catch (Exception e)
            {
                return false;
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