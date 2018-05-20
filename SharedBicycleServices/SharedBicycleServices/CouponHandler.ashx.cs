using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;

namespace SharedBicycleServices
{
    class CouponType
    {
        public String CouponTypeID { get; set; }
        public String CouponTypeName { get; set; }
        public String FavorablePrice { get; set; }
    }

    class Coupon
    {
        public String CouponID { get; set; }
        public String UserID { get; set; }
        public String CouponTypeID { get; set; }
        public String ExpirationDate { get; set; }
        public String CouponTypeName { get; set; }
        public String FavorablePrice { get; set; }
    }

    /// <summary>
    /// CouponHandler 的摘要说明
    /// </summary>
    public class CouponHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            Result result = new Result();
            result.status = false;
            try
            {
                SqlConnection con = new SqlConnection(Config.strCon);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                if (context.Request.HttpMethod.ToUpper() == "GET")
                {
                    String userID = context.Request.QueryString["UserID"];
                    cmd.CommandText = "delete from tblCoupon where UserID='" + userID + "' and ExpirationDate<'" + DateTime.Now.ToString() + "'";
                    cmd.ExecuteNonQuery();
                    String type = context.Request.QueryString["Type"];
                    if (type == "count")
                    {
                        cmd.CommandText = "select count(*) from tblCoupon where UserID='" + userID + "'";
                        SqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            result.couponCount = dr[0].ToString();
                            result.status = true;
                        }
                        else{
                            result.message = "未查到优惠券数量";
                        }
                        dr.Close();
                    }
                    else if (type == "detail")
                    {
                        cmd.CommandText = "select CouponID,UserID,tblCoupon.CouponTypeID,ExpirationDate,CouponTypeName,FavorablePrice from tblCoupon,tblCouponType where tblCoupon.CouponTypeID=tblCouponType.CouponTypeID and UserID='" + userID + "' order by CouponID DESC";
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Coupon> couponList = new List<Coupon>();
                        while (dr.Read())
                        {
                            Coupon coupon = new Coupon();
                            coupon.CouponID = dr["CouponID"].ToString();
                            coupon.UserID = dr["UserID"].ToString();
                            coupon.CouponTypeID = dr["CouponTypeID"].ToString();
                            coupon.ExpirationDate = (Convert.ToDateTime(dr["ExpirationDate"].ToString())).ToString("yyyy-MM-dd");
                            coupon.CouponTypeName = dr["CouponTypeName"].ToString();
                            coupon.FavorablePrice = dr["FavorablePrice"].ToString();
                            couponList.Add(coupon);
                        }
                        dr.Close();
                        result.couponList = couponList;
                    }
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    cmd.CommandText = "select * from tblCouponType";
                    SqlDataReader dr = cmd.ExecuteReader();
                    List<CouponType> couponTypeList = new List<CouponType>();
                    while (dr.Read())
                    {
                        CouponType couponType = new CouponType();
                        couponType.CouponTypeID = dr["CouponTypeID"].ToString();
                        couponType.CouponTypeName = dr["CouponTypeName"].ToString();
                        couponType.FavorablePrice = dr["FavorablePrice"].ToString();
                        couponTypeList.Add(couponType);
                    }
                    dr.Close();
                    Random rd = new Random();
                    int index = rd.Next(0,couponTypeList.Count);
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Coupon coupon = JsonConvert.DeserializeObject<Coupon>(data);
                    coupon.CouponTypeID = couponTypeList[index].CouponTypeID;
                    coupon.CouponTypeName = couponTypeList[index].CouponTypeName;
                    coupon.FavorablePrice = couponTypeList[index].FavorablePrice;
                    coupon.ExpirationDate = DateTime.Now.AddDays(7).ToString();
                    cmd.CommandText = "insert into tblCoupon(UserID,CouponTypeID,ExpirationDate) values(@UserID,@CouponTypeID,@ExpirationDate)";
                    cmd.Parameters.AddWithValue("@UserID", coupon.UserID);
                    cmd.Parameters.AddWithValue("@CouponTypeID", coupon.CouponTypeID);
                    cmd.Parameters.AddWithValue("@ExpirationDate", coupon.ExpirationDate);
                    cmd.ExecuteNonQuery();
                    result.coupon = coupon;
                }
                result.status = true;
                context.Response.Write(JsonConvert.SerializeObject(result));
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