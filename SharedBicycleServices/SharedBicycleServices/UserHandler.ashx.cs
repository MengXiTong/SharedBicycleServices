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

    class Identity
    {
        public String IdentityID { get; set; }
        public String IdentityName { get; set; }
    }

    class Illegal
    {
        public String IllegalID { get; set; }
        public String UserID { get; set; }
        public String IllegalContent { get; set; }
        public String DeductCreditScore { get; set; }
        public String IllegalTime { get; set; }
    }

    class Detailed
    {
        public String DetailedID { get; set; }
        public String UserID { get; set; }
        public String DetailedTypeID { get; set; }
        public String Sum { get; set; }
        public String DetailTime { get; set; }
        public String DetailedTypeName { get; set; }
    }

    class CreditScore
    {
        public String CreditScoreID { get; set; }
        public String Score { get; set; }
        public String Explain { get; set; }
        public String Time { get; set; }
        public String UserID { get; set; }
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
                    String type = context.Request.QueryString["Type"];
                    if (type == "user")
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
                            result.user = user;
                            result.status = true;
                        }
                        else
                        {
                            result.message = "未查到该用户信息";
                        }
                        dr.Close();
                    }
                    else if (type == "identity")
                    {
                        cmd.CommandText = "select * from tblIdentity";
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Identity> identityList = new List<Identity>();
                        while (dr.Read())
                        {
                            Identity identity = new Identity();
                            identity.IdentityID = dr["IdentityID"].ToString();
                            identity.IdentityName = dr["IdentityName"].ToString();
                            identityList.Add(identity);
                        }
                        result.identityList = identityList;
                        result.status = true;
                        dr.Close();
                    }
                    else if (type == "detailed")
                    {
                        String userID = context.Request.QueryString["UserID"];
                        String pageNum = context.Request.QueryString["PageNum"];
                        int end = int.Parse(pageNum) * 10;
                        int start = (int.Parse(pageNum) - 1) * 10 + 1;
                        cmd.CommandText = "select DetailedID,UserID,a.DetailedTypeID,Sum,DetailTime,DetailedTypeName from (select row_number()over(order by DetailedID Desc)rownumber,* from tblDetailed where UserID='" + userID + "')a,tblDetailType where a.DetailedTypeID=tblDetailType.DetailedTypeID and rownumber between " + start + " and " + end;
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Detailed> detailedList = new List<Detailed>();
                        while (dr.Read())
                        {
                            Detailed detailed = new Detailed();
                            detailed.DetailedID = dr["DetailedID"].ToString();
                            detailed.UserID = dr["UserID"].ToString();
                            detailed.DetailedTypeID = dr["DetailedTypeID"].ToString();
                            detailed.Sum = dr["Sum"].ToString();
                            detailed.DetailTime = (Convert.ToDateTime(dr["DetailTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                            detailed.DetailedTypeName = dr["DetailedTypeName"].ToString();
                            detailedList.Add(detailed);
                        }
                        result.detailedList = detailedList;
                        result.status = true;
                        dr.Close();
                    }
                    else if (type == "illegal")
                    {
                        String userID = context.Request.QueryString["UserID"];
                        String pageNum = context.Request.QueryString["PageNum"];
                        int end = int.Parse(pageNum) * 10;
                        int start = (int.Parse(pageNum) - 1) * 10 + 1;
                        cmd.CommandText = "select * from (select row_number()over(order by IllegalID Desc)rownumber,* from tblIllegal where UserID='" + userID + "')a where rownumber between " + start + " and " + end;
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Illegal> illegalList = new List<Illegal>();
                        while (dr.Read())
                        {
                            Illegal illegal = new Illegal();
                            illegal.IllegalID = dr["IllegalID"].ToString();
                            illegal.UserID = dr["UserID"].ToString();
                            illegal.IllegalContent = dr["IllegalContent"].ToString();
                            illegal.DeductCreditScore = dr["DeductCreditScore"].ToString();
                            illegal.IllegalTime = (Convert.ToDateTime(dr["IllegalTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                            illegalList.Add(illegal);
                        }
                        result.illegalList = illegalList;
                        result.status = true;
                        dr.Close();
                    }
                    else if (type == "creditScore")
                    {
                        String userID = context.Request.QueryString["UserID"];
                        String pageNum = context.Request.QueryString["PageNum"];
                        int end = int.Parse(pageNum) * 10;
                        int start = (int.Parse(pageNum) - 1) * 10 + 1;
                        cmd.CommandText = "select * from (select row_number()over(order by CreditScoreID Desc)rownumber,* from tblCreditScore where UserID='" + userID + "')a where rownumber between " + start + " and " + end;
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<CreditScore> creditScoreList = new List<CreditScore>();
                        while (dr.Read())
                        {
                            CreditScore creditScore = new CreditScore();
                            creditScore.CreditScoreID = dr["CreditScoreID"].ToString();
                            creditScore.Score = dr["CreditScore"].ToString();
                            creditScore.Explain = dr["Explain"].ToString();
                            creditScore.Time = (Convert.ToDateTime(dr["Time"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                            creditScore.UserID = dr["UserID"].ToString();
                            creditScoreList.Add(creditScore);
                        }
                        result.creditScoreList = creditScoreList;
                        result.status = true;
                        dr.Close();
                    }
                    context.Response.Write(JsonConvert.SerializeObject(result));
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    User user = JsonConvert.DeserializeObject<User>(data);
                    cmd.CommandText = "select * from tblUser Where UserID='"+user.UserID+"'";
                    SqlDataReader dr = cmd.ExecuteReader();
                    if(dr.Read()){
                        result.message = "该用户已注册";
                        context.Response.Write(JsonConvert.SerializeObject(result));
                        dr.Close();
                        con.Close();
                        return;
                    }
                    dr.Close();
                    cmd.CommandText = "insert into tblUser(UserID,Passward,Name,Sex,Birthday,IdentityID,Phone,CreditScore,Photo,Balance,Deposit) values(@UserID,@Passward,@Name,@Sex,@Birthday,@IdentityID,@Phone,@CreditScore,@Photo,@Balance,@Deposit)";
                    cmd.Parameters.AddWithValue("@UserID", user.UserID);
                    cmd.Parameters.AddWithValue("@Passward", user.Passward);
                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Sex", user.Sex);
                    cmd.Parameters.AddWithValue("@Birthday", user.Birthday);
                    cmd.Parameters.AddWithValue("@IdentityID", 1);
                    cmd.Parameters.AddWithValue("@Phone", user.Phone);
                    cmd.Parameters.AddWithValue("@CreditScore", 100);
                    cmd.Parameters.AddWithValue("@Balance", 0);
                    cmd.Parameters.AddWithValue("@Deposit", 0);
                    if (user.Photo.Equals(""))
                    {
                        cmd.Parameters.AddWithValue("@Photo", "");
                    }
                    else
                    {
                        if (Base64StringToImage(user.Photo, user.UserID))
                        {
                            cmd.Parameters.AddWithValue("@Photo", @"d:\image\" + user.UserID + ".jpg");
                        }
                    }
                    cmd.ExecuteNonQuery();
                    result.status = true;
                    context.Response.Write(JsonConvert.SerializeObject(result));
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
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
                        //充值
                        case "balance":
                            cmd.CommandText = "update tblUser set Balance = Balance + " + float.Parse(param.user.Balance) + " where UserID = '" + param.user.UserID + "'";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "insert into tblDetailed(UserID,DetailedTypeID,Sum,DetailTime) values('" + param.user.UserID + "','" + 2 + "','" + param.user.Balance + "','" + DateTime.Now.ToString() + "')";
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
        protected String ImgToBase64String(String Imagefilename)
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