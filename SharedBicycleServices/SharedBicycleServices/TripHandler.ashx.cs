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
                    String type = context.Request.QueryString["Type"];
                    if (type == "state")
                    {
                        cmd.CommandText = "select * from tblTrip where tblTrip.UserID = '" + userID + "' order by TripID DESC";
                        SqlDataReader dr = cmd.ExecuteReader();
                        Trip trip = new Trip();
                        trip.State = "finish";
                        if (dr.Read())
                        {
                            if (!dr["State"].ToString().Equals("finish"))
                            {
                                trip.TripID = dr["TripID"].ToString();
                                trip.UserID = dr["UserID"].ToString();
                                trip.BikeID = dr["BikeID"].ToString();
                                trip.StartTime = (Convert.ToDateTime(dr["StartTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                                if (!String.IsNullOrEmpty(dr["EndTime"].ToString()))
                                {
                                    trip.EndTime = (Convert.ToDateTime(dr["EndTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                                }
                                trip.Consume = dr["Consume"].ToString();
                                trip.Position = dr["Position"].ToString();
                                trip.State = dr["State"].ToString();
                            }
                        }
                        result.trip = trip;
                        result.status = true;
                        dr.Close();
                    }
                    else if (type == "info")
                    {
                        String pageNum = context.Request.QueryString["PageNum"];
                        int end = int.Parse(pageNum) * 10;
                        int start = (int.Parse(pageNum)-1)*10 + 1;
                        cmd.CommandText = "select * from (select row_number()over(order by TripID Desc)rownumber,* from tblTrip where UserID='" + userID + "')a where rownumber between " + start + " and " + end;
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Trip> tripList = new List<Trip>();
                        while(dr.Read()){
                            Trip trip = new Trip();
                            trip.TripID = dr["TripID"].ToString();
                            trip.BikeID = dr["BikeID"].ToString();
                            trip.Consume = dr["Consume"].ToString();
                            trip.StartTime = (Convert.ToDateTime(dr["StartTime"].ToString())).ToString("yyyy年MM月dd日 HH:mm:ss");
                            trip.State = dr["State"].ToString();
                            tripList.Add(trip);
                        }
                        result.tripList = tripList;
                        result.status = true;
                        dr.Close();
                    }
                    else if (type == "detail")
                    {
                        String tripID = context.Request.QueryString["TripID"];
                        cmd.CommandText = "select * from tblTrip where tblTrip.TripID = '" + tripID + "'";
                        SqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            Trip trip = new Trip();
                            trip.TripID = dr["TripID"].ToString();
                            trip.UserID = dr["UserID"].ToString();
                            trip.BikeID = dr["BikeID"].ToString();
                            trip.Consume = dr["Consume"].ToString();
                            trip.StartTime = (Convert.ToDateTime(dr["StartTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                            trip.EndTime = (Convert.ToDateTime(dr["EndTime"].ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                            trip.Position = dr["Position"].ToString();
                            trip.State = dr["State"].ToString();
                            result.trip = trip;
                            result.status = true;
                        }
                        else
                        {
                            result.message = "未查到该行程信息";
                        }
                        dr.Close();
                    }
                    else if (type == "lastUser")
                    {
                        String bikeID = context.Request.QueryString["BikeID"];
                        String time = context.Request.QueryString["Time"];
                        cmd.CommandText = "select * from tblTrip where BikeID='"+bikeID+"'  and StartTime<='"+time+"' order by TripID desc";
                        SqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            Trip trip = new Trip();
                            trip.TripID = dr["TripID"].ToString();
                            trip.UserID = dr["UserID"].ToString();
                            trip.BikeID = dr["BikeID"].ToString();
                            result.trip = trip;
                            result.status = true;
                        }
                        else
                        {
                            result.message = "该时间点之前该车未被使用";
                        }
                        dr.Close();
                    }
                    context.Response.Write(JsonConvert.SerializeObject(result));
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Trip trip = JsonConvert.DeserializeObject<Trip>(data);
                    cmd.CommandText = "select * from tblBike where BikeID='"+trip.BikeID+"'";
                    SqlDataReader drBike = cmd.ExecuteReader();
                    if (drBike.Read())
                    {
                        if (drBike["StateID"].ToString() == "1")
                        {
                            drBike.Close();
                            cmd.CommandText = "select * from tblUser where UserID='"+trip.UserID+"'";
                            SqlDataReader drUser = cmd.ExecuteReader();
                            if (drUser.Read())
                            {
                                if (float.Parse(drUser["Deposit"].ToString()) > 0)
                                {
                                    if (float.Parse(drUser["Balance"].ToString()) > 0)
                                    {
                                        drUser.Close();
                                        trip.State = "unfinish";
                                        cmd.CommandText = "insert into tblTrip(UserID,BikeID,StartTime,State,Position) values(@UserID,@BikeID,@StartTime,@State,'')";
                                        cmd.Parameters.AddWithValue("@UserID", trip.UserID);
                                        cmd.Parameters.AddWithValue("@BikeID", trip.BikeID);
                                        cmd.Parameters.AddWithValue("@StartTime", trip.StartTime);
                                        cmd.Parameters.AddWithValue("@State", trip.State);
                                        cmd.ExecuteNonQuery();
                                        cmd.CommandText = "update tblBike set StateID = 2 where BikeID='" + trip.BikeID + "'";
                                        cmd.ExecuteNonQuery();
                                        cmd.CommandText = "select * from tblTrip where tblTrip.UserID = '" + trip.UserID + "' order by TripID DESC";
                                        SqlDataReader dr = cmd.ExecuteReader();
                                        if (dr.Read())
                                        {
                                            trip.TripID = dr["TripID"].ToString();
                                        }
                                        result.trip = trip;
                                        result.status = true;
                                        dr.Close();
                                    }
                                    else
                                    {
                                        result.message = "余额不足，请充值";
                                        drUser.Close();
                                    }
                                }
                                else
                                {
                                    result.message = "请先交押金";
                                    drUser.Close();
                                }
                            }
                            else
                            {
                                result.message = "查不到用户信息";
                                drUser.Close();
                            }
                        }
                        else
                        {
                            result.message = "该车处于被使用或维修中";
                            drBike.Close();
                        }
                    }
                    else
                    {
                        result.message = "查不到该车信息";
                        drBike.Close();
                    }
                    context.Response.Write(JsonConvert.SerializeObject(result));
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
                                    cmd.CommandText = "update tblTrip set Position+=@Position where tblTrip.TripID=@TripID";
                                    cmd.Parameters.AddWithValue("@TripID", tripParam.trip.TripID);
                                    cmd.Parameters.AddWithValue("@Position", "|" + tripParam.trip.Position);
                                    cmd.ExecuteNonQuery();
                                    result.status = true;
                                }
                                else
                                {
                                    result.message = "本次行程已结束";
                                }
                                break;
                            }
                        //结束行程
                        case "end":
                            {
                                tripParam.trip.State = "defray";
                                cmd.CommandText = "update tblTrip set State=@State,Consume=@Consume,EndTime=@EndTime where tblTrip.TripID=@TripID";
                                cmd.Parameters.AddWithValue("@TripID", tripParam.trip.TripID);
                                cmd.Parameters.AddWithValue("@State", tripParam.trip.State);
                                cmd.Parameters.AddWithValue("@Consume", tripParam.trip.Consume);
                                cmd.Parameters.AddWithValue("@EndTime", tripParam.trip.EndTime);
                                cmd.ExecuteNonQuery();
                                String strLat = tripParam.trip.Position.Substring(0, tripParam.trip.Position.IndexOf(','));
                                String strLong = tripParam.trip.Position.Substring(tripParam.trip.Position.IndexOf(',') + 1);
                                cmd.CommandText = "update tblBike set StateID='1',BikeLongitude='" + strLong + "',BikeLatitude='" + strLat + "' where tblBike.BikeID='" + tripParam.trip.BikeID + "'";
                                cmd.ExecuteNonQuery();
                                DateTime startTime = Convert.ToDateTime(tripParam.trip.StartTime);
                                DateTime endTime = Convert.ToDateTime(tripParam.trip.EndTime);
                                TimeSpan midTime = endTime - startTime;
                                //违规处理,目前是当用户用车时间超过24小时时，出现违规处理，扣除信用分5分。
                                if (midTime.Days > 0)
                                {
                                    cmd.CommandText = "update tblUser set CreditScore = CreditScore-5 where UserID = '" + tripParam.trip.UserID + "'";
                                    cmd.ExecuteNonQuery();
                                    cmd.CommandText = "insert into tblIllegal(UserID,IllegalContent,DeductCreditScore,IllegalTime) values('" + tripParam.trip.UserID + "','" + "未在规定时间内结束用车" + "','" + 5 + "','" + DateTime.Now.ToString() + "')";
                                    cmd.ExecuteNonQuery();
                                    cmd.CommandText = "insert into tblCreditScore(CreditScore,Explain,Time,UserID) values('-5','违规超时','" + DateTime.Now.ToString() + "','" + tripParam.trip.UserID + "')";
                                    cmd.ExecuteNonQuery();
                                }
                                result.status = true;
                                break;
                            }
                        //支付------------每完成一次消费添加信用分1分。
                        case "pay":
                            {
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
                                    cmd.CommandText = "insert into tblCreditScore(CreditScore,Explain,Time,UserID) values('1','完成骑行','" + DateTime.Now.ToString() + "','" + tripParam.trip.UserID + "')";
                                    cmd.ExecuteNonQuery();
                                }
                                cmd.CommandText = "insert into tblDetailed(UserID,DetailedTypeID,Sum,DetailTime) values('" + tripParam.trip.UserID + "','" + 1 + "','" + consume + "','" + DateTime.Now.ToString() + "')";
                                cmd.ExecuteNonQuery();
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