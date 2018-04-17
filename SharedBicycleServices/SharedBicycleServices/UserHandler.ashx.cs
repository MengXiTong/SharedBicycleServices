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
        public String IdentityName { get; set; }
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
                    cmd.CommandText = "select UserID,Passward,Name,Sex,Birthday,tblUser.IdentityID,IdentityName,Phone,CreditScore,Photo,Balance,Deposit from tblUser,tblIdentity where tblUser.UserID = '" + userID + "' and tblUser.IdentityID = tblIdentity.IdentityID";
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        User user = new User();
                        user.UserID = dr["UserID"].ToString();
                        user.Passward = dr["Passward"].ToString();
                        user.Name = dr["Name"].ToString();
                        user.Sex = dr["Sex"].ToString();
                        user.Birthday = (Convert.ToDateTime(dr["Birthday"].ToString())).ToString("yyyy-MM-dd");
                        user.IdentityID = dr["IdentityID"].ToString();
                        user.IdentityName = dr["IdentityName"].ToString();
                        user.Phone = dr["Phone"].ToString();
                        user.CreditScore = dr["CreditScore"].ToString();
                        user.Photo = dr["Photo"].ToString();
                        user.Balance = dr["Balance"].ToString();
                        user.Deposit = dr["Deposit"].ToString();
                        if (!user.Photo.Equals(String.Empty))
                        {
                            user.Photo = ImgToBase64String(user.Photo);
                        }
                        context.Response.Write(JsonConvert.SerializeObject(user));
                    }
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    string data = sr.ReadToEnd();
                    Param param = JsonConvert.DeserializeObject<Param>(data);
                    cmd.CommandText = "insert into tblUser values(@UserID,@Passward,@Name)";
                    cmd.Parameters.AddWithValue("@UserID", param.user.UserID);
                    if (Base64StringToImage(param.user.Photo, param.user.UserID))
                    {
                        cmd.ExecuteNonQuery();
                        result.status = true;
                    }
                    context.Response.Write(JsonConvert.SerializeObject(result));
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    string data = sr.ReadToEnd();
                    Param param = JsonConvert.DeserializeObject<Param>(data);
                    switch (param.type)
                    {
                        //头像
                        case "photo":
                            if (Base64StringToImage(param.user.Photo, param.user.UserID))
                            {
                                cmd.CommandText = "update tblUser set Photo='" + @"d:\image\" + param.user.UserID + ".jpg' where UserID='" + param.user.UserID + "'";
                                cmd.ExecuteNonQuery();
                                result.status = true;
                            }
                            else
                            {
                                result.message = "图片保存到本地失败";
                            }
                            break;
                        //姓名
                        case "name":
                            cmd.CommandText = "update tblUser set Name='"+ param.user.Name +"' where UserID='" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //性别
                        case "sex":
                            cmd.CommandText = "update tblUser set Sex='" + param.user.Sex + "' where UserID='" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //生日
                        case "birthday":
                            cmd.CommandText = "update tblUser set Birthday='" + param.user.Birthday + "' where UserID='" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //手机号
                        case "phone":
                            cmd.CommandText = "update tblUser set Phone='" + param.user.Phone + "' where UserID='" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //密码
                        case "passward":
                            cmd.CommandText = "update tblUser set Passward = '" + param.user.Passward + "' where UserID = '" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //身份
                        case "identity":
                            cmd.CommandText = "update tblUser set IdentityID = '" + param.user.IdentityID + "' where UserID = '" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //违规处理,目前是当用户用车时间超过24小时时，出现违规处理，扣除信用分5分。
                        case "illegal":
                            cmd.CommandText = "update tblUser set CreditScore = CreditScore-5 where UserID = '" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "insert into tblIllegal(UserID,IllegalContent,DeductCreditScore,IllegalTime) values('" + param.user.UserID + "','" + "未在规定时间内结束用车" + "','" + 5 + "','" + DateTime.Now.ToString() + "')";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //充值&消费----------充值传正数，消费传负数，每完成一次消费添加信用分1分。
                        case "balance":
                            cmd.CommandText = "update tblUser set Balance = Balance + " + float.Parse(param.user.Balance) + " where UserID = '" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            int detailedTypeID = 2;
                            float sum = Math.Abs(float.Parse(param.user.Balance));
                            if (float.Parse(param.user.Balance) < 0)
                            {
                                detailedTypeID = 1;
                                cmd.CommandText = "update tblUser set CreditScore = CreditScore+1 where UserID = '" + param.user.UserID + "'";
                                cmd.ExecuteNonQuery();
                            }
                            cmd.CommandText = "insert into tblDetailed(UserID,DetailedTypeID,Sum,DetailTime) values('" + param.user.UserID + "','" + detailedTypeID + "','" + sum + "','" + DateTime.Now.ToString() + "')";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                        //押金
                        case "deposit":
                            cmd.CommandText = "update tblUser set Deposit = '" + param.user.Deposit + "' where UserID = '" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "insert into tblDetailed(UserID,DetailedTypeID,Sum,DetailTime) values('" + param.user.UserID + "','" + 3 + "','" + param.user.Deposit + "','" + DateTime.Now.ToString() + "')";
                            cmd.ExecuteNonQuery();
                            result.status = true;
                            break;
                    }
                    context.Response.Write(JsonConvert.SerializeObject(result));
                }
                con.Close();
            }
            catch (Exception error)
            {
                result.message = error.ToString();
                context.Response.Write(JsonConvert.SerializeObject(result));
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
                bmp.Save(@"d:\image\" + userId + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
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