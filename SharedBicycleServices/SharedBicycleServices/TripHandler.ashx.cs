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
    /// TripHandler 的摘要说明
    /// </summary>

    class Trip
    {
        public String TripID { get; set; }
        public String UserID { get; set; }
        public String BikeID { get; set; }
        public String StartTime { get; set; }
        public String EndTime { get; set; }
        public String Consume { get; set; }
        public String Position { get; set; }
        public String State { get; set; }
        public String CouponID { get; set; }
    }

    class Bike
    {
        public String BikeID { get; set; }
        public String ModelID { get; set; }
        public String StateID { get; set; }
        public String BikeLongitude { get; set; }
        public String BikeLatitude { get; set; }
    }

    class TripParam
    {
        public String type { get; set; }
        public Trip trip { get; set; }
    }

    public class TripHandler : IHttpHandler
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
                    cmd.CommandText = "select * from tblTrip where tblTrip.UserID = '" + userID + "' order by TripID DESC";
                    SqlDataReader dr = cmd.ExecuteReader();
                    Trip trip = new Trip();
                    trip.State = "finish";
                    if (dr.Read())
                    {
                        trip.TripID = dr["TripID"].ToString();
                        trip.UserID = dr["UserID"].ToString();
                        trip.BikeID = dr["BikeID"].ToString();
                        trip.StartTime = dr["StartTime"].ToString();
                        trip.EndTime = dr["EndTime"].ToString();
                        trip.Consume = dr["Consume"].ToString();
                        trip.Position = dr["Position"].ToString();
                        trip.State = dr["State"].ToString();
                    }
                    result.trip = trip;
                    result.status = true;
                    context.Response.Write(JsonConvert.SerializeObject(result));
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Trip trip = JsonConvert.DeserializeObject<Trip>(data);
                    trip.State = "unfinish";
                    cmd.CommandText = "insert into tblTrip(UserID,BikeID,StartTime,State) values(@UserID,@BikeID,@StartTime,@State)";
                    cmd.Parameters.AddWithValue("@UserID", trip.UserID);
                    cmd.Parameters.AddWithValue("@BikeID", trip.BikeID);
                    cmd.Parameters.AddWithValue("@StartTime", trip.StartTime);
                    cmd.Parameters.AddWithValue("@State", trip.State);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "select * from tblTrip where tblTrip.UserID = '" + trip.UserID + "' order by TripID DESC";
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        trip.TripID = dr["TripID"].ToString();
                    }
                    result.trip = trip;
                    result.status = true;
                    context.Response.Write(JsonConvert.SerializeObject(result));
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    TripParam tripParam = JsonConvert.DeserializeObject<TripParam>(data);
                    switch (tripParam.type)
                    {
                        //更新位置
                        case "position":
                            {
                                if (tripParam.trip.State == "unfinish")
                                {
                                    cmd.CommandText = "update tblTrip set Position+=@Position,EndTime=@EndTime where tblTrip.TripID=@TripID";
                                    cmd.Parameters.AddWithValue("@TripID", tripParam.trip.TripID);
                                    cmd.Parameters.AddWithValue("@Position", "|"+tripParam.trip.Position);
                                    cmd.Parameters.AddWithValue("@EndTime", tripParam.trip.EndTime);
                                    cmd.ExecuteNonQuery();
                                }
                                result.status = true;
                                break;
                            }
                        //结束行程
                        case "end":
                            {
                                tripParam.trip.State = "defray";
                                cmd.CommandText = "update tblTrip set State=@State where tblTrip.TripID=@TripID";
                                cmd.Parameters.AddWithValue("@TripID", tripParam.trip.TripID);
                                cmd.Parameters.AddWithValue("@State", tripParam.trip.State);
                                cmd.ExecuteNonQuery();
                                result.status = true;
                                break;
                            }
                        //支付------------每完成一次消费添加信用分1分。
                        case "pay":
                            {
                                DateTime startTime = Convert.ToDateTime(tripParam.trip.StartTime);
                                DateTime endTime = Convert.ToDateTime(tripParam.trip.EndTime);
                                TimeSpan midTime = endTime - startTime;
                                //超过1天扣除信用分
                                if (midTime.Days > 0)
                                {
                                    cmd.CommandText = "update tblUser set CreditScore = CreditScore-20 where UserID = '" + tripParam.trip.UserID + "'";
                                    cmd.ExecuteNonQuery();
                                }
                                //使用优惠券
                                if (!String.IsNullOrEmpty(tripParam.trip.CouponID))
                                {
                                    cmd.CommandText = "delete from tblCoupon where CouponID = '" + tripParam.trip.CouponID + "'";
                                    cmd.ExecuteNonQuery();
                                }
                                //扣除余额以及添加明细
                                float consume = float.Parse(tripParam.trip.Consume);
                                if (consume > 0)
                                {
                                    cmd.CommandText = "update tblUser set Balance = Balance - " + consume + " where UserID = '" + tripParam.trip.UserID + "'";
                                    cmd.ExecuteNonQuery();
                                    cmd.CommandText = "insert into tblDetailed(UserID,DetailedTypeID,Sum,DetailTime) values('" + tripParam.trip.UserID + "','" + 1 + "','" + consume + "','" + DateTime.Now.ToString() + "')";
                                    cmd.ExecuteNonQuery();
                                }
                                //更新行程状态以及消费金额
                                tripParam.trip.State = "finish";
                                cmd.CommandText = "update tblTrip set Consume=@Consume,State=@State where tblTrip.TripID=@TripID";
                                cmd.Parameters.AddWithValue("@TripID", tripParam.trip.TripID);
                                cmd.Parameters.AddWithValue("@Consume", tripParam.trip.Consume);
                                cmd.Parameters.AddWithValue("@State", tripParam.trip.State);
                                cmd.ExecuteNonQuery();
                                result.status = true;
                                break;
                            }
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

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}